using FModBankParser;

namespace BANK2FSPRO;

public partial class Decompiler {
    private string _assetsDirectory = string.Empty;
    private string _metadataDirectory = string.Empty;
    private string _audioFileMetadataDirectory = string.Empty;
    private string _bankMetadataDirectory = string.Empty;
    private string _assetMetadataDirectory = string.Empty;
    private string _bankFolderMetadataDirectory = string.Empty;
    private string _eventFolderMetadataDirectory = string.Empty;
    private string _platformMetadataDirectory = string.Empty;
    private string _encodingSettingMetadataDirectory = string.Empty;
    private string _effectPresetFolderMetadataDirectory = string.Empty;
    private string _parameterPresetFolderMetadataDirectory = string.Empty;
    private string _profilerFolderMetadataDirectory = string.Empty;
    private string _sandboxFolderMetadataDirectory = string.Empty;
    private string _snapshotGroupMetadataDirectory = string.Empty;

    private void SetupDirectories() {
        _assetsDirectory = Path.Combine(_outputDirectory, "Assets");
        _metadataDirectory = Path.Combine(_outputDirectory, "Metadata");
        _audioFileMetadataDirectory = Path.Combine(_metadataDirectory, "AudioFile");
        _bankMetadataDirectory = Path.Combine(_metadataDirectory, "Bank");
        _assetMetadataDirectory = Path.Combine(_metadataDirectory, "Asset");
        _bankFolderMetadataDirectory = Path.Combine(_metadataDirectory, "BankFolder");
        _eventFolderMetadataDirectory = Path.Combine(_metadataDirectory, "EventFolder");
        _platformMetadataDirectory = Path.Combine(_metadataDirectory, "Platform");
        _encodingSettingMetadataDirectory = Path.Combine(_metadataDirectory, "EncodingSetting");
        _effectPresetFolderMetadataDirectory = Path.Combine(_metadataDirectory, "EffectPresetFolder");
        _parameterPresetFolderMetadataDirectory = Path.Combine(_metadataDirectory, "ParameterPresetFolder");
        _profilerFolderMetadataDirectory = Path.Combine(_metadataDirectory, "ProfilerFolder");
        _sandboxFolderMetadataDirectory = Path.Combine(_metadataDirectory, "SandboxFolder");
        _snapshotGroupMetadataDirectory = Path.Combine(_metadataDirectory, "SnapshotGroup");

        if (Directory.Exists(_outputDirectory)) { Directory.Delete(_outputDirectory, true); }

        Directory.CreateDirectory(_outputDirectory);
        Directory.CreateDirectory(_assetsDirectory);
        Directory.CreateDirectory(_metadataDirectory);
        Directory.CreateDirectory(_audioFileMetadataDirectory);
        Directory.CreateDirectory(_bankMetadataDirectory);
        Directory.CreateDirectory(_assetMetadataDirectory);
        Directory.CreateDirectory(_bankFolderMetadataDirectory);
        Directory.CreateDirectory(_eventFolderMetadataDirectory);
        Directory.CreateDirectory(_platformMetadataDirectory);
        Directory.CreateDirectory(_encodingSettingMetadataDirectory);
        Directory.CreateDirectory(_effectPresetFolderMetadataDirectory);
        Directory.CreateDirectory(_parameterPresetFolderMetadataDirectory);
        Directory.CreateDirectory(_profilerFolderMetadataDirectory);
        Directory.CreateDirectory(_sandboxFolderMetadataDirectory);
        Directory.CreateDirectory(_snapshotGroupMetadataDirectory);

        foreach (FModReader bank in _banks) {
            if (bank.SoundBankData.Count == 0) { continue; }
            Directory.CreateDirectory(Path.Combine(_assetsDirectory, bank.BankName));
        }
    }
}