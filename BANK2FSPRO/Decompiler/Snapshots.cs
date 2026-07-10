using System.Xml.Linq;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Effects;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler {
    private readonly Dictionary<string, Guid> _snapshotGroupGuids = new Dictionary<string, Guid>(StringComparer.Ordinal);

    private void ExtractSnapshots() {
        List<Guid> rootSnapshotItems = [];
        Dictionary<string, List<Guid>> groupItems = new Dictionary<string, List<Guid>>(StringComparer.Ordinal);

        foreach ((Guid eventGuid, EventNode eventNode) in _collectedBank.EventNodes) {
            if (!TryGetSnapshotPath(eventGuid, out string snapshotName, out string groupPath)) { continue; }

            WriteSnapshotDocument(eventGuid, eventNode, snapshotName);

            if (string.IsNullOrEmpty(groupPath)) {
                rootSnapshotItems.Add(eventGuid);
            }
            else {
                if (!groupItems.TryGetValue(groupPath, out List<Guid>? items)) {
                    items = [];
                    groupItems[groupPath] = items;
                }
                items.Add(eventGuid);
            }
        }

        List<Guid> snapshotListItems = [.. rootSnapshotItems];
        foreach ((string groupPath, List<Guid> items) in groupItems) {
            Guid groupGuid = EnsureSnapshotGroup(groupPath, items);
            snapshotListItems.Add(groupGuid);
        }

        List<object> snapshotListContent = [
            XmlBuilder.Relationship("mixer", _masterMixerGuid),
        ];
        if (snapshotListItems.Count > 0) {
            snapshotListContent.Insert(0, XmlBuilder.Relationship("items", snapshotListItems));
        }
        XDocument snapshotListMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("SnapshotList", _masterSnapshotListGuid, snapshotListContent.ToArray())
        );
        snapshotListMetadata.Save(Path.Combine(_snapshotGroupMetadataDirectory, $"{_masterSnapshotListGuid.AsFmodStringFormat()}.xml"));
    }

    private Guid EnsureSnapshotGroup(string groupPath, List<Guid> items) {
        if (_snapshotGroupGuids.TryGetValue(groupPath, out Guid existing)) { return existing; }

        string? parentPath = null;
        string groupName = groupPath;
        int slash = groupPath.LastIndexOf('/');
        if (slash >= 0) {
            parentPath = groupPath[..slash];
            groupName = groupPath[(slash + 1)..];
        }

        Guid folderGuid = parentPath is null
            ? _masterSnapshotListGuid
            : EnsureSnapshotGroup(parentPath, []);

        Guid groupGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/SnapshotGroup/{groupPath}");
        _snapshotGroupGuids[groupPath] = groupGuid;

        List<object> content = [
            XmlBuilder.Property("name", groupName),
            XmlBuilder.Relationship("folder", folderGuid),
        ];
        if (items.Count > 0) {
            content.Add(XmlBuilder.Relationship("items", items));
        }

        XDocument document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("SnapshotGroup", groupGuid, content.ToArray())
        );
        document.Save(Path.Combine(_snapshotGroupMetadataDirectory, $"{groupGuid.AsFmodStringFormat()}.xml"));
        return groupGuid;
    }

    private void WriteSnapshotDocument(Guid snapshotGuid, EventNode eventNode, string snapshotName) {
        SnapshotNode? body = null;
        if (!eventNode.SnapshotGuid.IsEmpty) {
            _collectedBank.SnapshotNodes.TryGetValue(eventNode.SnapshotGuid.ToGuid(), out body);
        }
        if (body is null) {
            _collectedBank.SnapshotNodes.TryGetValue(snapshotGuid, out body);
        }

        Guid automatableGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{snapshotGuid}/AutomatableProperties");
        Guid markerTrackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{snapshotGuid}/MarkerTrack");
        Guid timelineGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{snapshotGuid}/Timeline");
        Guid masterTrackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{snapshotGuid}/SnapshotMasterTrack");

        List<Guid> propertyGuids = [];
        List<Guid> trackGuids = [];
        List<object> extraObjects = [];
        HashSet<Guid> trackedStrips = [];

        if (body is not null) {
            for (int i = 0; i < body.Snapshots.Length; i++) {
                FSnapshot entry = body.Snapshots[i];
                Guid objectGuid = entry.SnapshotGuid.ToGuid();
                if (!IsSnapshotAutomatableObject(objectGuid)) { continue; }

                Guid propertyGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{snapshotGuid}/SnapshotProperty/{i}");
                propertyGuids.Add(propertyGuid);

                string propertyName = ResolveSnapshotPropertyName(objectGuid, entry);
                extraObjects.Add(XmlBuilder.Object("SnapshotProperty", propertyGuid,
                    XmlBuilder.Property("propertyName", propertyName),
                    XmlBuilder.Property("value", entry.Value),
                    XmlBuilder.Relationship("automatableObject", objectGuid)
                ));

                Guid stripGuid = ResolveSnapshotTrackStrip(objectGuid);
                if (!IsSnapshotTrackStrip(stripGuid)) { continue; }
                if (trackedStrips.Add(stripGuid)) {
                    Guid trackGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{snapshotGuid}/SnapshotTrack/{stripGuid}");
                    trackGuids.Add(trackGuid);
                    extraObjects.Add(XmlBuilder.Object("SnapshotTrack", trackGuid,
                        XmlBuilder.Relationship("mixerStrip", stripGuid)
                    ));
                }
            }
        }

        List<object> snapshotContent = [
            XmlBuilder.Property("name", snapshotName),
        ];
        if (body is not null) {
            if (body.BlendingSnapshot) {
                snapshotContent.Add(XmlBuilder.Property("behavior", 1));
            }
            if (body.Priority != 0) {
                snapshotContent.Add(XmlBuilder.Property("priority", body.Priority));
            }
        }
        snapshotContent.Add(XmlBuilder.Relationship("mixer", _masterMixerGuid));
        snapshotContent.Add(XmlBuilder.Relationship("automatableProperties", automatableGuid));
        snapshotContent.Add(XmlBuilder.Relationship("markerTracks", markerTrackGuid));
        snapshotContent.Add(XmlBuilder.Relationship("timeline", timelineGuid));
        snapshotContent.Add(XmlBuilder.Relationship("snapshotMasterTrack", masterTrackGuid));
        if (propertyGuids.Count > 0) {
            snapshotContent.Add(XmlBuilder.Relationship("snapshotProperties", propertyGuids));
        }
        if (trackGuids.Count > 0) {
            snapshotContent.Add(XmlBuilder.Relationship("snapshotTracks", trackGuids));
        }

        List<object> automatableContent = [];
        if (body is not null && body.Intensity != 0 && body.Intensity != 100) {
            automatableContent.Add(XmlBuilder.Property("snapshotIntensity", body.Intensity));
        }

        List<object> documentObjects = [
            XmlBuilder.Object("Snapshot", snapshotGuid, snapshotContent.ToArray()),
            XmlBuilder.Object("EventAutomatableProperties", automatableGuid, automatableContent.ToArray()),
            XmlBuilder.Object("MarkerTrack", markerTrackGuid),
            XmlBuilder.Object("Timeline", timelineGuid),
            XmlBuilder.Object("SnapshotMasterTrack", masterTrackGuid),
        ];
        documentObjects.AddRange(extraObjects);

        XDocument document = XmlBuilder.CreateDocument(documentObjects.ToArray());
        document.Save(Path.Combine(_snapshotMetadataDirectory, $"{snapshotGuid.AsFmodStringFormat()}.xml"));
    }

    private bool IsSnapshotAutomatableObject(Guid objectGuid) {
        if (_collectedBank.EffectNodes.ContainsKey(objectGuid)) { return true; }
        if (_collectedBank.VCANodes.ContainsKey(objectGuid)) { return true; }
        if (!_collectedBank.BusNodes.TryGetValue(objectGuid, out BaseBusNode? bus)) { return false; }
        // Only mixer buses we actually export (named bus:/ groups and returns).
        return bus is GroupBusNode or ReturnBusNode && TryGetMixerBusName(objectGuid, out _);
    }

    private bool IsSnapshotTrackStrip(Guid stripGuid) {
        if (_collectedBank.VCANodes.ContainsKey(stripGuid)) { return true; }
        if (!_collectedBank.BusNodes.TryGetValue(stripGuid, out BaseBusNode? bus)) { return false; }
        return bus is GroupBusNode or ReturnBusNode && TryGetMixerBusName(stripGuid, out _);
    }

    private Guid ResolveSnapshotTrackStrip(Guid objectGuid) {
        if (_collectedBank.BusNodes.ContainsKey(objectGuid) || _collectedBank.VCANodes.ContainsKey(objectGuid)) {
            return objectGuid;
        }

        foreach ((Guid busGuid, BaseBusNode bus) in _collectedBank.BusNodes) {
            BusNode? body = bus.BusBody;
            if (body is null) { continue; }
            if (body.PreFaderEffects.Any(e => e.ToGuid() == objectGuid) ||
                body.PostFaderEffects.Any(e => e.ToGuid() == objectGuid)) {
                return busGuid;
            }
        }

        return objectGuid;
    }

    private string ResolveSnapshotPropertyName(Guid objectGuid, FSnapshot entry) {
        _ = entry;
        if (_collectedBank.BusNodes.ContainsKey(objectGuid)) {
            return "volume";
        }
        if (_collectedBank.EffectNodes.TryGetValue(objectGuid, out BaseEffectNode? effect)) {
            if (effect is SendEffectNode) { return "level"; }
            return "wetLevel";
        }
        if (_collectedBank.VCANodes.ContainsKey(objectGuid)) {
            return "volume";
        }
        return "volume";
    }

    private bool TryGetSnapshotPath(Guid snapshotGuid, out string snapshotName, out string groupPath) {
        snapshotName = string.Empty;
        groupPath = string.Empty;
        if (!TryGetStringTablePath(snapshotGuid, out string path)) { return false; }
        if (!path.StartsWith("snapshot:/", StringComparison.Ordinal)) { return false; }

        string rest = path["snapshot:/".Length..];
        int slash = rest.LastIndexOf('/');
        if (slash < 0) {
            snapshotName = rest;
            groupPath = string.Empty;
        }
        else {
            groupPath = rest[..slash];
            snapshotName = rest[(slash + 1)..];
        }
        return snapshotName.Length > 0;
    }
}
