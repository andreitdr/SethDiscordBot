using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others;

public class Cryptography
{
    /// <summary>
    ///     Translate hex to string
    /// </summary>
    /// <param name="hexString">The encrypted string</param>
    /// <returns></returns>
    public static string FromHexToString(string hexString)
    {
        var bytes                                       = new byte[hexString.Length / 2];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

        return Encoding.Unicode.GetString(bytes);
    }

    /// <summary>
    ///     Translate string to hex
    /// </summary>
    /// <param name="str">The string to encrypt</param>
    /// <returns></returns>
    public static string ToHexString(string str)
    {
        var sb = new StringBuilder();

        var bytes = Encoding.Unicode.GetBytes(str);
        foreach (var t in bytes) sb.Append(t.ToString("X2"));

        return sb.ToString();
    }

    /// <summary>
    ///     Create MD5 hash
    /// </summary>
    /// <param name="text">The text to encrypt</param>
    /// <returns></returns>
    public static async Task<string> CreateMD5(string text)
    {
        var output = "";
        using (var md5 = MD5.Create())
        {
            using (var s = GenerateStreamFromString(text))
            {
                var t = await md5.ComputeHashAsync(s);
                output = Convert.ToBase64String(t);
            }
        }

        return output;
    }

    /// <summary>
    ///     Create SHA256 hash
    /// </summary>
    /// <param name="text">The text to encrypt</param>
    /// <returns></returns>
    public static async Task<string> CreateSHA256(string text)
    {
        var output = "";
        using (var sha = SHA256.Create())
        {
            using (var s = GenerateStreamFromString(text))
            {
                var t = await sha.ComputeHashAsync(s);
                output = Convert.ToBase64String(t);
            }
        }

        return output;
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
