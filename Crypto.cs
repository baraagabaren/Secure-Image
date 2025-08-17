using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Secure_Image
{
    static class Crypto
    {
        // Payload format:
        // [ "STEG" (4 bytes) ][ ver (1) ][ salt (16) ][ nonce (12) ][ tag (16) ][ cipherLen (4, BE) ][ ciphertext (N) ]
        private static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("STEG");
        private const byte VERSION = 1;

        public static byte[] MakeEncryptedPayload(byte[] plaintext, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] nonce = RandomNumberGenerator.GetBytes(12);

            // Derive key
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 200_000, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(32);

            // Encrypt
            byte[] cipher = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            using (var gcm = new AesGcm(key))
            {
                gcm.Encrypt(nonce, plaintext, cipher, tag);
            }

            // Build payload
            byte[] beCipherLen = BitConverter.GetBytes((UInt32)cipher.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(beCipherLen);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(MAGIC);           // 4
            bw.Write(VERSION);         // 1
            bw.Write(salt);            // 16
            bw.Write(nonce);           // 12
            bw.Write(tag);             // 16
            bw.Write(beCipherLen);     // 4
            bw.Write(cipher);          // N
            bw.Flush();
            return ms.ToArray();
        }

        public static byte[] DecryptPayload(byte[] payload, string password)
        {
            using var ms = new MemoryStream(payload);
            using var br = new BinaryReader(ms);

            var magic = br.ReadBytes(4);
            if (magic.Length != 4 || Encoding.ASCII.GetString(magic) != "STEG")
                throw new Exception("Invalid payload magic.");

            byte ver = br.ReadByte();
            if (ver != 1) throw new Exception("Unsupported payload version.");

            byte[] salt = br.ReadBytes(16);
            byte[] nonce = br.ReadBytes(12);
            byte[] tag = br.ReadBytes(16);

            byte[] beLen = br.ReadBytes(4);
            if (beLen.Length != 4) throw new Exception("Corrupt payload.");
            if (BitConverter.IsLittleEndian) Array.Reverse(beLen);
            int cipherLen = checked((int)BitConverter.ToUInt32(beLen, 0));

            byte[] cipher = br.ReadBytes(cipherLen);
            if (cipher.Length != cipherLen) throw new Exception("Truncated payload.");

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 200_000, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(32);

            byte[] plain = new byte[cipher.Length];
            using (var gcm = new AesGcm(key))
            {
                gcm.Decrypt(nonce, cipher, tag, plain);
            }
            return plain;
        }
    }
}
