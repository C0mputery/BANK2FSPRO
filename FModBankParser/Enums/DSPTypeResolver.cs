namespace FModBankParser.Enums;

public static class DSPTypeResolver
{
    /// <summary>
    /// FMOD Studio 1.10 bank file version (decimal 101).
    /// </summary>
    public const int FmodStudio110BankVersion = 0x65;

    /// <summary>
    /// Values that only exist in the legacy enum layout.
    /// </summary>
    private static bool IsLegacyOnlyValue(uint dspType) =>
        dspType is 0xF or 0x10 or 0x17 or 0x1F;

    /// <summary>
    /// Values that only exist in the modern enum layout (FMOD Engine 2.03+).
    /// </summary>
    private static bool IsModernOnlyValue(uint dspType) =>
        dspType is 0x21 or 0x22;

    /// <summary>
    /// Heuristic: banks below file version 0x92 were built before the FMOD 2.03 enum renumbering.
    /// </summary>
    public static bool UsesLegacyDspTypeLayout(int fileVersion) =>
        fileVersion < (int)EFModVersion.FILEVERSION_EFFECT_WET_DRY;

    public static bool UsesLegacyDspTypeLayout(uint dspType, int fileVersion) =>
        IsLegacyOnlyValue(dspType) ||
        (UsesLegacyDspTypeLayout(fileVersion) && !IsModernOnlyValue(dspType));

    public static EDSPTypeLegacy ToLegacy(uint dspType) => (EDSPTypeLegacy)dspType;

    public static EDSPType ToModern(uint dspType) => (EDSPType)dspType;

    public static string GetName(uint dspType, int fileVersion)
    {
        if (UsesLegacyDspTypeLayout(dspType, fileVersion))
        {
            if (Enum.IsDefined(typeof(EDSPTypeLegacy), dspType))
                return ToLegacy(dspType).ToString();

            return $"FMOD_DSP_TYPE_UNKNOWN_0x{dspType:X}";
        }

        if (Enum.IsDefined(typeof(EDSPType), dspType))
            return ToModern(dspType).ToString();

        return $"FMOD_DSP_TYPE_UNKNOWN_0x{dspType:X}";
    }

    /// <summary>
    /// Typical parameter counts for built-in effects. Useful when the raw DSP type is ambiguous.
    /// </summary>
    public static string? GuessFromParameterCount(int parameterCount) => parameterCount switch
    {
        5 => "FMOD_DSP_TYPE_ITECHO (legacy) or similar",
        7 => "FMOD_DSP_TYPE_COMPRESSOR (legacy)",
        13 => "FMOD_DSP_TYPE_SFXREVERB (legacy)",
        20 => "FMOD_DSP_TYPE_MULTIBAND_EQ (legacy)",
        _ => null
    };
}
