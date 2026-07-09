using FModBankParser;
using FModBankParser.Nodes.Buses;

namespace BANK2FSPRO;

internal static class Program {
    const string TargetBanks = @"C:\Users\Computery\Desktop\StreamingAssets";
    const string ProjectOutput = @"S:\RehabG\FmodProjectNew";

    static void Main(string[] args) {
        string[] stringBankFiles = Directory.GetFiles(TargetBanks, "*.strings.bank");
        if (stringBankFiles.Length != 1) { throw new NotImplementedException(); } // TODO
        string stringBankPath = stringBankFiles[0];
        FModReader stringBank = FModBankParser.FModBankParser.LoadSoundBank(new FileInfo(stringBankPath));
        if (stringBank.StringTable == null) { throw new NotImplementedException(); } // TODO
        
        FModReader? masterBank = null;
        List<FModReader> banks = [];

        string[] bankFiles = Directory.GetFiles(TargetBanks, "*.bank");
        foreach (string bankFile in bankFiles) {
            if (bankFile.EndsWith(".strings.bank")) { continue; }
            
            FModReader bank = FModBankParser.FModBankParser.LoadSoundBank(new FileInfo(bankFile));
            bool isMaster = bank.BusNodes.Values.Any(b => b is MasterBusNode); // I think only the master bank has the mixer/buses, may be wrong, works for my banks
            if (isMaster) { masterBank = bank; }
            banks.Add(bank);
        }
        if (masterBank == null) { throw new NotImplementedException(); } // TODO

        Decompiler decompiler = new Decompiler(ProjectOutput, stringBank, masterBank, banks.ToArray(), "RehabG");
        decompiler.Decompile();
    }
}