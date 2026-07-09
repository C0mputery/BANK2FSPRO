using System.Xml.Linq;
using FModBankParser;
using FModBankParser.Nodes.Buses;

namespace BANK2FSPRO;

public partial class Decompiler {
    private void SetupProjectFiles() {
        if (Directory.Exists(_outputDirectory)) { Directory.Delete(_outputDirectory, true); }
        Directory.CreateDirectory(_outputDirectory);
        Directory.CreateDirectory(_assetsDirectory);
        Directory.CreateDirectory(_metadataDirectory);

        CreateFsproFile();
        CreateBuiltinMetadata();
        CreateBankMetadata();
    }

    private void CreateFsproFile() {
        XDocument document = XmlBuilder.CreateDocument();
        document.Save(Path.Combine(_outputDirectory, $"{_projectName}.fspro"));
    }

    private void CreateBuiltinMetadata() {
        XDocument document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterAssetFolder", _masterAssetFolderGuid)
        );
        document.Save(Path.Combine(_assetMetadataDirectory, $"{_masterAssetFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterBankFolder", _masterBankFolderGuid)
        );
        document.Save(Path.Combine(_bankFolderMetadataDirectory, $"{_masterBankFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterEventFolder", _masterEventFolderGuid, 
                XmlBuilder.Property("name", "Master")
                )
        );
        document.Save(Path.Combine(_eventFolderMetadataDirectory, $"{_masterEventFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("Platform", _masterPlatformGuid,
                XmlBuilder.Property("hardwareType", "0"),
                XmlBuilder.Property("name", "Desktop"),
                XmlBuilder.Property("subDirectory", "Desktop"),
                XmlBuilder.Property("speakerFormat", "5")
            )
        );
        document.Save(Path.Combine(_platformMetadataDirectory, $"{_masterPlatformGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("EncodingSetting", _masterEncodingSettingGuid,
                XmlBuilder.Property("encodingFormat", "3"),
                XmlBuilder.Property("quality", "37"),
                XmlBuilder.Relationship("platform", _masterPlatformGuid),
                XmlBuilder.Relationship("encodable", _masterPlatformGuid)
            )
        );
        document.Save(Path.Combine(_encodingSettingMetadataDirectory, $"{_masterEncodingSettingGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterEffectPresetFolder", _masterEffectPresetFolderGuid)
        );
        document.Save(Path.Combine(_effectPresetFolderMetadataDirectory, $"{_masterEffectPresetFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterParameterPresetFolder", _masterParameterPresetFolderGuid)
        );
        document.Save(Path.Combine(_parameterPresetFolderMetadataDirectory, $"{_masterParameterPresetFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("ProfilerSessionFolder", _masterProfilerFolderGuid)
        );
        document.Save(Path.Combine(_profilerFolderMetadataDirectory, $"{_masterProfilerFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterSandboxFolder", _masterSandboxFolderGuid)
        );
        document.Save(Path.Combine(_sandboxFolderMetadataDirectory, $"{_masterSandboxFolderGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("SnapshotList", _masterSnapshotListGuid,
                XmlBuilder.Relationship("mixer", _masterMixerGuid)
            )
        );
        document.Save(Path.Combine(_snapshotGroupMetadataDirectory, $"{_masterSnapshotListGuid.AsFmodStringFormat()}.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MixerMaster", _masterMixerMasterGuid,
                XmlBuilder.Property("name", "Master Bus"),
                XmlBuilder.Relationship("effectChain", _masterEffectChainGuid),
                XmlBuilder.Relationship("panner", _masterPannerGuid),
                XmlBuilder.Relationship("mixer", _masterMixerGuid)
            ),
            XmlBuilder.Object("MixerBusEffectChain", _masterEffectChainGuid,
                XmlBuilder.Relationship("effects", _masterFaderGuid)
            ),
            XmlBuilder.Object("MixerBusPanner", _masterPannerGuid,
                XmlBuilder.Property("overridingOutputFormat", "2")
            ),
            XmlBuilder.Object("MixerBusFader", _masterFaderGuid)
        );
        document.Save(Path.Combine(_metadataDirectory, "Master.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("Mixer", _masterMixerGuid,
                XmlBuilder.Relationship("masterBus", _masterMixerMasterGuid),
                XmlBuilder.Relationship("snapshotList", _masterSnapshotListGuid)
            )
        );
        document.Save(Path.Combine(_metadataDirectory, "Mixer.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterTagFolder", _masterTagFolderGuid,
                XmlBuilder.Property("name", "Master")
            )
        );
        document.Save(Path.Combine(_metadataDirectory, "Tags.xml"));

        document = XmlBuilder.CreateDocument(
            XmlBuilder.Object("Workspace", _masterWorkspaceGuid,
                XmlBuilder.Relationship("masterEventFolder", _masterEventFolderGuid),
                XmlBuilder.Relationship("masterTagFolder", _masterTagFolderGuid),
                XmlBuilder.Relationship("masterEffectPresetFolder", _masterEffectPresetFolderGuid),
                XmlBuilder.Relationship("masterParameterPresetFolder", _masterParameterPresetFolderGuid),
                XmlBuilder.Relationship("masterBankFolder", _masterBankFolderGuid),
                XmlBuilder.Relationship("masterSandboxFolder", _masterSandboxFolderGuid),
                XmlBuilder.Relationship("masterAssetFolder", _masterAssetFolderGuid),
                XmlBuilder.Relationship("mixer", _masterMixerGuid),
                XmlBuilder.Relationship("profilerSessionFolder", _masterProfilerFolderGuid),
                XmlBuilder.Relationship("platforms", _masterPlatformGuid)
            )
        );
        document.Save(Path.Combine(_metadataDirectory, "Workspace.xml"));
    }

    private void CreateBankMetadata() {
        foreach (FModReader bank in _banks) {
            if (ReferenceEquals(bank, _stringBank)) { continue; }
            if (bank.BankName.Contains(".strings", StringComparison.OrdinalIgnoreCase)) { continue; }

            string bankName = bank.BankName.EndsWith(".bank", StringComparison.OrdinalIgnoreCase)
                ? bank.BankName[..^5]
                : bank.BankName;
            string bankAssetPath = $"{bankName}/";
            bool isMasterBank = ReferenceEquals(bank, _masterBank)
                || bank.BusNodes.Values.Any(b => b is MasterBusNode);

            Guid bankGuid = bank.BankInfo.BaseGuid.ToGuid();
            if (isMasterBank) {
                XDocument document = XmlBuilder.CreateDocument(
                    XmlBuilder.Object("Bank", bankGuid,
                        XmlBuilder.Property("name", bankAssetPath),
                        XmlBuilder.Property("isMasterBank", "true"),
                        XmlBuilder.Relationship("folder", _masterBankFolderGuid)
                    )
                );
                document.Save(Path.Combine(_bankMetadataDirectory, $"{bankGuid.AsFmodStringFormat()}.xml"));
            }
            else {
                XDocument document = XmlBuilder.CreateDocument(
                    XmlBuilder.Object("Bank", bankGuid,
                        XmlBuilder.Property("name", bankAssetPath),
                        XmlBuilder.Relationship("folder", _masterBankFolderGuid)
                    )
                );
                document.Save(Path.Combine(_bankMetadataDirectory, $"{bankGuid.AsFmodStringFormat()}.xml"));
            }

            Guid assetGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/Asset/{bankAssetPath}");
            XDocument assetDocument = XmlBuilder.CreateDocument(
                XmlBuilder.Object("EncodableAsset", assetGuid,
                    XmlBuilder.Property("assetPath", bankAssetPath),
                    XmlBuilder.Relationship("masterAssetFolder", _masterAssetFolderGuid)
                )
            );
            assetDocument.Save(Path.Combine(_assetMetadataDirectory, $"{assetGuid.AsFmodStringFormat()}.xml"));
        }
    }
}
