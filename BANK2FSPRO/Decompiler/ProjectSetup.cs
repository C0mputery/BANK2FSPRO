using System.Xml.Linq;
using FModBankParser;
using FModBankParser.Nodes.Buses;

namespace BANK2FSPRO;

public partial class Decompiler {
    private void SetupProjectFiles() {
        CreateFsproFile();
        CreateBuiltinMetadata();
        CreateBankMetadata();
    }

    private void CreateFsproFile() {
        XDocument document = XmlBuilder.CreateDocument();
        document.Save(Path.Combine(outputDirectory, $"{projectName}.fspro"));
    }

    private void CreateBuiltinMetadata() {
        XDocument masterAssetFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterAssetFolder", _masterAssetFolderGuid)
        );
        masterAssetFolderMetadata.Save(Path.Combine(_assetMetadataDirectory, $"{_masterAssetFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument masterBankFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterBankFolder", _masterBankFolderGuid)
        );
        masterBankFolderMetadata.Save(Path.Combine(_bankFolderMetadataDirectory, $"{_masterBankFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument masterEventFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterEventFolder", _masterEventFolderGuid,
                XmlBuilder.Property("name", "Master")
            )
        );
        masterEventFolderMetadata.Save(Path.Combine(_eventFolderMetadataDirectory, $"{_masterEventFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument platformMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("Platform", _masterPlatformGuid,
                XmlBuilder.Property("hardwareType", "0"),
                XmlBuilder.Property("name", "Desktop"),
                XmlBuilder.Property("subDirectory", "Desktop"),
                XmlBuilder.Property("speakerFormat", "5")
            )
        );
        platformMetadata.Save(Path.Combine(_platformMetadataDirectory, $"{_masterPlatformGuid.AsFmodStringFormat()}.xml"));

        XDocument encodingSettingMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("EncodingSetting", _masterEncodingSettingGuid,
                XmlBuilder.Property("encodingFormat", "3"),
                XmlBuilder.Property("quality", "37"),
                XmlBuilder.Relationship("platform", _masterPlatformGuid),
                XmlBuilder.Relationship("encodable", _masterPlatformGuid)
            )
        );
        encodingSettingMetadata.Save(Path.Combine(_encodingSettingMetadataDirectory, $"{_masterEncodingSettingGuid.AsFmodStringFormat()}.xml"));

        XDocument masterEffectPresetFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterEffectPresetFolder", _masterEffectPresetFolderGuid)
        );
        masterEffectPresetFolderMetadata.Save(Path.Combine(_effectPresetFolderMetadataDirectory, $"{_masterEffectPresetFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument masterParameterPresetFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterParameterPresetFolder", _masterParameterPresetFolderGuid)
        );
        masterParameterPresetFolderMetadata.Save(Path.Combine(_parameterPresetFolderMetadataDirectory, $"{_masterParameterPresetFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument profilerSessionFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("ProfilerSessionFolder", _masterProfilerFolderGuid)
        );
        profilerSessionFolderMetadata.Save(Path.Combine(_profilerFolderMetadataDirectory, $"{_masterProfilerFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument masterSandboxFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterSandboxFolder", _masterSandboxFolderGuid)
        );
        masterSandboxFolderMetadata.Save(Path.Combine(_sandboxFolderMetadataDirectory, $"{_masterSandboxFolderGuid.AsFmodStringFormat()}.xml"));

        XDocument snapshotListMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("SnapshotList", _masterSnapshotListGuid,
                XmlBuilder.Relationship("mixer", _masterMixerGuid)
            )
        );
        snapshotListMetadata.Save(Path.Combine(_snapshotGroupMetadataDirectory, $"{_masterSnapshotListGuid.AsFmodStringFormat()}.xml"));

        XDocument masterBusMetadata = XmlBuilder.CreateDocument(
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
        masterBusMetadata.Save(Path.Combine(_metadataDirectory, "Master.xml"));

        XDocument mixerMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("Mixer", _masterMixerGuid,
                XmlBuilder.Relationship("masterBus", _masterMixerMasterGuid),
                XmlBuilder.Relationship("snapshotList", _masterSnapshotListGuid)
            )
        );
        mixerMetadata.Save(Path.Combine(_metadataDirectory, "Mixer.xml"));

        XDocument masterTagFolderMetadata = XmlBuilder.CreateDocument(
            XmlBuilder.Object("MasterTagFolder", _masterTagFolderGuid,
                XmlBuilder.Property("name", "Master")
            )
        );
        masterTagFolderMetadata.Save(Path.Combine(_metadataDirectory, "Tags.xml"));

        XDocument workspaceMetadata = XmlBuilder.CreateDocument(
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
        workspaceMetadata.Save(Path.Combine(_metadataDirectory, "Workspace.xml"));
    }

    private void CreateBankMetadata() {
        foreach (FModReader bank in banks) {
            string bankName = Path.GetFileNameWithoutExtension(bank.BankName);
            string bankAssetPath = $"{bankName}/";
            
            bool isMasterBank = bank == masterBank;

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

            Guid assetGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/Asset/{bankAssetPath}");
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