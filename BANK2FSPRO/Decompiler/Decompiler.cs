using Fmod5Sharp.FmodTypes;
using FModBankParser;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Effects;
using FModBankParser.Nodes.Instruments;
using FModBankParser.Nodes.Transitions;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler {
    public Decompiler(string outputDirectory, FModReader stringBank, FModReader masterBank, FModReader[] banks, string projectName = "DecompiledProject") {
        _outputDirectory = outputDirectory;
        _stringBank = stringBank;
        _masterBank = masterBank;
        _banks = banks;
        _projectName = projectName;
    }

    private readonly string _outputDirectory;
    private readonly FModReader _stringBank;
    private readonly FModReader _masterBank;
    private readonly FModReader[] _banks;
    private readonly string _projectName;
    private readonly CollectedBank _collectedBank = new CollectedBank();
    
    public readonly Dictionary<string, FModGuid> SoundFileReferences = new Dictionary<string, FModGuid>();
    public void Decompile() {
        SetupGuids();
        CollectNodes();

        SetupProjectFiles();
        
        ExtractSoundFiles();
    }

    public Guid MasterBankFolderGuid = Guid.Empty;
    private void SetupGuids() {
        MasterBankFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, _projectName);
    }

    private void SetupProjectFiles() { 
        if (Directory.Exists(_outputDirectory)) { Directory.Delete(_outputDirectory, true); }
        Directory.CreateDirectory(_outputDirectory);
        
        string bankFolderDirectory = Path.Combine(_outputDirectory, "Metadata", "BankFolder");
        
        XmlBuilder.Save(
            XmlBuilder.CreateDocument(),
            Path.Combine(_outputDirectory, $"{_projectName}.fspro")
        );
        
        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterBankFolder", MasterBankFolderGuid)
            ),
            Path.Combine(bankFolderDirectory, $"{MasterBankFolderGuid.ToFmodFormat()}.xml")
        );
    }
    
    private void CollectNodes() {
        foreach (FModReader bank in _banks) {
            foreach ((FModGuid key, EventNode node) in bank.EventNodes) { _collectedBank.EventNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseBusNode node) in bank.BusNodes) { _collectedBank.BusNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseEffectNode node) in bank.EffectNodes) { _collectedBank.EffectNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, TimelineNode node) in bank.TimelineNodes) { _collectedBank.TimelineNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseTransitionNode node) in bank.TransitionNodes) { _collectedBank.TransitionNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseInstrumentNode node) in bank.InstrumentNodes) { _collectedBank.InstrumentNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, WaveformResourceNode node) in bank.WavEntries) {
                _collectedBank.WavEntries.TryAdd(key.ToGuid(), node);
                
                foreach ((FModGuid fmodGuid, WaveformResourceNode waveformResourceNode) in bank.WavEntries) {
                    string? wavSampleName = bank.SoundBankData[waveformResourceNode.SoundBankIndex].Samples[waveformResourceNode.SubsoundIndex].Name;
                    if (wavSampleName is null) {
                        throw new NotImplementedException(); // TODO works for my bank
                    }

                    Guid guid = fmodGuid.ToGuid();
                    _collectedBank.SoundNameToGuid.Add(wavSampleName, guid);
                }
            }
            foreach ((FModGuid key, ParameterNode node) in bank.ParameterNodes) { _collectedBank.ParameterNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, ModulatorNode node) in bank.ModulatorNodes) { _collectedBank.ModulatorNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, CurveNode node) in bank.CurveNodes) { _collectedBank.CurveNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, PropertyNode node) in bank.PropertyNodes) { _collectedBank.PropertyNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, MappingNode node) in bank.MappingNodes) { _collectedBank.MappingNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, ParameterLayoutNode node) in bank.ParameterLayoutNodes) { _collectedBank.ParameterLayoutNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, ControllerNode node) in bank.ControllerNodes) { _collectedBank.ControllerNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, SnapshotNode node) in bank.SnapshotNodes) { _collectedBank.SnapshotNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, VCANode node) in bank.VCANodes) { _collectedBank.VCANodes.TryAdd(key.ToGuid(), node); }
        }
    }
    
    private void ExtractSoundFiles() {
        string metadataFileDirectory = Path.Combine(_outputDirectory, "Metadata", "AudioFile");

        foreach (FModReader bank in _banks) {
            if (bank.SoundBankData.Count == 0) { continue; }

            string soundFileDirectory = Path.Combine(_outputDirectory, "Assets", bank.BankName);
            Directory.CreateDirectory(soundFileDirectory);

            foreach (FmodSoundBank soundBank in bank.SoundBankData) {
                foreach (FmodSample samples in soundBank.Samples) {
                    string name = samples.Name!; // TODO works for my bank
                    
                    if (!samples.RebuildAsStandardFileFormat(out byte[]? data, out string? extension)) {
                        throw new NotImplementedException(); // TODO works for my bank
                    }
                    string filePath = Path.Combine(soundFileDirectory, $"{name}.{extension}");
                    File.WriteAllBytes(filePath, data);

                    if (samples.Metadata == null) { throw new NotImplementedException(); } // TODO works for my bank

                    if (!_collectedBank.SoundNameToGuid.TryGetValue(name, out Guid soundGuid)) { throw new NotImplementedException(); } // TODO works for my bank

                    string assetPath = filePath.Replace("\\", "/");
                    XmlBuilder.Save(
                        XmlBuilder.CreateDocument(
                            XmlBuilder.Object("AudioFile", soundGuid,
                                XmlBuilder.Property("assetPath", assetPath),
                                XmlBuilder.Property("frequencyInKHz", samples.Metadata.Frequency / 1000),
                                XmlBuilder.Property("channelCount", samples.Metadata.Channels),
                                XmlBuilder.Property("length", samples.Metadata.SampleCount / (double)samples.Metadata.Frequency),
                                XmlBuilder.Relationship("masterAssetFolder", MasterBankFolderGuid)
                            )
                        ),
                        Path.Combine(metadataFileDirectory, $"{soundGuid.ToFmodFormat()}.xml")
                    );
                }
            }
        }
    }
}