using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Effects;
using FModBankParser.Nodes.Instruments;
using FModBankParser.Nodes.Transitions;

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
}