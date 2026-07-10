using FModBankParser.Nodes.Effects;
using FModBankParser.Objects;

namespace BANK2FSPRO;

/// <summary>
/// Maps bank DSP parameter indices to FMOD Studio XML property names.
/// Studio 1.10 banks store effect knobs in <see cref="ParameterizedEffectNode"/>;
/// without these, Studio falls back to defaults (e.g. reverb dryLevel=0 instead of -80).
/// </summary>
internal static class EffectParameterMaps {
    // Property names match FMOD Studio 1.10 metadata (see Documents/FMOD Studio/examples).
    private static readonly Dictionary<EDSPTypeLegacy, string?[]> PropertyNames = new() {
        [EDSPTypeLegacy.FMOD_DSP_TYPE_SFXREVERB] = [
            "decayTime",
            "earlyDelay",
            "lateDelay",
            "HFReference",
            "HFDecayRatio",
            "diffusion",
            "density",
            "lowShelfFrequency",
            "lowShelfGain",
            "highCut",
            "earlyLateMix",
            "wetLevel",
            "dryLevel",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_COMPRESSOR] = [
            "threshold",
            "ratio",
            "attackTime",
            "releaseTime",
            "gainMakeup",
            null, // USESIDECHAIN data buffer — use sidechainEnabled instead
            "linked",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_THREE_EQ] = [
            "lowGain",
            "midGain",
            "highGain",
            "lowCrossover",
            "highCrossover",
            "crossoverSlope",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_DISTORTION] = [
            "level",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_CHORUS] = [
            "mix",
            "rate",
            "depth",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_FLANGE] = [
            "mix",
            "depth",
            "rate",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_TREMOLO] = [
            "frequency",
            "depth",
            "shape",
            "skew",
            "duty",
            "square",
            "phase",
            "spread",
        ],
        [EDSPTypeLegacy.FMOD_DSP_TYPE_PITCHSHIFT] = [
            "pitch",
            "fftSize",
            "overlap",
            "maxChannels",
        ],
    };

    public static void AppendBuiltInEffectProperties(BuiltInEffectNode builtIn, EDSPTypeLegacy dspType, List<object> effectContent) {
        Dictionary<string, object> properties = new Dictionary<string, object>(StringComparer.Ordinal);

        if (builtIn.ParamEffectBody is { } paramBody) {
            AppendParameterizedProperties(dspType, paramBody, properties);
            if (dspType is EDSPTypeLegacy.FMOD_DSP_TYPE_COMPRESSOR) {
                properties["sidechainEnabled"] = paramBody.SideChainEnabled;
            }
        }

        if (builtIn.EffectBody is { } body) {
            // Newer banks (file version >= 0x92) store an effect-level wet/dry mix here.
            // Only apply when ParamEffectBody did not already supply those knobs, so we
            // don't clobber SFX reverb DSP wet/dry (the values Studio actually uses).
            if (!properties.ContainsKey("wetLevel")) {
                properties["wetLevel"] = body.WetLevel;
            }
            if (!properties.ContainsKey("dryLevel")) {
                properties["dryLevel"] = body.DryLevel;
            }
            properties["wetMix"] = body.WetMix;
            properties["inputGain"] = body.InputGain;
        }

        foreach ((string name, object value) in properties) {
            effectContent.Add(XmlBuilder.Property(name, value));
        }
    }

    private static void AppendParameterizedProperties(EDSPTypeLegacy dspType, ParameterizedEffectNode paramBody, Dictionary<string, object> properties) {
        if (!PropertyNames.TryGetValue(dspType, out string?[]? names)) {
            return;
        }

        int count = Math.Min(names.Length, paramBody.Parameters.Length);
        for (int i = 0; i < count; i++) {
            string? name = names[i];
            if (name is null) {
                continue;
            }

            FEffectParameter parameter = paramBody.Parameters[i];
            if (!TryGetPropertyValue(parameter, out object value)) {
                continue;
            }

            properties[name] = value;
        }
    }

    private static bool TryGetPropertyValue(FEffectParameter parameter, out object value) {
        switch (parameter.Type) {
            case 0: // float
                value = parameter.FloatValue;
                return true;
            case 1: // int stored as float bits
                value = BitConverter.SingleToInt32Bits(parameter.FloatValue);
                return true;
            case 2: // bool
                value = parameter.FloatValue != 0f;
                return true;
            default: // buffer / unknown — not represented as a simple Studio property
                value = null!;
                return false;
        }
    }
}
