using System;
using System.Collections;
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
                    sb.Append($"{hash[i]:x2}");
                return sb.ToString();
            }
            else
            {
                throw new Exception("Failed to generate hash.");
            }
        }

        public static string Stringify(object o, int maxLength = 200)
        {
            StringBuilder sb = new();

            if (o is IEnumerable enumerable)
            {
                sb.Append('[');

                int index = 0;
                foreach (object x in enumerable)
                {
                    int stringIndex = sb.Length;
                    if (index++ > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(Stringify(o));
                }

                sb.Append(']');
            }
            else
            {
                sb.Append(o.ToString());
            }

            return sb.ToString();
        }
    }
}
