using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ink_Canvas.Tests.CI
{
    /// <summary>
    /// Tests to validate GitHub Actions workflow files
    /// </summary>
    public class WorkflowValidationTests
    {
        private readonly string _repoRoot;

        public WorkflowValidationTests()
        {
            // Navigate up from test project to repo root
            _repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        }

        [Fact]
        public void DotnetDesktopWorkflow_FileExists()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            // Assert
            File.Exists(workflowPath).Should().BeTrue(
                $"dotnet-desktop.yml workflow file should exist at {workflowPath}");
        }

        [Fact]
        public void DotnetDesktopWorkflow_IsValidYaml()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                // Skip test if file doesn't exist (already covered by another test)
                return;
            }

            var yaml = File.ReadAllText(workflowPath);

            // Act
            Action act = () =>
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                deserializer.Deserialize<object>(yaml);
            };

            // Assert
            act.Should().NotThrow("workflow YAML should be valid");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasRequiredTriggers()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("on:", "workflow should define triggers");
            content.Should().MatchRegex(@"(push:|pull_request:|workflow_dispatch:)",
                "workflow should have at least one trigger type");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasBuildJob()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("jobs:", "workflow should define jobs");
            content.Should().MatchRegex(@"(build|Build)",
                "workflow should have a build-related job");
        }

        [Fact]
        public void DotnetDesktopWorkflow_UsesWindowsRunner()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("windows-latest",
                "workflow should use Windows runner for .NET Framework build");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasMSBuildSetup()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("microsoft/setup-msbuild",
                "workflow should setup MSBuild for compilation");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasNuGetSetup()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().MatchRegex(@"(NuGet/setup-nuget|nuget restore)",
                "workflow should setup or use NuGet for package restoration");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasCacheConfiguration()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("actions/cache",
                "workflow should use caching for NuGet packages");
        }

        [Fact]
        public void DotnetDesktopWorkflow_ChecksExecutableGeneration()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().MatchRegex(@"(Check.*exe|InkCanvasForClass\.exe)",
                "workflow should verify executable file generation");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasArtifactUpload()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("actions/upload-artifact",
                "workflow should upload build artifacts");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HasPRCommentFeature()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().MatchRegex(@"(pull_request|PR|comment)",
                "workflow should have PR-related features");
        }

        [Fact]
        public void DotnetDesktopWorkflow_HandlesConcurrency()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("concurrency:",
                "workflow should define concurrency control");
            content.Should().Contain("cancel-in-progress",
                "workflow should cancel in-progress builds");
        }

        [Fact]
        public void GetLockFileWorkflow_Exists()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "GetLockFile.yml");

            // Act & Assert
            File.Exists(workflowPath).Should().BeTrue(
                $"GetLockFile.yml workflow should exist");
        }

        /// <summary>
        /// Regression test: Verify workflow doesn't contain common mistakes
        /// </summary>
        [Fact]
        public void DotnetDesktopWorkflow_DoesNotContainCommonMistakes()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().NotContain("TODO", "workflow should not contain TODO comments");
            content.Should().NotContain("FIXME", "workflow should not contain FIXME comments");
            content.Should().NotMatchRegex(@"password:\s*\w+",
                "workflow should not contain hardcoded passwords");
        }

        /// <summary>
        /// Boundary test: Verify workflow file size is reasonable
        /// </summary>
        [Fact]
        public void DotnetDesktopWorkflow_HasReasonableSize()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var fileInfo = new FileInfo(workflowPath);

            // Assert
            fileInfo.Length.Should().BeGreaterThan(100,
                "workflow file should not be suspiciously small");
            fileInfo.Length.Should().BeLessThan(1024 * 1024,
                "workflow file should not be unreasonably large (>1MB)");
        }

        /// <summary>
        /// Integration test: Verify workflow has proper permissions
        /// </summary>
        [Fact]
        public void DotnetDesktopWorkflow_DefinesPermissions()
        {
            // Arrange
            var workflowPath = Path.Combine(_repoRoot, ".github", "workflows", "dotnet-desktop.yml");

            if (!File.Exists(workflowPath))
            {
                return;
            }

            var content = File.ReadAllText(workflowPath);

            // Assert
            content.Should().Contain("permissions:",
                "workflow should define explicit permissions for security");
        }
    }
}