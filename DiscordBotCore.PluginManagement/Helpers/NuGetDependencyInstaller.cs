using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Models;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace DiscordBotCore.PluginManagement.Helpers;

/// <summary>A package couldn't be installed. The message is meant for the person installing the plugin.</summary>
public sealed class NuGetInstallException(string message, Exception? innerException = null) : Exception(message, innerException);

public interface INuGetPackageSource
{
    /// <summary>The bytes of a .nupkg; null when that package or version doesn't exist.</summary>
    Task<byte[]?> DownloadAsync(string packageId, NuGetVersion version, CancellationToken cancellationToken);

    Task<IReadOnlyList<NuGetVersion>> GetVersionsAsync(string packageId, CancellationToken cancellationToken);
}

/// <summary>The official nuget.org repository.</summary>
public sealed class NuGetOrgPackageSource : INuGetPackageSource
{
    private const string SourceUrl = "https://api.nuget.org/v3/index.json";

    private readonly SourceRepository _repository = Repository.Factory.GetCoreV3(new PackageSource(SourceUrl));
    private readonly SourceCacheContext _cache = new();

    public async Task<byte[]?> DownloadAsync(string packageId, NuGetVersion version, CancellationToken cancellationToken)
    {
        FindPackageByIdResource find = (await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken))!;
        using var stream = new MemoryStream();
        bool found = await find.CopyNupkgToStreamAsync(packageId, version, stream, _cache, NullLogger.Instance, cancellationToken);
        return found ? stream.ToArray() : null;
    }

    public async Task<IReadOnlyList<NuGetVersion>> GetVersionsAsync(string packageId, CancellationToken cancellationToken)
    {
        FindPackageByIdResource find = (await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken))!;
        return (await find.GetAllVersionsAsync(packageId, _cache, NullLogger.Instance, cancellationToken)).ToList();
    }
}

public interface INuGetDependencyInstaller
{
    /// <summary>Downloads the packages and everything they depend on into a plugin's own folder.</summary>
    /// <exception cref="NuGetInstallException">A package couldn't be resolved, downloaded or used with this bot.</exception>
    Task InstallAsync(IReadOnlyCollection<OnlineNuGetDependency> packages, string targetFolder, CancellationToken cancellationToken = default);
}

public sealed class NuGetDependencyInstaller : INuGetDependencyInstaller
{
    private static readonly NuGetFramework _TargetFramework = NuGetFramework.Parse("net10.0");

    private readonly INuGetPackageSource _source;
    private readonly ILogger _logger;

    public NuGetDependencyInstaller(INuGetPackageSource source, ILogger logger)
    {
        _source = source;
        _logger = logger;
    }

    public async Task InstallAsync(IReadOnlyCollection<OnlineNuGetDependency> packages, string targetFolder, CancellationToken cancellationToken = default)
    {
        try
        {
            Dictionary<string, ResolvedPackage> resolved = await ResolveAsync(packages, cancellationToken);
            foreach (ResolvedPackage package in resolved.Values)
            {
                Extract(package, targetFolder);
            }
        }
        catch (Exception exception) when (exception is FatalProtocolException or HttpRequestException or InvalidDataException or PackagingException)
        {
            throw new NuGetInstallException($"Couldn't get the plugin's packages from nuget.org: {exception.Message}", exception);
        }
    }

    private sealed record ResolvedPackage(string Id, NuGetVersion Version, byte[] Content);

    /// <summary>Walks the dependencies of the packages. When several versions of a package are asked for, the highest wins.</summary>
    private async Task<Dictionary<string, ResolvedPackage>> ResolveAsync(IReadOnlyCollection<OnlineNuGetDependency> packages, CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<string, ResolvedPackage>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string Id, NuGetVersion Version)>();

        foreach (OnlineNuGetDependency package in packages)
        {
            if (!NuGetVersion.TryParse(package.Version, out NuGetVersion? version))
            {
                throw new NuGetInstallException($"{package.PackageId} has an invalid version: {package.Version}");
            }

            queue.Enqueue((package.PackageId, version));
        }

        while (queue.TryDequeue(out (string Id, NuGetVersion Version) next))
        {
            if (resolved.TryGetValue(next.Id, out ResolvedPackage? existing) && existing.Version >= next.Version)
            {
                continue;
            }

            byte[] content = await _source.DownloadAsync(next.Id, next.Version, cancellationToken)
                ?? throw new NuGetInstallException($"NuGet package {next.Id} {next.Version} doesn't exist on nuget.org.");

            resolved[next.Id] = new ResolvedPackage(next.Id, next.Version, content);
            _logger.Log($"Resolved NuGet package {next.Id} {next.Version}", this);

            using var reader = new PackageArchiveReader(new MemoryStream(content));
            NuGetFramework? nearest = new FrameworkReducer().GetNearest(_TargetFramework, reader.NuspecReader.GetDependencyGroups().Select(group => group.TargetFramework));
            if (nearest is null)
            {
                continue;
            }

            PackageDependencyGroup group = reader.NuspecReader.GetDependencyGroups().First(g => g.TargetFramework.Equals(nearest));
            foreach (PackageDependency dependency in group.Packages)
            {
                NuGetVersion version = dependency.VersionRange.MinVersion ?? await FindBestVersionAsync(dependency, cancellationToken);
                if (!IsCoveredByTheBot(dependency.Id, version))
                {
                    queue.Enqueue((dependency.Id, version));
                }
            }
        }

        return resolved;
    }

    private async Task<NuGetVersion> FindBestVersionAsync(PackageDependency dependency, CancellationToken cancellationToken)
    {
        IReadOnlyList<NuGetVersion> versions = await _source.GetVersionsAsync(dependency.Id, cancellationToken);
        return dependency.VersionRange.FindBestMatch(versions)
            ?? throw new NuGetInstallException($"No version of {dependency.Id} matches {dependency.VersionRange}.");
    }

    /// <summary>
    /// Assembly versions usually lag behind package versions, so a package that looks newer isn't refused here: it is
    /// downloaded, and Extract compares the actual assemblies.
    /// </summary>
    private static bool IsCoveredByTheBot(string packageId, NuGetVersion version)
    {
        Version? provided = HostRuntime.GetProvidedVersion(packageId);
        return provided is not null && provided >= version.Version;
    }

    private void Extract(ResolvedPackage package, string targetFolder)
    {
        using var reader = new PackageArchiveReader(new MemoryStream(package.Content));
        List<string> files = reader.GetFiles().ToList();

        foreach (string file in SelectManagedFiles(reader, files))
        {
            ExtractManaged(reader, package, file, targetFolder);
        }

        string? nativeRoot = HostRuntime.RuntimeIdentifiers
            .Select(rid => $"runtimes/{rid}/native/")
            .FirstOrDefault(root => files.Any(file => file.StartsWith(root, StringComparison.OrdinalIgnoreCase)));

        if (nativeRoot is not null)
        {
            foreach (string file in files.Where(file => file.StartsWith(nativeRoot, StringComparison.OrdinalIgnoreCase) && !file.EndsWith('/')))
            {
                Write(reader, file, file, targetFolder);
            }
        }
    }

    /// <summary>The DLLs for this platform and framework: a package's runtimes/ files replace its lib/ ones when both exist.</summary>
    private static List<string> SelectManagedFiles(PackageArchiveReader reader, List<string> files)
    {
        foreach (string rid in HostRuntime.RuntimeIdentifiers)
        {
            string root = $"runtimes/{rid}/lib/";
            var candidates = files
                .Where(file => file.StartsWith(root, StringComparison.OrdinalIgnoreCase) && file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .GroupBy(file => file[root.Length..].Split('/')[0])
                .ToDictionary(group => NuGetFramework.Parse(group.Key), group => group.ToList());

            NuGetFramework? nearestRuntime = new FrameworkReducer().GetNearest(_TargetFramework, candidates.Keys);
            if (nearestRuntime is not null)
            {
                return candidates[nearestRuntime];
            }
        }

        List<FrameworkSpecificGroup> groups = reader.GetLibItems().ToList();
        NuGetFramework? nearest = new FrameworkReducer().GetNearest(_TargetFramework, groups.Select(group => group.TargetFramework));
        return nearest is null
            ? []
            : groups.First(group => group.TargetFramework.Equals(nearest)).Items
                .Where(file => file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private void ExtractManaged(PackageArchiveReader reader, ResolvedPackage package, string file, string targetFolder)
    {
        string name = Path.GetFileNameWithoutExtension(file);
        Version? provided = HostRuntime.GetProvidedVersion(name);
        if (provided is not null)
        {
            Version? needed = ReadAssemblyVersion(reader, file);
            if (needed is not null && needed > provided)
            {
                throw new NuGetInstallException($"{package.Id} {package.Version} needs {name} {needed}, but this bot ships {provided}. Update the bot.");
            }

            _logger.Log($"{name} comes from the bot, so {package.Id}'s copy isn't used", this);
            return;
        }

        // Satellite assemblies keep their culture folder, the rest go straight into the plugin's folder
        string[] segments = file.Split('/');
        string relative = name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) && segments.Length >= 2
            ? $"{segments[^2]}/{segments[^1]}"
            : segments[^1];
        Write(reader, file, relative, targetFolder);
    }

    private static Version? ReadAssemblyVersion(PackageArchiveReader reader, string file)
    {
        try
        {
            using Stream entry = reader.GetStream(file);
            using var buffer = new MemoryStream();
            entry.CopyTo(buffer);
            buffer.Position = 0;
            using var pe = new PEReader(buffer);
            return pe.GetMetadataReader().GetAssemblyDefinition().Version;
        }
        catch (Exception exception) when (exception is BadImageFormatException or InvalidOperationException)
        {
            return null;
        }
    }

    private static void Write(PackageArchiveReader reader, string file, string relativePath, string targetFolder)
    {
        string root = Path.GetFullPath(targetFolder);
        string destination = Path.GetFullPath(Path.Combine(root, relativePath));

        // A package is someone else's archive, so its paths are checked before anything is written
        if (!destination.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new NuGetInstallException($"A package contains a file that would be saved outside the plugin's folder: {file}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using Stream entry = reader.GetStream(file);
        using FileStream output = File.Create(destination);
        entry.CopyTo(output);
    }
}
