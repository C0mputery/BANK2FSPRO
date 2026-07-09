namespace BANK2FSPRO;

public partial class Decompiler {
    private Guid _masterAssetFolderGuid = Guid.Empty;
    private Guid _masterBankFolderGuid = Guid.Empty;
    private Guid _masterEventFolderGuid = Guid.Empty;
    private Guid _masterPlatformGuid = Guid.Empty;
    private Guid _masterEncodingSettingGuid = Guid.Empty;
    private Guid _masterEffectPresetFolderGuid = Guid.Empty;
    private Guid _masterParameterPresetFolderGuid = Guid.Empty;
    private Guid _masterProfilerFolderGuid = Guid.Empty;
    private Guid _masterSandboxFolderGuid = Guid.Empty;
    private Guid _masterSnapshotListGuid = Guid.Empty;
    private Guid _masterMixerMasterGuid = Guid.Empty;
    private Guid _masterMixerGuid = Guid.Empty;
    private Guid _masterTagFolderGuid = Guid.Empty;
    private Guid _masterWorkspaceGuid = Guid.Empty;
    private Guid _masterEffectChainGuid = Guid.Empty;
    private Guid _masterPannerGuid = Guid.Empty;
    private Guid _masterFaderGuid = Guid.Empty;

    private void SetupGuids() {
        _masterAssetFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MasterAssetFolder");
        _masterBankFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, _projectName);
        _masterEventFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MasterEventFolder");
        _masterPlatformGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/Platform");
        _masterEncodingSettingGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/EncodingSetting");
        _masterEffectPresetFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MasterEffectPresetFolder");
        _masterParameterPresetFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MasterParameterPresetFolder");
        _masterProfilerFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/ProfilerSessionFolder");
        _masterSandboxFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MasterSandboxFolder");
        _masterSnapshotListGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/SnapshotList");
        _masterMixerMasterGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MixerMaster");
        _masterMixerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/Mixer");
        _masterTagFolderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MasterTagFolder");
        _masterWorkspaceGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/Workspace");
        _masterEffectChainGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MixerBusEffectChain");
        _masterPannerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MixerBusPanner");
        _masterFaderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{_projectName}/MixerBusFader");
    }
}