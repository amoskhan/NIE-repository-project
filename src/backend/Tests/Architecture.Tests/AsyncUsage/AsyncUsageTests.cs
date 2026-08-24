namespace Architecture.Tests.AsyncUsage;

public class AsyncUsageTests
{
    private static readonly DirectoryInfo RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Production_code_does_not_block_on_tasks_or_wrap_synchronous_work_in_task_run()
    {
        var backendRoot = new DirectoryInfo(Path.Combine(RepositoryRoot.FullName, "src", "backend"));
        var violations = backendRoot
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(file => !IsExcluded(file))
            .SelectMany(file => File.ReadLines(file.FullName)
                .Select((line, index) => new { Line = line, Number = index + 1 })
                .Where(item => item.Line.Contains(".GetAwaiter().GetResult()", StringComparison.Ordinal)
                    || item.Line.Contains("Task.Run(", StringComparison.Ordinal))
                .Select(item => $"{Path.GetRelativePath(RepositoryRoot.FullName, file.FullName)}:{item.Number}"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Production code must stay async end-to-end. Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static bool IsExcluded(FileInfo file)
    {
        var relativePath = Path
            .GetRelativePath(RepositoryRoot.FullName, file.FullName)
            .Replace(Path.DirectorySeparatorChar, '/');

        return relativePath.Contains("/Tests/", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
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
