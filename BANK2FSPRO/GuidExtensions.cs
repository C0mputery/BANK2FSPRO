using System.Security.Cryptography;
using System.Text;

namespace BANK2FSPRO;

public static class GuidExtensions {
    extension(Guid) {
        public static Guid DeterministicGuid(Guid namespaceGuid, string name) {
            using SHA1 sha1 = SHA1.Create();
            byte[] namespaceBytes = namespaceGuid.ToByteArray();
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            
            byte[] combinedBytes = new byte[namespaceBytes.Length + nameBytes.Length];
            Buffer.BlockCopy(namespaceBytes, 0, combinedBytes, 0, namespaceBytes.Length);
            Buffer.BlockCopy(nameBytes, 0, combinedBytes, namespaceBytes.Length, nameBytes.Length);

            byte[] hash = sha1.ComputeHash(combinedBytes);

            hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
            hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

            byte[] targetBytes = new byte[16];
            Buffer.BlockCopy(hash, 0, targetBytes, 0, 16);
            return new Guid(targetBytes);
        }
    }

    public static string ToFmodFormat(this Guid guid) { return $"{{{guid}}}"; }
}