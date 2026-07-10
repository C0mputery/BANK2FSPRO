using System.Xml.Linq;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Instruments;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler {
    private const double TimelineUnitsPerSecond = 48000.0;

    private readonly Dictionary<string, Guid> _eventFolderGuids = new Dictionary<string, Guid>(StringComparer.Ordinal);
    private Dictionary<Guid, List<Guid>> _eventMasterToGroups = new Dictionary<Guid, List<Guid>>();

    public void ExtractEvents() {
        _eventMasterToGroups = BuildEventMasterToGroupsMap();
        EnsureEventFolders();

        foreach ((Guid eventGuid, EventNode eventNode) in _collectedBank.EventNodes) {
            if (!TryGetEventPath(eventGuid, out _, out _)) { continue; }
            WriteEventDocument(eventGuid, eventNode);
        }
    }

    private Dictionary<Guid, List<Guid>> BuildEventMasterToGroupsMap() {
        Dictionary<Guid, List<Guid>> map = new Dictionary<Guid, List<Guid>>();
        foreach ((Guid busGuid, BaseBusNode bus) in _collectedBank.BusNodes) {
            if (bus is not GroupBusNode) { continue; }
            Guid masterGuid = bus.Routable.BaseGuid.ToGuid();
            if (masterGuid == Guid.Empty) { continue; }
            if (!map.TryGetValue(masterGuid, out List<Guid>? groups)) {
                groups = [];
                map[masterGuid] = groups;
            }
            groups.Add(busGuid);
        }
        return map;
    }

    private void EnsureEventFolders() {
        foreach (Guid eventGuid in _collectedBank.EventNodes.Keys) {
            if (!TryGetEventPath(eventGuid, out _, out string folderPath)) { continue; }
            EnsureEventFolderPath(folderPath);
        }
    }

    private Guid EnsureEventFolderPath(string folderPath) {
        if (string.IsNullOrEmpty(folderPath)) { return _masterEventFolderGuid; }
        if (_eventFolderGuids.TryGetValue(folderPath, out Guid existing)) { return existing; }

        string? parentPath = null;
        string folderName = folderPath;
        int slash = folderPath.LastIndexOf('/');
        if (slash >= 0) {
            parentPath = folderPath[..slash];
            folderName = folderPath[(slash + 1)..];
        }

        Guid parentGuid = parentPath is null ? _masterEventFolderGuid : EnsureEventFolderPath(parentPath);
        Guid folderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/EventFolder/{folderPath}");
        _eventFolderGuids[folderPath] = folderGuid;

        XDocument document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("EventFolder", folderGuid,
                XmlBuilder.Property("name", folderName),
                XmlBuilder.Relationship("folder", parentGuid)
            )
        );
        document.Save(Path.Combine(_eventFolderMetadataDirectory, $"{folderGuid.AsFmodStringFormat()}.xml"));
        return folderGuid;
    }

    private void WriteEventDocument(Guid eventGuid, EventNode eventNode) {
        if (!TryGetEventPath(eventGuid, out string eventName, out string folderPath)) {
            throw new NotImplementedException($"Event {eventGuid} missing event:/ string table path");
        }

        Guid folderGuid = EnsureEventFolderPath(folderPath);
        Guid masterBusGuid = eventNode.MasterTrackGuid.ToGuid();
        Guid inputBusGuid = eventNode.InputBusGuid.ToGuid();
        Guid timelineGuid = eventNode.TimelineGuid.ToGuid();

        if (!_collectedBank.BusNodes.TryGetValue(masterBusGuid, out BaseBusNode? masterBus)) {
            throw new NotImplementedException($"Event {eventName} missing master bus {masterBusGuid}");
        }
        if (!_collectedBank.BusNodes.TryGetValue(inputBusGuid, out BaseBusNode? inputBus)) {
            throw new NotImplementedException($"Event {eventName} missing input bus {inputBusGuid}");
        }

        List<Guid> groupBusGuids = ResolveEventGroupBuses(eventNode, masterBusGuid);
        Dictionary<Guid, List<Guid>> trackModules = BuildTrackModuleMap(eventNode, groupBusGuids, out List<Guid> timelineModules, out Dictionary<Guid, List<Guid>> parameterModules, out Dictionary<Guid, FTriggerBox> instrumentBoxes);

        Guid eventMixerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{eventGuid}/EventMixer");
        Guid masterTrackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{eventGuid}/MasterTrack");
        Guid automatableGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{eventGuid}/AutomatableProperties");

        List<Guid> groupTrackGuids = [];
        foreach (Guid groupBusGuid in groupBusGuids) {
            groupTrackGuids.Add(Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{groupBusGuid}/GroupTrack"));
        }

        // Precompute automation tracks per group from instrument controllers.
        Dictionary<Guid, List<Guid>> groupAutomationTracks = [];
        foreach (Guid groupBusGuid in groupBusGuids) {
            List<Guid> automationTracks = [];
            if (trackModules.TryGetValue(groupBusGuid, out List<Guid>? modules)) {
                foreach (Guid instrumentGuid in modules) {
                    foreach ((Guid controllerGuid, ControllerNode controller) in _collectedBank.ControllerNodes) {
                        if (controller.PropertyOwnerGuid.ToGuid() != instrumentGuid) { continue; }
                        if (!_collectedBank.CurveNodes.TryGetValue(controller.CurveGuid.ToGuid(), out CurveNode? curve)) { continue; }
                        if (curve.CurvePoints.Length == 0 || IsDegenerateCurve(curve)) { continue; }
                        automationTracks.Add(Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{controllerGuid}/AutomationTrack"));
                    }
                }
            }
            groupAutomationTracks[groupBusGuid] = automationTracks;
        }

        List<object> documentObjects = [];
        List<Guid> markerTrackGuids = [];
        List<object> timelineContent = [];
        if (timelineModules.Count > 0) {
            timelineContent.Add(XmlBuilder.Relationship("modules", timelineModules));
        }

        // Markers/transitions first so markerTrackGuids are known for the Event object.
        List<object> markerObjects = [];
        AppendTimelineMarkersAndTransitions(markerObjects, eventNode, timelineGuid, timelineContent, markerTrackGuids);
        if (markerTrackGuids.Count == 0) {
            Guid fallbackMarkerTrack = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{eventGuid}/MarkerTrack");
            markerTrackGuids.Add(fallbackMarkerTrack);
            markerObjects.Add(XmlBuilder.Object("MarkerTrack", fallbackMarkerTrack));
        }

        List<object> eventContent = [
            XmlBuilder.Property("name", eventName),
            XmlBuilder.Property("outputFormat", ResolveEventOutputFormat(masterBus)),
            XmlBuilder.Relationship("folder", folderGuid),
            XmlBuilder.Relationship("mixer", eventMixerGuid),
            XmlBuilder.Relationship("masterTrack", masterTrackGuid),
            XmlBuilder.Relationship("mixerInput", inputBusGuid),
            XmlBuilder.Relationship("automatableProperties", automatableGuid),
            XmlBuilder.Relationship("markerTracks", markerTrackGuids),
        ];
        if (groupTrackGuids.Count > 0) {
            eventContent.Add(XmlBuilder.Relationship("groupTracks", groupTrackGuids));
        }
        eventContent.Add(XmlBuilder.Relationship("timeline", timelineGuid));

        List<Guid> parameterProxyGuids = [];
        foreach (FModGuid layoutRef in eventNode.ParameterLayouts) {
            parameterProxyGuids.Add(layoutRef.ToGuid());
        }
        if (parameterProxyGuids.Count > 0) {
            eventContent.Add(XmlBuilder.Relationship("parameters", parameterProxyGuids));
        }

        documentObjects.Add(XmlBuilder.Object("Event", eventGuid, eventContent.ToArray()));
        documentObjects.Add(XmlBuilder.Object("EventMixer", eventMixerGuid,
            XmlBuilder.Relationship("masterBus", masterBusGuid)
        ));
        documentObjects.Add(XmlBuilder.Object("MasterTrack", masterTrackGuid,
            XmlBuilder.Relationship("mixerGroup", masterBusGuid)
        ));
        AppendEventBusObjects(documentObjects, "MixerInput", inputBusGuid, inputBus, ResolveMixerOutputGuid(inputBus.Routable.BaseGuid.ToGuid()), mixerGuid: null, eventNode: eventNode, timelineGuid: timelineGuid);
        documentObjects.Add(BuildAutomatablePropertiesObject(automatableGuid, eventNode));
        documentObjects.AddRange(markerObjects);

        for (int i = 0; i < groupBusGuids.Count; i++) {
            Guid groupBusGuid = groupBusGuids[i];
            Guid groupTrackGuid = groupTrackGuids[i];
            List<object> groupTrackContent = [];
            if (groupAutomationTracks[groupBusGuid].Count > 0) {
                groupTrackContent.Add(XmlBuilder.Relationship("automationTracks", groupAutomationTracks[groupBusGuid]));
            }
            if (trackModules.TryGetValue(groupBusGuid, out List<Guid>? modules) && modules.Count > 0) {
                groupTrackContent.Add(XmlBuilder.Relationship("modules", modules));
            }
            groupTrackContent.Add(XmlBuilder.Relationship("mixerGroup", groupBusGuid));
            documentObjects.Add(XmlBuilder.Object("GroupTrack", groupTrackGuid, groupTrackContent.ToArray()));
        }

        documentObjects.Add(XmlBuilder.Object("Timeline", timelineGuid, timelineContent.ToArray()));

        foreach (FModGuid layoutRef in eventNode.ParameterLayouts) {
            Guid layoutGuid = layoutRef.ToGuid();
            if (!_collectedBank.ParameterLayoutNodes.TryGetValue(layoutGuid, out ParameterLayoutNode? layout)) {
                throw new NotImplementedException($"Missing parameter layout {layoutGuid}");
            }

            List<object> proxyContent = [];
            if (parameterModules.TryGetValue(layoutGuid, out List<Guid>? modules) && modules.Count > 0) {
                proxyContent.Add(XmlBuilder.Relationship("modules", modules));
            }
            proxyContent.Add(XmlBuilder.Relationship("preset", layout.ParameterGuid.ToGuid()));
            documentObjects.Add(XmlBuilder.Object("ParameterProxy", layoutGuid, proxyContent.ToArray()));
        }

        AppendEventBusObjects(documentObjects, "EventMixerMaster", masterBusGuid, masterBus, outputGuid: null, mixerGuid: eventMixerGuid, eventNode: eventNode, timelineGuid: timelineGuid);

        HashSet<Guid> writtenInstruments = [];
        for (int i = 0; i < groupBusGuids.Count; i++) {
            Guid groupBusGuid = groupBusGuids[i];
            if (!_collectedBank.BusNodes.TryGetValue(groupBusGuid, out BaseBusNode? groupBus)) {
                throw new NotImplementedException($"Missing event group bus {groupBusGuid}");
            }
            string trackName = groupBusGuids.Count == 1 ? "Audio" : $"Audio {i + 1}";
            AppendEventBusObjects(documentObjects, "EventMixerGroup", groupBusGuid, groupBus, masterBusGuid, mixerGuid: null, name: trackName, eventNode: eventNode, timelineGuid: timelineGuid);

            if (!trackModules.TryGetValue(groupBusGuid, out List<Guid>? modules)) { continue; }
            foreach (Guid instrumentGuid in modules) {
                if (!writtenInstruments.Add(instrumentGuid)) { continue; }
                instrumentBoxes.TryGetValue(instrumentGuid, out FTriggerBox box);
                AppendInstrumentObjects(documentObjects, instrumentGuid, box, writtenInstruments, eventNode, timelineGuid, emitAutomationTracks: true);
            }
        }

        foreach (Guid instrumentGuid in timelineModules.Concat(parameterModules.Values.SelectMany(g => g))) {
            if (!writtenInstruments.Add(instrumentGuid)) { continue; }
            instrumentBoxes.TryGetValue(instrumentGuid, out FTriggerBox box);
            AppendInstrumentObjects(documentObjects, instrumentGuid, box, writtenInstruments, eventNode, timelineGuid, emitAutomationTracks: false);
        }

        XDocument document = XmlBuilder.CreateDocument(documentObjects.ToArray());
        document.Save(Path.Combine(_eventMetadataDirectory, $"{eventGuid.AsFmodStringFormat()}.xml"));
    }

    private List<Guid> ResolveEventGroupBuses(EventNode eventNode, Guid masterBusGuid) {
        if (eventNode.NonMasterTracks.Length > 0) {
            return eventNode.NonMasterTracks.Select(t => t.ToGuid()).ToList();
        }
        if (_eventMasterToGroups.TryGetValue(masterBusGuid, out List<Guid>? groups)) {
            return [.. groups];
        }
        return [];
    }

    private Dictionary<Guid, List<Guid>> BuildTrackModuleMap(
        EventNode eventNode,
        List<Guid> groupBusGuids,
        out List<Guid> timelineModules,
        out Dictionary<Guid, List<Guid>> parameterModules,
        out Dictionary<Guid, FTriggerBox> instrumentBoxes
    ) {
        Dictionary<Guid, List<Guid>> trackModules = new Dictionary<Guid, List<Guid>>();
        timelineModules = [];
        parameterModules = new Dictionary<Guid, List<Guid>>();
        instrumentBoxes = new Dictionary<Guid, FTriggerBox>();
        HashSet<Guid> groupSet = [.. groupBusGuids];

        void AddToTrack(Guid instrumentGuid, Guid routeGuid) {
            if (!groupSet.Contains(routeGuid)) { return; }
            if (!trackModules.TryGetValue(routeGuid, out List<Guid>? list)) {
                list = [];
                trackModules[routeGuid] = list;
            }
            if (!list.Contains(instrumentGuid)) { list.Add(instrumentGuid); }
        }

        if (_collectedBank.TimelineNodes.TryGetValue(eventNode.TimelineGuid.ToGuid(), out TimelineNode? timeline)) {
            foreach (FTriggerBox box in timeline.TimeLockedTriggerBoxes.Concat(timeline.TriggerBoxes)) {
                Guid instrumentGuid = box.Guid.ToGuid();
                instrumentBoxes[instrumentGuid] = box;
                if (!timelineModules.Contains(instrumentGuid)) { timelineModules.Add(instrumentGuid); }
                if (_collectedBank.InstrumentNodes.TryGetValue(instrumentGuid, out BaseInstrumentNode? instrument) &&
                    instrument.InstrumentBody is not null) {
                    AddToTrack(instrumentGuid, instrument.InstrumentBody.Routable.BaseGuid.ToGuid());
                }
            }
        }

        foreach (FModGuid layoutRef in eventNode.ParameterLayouts) {
            Guid layoutGuid = layoutRef.ToGuid();
            if (!_collectedBank.ParameterLayoutNodes.TryGetValue(layoutGuid, out ParameterLayoutNode? layout)) { continue; }

            List<Guid> modules = [];
            foreach (FModGuid instrumentRef in layout.Instruments) {
                Guid instrumentGuid = instrumentRef.ToGuid();
                modules.Add(instrumentGuid);
                if (_collectedBank.InstrumentNodes.TryGetValue(instrumentGuid, out BaseInstrumentNode? instrument) &&
                    instrument.InstrumentBody is not null) {
                    AddToTrack(instrumentGuid, instrument.InstrumentBody.Routable.BaseGuid.ToGuid());
                }
            }
            foreach (FTriggerBoxParameterLayout box in layout.TriggerBoxes) {
                Guid instrumentGuid = box.InstrumentGuid.ToGuid();
                if (!modules.Contains(instrumentGuid)) { modules.Add(instrumentGuid); }
                if (_collectedBank.InstrumentNodes.TryGetValue(instrumentGuid, out BaseInstrumentNode? instrument) &&
                    instrument.InstrumentBody is not null) {
                    AddToTrack(instrumentGuid, instrument.InstrumentBody.Routable.BaseGuid.ToGuid());
                }
            }
            if (modules.Count > 0) { parameterModules[layoutGuid] = modules; }
        }

        return trackModules;
    }

    private void AppendEventBusObjects(List<object> documentObjects, string className, Guid busGuid, BaseBusNode bus, Guid? outputGuid, Guid? mixerGuid, string? name = null, EventNode? eventNode = null, Guid timelineGuid = default) {
        BusNode body = bus.BusBody ?? throw new NotImplementedException($"Event bus {busGuid} missing BusBody");

        Guid effectChainGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{busGuid}/effectChain");
        Guid pannerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{busGuid}/panner");
        Guid faderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{busGuid}/fader");

        List<Guid> effectIds = [];
        List<object> effectObjects = [];

        foreach (FModGuid effectRef in body.PreFaderEffects) {
            AppendMixerEffect(effectRef.ToGuid(), effectIds, effectObjects);
        }
        effectIds.Add(faderGuid);
        effectObjects.Add(XmlBuilder.Object("MixerBusFader", faderGuid));
        foreach (FModGuid effectRef in body.PostFaderEffects) {
            AppendMixerEffect(effectRef.ToGuid(), effectIds, effectObjects);
        }

        List<object> busContent = [];
        if (name is not null) {
            busContent.Add(XmlBuilder.Property("name", name));
        }
        if (body.MixerStrip.Volume != 0) {
            busContent.Add(XmlBuilder.Property("volume", body.MixerStrip.Volume));
        }
        if (body.MixerStrip.Pitch != 0) {
            busContent.Add(XmlBuilder.Property("pitch", body.MixerStrip.Pitch));
        }

        if (eventNode is not null) {
            AppendOwnerAutomationAndModulators(documentObjects, busGuid, eventNode, timelineGuid, busContent);
        }

        busContent.Add(XmlBuilder.Relationship("effectChain", effectChainGuid));
        busContent.Add(XmlBuilder.Relationship("panner", pannerGuid));
        if (outputGuid is not null) {
            busContent.Add(XmlBuilder.Relationship("output", outputGuid.Value));
        }
        if (mixerGuid is not null) {
            busContent.Add(XmlBuilder.Relationship("mixer", mixerGuid.Value));
        }

        documentObjects.Add(XmlBuilder.Object(className, busGuid, busContent.ToArray()));
        documentObjects.Add(XmlBuilder.Object("MixerBusEffectChain", effectChainGuid,
            XmlBuilder.Relationship("effects", effectIds)
        ));
        documentObjects.Add(XmlBuilder.Object("MixerBusPanner", pannerGuid));
        documentObjects.AddRange(effectObjects);
    }

    private static XElement BuildAutomatablePropertiesObject(Guid automatableGuid, EventNode eventNode) {
        List<object> content = [];
        if (eventNode.Flags is not null && (eventNode.Flags.Value & 0x2) != 0) {
            content.Add(XmlBuilder.Property("isPersistent", true));
        }
        if (eventNode.MaximumPolyphony != 0) {
            content.Add(XmlBuilder.Property("maxVoices", eventNode.MaximumPolyphony));
        }
        if (eventNode.PolyphonyLimitBehavior) {
            content.Add(XmlBuilder.Property("voiceStealing", 1));
        }
        if (eventNode.Priority != 0) {
            content.Add(XmlBuilder.Property("priority", eventNode.Priority));
        }
        if (eventNode.DopplerScale is not null && eventNode.DopplerScale.Value != 0) {
            content.Add(XmlBuilder.Property("dopplerEnabled", true));
            content.Add(XmlBuilder.Property("dopplerScale", eventNode.DopplerScale.Value));
        }
        if (eventNode.TriggerCooldown is not null && eventNode.TriggerCooldown.Value != 0) {
            content.Add(XmlBuilder.Property("triggerCooldown", eventNode.TriggerCooldown.Value));
        }
        return XmlBuilder.Object("EventAutomatableProperties", automatableGuid, content.ToArray());
    }

    private void AppendInstrumentObjects(List<object> documentObjects, Guid instrumentGuid, FTriggerBox box, HashSet<Guid> writtenInstruments, EventNode eventNode, Guid timelineGuid, bool emitAutomationTracks) {
        if (!_collectedBank.InstrumentNodes.TryGetValue(instrumentGuid, out BaseInstrumentNode? instrument)) {
            throw new NotImplementedException($"Missing instrument {instrumentGuid}");
        }

        switch (instrument) {
            case WaveformInstrumentNode waveform:
                documentObjects.Add(BuildSingleSoundObject(documentObjects, instrumentGuid, waveform, instrument.InstrumentBody, box, eventNode, timelineGuid, emitAutomationTracks));
                break;

            case MultiInstrumentNode multi: {
                List<Guid> soundGuids = [];
                if (multi.PlaylistBody is not null) {
                    foreach (FPlaylistEntry entry in multi.PlaylistBody.Entries) {
                        soundGuids.Add(entry.Guid.ToGuid());
                    }
                }
                List<object> multiContent = [];
                AppendCommonInstrumentProperties(multiContent, instrument.InstrumentBody, box);
                AppendOwnerAutomationAndModulators(
                    documentObjects,
                    instrumentGuid,
                    eventNode,
                    timelineGuid,
                    multiContent,
                    emitAutomationTracks ? [] : null
                );
                if (soundGuids.Count > 0) {
                    multiContent.Add(XmlBuilder.Relationship("sounds", soundGuids));
                }
                documentObjects.Add(XmlBuilder.Object("MultiSound", instrumentGuid, multiContent.ToArray()));
                foreach (Guid entryGuid in soundGuids) {
                    if (writtenInstruments.Add(entryGuid)) {
                        AppendInstrumentObjects(documentObjects, entryGuid, default, writtenInstruments, eventNode, timelineGuid, emitAutomationTracks: false);
                    }
                }
                break;
            }

            case EventInstrumentNode eventInstrument: {
                List<object> content = [];
                AppendCommonInstrumentProperties(content, instrument.InstrumentBody, box);
                AppendOwnerAutomationAndModulators(documentObjects, instrumentGuid, eventNode, timelineGuid, content);
                if (eventInstrument.SnapshotIntensity != 0) {
                    content.Add(XmlBuilder.Property("intensity", eventInstrument.SnapshotIntensity));
                    content.Add(XmlBuilder.Relationship("event", eventInstrument.EventGuid.ToGuid()));
                    documentObjects.Add(XmlBuilder.Object("SnapshotModule", instrumentGuid, content.ToArray()));
                }
                else {
                    content.Add(XmlBuilder.Relationship("event", eventInstrument.EventGuid.ToGuid()));
                    documentObjects.Add(XmlBuilder.Object("EventSound", instrumentGuid, content.ToArray()));
                }
                break;
            }

            case SilenceInstrumentNode:
                List<object> silenceContent = BuildCommonInstrumentContent(instrument.InstrumentBody, box);
                AppendOwnerAutomationAndModulators(documentObjects, instrumentGuid, eventNode, timelineGuid, silenceContent);
                documentObjects.Add(XmlBuilder.Object("SilenceSound", instrumentGuid, silenceContent.ToArray()));
                break;

            default:
                throw new NotImplementedException($"Unsupported instrument type {instrument.GetType().Name}");
        }
    }

    private XElement BuildSingleSoundObject(List<object> documentObjects, Guid instrumentGuid, WaveformInstrumentNode waveform, InstrumentNode? body, FTriggerBox box, EventNode eventNode, Guid timelineGuid, bool emitAutomationTracks) {
        List<object> content = BuildCommonInstrumentContent(body, box);
        List<Guid>? automationTrackGuids = emitAutomationTracks ? [] : null;
        AppendOwnerAutomationAndModulators(documentObjects, instrumentGuid, eventNode, timelineGuid, content, automationTrackGuids);
        content.Add(XmlBuilder.Relationship("audioFile", waveform.WaveformResourceGuid.ToGuid()));
        return XmlBuilder.Object("SingleSound", instrumentGuid, content.ToArray());
    }

    private static List<object> BuildCommonInstrumentContent(InstrumentNode? body, FTriggerBox box) {
        List<object> content = [];
        AppendCommonInstrumentProperties(content, body, box);
        return content;
    }

    private static void AppendCommonInstrumentProperties(List<object> content, InstrumentNode? body, FTriggerBox box) {
        if (box.StartTime != 0) {
            content.Add(XmlBuilder.Property("start", box.StartTime / TimelineUnitsPerSecond));
        }
        if (box.Length != 0) {
            content.Add(XmlBuilder.Property("length", box.Length / TimelineUnitsPerSecond));
        }
        if (body is null) { return; }

        if (body.TriggerChancePercent is not 0 and not 100) {
            content.Add(XmlBuilder.Property("triggerProbabilityEnabled", true));
            content.Add(XmlBuilder.Property("triggerProbability", body.TriggerChancePercent));
        }
        if (body.InitialSeekPercent != 0) {
            content.Add(XmlBuilder.Property("startOffset", body.InitialSeekPercent));
        }
        if (body.Volume != 0) {
            content.Add(XmlBuilder.Property("volume", body.Volume));
        }
        if (body.Pitch != 0) {
            content.Add(XmlBuilder.Property("pitch", body.Pitch));
        }
        if (body.LoopCount != 0) {
            content.Add(XmlBuilder.Property("looping", true));
        }
    }

    private static uint ResolveEventOutputFormat(BaseBusNode masterBus) {
        if (masterBus.BusBody is not null && masterBus.BusBody.InputChannelLayout != 0) {
            return masterBus.BusBody.InputChannelLayout;
        }
        return 2;
    }

    private bool TryGetEventPath(Guid eventGuid, out string eventName, out string folderPath) {
        eventName = string.Empty;
        folderPath = string.Empty;
        if (!TryGetStringTablePath(eventGuid, out string path)) { return false; }
        if (!path.StartsWith("event:/", StringComparison.Ordinal)) { return false; }

        string rest = path["event:/".Length..];
        int slash = rest.LastIndexOf('/');
        if (slash < 0) {
            eventName = rest;
            folderPath = string.Empty;
        }
        else {
            folderPath = rest[..slash];
            eventName = rest[(slash + 1)..];
        }
        return eventName.Length > 0;
    }
}
