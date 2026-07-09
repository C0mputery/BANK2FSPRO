using System.Text;
using FModBankParser.Enums;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Effects;
using FModBankParser.Nodes.Instruments;
using FModBankParser.Nodes.ModulatorSubnodes;
using FModBankParser.Nodes.Transitions;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public class CollectedBank {
    public readonly Dictionary<string, Guid> SoundNameToGuid = new Dictionary<string, Guid>();

    public readonly Dictionary<Guid, EventNode> EventNodes = new Dictionary<Guid, EventNode>();
    public readonly Dictionary<Guid, BaseBusNode> BusNodes = new Dictionary<Guid, BaseBusNode>();
    public readonly Dictionary<Guid, BaseEffectNode> EffectNodes = new Dictionary<Guid, BaseEffectNode>();
    public readonly Dictionary<Guid, TimelineNode> TimelineNodes = new Dictionary<Guid, TimelineNode>();
    public readonly Dictionary<Guid, BaseTransitionNode> TransitionNodes = new Dictionary<Guid, BaseTransitionNode>();
    public readonly Dictionary<Guid, BaseInstrumentNode> InstrumentNodes = new Dictionary<Guid, BaseInstrumentNode>();
    public readonly Dictionary<Guid, WaveformResourceNode> WavEntries = new Dictionary<Guid, WaveformResourceNode>();
    public readonly Dictionary<Guid, ParameterNode> ParameterNodes = new Dictionary<Guid, ParameterNode>();
    public readonly Dictionary<Guid, ModulatorNode> ModulatorNodes = new Dictionary<Guid, ModulatorNode>();
    public readonly Dictionary<Guid, CurveNode> CurveNodes = new Dictionary<Guid, CurveNode>();
    public readonly Dictionary<Guid, PropertyNode> PropertyNodes = new Dictionary<Guid, PropertyNode>();
    public readonly Dictionary<Guid, MappingNode> MappingNodes = new Dictionary<Guid, MappingNode>();
    public readonly Dictionary<Guid, ParameterLayoutNode> ParameterLayoutNodes = new Dictionary<Guid, ParameterLayoutNode>();
    public readonly Dictionary<Guid, ControllerNode> ControllerNodes = new Dictionary<Guid, ControllerNode>();
    public readonly Dictionary<Guid, SnapshotNode> SnapshotNodes = new Dictionary<Guid, SnapshotNode>();
    public readonly Dictionary<Guid, VCANode> VCANodes = new Dictionary<Guid, VCANode>();

    public void Debug() {
        SortedSet<string> features = new SortedSet<string>(StringComparer.Ordinal);

        CollectEventFeatures(features);
        CollectBusFeatures(features);
        CollectEffectFeatures(features);
        CollectTimelineFeatures(features);
        CollectTransitionFeatures(features);
        CollectInstrumentFeatures(features);
        CollectWaveformFeatures(features);
        CollectParameterFeatures(features);
        CollectModulatorFeatures(features);
        CollectCurveFeatures(features);
        CollectPropertyFeatures(features);
        CollectMappingFeatures(features);
        CollectParameterLayoutFeatures(features);
        CollectControllerFeatures(features);
        CollectSnapshotFeatures(features);
        CollectVcaFeatures(features);
        CollectSoundFeatures(features);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== Unique Reconstruction Features ==========");
        sb.AppendLine($"Count: {features.Count}");
        sb.AppendLine();
        foreach (string feature in features) {
            sb.AppendLine($"  {feature}");
        }
        sb.AppendLine("========== End Unique Reconstruction Features ==========");
        Console.WriteLine(sb.ToString());
    }

    private void CollectEventFeatures(SortedSet<string> features) {
        if (EventNodes.Count == 0) { return; }
        features.Add("Event");

        foreach (EventNode node in EventNodes.Values) {
            if (!node.SnapshotGuid.IsEmpty) { features.Add("Event.SnapshotReference"); }
            if (!node.TimelineGuid.IsEmpty) { features.Add("Event.Timeline"); }
            if (!node.InputBusGuid.IsEmpty) { features.Add("Event.InputBus"); }
            if (!node.MasterTrackGuid.IsEmpty) { features.Add("Event.MasterTrack"); }
            if (node.MaximumPolyphony != 0) { features.Add("Event.MaximumPolyphony"); }
            if (node.Priority != 0) { features.Add("Event.Priority"); }
            if (node.PolyphonyLimitBehavior) { features.Add("Event.PolyphonyLimitBehavior"); }
            if (node.SchedulingMode != 0) { features.Add($"Event.SchedulingMode={node.SchedulingMode}"); }
            if (node.ParameterLayouts.Length > 0) { features.Add("Event.ParameterLayouts"); }
            if (node.UserPropertyFloatList.Length > 0) { features.Add("Event.UserPropertyFloat"); }
            if (node.UserPropertyStringList.Length > 0) { features.Add("Event.UserPropertyString"); }
            if (node.DopplerScale is not null) { features.Add("Event.DopplerScale"); }
            if (node.TriggerCooldown is not null) { features.Add("Event.TriggerCooldown"); }
            if (node.Flags is not null) { features.Add($"Event.Flags=0x{node.Flags.Value:X}"); }
            if (node.NonMasterTracks.Length > 0) { features.Add("Event.NonMasterTracks"); }
            if (node.ParameterIds.Length > 0) { features.Add("Event.ParameterIds"); }
            if (node.EventTriggeredInstruments.Length > 0) { features.Add("Event.EventTriggeredInstruments"); }
            if (node.MinimumDistance is not null || node.MaximumDistance is not null) { features.Add("Event.DistanceAttenuation"); }
        }
    }

    private void CollectBusFeatures(SortedSet<string> features) {
        if (BusNodes.Count == 0) { return; }

        foreach (BaseBusNode node in BusNodes.Values) {
            features.Add($"Bus.{node.GetType().Name}");
            if (!node.Routable.BaseGuid.IsEmpty || node.Routable.OutputChannelLayout != 0 || node.Routable.ChannelMask != 0) {
                features.Add("Bus.Routable");
            }

            BusNode? body = node.BusBody;
            if (body is null) { continue; }

            features.Add("Bus.BusBody");
            if (body.Flags != 0) { features.Add($"Bus.Flags=0x{body.Flags:X}"); }
            if (body.InputChannelLayout != 0) { features.Add("Bus.InputChannelLayout"); }
            if (body.PreFaderEffects.Length > 0) { features.Add("Bus.PreFaderEffects"); }
            if (body.PostFaderEffects.Length > 0) { features.Add("Bus.PostFaderEffects"); }
            CollectMixerStripFeatures(features, "Bus", body.MixerStrip);
            if (body.MaximumPolyphony != 0) { features.Add("Bus.MaximumPolyphony"); }
            if (body.PolyphonyLimitBehavior != 0) { features.Add($"Bus.PolyphonyLimitBehavior={body.PolyphonyLimitBehavior}"); }
            if (body.PreFaderInputChannelLayouts.Length > 0) { features.Add("Bus.PreFaderInputChannelLayouts"); }
            if (body.PostFaderInputChannelLayouts.Length > 0) { features.Add("Bus.PostFaderInputChannelLayouts"); }
            if (body.ObjectPannerIndex != 0) { features.Add("Bus.ObjectPannerIndex"); }
            if (body.PortType != default) { features.Add($"Bus.PortType={body.PortType}"); }
        }
    }

    private void CollectEffectFeatures(SortedSet<string> features) {
        if (EffectNodes.Count == 0) { return; }

        foreach (BaseEffectNode node in EffectNodes.Values) {
            features.Add($"Effect.{node.GetType().Name}");

            if (node.EffectBody is not null) {
                features.Add("Effect.EffectBody");
                EffectNode body = node.EffectBody;
                if (body.Flags != 0) { features.Add($"Effect.Flags=0x{body.Flags:X}"); }
                if (body.WetMix != 0) { features.Add("Effect.WetMix"); }
                if (body.WetLevel != 0) { features.Add("Effect.WetLevel"); }
                if (body.DryLevel != 0) { features.Add("Effect.DryLevel"); }
                if (body.InputGain != 0) { features.Add("Effect.InputGain"); }
            }

            switch (node) {
                case BuiltInEffectNode builtIn:
                    features.Add($"Effect.BuiltIn.DSPType={builtIn.DSPType}");
                    if (builtIn.InputChannelLayout != 0) { features.Add("Effect.BuiltIn.InputChannelLayout"); }
                    CollectParameterizedEffectFeatures(features, builtIn.ParamEffectBody);
                    break;
                case PluginEffectNode plugin:
                    features.Add($"Effect.Plugin.PluginName={plugin.PluginName}");
                    if (!string.IsNullOrEmpty(plugin.Name)) { features.Add($"Effect.Plugin.Name={plugin.Name}"); }
                    CollectParameterizedEffectFeatures(features, plugin.ParamEffectBody);
                    break;
                case SendEffectNode send:
                    features.Add("Effect.Send");
                    if (!send.ReturnGuid.IsEmpty) { features.Add("Effect.Send.ReturnGuid"); }
                    if (send.SendLevel != 0) { features.Add("Effect.Send.SendLevel"); }
                    if (send.InputChannelLayout != 0) { features.Add("Effect.Send.InputChannelLayout"); }
                    break;
                case SideChainEffectNode sideChain:
                    features.Add("Effect.SideChain");
                    if (sideChain.Targets.Length > 0) { features.Add("Effect.SideChain.Targets"); }
                    if (sideChain.Modulators.Length > 0) { features.Add("Effect.SideChain.Modulators"); }
                    if (sideChain.SideChainLevel != 0) { features.Add("Effect.SideChain.Level"); }
                    break;
                case SpectralSideChainEffectNode spectral:
                    features.Add("Effect.SpectralSideChain");
                    if (spectral.Targets.Length > 0) { features.Add("Effect.SpectralSideChain.Targets"); }
                    if (spectral.Flags != 0) { features.Add($"Effect.SpectralSideChain.Flags=0x{spectral.Flags:X}"); }
                    break;
            }
        }
    }

    private static void CollectParameterizedEffectFeatures(SortedSet<string> features, ParameterizedEffectNode? body) {
        if (body is null) { return; }
        features.Add("Effect.Parameterized");
        if (body.SideChainEnabled) { features.Add("Effect.Parameterized.SideChainEnabled"); }
        foreach (FEffectParameter parameter in body.Parameters) {
            features.Add($"Effect.Parameterized.ParameterType={parameter.Type}");
            if (parameter.Buffer is { Length: > 0 }) { features.Add("Effect.Parameterized.BufferParameter"); }
        }
    }

    private void CollectTimelineFeatures(SortedSet<string> features) {
        if (TimelineNodes.Count == 0) { return; }
        features.Add("Timeline");

        foreach (TimelineNode node in TimelineNodes.Values) {
            if (!node.LegacyGuid.IsEmpty) { features.Add("Timeline.LegacyGuid"); }
            if (node.TriggerBoxes.Length > 0) { features.Add("Timeline.TriggerBoxes"); }
            if (node.TimeLockedTriggerBoxes.Length > 0) { features.Add("Timeline.TimeLockedTriggerBoxes"); }
            if (node.SustainPoints.Length > 0) {
                features.Add("Timeline.SustainPoints");
                foreach (FSustainPoint point in node.SustainPoints) {
                    CollectEvaluatorFeatures(features, "Timeline.SustainPoint", point.Evaluators);
                }
            }
            if (node.TimelineNamedMarkers.Length > 0) { features.Add("Timeline.NamedMarkers"); }
            if (node.TimelineTempoMarkers.Length > 0) { features.Add("Timeline.TempoMarkers"); }
            if (node.LegacyUIntArray.Length > 0) { features.Add("Timeline.LegacyUIntArray"); }
        }
    }

    private void CollectTransitionFeatures(SortedSet<string> features) {
        if (TransitionNodes.Count == 0) { return; }

        foreach (BaseTransitionNode node in TransitionNodes.Values) {
            features.Add($"Transition.{node.GetType().Name}");

            if (node is TransitionRegionNode region) {
                if (!region.DestinationGuid.IsEmpty) { features.Add("Transition.Destination"); }
                if (region.Start != 0 || region.End != 0) { features.Add("Transition.StartEnd"); }
                if (region.LegacyParameterConditions is not null) { features.Add("Transition.LegacyParameterConditions"); }
                CollectEvaluatorFeatures(features, "Transition", region.Evaluators);
                CollectQuantizationFeatures(features, "Transition", region.Quantization);
                if (region.TransitionChancePercent is not 0 and not 100) { features.Add("Transition.ChancePercent"); }
                if (region.Flags != 0) { features.Add($"Transition.Flags=0x{region.Flags:X}"); }
            }

            TransitionTimelineNode? body = node.TransitionBody;
            if (body is null) { continue; }

            features.Add("Transition.TransitionTimeline");
            if (body.Length != 0) { features.Add("Transition.Length"); }
            if (body.TimeLockedTriggerBoxes.Length > 0) { features.Add("Transition.TimeLockedTriggerBoxes"); }
            if (body.TriggeredTriggerBoxes.Length > 0) { features.Add("Transition.TriggeredTriggerBoxes"); }
            if (body.LeadInLength != 0) { features.Add("Transition.LeadIn"); }
            if (body.LeadOutLength != 0) { features.Add("Transition.LeadOut"); }
            if (body.LeadInCurves.Length > 0) { features.Add("Transition.LeadInCurves"); }
            if (body.LeadOutCurves.Length > 0) { features.Add("Transition.LeadOutCurves"); }
            if (!body.CurveMappingGuid.IsEmpty) { features.Add("Transition.CurveMapping"); }
            if (body.FadeOverrides.Length > 0) { features.Add("Transition.FadeOverrides"); }
        }
    }

    private void CollectInstrumentFeatures(SortedSet<string> features) {
        if (InstrumentNodes.Count == 0) { return; }

        foreach (BaseInstrumentNode node in InstrumentNodes.Values) {
            features.Add($"Instrument.{node.GetType().Name}");

            if (node.InstrumentBody is not null) {
                features.Add("Instrument.InstrumentBody");
                CollectInstrumentBodyFeatures(features, node.InstrumentBody);
            }

            switch (node) {
                case WaveformInstrumentNode waveform:
                    features.Add($"Instrument.Waveform.LoadingMode={waveform.LegacyLoadingMode}");
                    if (!waveform.WaveformResourceGuid.IsEmpty) { features.Add("Instrument.Waveform.Resource"); }
                    break;
                case MultiInstrumentNode multi:
                    CollectPlaylistFeatures(features, "Instrument.Multi", multi.PlaylistBody);
                    break;
                case EventInstrumentNode eventInstrument:
                    features.Add("Instrument.EventReference");
                    if (eventInstrument.SnapshotIntensity != 0) { features.Add("Instrument.Event.SnapshotIntensity"); }
                    if (eventInstrument.EventParameterStubs.Length > 0) { features.Add("Instrument.Event.ParameterStubs"); }
                    break;
                case SilenceInstrumentNode silence:
                    features.Add("Instrument.Silence");
                    if (silence.Duration != 0) { features.Add("Instrument.Silence.Duration"); }
                    break;
                case ScattererInstrumentNode scatterer:
                    features.Add("Instrument.Scatterer");
                    if (scatterer.MaximumSpawnPolyphony != 0) { features.Add("Instrument.Scatterer.MaximumSpawnPolyphony"); }
                    if (scatterer.SpawnCount != 0) { features.Add("Instrument.Scatterer.SpawnCount"); }
                    if (scatterer.SpawnTime.Minimum != 0 || scatterer.SpawnTime.Maximum != 0) { features.Add("Instrument.Scatterer.SpawnTime"); }
                    if (scatterer.SpawnPolyphonyLimitBehavior != 0) { features.Add($"Instrument.Scatterer.SpawnPolyphonyLimitBehavior={scatterer.SpawnPolyphonyLimitBehavior}"); }
                    if (scatterer.SpawnRate != 0) { features.Add("Instrument.Scatterer.SpawnRate"); }
                    if (scatterer.SpawnQuantization is not null) {
                        CollectQuantizationFeatures(features, "Instrument.Scatterer", scatterer.SpawnQuantization.Value);
                    }
                    CollectPlaylistFeatures(features, "Instrument.Scatterer", scatterer.PlaylistBody);
                    break;
                case ProgrammerInstrumentNode programmer:
                    features.Add("Instrument.Programmer");
                    if (!string.IsNullOrEmpty(programmer.Name)) { features.Add("Instrument.Programmer.Name"); }
                    break;
                case EffectInstrumentNode effectInstrument:
                    features.Add("Instrument.Effect");
                    if (!effectInstrument.EffectGuid.IsEmpty) { features.Add("Instrument.Effect.EffectGuid"); }
                    break;
                case CommandInstrumentNode command:
                    features.Add($"Instrument.Command.Type={command.CommandType}");
                    if (!command.TargetGuid.IsEmpty) { features.Add("Instrument.Command.Target"); }
                    if (command.Value != 0) { features.Add("Instrument.Command.Value"); }
                    break;
            }
        }
    }

    private static void CollectInstrumentBodyFeatures(SortedSet<string> features, InstrumentNode body) {
        if (!body.TimelineGuid.IsEmpty) { features.Add("Instrument.Timeline"); }
        if (body.Volume != 0) { features.Add("Instrument.Volume"); }
        if (body.Pitch != 0) { features.Add("Instrument.Pitch"); }
        if (body.LoopCount != 0) { features.Add($"Instrument.LoopCount"); }
        if (body.Flags != 0) { features.Add($"Instrument.Flags=0x{body.Flags:X}"); }
        if (body.Offset3DDistance != 0) { features.Add("Instrument.Offset3DDistance"); }
        if (body.TriggerChancePercent is not 0 and not 100) { features.Add("Instrument.TriggerChancePercent"); }
        if (body.TriggerDelay.Min != 0 || body.TriggerDelay.Max != 0) { features.Add("Instrument.TriggerDelay"); }
        CollectQuantizationFeatures(features, "Instrument", body.Quantization);
        if (!body.ControlParameterGuid.IsEmpty) { features.Add("Instrument.ControlParameter"); }
        if (body.AutoPitchReference != 0) { features.Add("Instrument.AutoPitchReference"); }
        if (body.InitialSeekPosition != 0) { features.Add("Instrument.InitialSeekPosition"); }
        if (body.MaximumPolyphony != 0) { features.Add("Instrument.MaximumPolyphony"); }
        if (!body.Routable.BaseGuid.IsEmpty || body.Routable.OutputChannelLayout != 0 || body.Routable.ChannelMask != 0) {
            features.Add("Instrument.Routable");
        }
        if (body.PolyphonyLimitBehavior != 0) { features.Add($"Instrument.PolyphonyLimitBehavior={body.PolyphonyLimitBehavior}"); }
        if (body.LeftTrimOffset != 0) { features.Add("Instrument.LeftTrimOffset"); }
        if (body.InitialSeekPercent != 0) { features.Add("Instrument.InitialSeekPercent"); }
        if (body.AutoPitchAtMinimum != 0) { features.Add("Instrument.AutoPitchAtMinimum"); }
        CollectEvaluatorFeatures(features, "Instrument", body.Evaluators);
    }

    private static void CollectPlaylistFeatures(SortedSet<string> features, string prefix, PlaylistNode? playlist) {
        if (playlist is null) { return; }
        features.Add($"{prefix}.Playlist");
        features.Add($"{prefix}.Playlist.PlayMode={playlist.PlayMode}");
        features.Add($"{prefix}.Playlist.SelectionMode={playlist.SelectionMode}");
        if (playlist.Entries.Length > 0) { features.Add($"{prefix}.Playlist.Entries"); }
    }

    private void CollectWaveformFeatures(SortedSet<string> features) {
        if (WavEntries.Count == 0) { return; }
        features.Add("WaveformResource");

        foreach (WaveformResourceNode node in WavEntries.Values) {
            features.Add($"WaveformResource.LoadingMode={node.LoadingMode}");
        }
    }

    private void CollectParameterFeatures(SortedSet<string> features) {
        if (ParameterNodes.Count == 0) { return; }
        features.Add("Parameter");

        foreach (ParameterNode node in ParameterNodes.Values) {
            features.Add($"Parameter.Type={node.Type}");
            if (node.Flags != 0) { features.Add($"Parameter.Flags=0x{node.Flags:X}"); }
            if (!string.IsNullOrEmpty(node.Name)) { features.Add("Parameter.Name"); }
            if (node.Minimum != 0 || node.Maximum != 0) { features.Add("Parameter.Range"); }
            if (node.DefaultValue != 0) { features.Add("Parameter.DefaultValue"); }
            if (node.Velocity != 0) { features.Add("Parameter.Velocity"); }
            if (node.SeekSpeed != 0) { features.Add("Parameter.SeekSpeed"); }
            if (node.SeekSpeedDown != 0) { features.Add("Parameter.SeekSpeedDown"); }
            if (node.Labels.Length > 0) { features.Add("Parameter.Labels"); }
        }
    }

    private void CollectModulatorFeatures(SortedSet<string> features) {
        if (ModulatorNodes.Count == 0) { return; }
        features.Add("Modulator");

        foreach (ModulatorNode node in ModulatorNodes.Values) {
            features.Add($"Modulator.Type={node.Type}");
            features.Add($"Modulator.PropertyType={node.PropertyType}");
            features.Add($"Modulator.ClockSource={node.ClockSource}");
            if (!node.OwnerGuid.IsEmpty) { features.Add("Modulator.Owner"); }
            if (node.PropertyIndex != 0) { features.Add("Modulator.PropertyIndex"); }

            switch (node.Subnode) {
                case ADSRModulatorNode:
                    features.Add("Modulator.ADSR");
                    break;
                case RandomModulatorNode:
                    features.Add("Modulator.Random");
                    break;
                case EnvelopeModulatorNode envelope:
                    features.Add("Modulator.Envelope");
                    if (envelope.EffectId is not null) { features.Add("Modulator.Envelope.EffectId"); }
                    break;
                case LFOModulatorNode lfo:
                    features.Add("Modulator.LFO");
                    features.Add($"Modulator.LFO.Shape={lfo.Shape}");
                    if (lfo.Flags != 0) { features.Add($"Modulator.LFO.Flags=0x{lfo.Flags:X}"); }
                    break;
                case SeekModulatorNode seek:
                    features.Add("Modulator.Seek");
                    if (seek.Flags != 0) { features.Add($"Modulator.Seek.Flags=0x{seek.Flags:X}"); }
                    break;
                case SpectralSidechainModulatorNode spectral:
                    features.Add("Modulator.SpectralSidechain");
                    features.Add($"Modulator.SpectralSidechain.Mode={spectral.Mode}");
                    if (!spectral.ThresholdMapping.IsEmpty) { features.Add("Modulator.SpectralSidechain.ThresholdMapping"); }
                    break;
                case null:
                    break;
                default:
                    features.Add($"Modulator.UnknownSubnode={node.Subnode.GetType().Name}");
                    break;
            }
        }
    }

    private void CollectCurveFeatures(SortedSet<string> features) {
        if (CurveNodes.Count == 0) { return; }
        features.Add("Curve");

        foreach (CurveNode node in CurveNodes.Values) {
            if (!node.OwnerGuid.IsEmpty) { features.Add("Curve.Owner"); }
            if (node.CurvePoints.Length > 0) {
                features.Add("Curve.Points");
                foreach (FCurvePoint point in node.CurvePoints) {
                    features.Add($"Curve.Point.Type={point.Type}");
                    if (point.Shape != 0) { features.Add("Curve.Point.Shape"); }
                }
            }
        }
    }

    private void CollectPropertyFeatures(SortedSet<string> features) {
        if (PropertyNodes.Count == 0) { return; }
        features.Add("Property");

        foreach (PropertyNode node in PropertyNodes.Values) {
            features.Add($"Property.Index={node.Index}");
            features.Add($"Property.Method={node.Method}");
            features.Add($"Property.Type={node.Type}");
            if (!node.MappingGuid.IsEmpty) { features.Add("Property.Mapping"); }
            if (node.Controllers.Length > 0) { features.Add("Property.Controllers"); }
            if (node.Modulators.Length > 0) { features.Add("Property.Modulators"); }
        }
    }

    private void CollectMappingFeatures(SortedSet<string> features) {
        if (MappingNodes.Count == 0) { return; }
        features.Add("Mapping");
        if (MappingNodes.Values.Any(n => n.MappingPoints.Length > 0)) {
            features.Add("Mapping.Points");
        }
    }

    private void CollectParameterLayoutFeatures(SortedSet<string> features) {
        if (ParameterLayoutNodes.Count == 0) { return; }
        features.Add("ParameterLayout");

        foreach (ParameterLayoutNode node in ParameterLayoutNodes.Values) {
            if (!node.ParameterGuid.IsEmpty) { features.Add("ParameterLayout.Parameter"); }
            if (!node.LegacyGuid.IsEmpty) { features.Add("ParameterLayout.LegacyGuid"); }
            if (node.Instruments.Length > 0) { features.Add("ParameterLayout.Instruments"); }
            if (node.Flags != 0) { features.Add($"ParameterLayout.Flags=0x{node.Flags:X}"); }
            if (node.TriggerBoxes.Length > 0) { features.Add("ParameterLayout.TriggerBoxes"); }
        }
    }

    private void CollectControllerFeatures(SortedSet<string> features) {
        if (ControllerNodes.Count == 0) { return; }
        features.Add("Controller");

        foreach (ControllerNode node in ControllerNodes.Values) {
            if (!node.PropertyOwnerGuid.IsEmpty) { features.Add("Controller.PropertyOwner"); }
            if (!node.CurveGuid.IsEmpty) { features.Add("Controller.Curve"); }
            if (node.PropertyIndex != 0) { features.Add("Controller.PropertyIndex"); }
        }
    }

    private void CollectSnapshotFeatures(SortedSet<string> features) {
        if (SnapshotNodes.Count == 0) { return; }
        features.Add("Snapshot");

        foreach (SnapshotNode node in SnapshotNodes.Values) {
            if (node.Priority != 0) { features.Add("Snapshot.Priority"); }
            if (node.Snapshots.Length > 0) { features.Add("Snapshot.Entries"); }
            if (node.BlendingSnapshot) { features.Add("Snapshot.Blending"); }
            features.Add($"Snapshot.GroupResolutionMethod={node.GroupResolutionMethod}");
            if (node.Intensity != 0) { features.Add("Snapshot.Intensity"); }
        }
    }

    private void CollectVcaFeatures(SortedSet<string> features) {
        if (VCANodes.Count == 0) { return; }
        features.Add("VCA");

        foreach (VCANode node in VCANodes.Values) {
            if (node.Strips.Length > 0) { features.Add("VCA.Strips"); }
            CollectMixerStripFeatures(features, "VCA", node.MixerStrip);
        }
    }

    private void CollectSoundFeatures(SortedSet<string> features) {
        if (SoundNameToGuid.Count == 0) { return; }
        features.Add("AudioFile");
        features.Add("AudioFile.SoundNameToGuid");
    }

    private static void CollectMixerStripFeatures(SortedSet<string> features, string prefix, FMixerStrip strip) {
        if (strip.Volume != 0) { features.Add($"{prefix}.MixerStrip.Volume"); }
        if (strip.Pitch != 0) { features.Add($"{prefix}.MixerStrip.Pitch"); }
        if (strip.VCAs.Length > 0) { features.Add($"{prefix}.MixerStrip.VCAs"); }
    }

    private static void CollectQuantizationFeatures(SortedSet<string> features, string prefix, FQuantization quantization) {
        if (quantization.Unit == default && quantization.Multiplier == 0) { return; }
        features.Add($"{prefix}.Quantization");
        features.Add($"{prefix}.Quantization.Unit={quantization.Unit}");
        if (quantization.Multiplier != 0) { features.Add($"{prefix}.Quantization.Multiplier"); }
    }

    private static void CollectEvaluatorFeatures(SortedSet<string> features, string prefix, IReadOnlyList<FEvaluator> evaluators) {
        if (evaluators.Count == 0) { return; }
        features.Add($"{prefix}.Evaluators");
        foreach (FEvaluator evaluator in evaluators) {
            features.Add($"{prefix}.Evaluator.Type={evaluator.Type}");
        }
    }
}
