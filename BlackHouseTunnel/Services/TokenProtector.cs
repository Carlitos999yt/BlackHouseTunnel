using System;
using System.Text;

namespace BlackHouseTunnel.Services
{
    public static class TokenProtector
    {
        private static readonly byte[] EntropyKey = new byte[] { 0x42, 0x6C, 0x61, 0x63, 0x6B, 0x48, 0x6F, 0x75, 0x73, 0x65, 0x39, 0x39, 0x39 };

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            byte[] result = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                result[i] = (byte)(bytes[i] ^ EntropyKey[i % EntropyKey.Length]);
            }
            return Convert.ToBase64String(result);
        }

        public static string Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return "";
            try
            {
                byte[] bytes = Convert.FromBase64String(protectedText);
                byte[] result = new byte[bytes.Length];
                for (int i = 0; i < bytes.Length; i++)
                {
                    result[i] = (byte)(bytes[i] ^ EntropyKey[i % EntropyKey.Length]);
                }
                return Encoding.UTF8.GetString(result);
            }
            catch
            {
                return "";
            }
        }
    }
}
