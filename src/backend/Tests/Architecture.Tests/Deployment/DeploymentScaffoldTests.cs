namespace Architecture.Tests.Deployment;

public class DeploymentScaffoldTests
{
    private static readonly DirectoryInfo RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Helm_chart_supports_shared_hosting_and_resilient_replica_placement()
    {
        var ingress = ReadRepositoryFile("deploy/helm/application/templates/ingress.yaml");
        var workloads = ReadRepositoryFile("deploy/helm/application/templates/workloads.yaml");

        Assert.Contains("if ne $.Values.hostingMode \"path\"", ingress, StringComparison.Ordinal);
        Assert.Contains("podAntiAffinity:", workloads, StringComparison.Ordinal);
        Assert.Contains("if gt (int (default 1 $workload.replicas)) 1", workloads, StringComparison.Ordinal);
        Assert.Contains("topologyKey: kubernetes.io/hostname", workloads, StringComparison.Ordinal);
        Assert.Contains("topologyKey: topology.kubernetes.io/zone", workloads, StringComparison.Ordinal);
    }

    [Fact]
    public void Aws_release_flow_packages_an_exact_git_ref_and_waits_for_codepipeline()
    {
        var powerShellReleaseScript = ReadRepositoryFile("deploy/pipeline/Start-ApplicationRelease.ps1");
        var shellReleaseScript = ReadRepositoryFile("deploy/pipeline/Start-ApplicationRelease.sh");
        var jenkinsfile = ReadRepositoryFile("build/Jenkinsfile");

        Assert.Contains("git -C $sourceRoot archive", powerShellReleaseScript, StringComparison.Ordinal);
        Assert.DoesNotContain("--others", powerShellReleaseScript, StringComparison.Ordinal);
        Assert.Contains("git -C \"$source_repo_path\" archive", shellReleaseScript, StringComparison.Ordinal);
        Assert.DoesNotContain("--others", shellReleaseScript, StringComparison.Ordinal);
        Assert.Contains("SOURCE_OBJECT_KEY", jenkinsfile, StringComparison.Ordinal);
        Assert.Contains("Validate Deployment Identity", jenkinsfile, StringComparison.Ordinal);
        Assert.Contains("Test-DeploymentIdentity.ps1", powerShellReleaseScript, StringComparison.Ordinal);
        Assert.Contains("-GitCommit $resolvedCommit", powerShellReleaseScript, StringComparison.Ordinal);
        Assert.Contains("Test-DeploymentIdentity.sh", shellReleaseScript, StringComparison.Ordinal);
        Assert.Contains("--git-commit \"$resolved_commit\"", shellReleaseScript, StringComparison.Ordinal);
        Assert.Contains("pipelineExecutionId", jenkinsfile, StringComparison.Ordinal);
        Assert.Contains("get-pipeline-execution", jenkinsfile, StringComparison.Ordinal);
        Assert.DoesNotContain("/stg/source.zip", jenkinsfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_compose_supports_the_same_build_or_pull_workflow_as_the_live_application()
    {
        var compose = ReadRepositoryFile("build/docker-compose.yml");

        Assert.Contains("${DOCKER_REGISTRY_URL:-local}/application-ui:${COMMIT_ID:-latest}", compose, StringComparison.Ordinal);
        Assert.Contains("dockerfile: build/Dockerfile.ui", compose, StringComparison.Ordinal);
        Assert.Contains("dockerfile: build/Dockerfile.auth", compose, StringComparison.Ordinal);
        Assert.Contains("dockerfile: build/Dockerfile.api", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Copier_renders_application_identity_into_deployment_artifacts()
    {
        AssertFileContains(
            "deploy/helm/[[ project_name ]]/Chart.yaml.jinja",
            "project_title");
        AssertFileContains(
            "deploy/helm/[[ project_name ]]/values.yaml.jinja",
            "project_name");
        AssertFileContains(
            "deploy/pipeline/Start-[[ project_name ]]Release.ps1.jinja",
            "project_name");
        AssertFileContains(
            "deploy/pipeline/Start-[[ project_name ]]Release.sh.jinja",
            "project_name");
        AssertFileContains("build/Jenkinsfile.jinja", "project_name");
        AssertFileContains("build/docker-compose.yml.jinja", "project_name");
        AssertFileContains("build/nginx.conf.jinja", "project_name");
        AssertFileContains("deploy/pipeline/README.md.jinja", "Start-ApplicationRelease.sh");
        AssertFileContains("deploy/pipeline/Test-DeploymentIdentity.ps1.jinja", "project_name");
        AssertFileContains("deploy/pipeline/Test-DeploymentIdentity.sh.jinja", "project_name");
    }

    [Fact]
    public void Copier_updates_are_guarded_against_ambiguous_legacy_deployment_paths()
    {
        var powerShellGuard = ReadRepositoryFile("deploy/pipeline/Test-DeploymentIdentity.ps1");
        var shellGuard = ReadRepositoryFile("deploy/pipeline/Test-DeploymentIdentity.sh");
        var powerShellMigrationTest = ReadRepositoryFile("deploy/pipeline/Test-DeploymentIdentityMigration.ps1");
        var shellMigrationTest = ReadRepositoryFile("deploy/pipeline/Test-DeploymentIdentityMigration.sh");
        var distributionGuide = ReadRepositoryFile("docs/template-distribution.md");

        Assert.Contains("deploy\\helm\\application", powerShellGuard, StringComparison.Ordinal);
        Assert.Contains("Start-ApplicationRelease.ps1", powerShellGuard, StringComparison.Ordinal);
        Assert.Contains("Start-ApplicationRelease.sh", powerShellGuard, StringComparison.Ordinal);
        Assert.Contains("Legacy generic deployment artifacts were found", powerShellGuard, StringComparison.Ordinal);
        Assert.Contains("deploy/helm/application", shellGuard, StringComparison.Ordinal);
        Assert.Contains("Start-ApplicationRelease.ps1", shellGuard, StringComparison.Ordinal);
        Assert.Contains("Start-ApplicationRelease.sh", shellGuard, StringComparison.Ordinal);
        Assert.Contains("Legacy generic deployment artifacts were found", shellGuard, StringComparison.Ordinal);
        Assert.Contains("Expected the migration guard to reject legacy artifacts", powerShellMigrationTest, StringComparison.Ordinal);
        Assert.Contains("Expected the commit-bound migration guard to reject a legacy SourceRef", powerShellMigrationTest, StringComparison.Ordinal);
        Assert.Contains("Expected the migration guard to reject legacy artifacts", shellMigrationTest, StringComparison.Ordinal);
        Assert.Contains("Expected the commit-bound migration guard to reject a legacy SourceRef", shellMigrationTest, StringComparison.Ordinal);
        Assert.Contains("Deployment identity migration on update", distributionGuide, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_PowerShell_entry_point_has_a_Bash_counterpart()
    {
        var scriptRoots = new[]
        {
            new DirectoryInfo(Path.Combine(RepositoryRoot.FullName, "build")),
            new DirectoryInfo(Path.Combine(RepositoryRoot.FullName, "deploy"))
        };
        var powerShellFiles = scriptRoots
            .SelectMany(root => root.EnumerateFiles("*.ps1", SearchOption.AllDirectories)
                .Concat(root.EnumerateFiles("*.ps1.jinja", SearchOption.AllDirectories)))
            .DistinctBy(file => file.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var powerShellFile in powerShellFiles)
        {
            var shellPath = powerShellFile.FullName.EndsWith(".ps1.jinja", StringComparison.OrdinalIgnoreCase)
                ? $"{powerShellFile.FullName[..^".ps1.jinja".Length]}.sh.jinja"
                : $"{powerShellFile.FullName[..^".ps1".Length]}.sh";
            Assert.True(
                File.Exists(shellPath),
                $"PowerShell entry point is missing its Bash counterpart: {powerShellFile.FullName}");
            var shellContent = File.ReadAllText(shellPath);
            if (shellPath.EndsWith(".jinja", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains("project_name", shellContent, StringComparison.Ordinal);
            }
            else
            {
                Assert.StartsWith("#!/usr/bin/env bash", shellContent, StringComparison.Ordinal);
            }
        }
    }

    private static void AssertFileContains(string relativePath, string expected)
    {
        var content = ReadRepositoryFile(relativePath);
        Assert.Contains(expected, content, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var path = Path.Combine(
            RepositoryRoot.FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Required deployment file is missing: {relativePath}");
        return File.ReadAllText(path);
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
