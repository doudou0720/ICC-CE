using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ink_Canvas.Tests.GitHub
{
    [TestClass]
    public class WorkflowValidationTests
    {
        private string _repoRoot;
        private string _workflowsPath;
        private string _issueTemplatesPath;

        [TestInitialize]
        public void Setup()
        {
            // Navigate to repo root from test assembly location
            _repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            _workflowsPath = Path.Combine(_repoRoot, ".github", "workflows");
            _issueTemplatesPath = Path.Combine(_repoRoot, ".github", "ISSUE_TEMPLATE");
        }

        #region Workflow File Structure Tests

        [TestMethod]
        public void Workflows_Directory_Exists()
        {
            // Assert
            Assert.IsTrue(Directory.Exists(_workflowsPath),
                $"Workflows directory should exist at {_workflowsPath}");
        }

        [TestMethod]
        public void Workflows_DotNetDesktop_FileExists()
        {
            // Arrange
            string dotnetDesktopPath = Path.Combine(_workflowsPath, "dotnet-desktop.yml");

            // Assert
            Assert.IsTrue(File.Exists(dotnetDesktopPath),
                "dotnet-desktop.yml workflow file should exist");
        }

        [TestMethod]
        public void Workflows_DotNetDesktop_IsValidYaml()
        {
            // Arrange
            string dotnetDesktopPath = Path.Combine(_workflowsPath, "dotnet-desktop.yml");

            if (!File.Exists(dotnetDesktopPath))
            {
                Assert.Inconclusive("dotnet-desktop.yml file not found");
                return;
            }

            // Act
            string yamlContent = File.ReadAllText(dotnetDesktopPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            // Assert - Should not throw exception
            var workflow = deserializer.Deserialize<object>(yamlContent);
            Assert.IsNotNull(workflow);
        }

        [TestMethod]
        public void Workflows_DotNetDesktop_HasRequiredFields()
        {
            // Arrange
            string dotnetDesktopPath = Path.Combine(_workflowsPath, "dotnet-desktop.yml");

            if (!File.Exists(dotnetDesktopPath))
            {
                Assert.Inconclusive("dotnet-desktop.yml file not found");
                return;
            }

            // Act
            string yamlContent = File.ReadAllText(dotnetDesktopPath);

            // Assert
            Assert.IsTrue(yamlContent.Contains("name:"), "Workflow should have a name");
            Assert.IsTrue(yamlContent.Contains("on:") || yamlContent.Contains("'on':"), "Workflow should have triggers");
            Assert.IsTrue(yamlContent.Contains("jobs:"), "Workflow should have jobs");
        }

        [TestMethod]
        public void Workflows_DotNetDesktop_NotEmpty()
        {
            // Arrange
            string dotnetDesktopPath = Path.Combine(_workflowsPath, "dotnet-desktop.yml");

            if (!File.Exists(dotnetDesktopPath))
            {
                Assert.Inconclusive("dotnet-desktop.yml file not found");
                return;
            }

            // Act
            string yamlContent = File.ReadAllText(dotnetDesktopPath);

            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(yamlContent),
                "Workflow file should not be empty");
            Assert.IsTrue(yamlContent.Length > 100,
                "Workflow file should have substantial content");
        }

        #endregion

        #region Issue Template Tests

        [TestMethod]
        public void IssueTemplates_Directory_Exists()
        {
            // Assert
            Assert.IsTrue(Directory.Exists(_issueTemplatesPath),
                $"Issue templates directory should exist at {_issueTemplatesPath}");
        }

        [TestMethod]
        public void IssueTemplates_BugReportYaml_Exists()
        {
            // Arrange
            string bugReportPath = Path.Combine(_issueTemplatesPath, "01-bug_report.yml");

            // Assert
            Assert.IsTrue(File.Exists(bugReportPath),
                "01-bug_report.yml template should exist");
        }

        [TestMethod]
        public void IssueTemplates_FeatureRequestYaml_Exists()
        {
            // Arrange
            string featureRequestPath = Path.Combine(_issueTemplatesPath, "02-feature_request.yml");

            // Assert
            Assert.IsTrue(File.Exists(featureRequestPath),
                "02-feature_request.yml template should exist");
        }

        [TestMethod]
        public void IssueTemplates_BugReportMarkdown_Exists()
        {
            // Arrange
            string bugReportMdPath = Path.Combine(_issueTemplatesPath, "03-bug_report.md");

            // Assert
            Assert.IsTrue(File.Exists(bugReportMdPath),
                "03-bug_report.md template should exist");
        }

        [TestMethod]
        public void IssueTemplates_FeatureRequestMarkdown_Exists()
        {
            // Arrange
            string featureRequestMdPath = Path.Combine(_issueTemplatesPath, "04-feature_request.md");

            // Assert
            Assert.IsTrue(File.Exists(featureRequestMdPath),
                "04-feature_request.md template should exist");
        }

        [TestMethod]
        public void IssueTemplates_YamlFiles_AreValidYaml()
        {
            // Arrange
            string[] yamlTemplates = { "01-bug_report.yml", "02-feature_request.yml" };
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var template in yamlTemplates)
            {
                string templatePath = Path.Combine(_issueTemplatesPath, template);

                if (!File.Exists(templatePath))
                {
                    Assert.Inconclusive($"{template} file not found");
                    continue;
                }

                // Act
                string yamlContent = File.ReadAllText(templatePath);

                // Assert - Should not throw exception
                var parsed = deserializer.Deserialize<object>(yamlContent);
                Assert.IsNotNull(parsed, $"{template} should be valid YAML");
            }
        }

        [TestMethod]
        public void IssueTemplates_YamlFiles_HaveRequiredFields()
        {
            // Arrange
            string[] yamlTemplates = { "01-bug_report.yml", "02-feature_request.yml" };

            foreach (var template in yamlTemplates)
            {
                string templatePath = Path.Combine(_issueTemplatesPath, template);

                if (!File.Exists(templatePath))
                {
                    Assert.Inconclusive($"{template} file not found");
                    continue;
                }

                // Act
                string yamlContent = File.ReadAllText(templatePath);

                // Assert
                Assert.IsTrue(yamlContent.Contains("name:"), $"{template} should have a name");
                Assert.IsTrue(yamlContent.Contains("description:"), $"{template} should have a description");
                Assert.IsTrue(yamlContent.Contains("body:"), $"{template} should have a body");
            }
        }

        [TestMethod]
        public void IssueTemplates_MarkdownFiles_NotEmpty()
        {
            // Arrange
            string[] markdownTemplates = { "03-bug_report.md", "04-feature_request.md" };

            foreach (var template in markdownTemplates)
            {
                string templatePath = Path.Combine(_issueTemplatesPath, template);

                if (!File.Exists(templatePath))
                {
                    Assert.Inconclusive($"{template} file not found");
                    continue;
                }

                // Act
                string content = File.ReadAllText(templatePath);

                // Assert
                Assert.IsFalse(string.IsNullOrWhiteSpace(content),
                    $"{template} should not be empty");
                Assert.IsTrue(content.Length > 50,
                    $"{template} should have substantial content");
            }
        }

        [TestMethod]
        public void IssueTemplates_MarkdownFiles_HaveFrontMatter()
        {
            // Arrange
            string[] markdownTemplates = { "03-bug_report.md", "04-feature_request.md" };

            foreach (var template in markdownTemplates)
            {
                string templatePath = Path.Combine(_issueTemplatesPath, template);

                if (!File.Exists(templatePath))
                {
                    Assert.Inconclusive($"{template} file not found");
                    continue;
                }

                // Act
                string content = File.ReadAllText(templatePath);

                // Assert
                Assert.IsTrue(content.StartsWith("---"),
                    $"{template} should have YAML front matter starting with ---");
            }
        }

        #endregion

        #region File Naming Tests

        [TestMethod]
        public void IssueTemplates_FollowNamingConvention()
        {
            // Arrange
            if (!Directory.Exists(_issueTemplatesPath))
            {
                Assert.Inconclusive("Issue templates directory not found");
                return;
            }

            var templateFiles = Directory.GetFiles(_issueTemplatesPath, "*.*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => name.EndsWith(".yml") || name.EndsWith(".md"))
                .ToArray();

            // Assert
            foreach (var file in templateFiles)
            {
                Assert.IsTrue(file.StartsWith("0") || file.StartsWith("config") || file.StartsWith("."),
                    $"Template file {file} should follow naming convention (e.g., 01-*, 02-*, config.yml)");
            }
        }

        [TestMethod]
        public void Workflows_HaveYmlExtension()
        {
            // Arrange
            if (!Directory.Exists(_workflowsPath))
            {
                Assert.Inconclusive("Workflows directory not found");
                return;
            }

            var workflowFiles = Directory.GetFiles(_workflowsPath, "*.*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToArray();

            // Assert
            foreach (var file in workflowFiles)
            {
                Assert.IsTrue(file.EndsWith(".yml") || file.EndsWith(".yaml"),
                    $"Workflow file {file} should have .yml or .yaml extension");
            }
        }

        #endregion

        #region Content Validation Tests

        [TestMethod]
        public void Workflows_DotNetDesktop_UsesExpectedActions()
        {
            // Arrange
            string dotnetDesktopPath = Path.Combine(_workflowsPath, "dotnet-desktop.yml");

            if (!File.Exists(dotnetDesktopPath))
            {
                Assert.Inconclusive("dotnet-desktop.yml file not found");
                return;
            }

            // Act
            string content = File.ReadAllText(dotnetDesktopPath);

            // Assert - Check for common GitHub Actions
            Assert.IsTrue(content.Contains("actions/checkout") || content.Contains("uses:"),
                "Workflow should use GitHub Actions");
        }

        [TestMethod]
        public void IssueTemplates_BugReport_HasExpectedSections()
        {
            // Arrange
            string bugReportYamlPath = Path.Combine(_issueTemplatesPath, "01-bug_report.yml");

            if (!File.Exists(bugReportYamlPath))
            {
                Assert.Inconclusive("01-bug_report.yml file not found");
                return;
            }

            // Act
            string content = File.ReadAllText(bugReportYamlPath).ToLower();

            // Assert - Check for typical bug report sections
            bool hasRelevantSections = content.Contains("description") ||
                                        content.Contains("steps") ||
                                        content.Contains("expected") ||
                                        content.Contains("actual") ||
                                        content.Contains("reproduce");

            Assert.IsTrue(hasRelevantSections,
                "Bug report template should have relevant sections");
        }

        [TestMethod]
        public void IssueTemplates_FeatureRequest_HasExpectedSections()
        {
            // Arrange
            string featureRequestYamlPath = Path.Combine(_issueTemplatesPath, "02-feature_request.yml");

            if (!File.Exists(featureRequestYamlPath))
            {
                Assert.Inconclusive("02-feature_request.yml file not found");
                return;
            }

            // Act
            string content = File.ReadAllText(featureRequestYamlPath).ToLower();

            // Assert - Check for typical feature request sections
            bool hasRelevantSections = content.Contains("description") ||
                                        content.Contains("feature") ||
                                        content.Contains("use case") ||
                                        content.Contains("benefit");

            Assert.IsTrue(hasRelevantSections,
                "Feature request template should have relevant sections");
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void Workflows_NoEmptyFiles()
        {
            // Arrange
            if (!Directory.Exists(_workflowsPath))
            {
                Assert.Inconclusive("Workflows directory not found");
                return;
            }

            var workflowFiles = Directory.GetFiles(_workflowsPath, "*.yml");

            // Assert
            foreach (var file in workflowFiles)
            {
                var fileInfo = new FileInfo(file);
                Assert.IsTrue(fileInfo.Length > 0,
                    $"Workflow file {Path.GetFileName(file)} should not be empty");
            }
        }

        [TestMethod]
        public void IssueTemplates_NoEmptyFiles()
        {
            // Arrange
            if (!Directory.Exists(_issueTemplatesPath))
            {
                Assert.Inconclusive("Issue templates directory not found");
                return;
            }

            var templateFiles = Directory.GetFiles(_issueTemplatesPath, "*.*")
                .Where(f => f.EndsWith(".yml") || f.EndsWith(".md"))
                .ToArray();

            // Assert
            foreach (var file in templateFiles)
            {
                var fileInfo = new FileInfo(file);
                Assert.IsTrue(fileInfo.Length > 0,
                    $"Template file {Path.GetFileName(file)} should not be empty");
            }
        }

        #endregion

        #region Negative Tests

        [TestMethod]
        public void Workflows_DotNetDesktop_NoSyntaxErrors()
        {
            // Arrange
            string dotnetDesktopPath = Path.Combine(_workflowsPath, "dotnet-desktop.yml");

            if (!File.Exists(dotnetDesktopPath))
            {
                Assert.Inconclusive("dotnet-desktop.yml file not found");
                return;
            }

            // Act
            string content = File.ReadAllText(dotnetDesktopPath);

            // Assert - Check for common YAML syntax errors
            Assert.IsFalse(content.Contains("\t"),
                "Workflow should use spaces, not tabs");

            var lines = content.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                // Check that colons in YAML keys have space after them (unless in quotes)
                if (line.Contains(":") && !line.Trim().StartsWith("#"))
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < line.Length - 1)
                    {
                        // This is a simple check, may have false positives with URLs
                        Assert.IsTrue(line[colonIndex + 1] == ' ' || line[colonIndex + 1] == '\r' ||
                                      line.Contains("http:") || line.Contains("https:"),
                            $"Line '{line.Trim()}' may have YAML syntax error (missing space after colon)");
                    }
                }
            }
        }

        #endregion
    }
}