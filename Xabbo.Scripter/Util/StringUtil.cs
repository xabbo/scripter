using System;
using System.Security.Cryptography;
using System.Text;

namespace Xabbo.Scripter.Util
{
    public static class StringUtil
    {
        public static string ComputeHash(string value)
        {
            int length = Encoding.UTF8.GetByteCount(value);
            Span<byte> bytes = stackalloc byte[length];
            Encoding.UTF8.GetBytes(value, bytes);

            Span<byte> hash = stackalloc byte[20];
            if (SHA1.TryHashData(bytes, hash, out int bytesWritten))
            {
                StringBuilder sb = new();
                for (int i = 0; i < hash.Length; i++)
                    sb.Append($"{i:x2}");
                return sb.ToString();
            }
            else
            {
                throw new Exception("Failed to generate hash.");
            }
        }
    }
}
