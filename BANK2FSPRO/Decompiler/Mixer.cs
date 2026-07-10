using System.Diagnostics;
using System.Xml.Linq;
using FModBankParser.Nodes;
using FModBankParser.Nodes.Buses;
using FModBankParser.Nodes.Effects;
using FModBankParser.Objects;

namespace BANK2FSPRO;

public partial class Decompiler {
    private void ExtractMixer() {
        CacheBankMasterBusGuid();
        Dictionary<Guid, List<Guid>> busToVcas = BuildBusToVcaMap();

        ExtractMixerReturns();
        ExtractMixerGroups(busToVcas);
        ExtractVCAs();
    }

    private void CacheBankMasterBusGuid() {
        foreach (Guid guid in _collectedBank.BusNodes.Keys) {
            if (!TryGetStringTablePath(guid, out string path)) { continue; }
            if (path != "bus:/") { continue; }
            _bankMasterBusGuid = guid;
        }
    }

    private Dictionary<Guid, List<Guid>> BuildBusToVcaMap() {
        Dictionary<Guid, List<Guid>> map = new Dictionary<Guid, List<Guid>>();
        foreach ((Guid vcaGuid, VCANode vca) in _collectedBank.VCANodes) {
            foreach (FModGuid strip in vca.Strips) {
                Guid busGuid = strip.ToGuid();
                if (!map.TryGetValue(busGuid, out List<Guid>? vcas)) {
                    vcas = [];
                    map[busGuid] = vcas;
                }
                vcas.Add(vcaGuid);
            }
        }
        return map;
    }

    private void ExtractMixerReturns() {
        foreach ((Guid busGuid, BaseBusNode node) in _collectedBank.BusNodes) {
            if (node is not ReturnBusNode) { continue; }
            if (!TryGetMixerBusName(busGuid, out string name)) { continue; }

            WriteMixerBusDocument(
                className: "MixerReturn",
                busGuid: busGuid,
                node: node,
                name: name,
                masters: null,
                outputDirectory: _returnMetadataDirectory
            );
        }
    }

    private void ExtractMixerGroups(Dictionary<Guid, List<Guid>> busToVcas) {
        foreach ((Guid busGuid, BaseBusNode node) in _collectedBank.BusNodes) {
            if (node is not GroupBusNode) { continue; }
            if (!TryGetMixerBusName(busGuid, out string name)) { continue; }

            busToVcas.TryGetValue(busGuid, out List<Guid>? masters);
            WriteMixerBusDocument(
                className: "MixerGroup",
                busGuid: busGuid,
                node: node,
                name: name,
                masters: masters,
                outputDirectory: _groupMetadataDirectory
            );
        }
    }

    private void ExtractVCAs() {
        foreach ((Guid vcaGuid, VCANode vca) in _collectedBank.VCANodes) {
            if (!TryGetStringTablePath(vcaGuid, out string path) || !path.StartsWith("vca:/", StringComparison.Ordinal)) {
                throw new NotImplementedException($"VCA {vcaGuid} missing vca:/ string table path");
            }

            string name = path["vca:/".Length..];
            XDocument document = XmlBuilder.CreateDocument(
                XmlBuilder.Object("MixerVCA", vcaGuid,
                    XmlBuilder.Property("name", name),
                    XmlBuilder.Relationship("mixer", _masterMixerGuid)
                )
            );
            document.Save(Path.Combine(_vcaMetadataDirectory, $"{vcaGuid.AsFmodStringFormat()}.xml"));
        }
    }

    private void WriteMixerBusDocument(string className, Guid busGuid, BaseBusNode node, string name, List<Guid>? masters, string outputDirectory) {
        BusNode body = node.BusBody ?? throw new NotImplementedException($"Mixer bus {name} missing BusBody");

        Guid effectChainGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{busGuid}/effectChain");
        Guid pannerGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{busGuid}/panner");
        Guid faderGuid = Guid.DeterministicGuid(SpecialGuids.GeneralNamespace, $"{busGuid}/fader");

        List<Guid> effectIds = [];
        List<object> documentObjects = [];

        foreach (FModGuid effectRef in body.PreFaderEffects) {
            AppendMixerEffect(effectRef.ToGuid(), effectIds, documentObjects);
        }
        effectIds.Add(faderGuid);
        documentObjects.Add(XmlBuilder.Object("MixerBusFader", faderGuid));
        foreach (FModGuid effectRef in body.PostFaderEffects) {
            AppendMixerEffect(effectRef.ToGuid(), effectIds, documentObjects);
        }

        List<object> busContent = [];
        if (body.InputChannelLayout != 0) {
            busContent.Add(XmlBuilder.Property("overridingInputFormat", body.InputChannelLayout));
            busContent.Add(XmlBuilder.Property("inputFormatOverridden", true));
        }
        if (body.MixerStrip.Volume != 0) {
            busContent.Add(XmlBuilder.Property("volume", body.MixerStrip.Volume));
        }
        busContent.Add(XmlBuilder.Property("name", name));
        if (body.MixerStrip.Pitch != 0) {
            busContent.Add(XmlBuilder.Property("pitch", body.MixerStrip.Pitch));
        }
        if (masters is { Count: > 0 }) {
            busContent.Add(XmlBuilder.Relationship("masters", masters));
        }
        busContent.Add(XmlBuilder.Relationship("effectChain", effectChainGuid));
        busContent.Add(XmlBuilder.Relationship("panner", pannerGuid));
        busContent.Add(XmlBuilder.Relationship("output", ResolveMixerOutputGuid(node.Routable.BaseGuid.ToGuid())));

        documentObjects.Insert(0, XmlBuilder.Object(className, busGuid, busContent.ToArray()));
        documentObjects.Insert(1, XmlBuilder.Object("MixerBusEffectChain", effectChainGuid,
            XmlBuilder.Relationship("effects", effectIds)
        ));
        documentObjects.Insert(2, XmlBuilder.Object("MixerBusPanner", pannerGuid));

        XDocument document = XmlBuilder.CreateDocument(documentObjects.ToArray());
        document.Save(Path.Combine(outputDirectory, $"{busGuid.AsFmodStringFormat()}.xml"));
    }

    private void AppendMixerEffect(Guid effectGuid, List<Guid> effectIds, List<object> documentObjects) {
        if (!_collectedBank.EffectNodes.TryGetValue(effectGuid, out BaseEffectNode? effect)) {
            throw new NotImplementedException($"Missing effect {effectGuid}");
        }

        switch (effect) {
            case SendEffectNode send:
                effectIds.Add(effectGuid);
                List<object> sendContent = [
                    XmlBuilder.Property("level", send.SendLevel),
                ];
                if (send.InputChannelLayout != 0) {
                    sendContent.Add(XmlBuilder.Property("inputFormat", send.InputChannelLayout));
                }
                sendContent.Add(XmlBuilder.Relationship("mixerReturn", send.ReturnGuid.ToGuid()));
                documentObjects.Add(XmlBuilder.Object("MixerSend", effectGuid, sendContent.ToArray()));
                break;

            case BuiltInEffectNode builtIn: {
                EDSPTypeLegacy dspType = (EDSPTypeLegacy)builtIn.DSPType;
                if (dspType is EDSPTypeLegacy.FMOD_DSP_TYPE_FADER) { break; }

                string className = GetBuiltInEffectClassName(dspType);
                effectIds.Add(effectGuid);
                List<object> effectContent = [];
                if (builtIn.EffectBody is not null) {
                    if (builtIn.EffectBody.WetLevel != 0) {
                        effectContent.Add(XmlBuilder.Property("wetLevel", builtIn.EffectBody.WetLevel));
                    }
                    if (builtIn.EffectBody.DryLevel != 0) {
                        effectContent.Add(XmlBuilder.Property("dryLevel", builtIn.EffectBody.DryLevel));
                    }
                    if (builtIn.EffectBody.WetMix != 0) {
                        effectContent.Add(XmlBuilder.Property("wetMix", builtIn.EffectBody.WetMix));
                    }
                    if (builtIn.EffectBody.InputGain != 0) {
                        effectContent.Add(XmlBuilder.Property("inputGain", builtIn.EffectBody.InputGain));
                    }
                }
                documentObjects.Add(XmlBuilder.Object(className, effectGuid, effectContent.ToArray()));
                break;
            }

            default:
                throw new NotImplementedException($"Unsupported mixer effect type {effect.GetType().Name}");
        }
    }

    private static string GetBuiltInEffectClassName(EDSPTypeLegacy dspType) => dspType switch {
        EDSPTypeLegacy.FMOD_DSP_TYPE_COMPRESSOR => "CompressorEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_THREE_EQ => "ThreeEQEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_MULTIBAND_EQ => "MultibandEqEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_CHANNELMIX => "ChannelMixEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_SFXREVERB => "SFXReverbEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_CHORUS => "ChorusEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_FLANGE => "FlangerEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_DISTORTION => "DistortionEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_TREMOLO => "TremoloEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_PITCHSHIFT => "PitchShifterEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_DELAY => "DelayEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_LOWPASS_SIMPLE => "LowpassSimpleEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_HIGHPASS_SIMPLE => "HighpassSimpleEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_LIMITER => "LimiterEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_CONVOLUTIONREVERB => "ConvolutionReverbEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_TRANSCEIVER => "TransceiverEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_OBJECTPAN => "ObjectSpatialiserEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_PAN => "SpatialiserEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_LOUDNESS_METER => "LoudnessMeterEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_ECHO => "EchoEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_NORMALIZE => "NormalizeEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_PARAMEQ => "ParamEqEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_LOWPASS => "LowpassEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_HIGHPASS => "HighpassEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_ITLOWPASS => "ITLowpassEffect",
        EDSPTypeLegacy.FMOD_DSP_TYPE_ITECHO => "ITEchoEffect",
        _ => throw new NotImplementedException($"No Studio XML class mapping for {dspType}"),
    };

    private Guid ResolveMixerOutputGuid(Guid outputGuid) {
        if (outputGuid == Guid.Empty) { return _masterMixerMasterGuid; }
        if (_bankMasterBusGuid != Guid.Empty && outputGuid == _bankMasterBusGuid) { return _masterMixerMasterGuid; }
        return outputGuid;
    }

    private bool TryGetMixerBusName(Guid busGuid, out string name) {
        name = string.Empty;
        if (!TryGetStringTablePath(busGuid, out string path)) { return false; }
        if (!path.StartsWith("bus:/", StringComparison.Ordinal)) { return false; }
        name = path["bus:/".Length..];
        return name.Length > 0;
    }
}
