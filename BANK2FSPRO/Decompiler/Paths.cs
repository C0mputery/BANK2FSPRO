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
    private string _parameterPresetMetadataDirectory = string.Empty;
    private string _snapshotGroupMetadataDirectory = string.Empty;

    private void SetupDirectories() {
        _assetsDirectory = Path.Combine(outputDirectory, "Assets");
        _metadataDirectory = Path.Combine(outputDirectory, "Metadata");
        _audioFileMetadataDirectory = Path.Combine(_metadataDirectory, "AudioFile");
        _bankMetadataDirectory = Path.Combine(_metadataDirectory, "Bank");
        _assetMetadataDirectory = Path.Combine(_metadataDirectory, "Asset");
        _bankFolderMetadataDirectory = Path.Combine(_metadataDirectory, "BankFolder");
        _eventFolderMetadataDirectory = Path.Combine(_metadataDirectory, "EventFolder");
        _platformMetadataDirectory = Path.Combine(_metadataDirectory, "Platform");
        _encodingSettingMetadataDirectory = Path.Combine(_metadataDirectory, "EncodingSetting");
        _effectPresetFolderMetadataDirectory = Path.Combine(_metadataDirectory, "EffectPresetFolder");
        _parameterPresetFolderMetadataDirectory = Path.Combine(_metadataDirectory, "ParameterPresetFolder");
        _parameterPresetMetadataDirectory = Path.Combine(_metadataDirectory, "ParameterPreset");
        _snapshotGroupMetadataDirectory = Path.Combine(_metadataDirectory, "SnapshotGroup");

        if (Directory.Exists(outputDirectory)) { Directory.Delete(outputDirectory, true); }

        Directory.CreateDirectory(outputDirectory);
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
        Directory.CreateDirectory(_parameterPresetMetadataDirectory);
        Directory.CreateDirectory(_snapshotGroupMetadataDirectory);
    }
}
