using FModBankParser;
using FModBankParser.Nodes.Buses;

namespace BANK2FSPRO;

public partial class Decompiler {
    private void SetupProjectFiles() {
        if (Directory.Exists(_outputDirectory)) { Directory.Delete(_outputDirectory, true); }
        Directory.CreateDirectory(_outputDirectory);
        Directory.CreateDirectory(Path.Combine(_outputDirectory, "Assets"));
        Directory.CreateDirectory(Path.Combine(_outputDirectory, "Metadata"));

        CreateFsproFile();
        CreateBuiltinMetadata();
        CreateBankMetadata();
    }

    private void CreateFsproFile() {
        XmlBuilder.Save(
            XmlBuilder.CreateDocument(),
            Path.Combine(_outputDirectory, $"{_projectName}.fspro")
        );
    }

    private void CreateBuiltinMetadata() {
        string metadata = Path.Combine(_outputDirectory, "Metadata");

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterAssetFolder", _masterAssetFolderGuid)
            ),
            Path.Combine(metadata, "Asset", $"{_masterAssetFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterBankFolder", _masterBankFolderGuid)
            ),
            Path.Combine(metadata, "BankFolder", $"{_masterBankFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterEventFolder", _masterEventFolderGuid,
                    XmlBuilder.Property("name", "Master")
                )
            ),
            Path.Combine(metadata, "EventFolder", $"{_masterEventFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("Platform", _masterPlatformGuid,
                    XmlBuilder.Property("hardwareType", "0"),
                    XmlBuilder.Property("name", "Desktop"),
                    XmlBuilder.Property("subDirectory", "Desktop"),
                    XmlBuilder.Property("speakerFormat", "5")
                )
            ),
            Path.Combine(metadata, "Platform", $"{_masterPlatformGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("EncodingSetting", _masterEncodingSettingGuid,
                    XmlBuilder.Property("encodingFormat", "3"),
                    XmlBuilder.Property("quality", "37"),
                    XmlBuilder.Relationship("platform", _masterPlatformGuid),
                    XmlBuilder.Relationship("encodable", _masterPlatformGuid)
                )
            ),
            Path.Combine(metadata, "EncodingSetting", $"{_masterEncodingSettingGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterEffectPresetFolder", _masterEffectPresetFolderGuid)
            ),
            Path.Combine(metadata, "EffectPresetFolder", $"{_masterEffectPresetFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterParameterPresetFolder", _masterParameterPresetFolderGuid)
            ),
            Path.Combine(metadata, "ParameterPresetFolder", $"{_masterParameterPresetFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("ProfilerSessionFolder", _masterProfilerFolderGuid)
            ),
            Path.Combine(metadata, "ProfilerFolder", $"{_masterProfilerFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterSandboxFolder", _masterSandboxFolderGuid)
            ),
            Path.Combine(metadata, "SandboxFolder", $"{_masterSandboxFolderGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("SnapshotList", _masterSnapshotListGuid,
                    XmlBuilder.Relationship("mixer", _masterMixerGuid)
                )
            ),
            Path.Combine(metadata, "SnapshotGroup", $"{_masterSnapshotListGuid.ToFmodFormat()}.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
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
            ),
            Path.Combine(metadata, "Master.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("Mixer", _masterMixerGuid,
                    XmlBuilder.Relationship("masterBus", _masterMixerMasterGuid),
                    XmlBuilder.Relationship("snapshotList", _masterSnapshotListGuid)
                )
            ),
            Path.Combine(metadata, "Mixer.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
                XmlBuilder.Object("MasterTagFolder", _masterTagFolderGuid,
                    XmlBuilder.Property("name", "Master")
                )
            ),
            Path.Combine(metadata, "Tags.xml")
        );

        XmlBuilder.Save(
            XmlBuilder.CreateDocument(
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
            ),
            Path.Combine(metadata, "Workspace.xml")
        );
    }

    private void CreateBankMetadata() {
        string metadata = Path.Combine(_outputDirectory, "Metadata");

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
                XmlBuilder.Save(
                    XmlBuilder.CreateDocument(
                        XmlBuilder.Object("Bank", bankGuid,
                            XmlBuilder.Property("name", bankAssetPath),
                            XmlBuilder.Property("isMasterBank", "true"),
                            XmlBuilder.Relationship("folder", _masterBankFolderGuid)
                        )
                    ),
                    Path.Combine(metadata, "Bank", $"{bankGuid.ToFmodFormat()}.xml")
                );
            }
            else {
                XmlBuilder.Save(
                    XmlBuilder.CreateDocument(
                        XmlBuilder.Object("Bank", bankGuid,
                            XmlBuilder.Property("name", bankAssetPath),
                            XmlBuilder.Relationship("folder", _masterBankFolderGuid)
                        )
                    ),
                    Path.Combine(metadata, "Bank", $"{bankGuid.ToFmodFormat()}.xml")
                );
            }

            Guid assetGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/Asset/{bankAssetPath}");
            XmlBuilder.Save(
                XmlBuilder.CreateDocument(
                    XmlBuilder.Object("EncodableAsset", assetGuid,
                        XmlBuilder.Property("assetPath", bankAssetPath),
                        XmlBuilder.Relationship("masterAssetFolder", _masterAssetFolderGuid)
                    )
                ),
                Path.Combine(metadata, "Asset", $"{assetGuid.ToFmodFormat()}.xml")
            );
        }
    }
}
