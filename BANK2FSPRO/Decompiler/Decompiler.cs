using System.Xml.Linq;
using Fmod5Sharp.FmodTypes;
using FModBankParser;
using FModBankParser.Enums;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Effects;
using FModBankParser.Nodes.Instruments;
using FModBankParser.Nodes.Transitions;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler(string outputDirectory, FModReader stringBank, FModReader masterBank, FModReader[] banks, string projectName = "DecompiledProject") {
    private readonly FModReader _stringBank = stringBank;
    private readonly CollectedBank _collectedBank = new CollectedBank();
    
    public void Decompile() {
        SetupGuids();
        SetupDirectories();
        SetupProjectFiles();

        CollectNodes();
        //_collectedBank.Debug();
        
        ExtractSoundFiles();
        ExtractParameters();
        ExtractMixer();
        ExtractEvents();
        ExtractSnapshots();
    }

    private void CollectNodes() {
        foreach (FModReader bank in banks) {
            Guid bankGuid = bank.BankInfo.BaseGuid.ToGuid();
            foreach ((FModGuid key, EventNode node) in bank.EventNodes) {
                Guid eventGuid = key.ToGuid();
                _collectedBank.EventNodes.TryAdd(eventGuid, node);
                if (!_collectedBank.EventToBanks.TryGetValue(eventGuid, out List<Guid>? bankGuids)) {
                    bankGuids = [];
                    _collectedBank.EventToBanks[eventGuid] = bankGuids;
                }
                if (!bankGuids.Contains(bankGuid)) {
                    bankGuids.Add(bankGuid);
                }
            }
            foreach ((FModGuid key, BaseBusNode node) in bank.BusNodes) { _collectedBank.BusNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseEffectNode node) in bank.EffectNodes) { _collectedBank.EffectNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, TimelineNode node) in bank.TimelineNodes) { _collectedBank.TimelineNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseTransitionNode node) in bank.TransitionNodes) { _collectedBank.TransitionNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, BaseInstrumentNode node) in bank.InstrumentNodes) { _collectedBank.InstrumentNodes.TryAdd(key.ToGuid(), node); }
            foreach ((FModGuid key, WaveformResourceNode node) in bank.WavEntries) {
                _collectedBank.WavEntries.Add(key.ToGuid(), node);

                foreach ((FModGuid fmodGuid, WaveformResourceNode waveformResourceNode) in bank.WavEntries) {
                    string? wavSampleName = bank.SoundBankData[waveformResourceNode.SoundBankIndex].Samples[waveformResourceNode.SubsoundIndex].Name;
                    if (wavSampleName is null) {
                        throw new NotImplementedException(); // TODO works for my bank
                    }

                    Guid guid = fmodGuid.ToGuid();
                    if (_collectedBank.SoundNameToGuid.TryAdd(wavSampleName, guid)) { continue; }
                    if (_collectedBank.SoundNameToGuid[wavSampleName] != guid) { 
                        throw new NotImplementedException(); // TODO works for my bank
                    }
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
        foreach (FModReader bank in banks) {
            foreach (FmodSoundBank soundBank in bank.SoundBankData) {
                foreach (FmodSample samples in soundBank.Samples) {
                    if (samples.Name == null) { throw new NotImplementedException(); } // TODO works for my bank

                    if (!samples.RebuildAsStandardFileFormat(out byte[]? data, out string? extension)) { throw new NotImplementedException(); } // TODO works for my bank
                    string fileName = $"{samples.Name}.{extension}";
                    string filePath = Path.Combine(_assetsDirectory, fileName);
                    File.WriteAllBytes(filePath, data);

                    if (samples.Metadata == null) { throw new NotImplementedException(); } // TODO works for my bank

                    if (!_collectedBank.SoundNameToGuid.TryGetValue(samples.Name, out Guid soundGuid)) { throw new NotImplementedException(); } // TODO works for my bank
                    if (!_collectedBank.WavEntries.TryGetValue(soundGuid, out WaveformResourceNode? waveformResource)) { throw new NotImplementedException(); } // TODO works for my bank

                    bool isStreaming = waveformResource.LoadingMode == EWaveformLoadingMode.WaveformLoadingMode_StreamFromDisk; // TODO works for my bank
                    List<object> audioFileContent = [
                        XmlBuilder.Property("assetPath", fileName),
                        XmlBuilder.Property("isStreaming", isStreaming),
                        XmlBuilder.Property("frequencyInKHz", samples.Metadata.Frequency / 1000.0),
                        XmlBuilder.Property("channelCount", samples.Metadata.Channels),
                        XmlBuilder.Property("length", samples.Metadata.SampleCount / (double)samples.Metadata.Frequency),
                        XmlBuilder.Relationship("masterAssetFolder", _masterAssetFolderGuid),
                    ];

                    XDocument document = XmlBuilder.CreateDocument(XmlBuilder.Object("AudioFile", soundGuid, audioFileContent.ToArray()));
                    document.Save(Path.Combine(_audioFileMetadataDirectory, $"{soundGuid.AsFmodStringFormat()}.xml"));
                }
            }
        }
    }

    private void ExtractParameters() {
        foreach (ParameterNode parameterNode in _collectedBank.ParameterNodes.Values) {
            Guid parameterPresetGuid = Guid.DeterministicGuid(SpecialGuids.ParameterNamespace, parameterNode.Name);
            
            XDocument document = XmlBuilder.CreateDocument(
                XmlBuilder.Object("ParameterPreset", parameterPresetGuid,
                    XmlBuilder.Property("name", parameterNode.Name),
                    XmlBuilder.Relationship("folder", _masterParameterPresetFolderGuid),
                    XmlBuilder.Relationship("parameter", parameterNode.BaseGuid.ToGuid())
                ),
                XmlBuilder.Object("GameParameter", parameterNode.BaseGuid.ToGuid(),
                    XmlBuilder.Property("parameterType", (uint)parameterNode.Type),
                    XmlBuilder.Property("maximum", parameterNode.Maximum),
                    XmlBuilder.Property("minimum", parameterNode.Minimum),
                    XmlBuilder.Property("initialValue", parameterNode.DefaultValue)
                )
            );
            document.Save(Path.Combine(_parameterPresetMetadataDirectory, $"{parameterPresetGuid.AsFmodStringFormat()}.xml"));
        }
    }
    
    private bool TryGetStringTablePath(Guid guid, out string path) {
        return _stringBank.StringTable!.RadixTree!.TryGetString(new FModGuid(guid), out path);
    }
}