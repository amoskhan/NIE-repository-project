namespace Architecture.Tests;

public class SourceFolderCohesionTests
{
    private const int MaximumDirectSourceFiles = 10;

    private static readonly DirectoryInfo RepositoryRoot = FindRepositoryRoot();

    private static readonly HashSet<string> SourceExtensions = new(
        [".cs", ".ts", ".vue"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> BuildOutputDirectoryNames = new(
        ["bin", "obj", "node_modules", "dist", "coverage"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ExcludedSourceRoots = new(
        ["src/backend/Infrastructure/Persistence/Migrations"],
        StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Source_folders_remain_bounded_and_feature_oriented()
    {
        var sourceRoots = new[]
        {
            Path.Combine(RepositoryRoot.FullName, "src", "backend"),
            Path.Combine(RepositoryRoot.FullName, "src", "frontend"),
        };

        var violations = sourceRoots
            .SelectMany(EnumerateSourceDirectories)
            .Select(directory => new
            {
                Directory = directory,
                DirectSourceFiles = directory
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(file => SourceExtensions.Contains(file.Extension))
                    .ToArray(),
            })
            .Where(candidate => candidate.DirectSourceFiles.Length > MaximumDirectSourceFiles)
            .OrderBy(candidate => candidate.Directory.FullName, StringComparer.Ordinal)
            .Select(candidate =>
                $"{Path.GetRelativePath(RepositoryRoot.FullName, candidate.Directory.FullName)} "
                + $"contains {candidate.DirectSourceFiles.Length} direct source files")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Source folders may contain at most {MaximumDirectSourceFiles} direct source files. "
            + "Create feature and responsibility subfolders instead:\n"
            + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<DirectoryInfo> EnumerateSourceDirectories(string sourceRoot)
    {
        var root = new DirectoryInfo(sourceRoot);
        Assert.True(root.Exists, $"Source root is missing: {sourceRoot}");

        return root
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Prepend(root)
            .Where(directory => !IsExplicitlyExcluded(directory));
    }

    private static bool IsExplicitlyExcluded(DirectoryInfo directory)
    {
        for (var current = directory; current is not null; current = current.Parent)
        {
            if (BuildOutputDirectoryNames.Contains(current.Name))
            {
                return true;
            }

            if (string.Equals(
                    current.FullName,
                    RepositoryRoot.FullName,
                    StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        var relativePath = Path
            .GetRelativePath(RepositoryRoot.FullName, directory.FullName)
            .Replace(Path.DirectorySeparatorChar, '/');

        return ExcludedSourceRoots.Any(excludedRoot =>
            string.Equals(relativePath, excludedRoot, StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"{excludedRoot}/", StringComparison.OrdinalIgnoreCase));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Repository root could not be located.");
    }
}
