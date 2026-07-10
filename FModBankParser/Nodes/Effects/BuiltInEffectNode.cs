using FModBankParser.Enums;
using FModBankParser.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FModBankParser.Nodes.Effects;

public class BuiltInEffectNode : BaseEffectNode
{
    public readonly FModGuid BaseGuid;
    public readonly uint InputChannelLayout;
    /// <summary>Raw value stored in the bank (layout depends on bank age).</summary>
    public readonly uint DSPTypeRaw;
    /// <summary>Interpreted with the modern FMOD_DSP_TYPE enum. Use <see cref="GetDspTypeName"/> for 1.x banks.</summary>
    public readonly EDSPType DSPType;
    public ParameterizedEffectNode? ParamEffectBody;

    public BuiltInEffectNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        if (FModReader.Version < 0x5B) InputChannelLayout = Ar.ReadUInt32();
        DSPTypeRaw = Ar.ReadUInt32();
        DSPType = (EDSPType)DSPTypeRaw;

        if (FModReader.Version >= 0x3D && FModReader.Version <= 0x91)
        {
            bool legacyBypass = Ar.ReadBoolean();
        }
    }

    /// <summary>
    /// Resolves the effect type name using the bank file version.
    /// FMOD Studio 1.10 banks (file version 0x65) require the legacy enum layout.
    /// </summary>
    public string GetDspTypeName(int fileVersion) => DSPTypeResolver.GetName(DSPTypeRaw, fileVersion);

    public EDSPTypeLegacy GetLegacyDspType() => DSPTypeResolver.ToLegacy(DSPTypeRaw);
}
