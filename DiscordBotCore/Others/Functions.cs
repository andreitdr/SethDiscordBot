using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace DiscordBotCore.Others;

/// <summary>
///     A special class with functions
/// </summary>
public static class Functions
{
    /// <summary>
    ///     The location for the Resources folder
    ///     String: ./Data/Resources/
    /// </summary>
    public static string dataFolder => Application.CurrentApplication.DataFolder;

    public static Color RandomColor
    {
        get
        {
            var random = new Random();
            return new Color(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        }
    }

    /// <summary>
    ///     Copy one Stream to another <see langword="async" />
    /// </summary>
    /// <param name="stream">The base stream</param>
    /// <param name="destination">The destination stream</param>
    /// <param name="bufferSize">The buffer to read</param>
    /// <param name="progress">The progress</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <exception cref="ArgumentNullException">Triggered if any <see cref="Stream" /> is empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Triggered if <paramref name="bufferSize" /> is less then or equal to 0</exception>
    /// <exception cref="InvalidOperationException">Triggered if <paramref name="stream" /> is not readable</exception>
    /// <exception cref="ArgumentException">Triggered in <paramref name="destination" /> is not writable</exception>
    public static async Task CopyToOtherStreamAsync(
        this Stream stream, Stream destination, int bufferSize,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (!stream.CanRead) throw new InvalidOperationException("The stream is not readable.");
        if (!destination.CanWrite)
            throw new ArgumentException("Destination stream is not writable", nameof(destination));

        var  buffer         = new byte[bufferSize];
        long totalBytesRead = 0;
        int  bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                        .ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }


    public static T SelectRandomValueOf<T>()
    {
        var enums  = Enum.GetValues(typeof(T));
        var random = new Random();
        return (T)enums.GetValue(random.Next(enums.Length));
    }

    public static T RandomValue<T>(this T[] values)
    {
        Random random = new();
        return values[random.Next(values.Length)];
    }

    public static string ToResourcesPath(this string path)
    {
        return Path.Combine(dataFolder, path);
    }
}
