namespace FModBankParser.Objects;

public readonly struct FSnapshot
{
    public readonly uint EntryIndex;
    public readonly FModGuid SnapshotGuid;
    public readonly uint TargetIndex;
    public readonly float Value;

    public FSnapshot(BinaryReader Ar)
    {
        EntryIndex = Ar.ReadUInt32();
        SnapshotGuid = new FModGuid(Ar);
        TargetIndex = Ar.ReadUInt32();
        Value = Ar.ReadSingle();
    }
}
