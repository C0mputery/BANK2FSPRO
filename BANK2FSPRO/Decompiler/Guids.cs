namespace BANK2FSPRO;

public partial class Decompiler {
    private Guid _masterAssetFolderGuid = Guid.Empty;
    private Guid _masterBankFolderGuid = Guid.Empty;
    private Guid _masterEventFolderGuid = Guid.Empty;
    private Guid _masterPlatformGuid = Guid.Empty;
    private Guid _masterEncodingSettingGuid = Guid.Empty;
    private Guid _masterEffectPresetFolderGuid = Guid.Empty;
    private Guid _masterParameterPresetFolderGuid = Guid.Empty;
    private Guid _masterSnapshotListGuid = Guid.Empty;
    private Guid _masterMixerMasterGuid = Guid.Empty;
    private Guid _masterMixerGuid = Guid.Empty;
    private Guid _masterTagFolderGuid = Guid.Empty;
    private Guid _masterWorkspaceGuid = Guid.Empty;
    private Guid _masterEffectChainGuid = Guid.Empty;
    private Guid _masterPannerGuid = Guid.Empty;
    private Guid _masterFaderGuid = Guid.Empty;
    private Guid _bankMasterBusGuid = Guid.Empty;

    private void SetupGuids() {
        _masterAssetFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MasterAssetFolder");
        _masterBankFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, projectName);
        _masterEventFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MasterEventFolder");
        _masterPlatformGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/Platform");
        _masterEncodingSettingGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/EncodingSetting");
        _masterEffectPresetFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MasterEffectPresetFolder");
        _masterParameterPresetFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MasterParameterPresetFolder");
        _masterSnapshotListGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/SnapshotList");
        _masterMixerMasterGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MixerMaster");
        _masterMixerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/Mixer");
        _masterTagFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MasterTagFolder");
        _masterWorkspaceGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/Workspace");
        _masterEffectChainGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MixerBusEffectChain");
        _masterPannerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MixerBusPanner");
        _masterFaderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{projectName}/MixerBusFader");
    }
}
