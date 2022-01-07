namespace PluginManager.Others
{
    public class Cryptography
    {
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

        public static System.IO.Stream GenerateStreamFromString(string s)
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