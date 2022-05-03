using System;

namespace PluginManager.Others
{
    public class Cryptography
    {

        /// <summary>
        /// Translate hex to string
        /// </summary>
        /// <param name="hexString">The encrypted string</param>
        /// <returns></returns>
        public static string FromHexToString(string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return System.Text.Encoding.Unicode.GetString(bytes);
        }

        /// <summary>
        /// Translate string to hex
        /// </summary>
        /// <param name="str">The string to encrypt</param>
        /// <returns></returns>
        public static string ToHexString(string str)
        {
            var sb = new System.Text.StringBuilder();

            var bytes = System.Text.Encoding.Unicode.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Create MD5 hash
        /// </summary>
        /// <param name="text">The text to encrypt</param>
        /// <returns></returns>
        public static async System.Threading.Tasks.Task<string> CreateMD5(string text)
        {
            string output = "";
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var s = GenerateStreamFromString(text))
                {
                    byte[] t = await md5.ComputeHashAsync(s);
                    output = System.Convert.ToBase64String(t);
                }
            }

            return output;
        }

        /// <summary>
        /// Create SHA256 hash
        /// </summary>
        /// <param name="text">The text to encrypt</param>
        /// <returns></returns>
        public static async System.Threading.Tasks.Task<string> CreateSHA256(string text)
        {
            string output = "";
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            {
                using (var s = GenerateStreamFromString(text))
                {
                    byte[] t = await sha.ComputeHashAsync(s);
                    output = System.Convert.ToBase64String(t);
                }
            }
            return output;
        }

        private static System.IO.Stream GenerateStreamFromString(string s)
        {
            var stream = new System.IO.MemoryStream();
            var writer = new System.IO.StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}