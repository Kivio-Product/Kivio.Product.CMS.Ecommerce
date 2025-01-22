using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Payments.PayU
{
    public static class ExtensionMethods
    {
        public static string ConvertToMd5(this string text)
        {
            var inputBytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = MD5.HashData(inputBytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string ConvertToSha256(this string text)
        {
            var hash = new StringBuilder();
            var crypto = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            foreach (var b in crypto)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string ConvertToSha384(this string text)
        {
            var hash = new StringBuilder();
            var crypto = SHA384.HashData(Encoding.UTF8.GetBytes(text));
            foreach (var b in crypto)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string ConvertToSha512(this string text)
        {
            var hash = new StringBuilder();
            var crypto = SHA512.HashData(Encoding.UTF8.GetBytes(text));
            foreach (var b in crypto)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
