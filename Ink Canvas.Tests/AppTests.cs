using System;
using FluentAssertions;
using Ink_Canvas;
using Xunit;

namespace Ink_Canvas.Tests
{
    public class AppTests
    {
        [Fact]
        public void App_RootPath_IsInitializedToApplicationBase()
        {
            // Arrange & Act
            var rootPath = App.RootPath;

            // Assert
            rootPath.Should().NotBeNullOrWhiteSpace();
            rootPath.Should().Be(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
        }

        [Fact]
        public void App_CrashActionType_HasTwoValues()
        {
            // Arrange & Act
            var values = Enum.GetValues(typeof(App.CrashActionType));

            // Assert
            values.Length.Should().Be(2);
            Enum.IsDefined(typeof(App.CrashActionType), App.CrashActionType.SilentRestart).Should().BeTrue();
            Enum.IsDefined(typeof(App.CrashActionType), App.CrashActionType.NoAction).Should().BeTrue();
        }

        [Fact]
        public void App_CrashAction_DefaultValue_IsSilentRestart()
        {
            // Arrange & Act
            var crashAction = App.CrashAction;

            // Assert
            crashAction.Should().Be(App.CrashActionType.SilentRestart);
        }

        [Fact]
        public void App_StartWithBoardMode_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var startWithBoardMode = App.StartWithBoardMode;

            // Assert
            startWithBoardMode.Should().BeFalse();
        }

        [Fact]
        public void App_StartWithShowMode_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var startWithShowMode = App.StartWithShowMode;

            // Assert
            startWithShowMode.Should().BeFalse();
        }

        [Fact]
        public void App_IsAppExitByUser_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var isAppExitByUser = App.IsAppExitByUser;

            // Assert
            isAppExitByUser.Should().BeFalse();
        }

        [Fact]
        public void App_IsUIAccessTopMostEnabled_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var isUIAccessTopMostEnabled = App.IsUIAccessTopMostEnabled;

            // Assert
            isUIAccessTopMostEnabled.Should().BeFalse();
        }

        [Theory]
        [InlineData(App.CrashActionType.SilentRestart)]
        [InlineData(App.CrashActionType.NoAction)]
        public void App_CrashAction_CanBeSetToValidValues(App.CrashActionType crashActionType)
        {
            // Arrange
            var originalValue = App.CrashAction;

            try
            {
                // Act
                App.CrashAction = crashActionType;

                // Assert
                App.CrashAction.Should().Be(crashActionType);
            }
            finally
            {
                // Cleanup - restore original value
                App.CrashAction = originalValue;
            }
        }

        [Fact]
        public void App_StartWithBoardMode_CanBeSet()
        {
            // Arrange
            var originalValue = App.StartWithBoardMode;

            try
            {
                // Act
                App.StartWithBoardMode = true;

                // Assert
                App.StartWithBoardMode.Should().BeTrue();

                // Act
                App.StartWithBoardMode = false;

                // Assert
                App.StartWithBoardMode.Should().BeFalse();
            }
            finally
            {
                // Cleanup - restore original value
                App.StartWithBoardMode = originalValue;
            }
        }

        [Fact]
        public void App_StartWithShowMode_CanBeSet()
        {
            // Arrange
            var originalValue = App.StartWithShowMode;

            try
            {
                // Act
                App.StartWithShowMode = true;

                // Assert
                App.StartWithShowMode.Should().BeTrue();

                // Act
                App.StartWithShowMode = false;

                // Assert
                App.StartWithShowMode.Should().BeFalse();
            }
            finally
            {
                // Cleanup - restore original value
                App.StartWithShowMode = originalValue;
            }
        }

        [Fact]
        public void App_IsAppExitByUser_CanBeSet()
        {
            // Arrange
            var originalValue = App.IsAppExitByUser;

            try
            {
                // Act
                App.IsAppExitByUser = true;

                // Assert
                App.IsAppExitByUser.Should().BeTrue();

                // Act
                App.IsAppExitByUser = false;

                // Assert
                App.IsAppExitByUser.Should().BeFalse();
            }
            finally
            {
                // Cleanup - restore original value
                App.IsAppExitByUser = originalValue;
            }
        }

        [Fact]
        public void App_IsUIAccessTopMostEnabled_CanBeSet()
        {
            // Arrange
            var originalValue = App.IsUIAccessTopMostEnabled;

            try
            {
                // Act
                App.IsUIAccessTopMostEnabled = true;

                // Assert
                App.IsUIAccessTopMostEnabled.Should().BeTrue();

                // Act
                App.IsUIAccessTopMostEnabled = false;

                // Assert
                App.IsUIAccessTopMostEnabled.Should().BeFalse();
            }
            finally
            {
                // Cleanup - restore original value
                App.IsUIAccessTopMostEnabled = originalValue;
            }
        }

        [Fact]
        public void App_StartArgs_CanBeSetAndRetrieved()
        {
            // Arrange
            var testArgs = new[] { "--test", "--arg1", "value1" };
            var originalArgs = App.StartArgs;

            try
            {
                // Act
                App.StartArgs = testArgs;

                // Assert
                App.StartArgs.Should().NotBeNull();
                App.StartArgs.Should().BeEquivalentTo(testArgs);
                App.StartArgs.Length.Should().Be(3);
                App.StartArgs[0].Should().Be("--test");
                App.StartArgs[1].Should().Be("--arg1");
                App.StartArgs[2].Should().Be("value1");
            }
            finally
            {
                // Cleanup - restore original value
                App.StartArgs = originalArgs;
            }
        }

        [Fact]
        public void App_WatchdogProcess_CanBeSetAndRetrieved()
        {
            // Arrange
            var originalProcess = App.watchdogProcess;

            try
            {
                // Act
                App.watchdogProcess = null;

                // Assert
                App.watchdogProcess.Should().BeNull();
            }
            finally
            {
                // Cleanup - restore original value
                App.watchdogProcess = originalProcess;
            }
        }
    }

    // Tests for CrashActionType enum
    public class CrashActionTypeTests
    {
        [Fact]
        public void CrashActionType_SilentRestart_HasValue0()
        {
            // Arrange & Act
            var value = (int)App.CrashActionType.SilentRestart;

            // Assert
            value.Should().Be(0);
        }

        [Fact]
        public void CrashActionType_NoAction_HasValue1()
        {
            // Arrange & Act
            var value = (int)App.CrashActionType.NoAction;

            // Assert
            value.Should().Be(1);
        }

        [Fact]
        public void CrashActionType_CanBeCastFromInt()
        {
            // Arrange & Act
            var silentRestart = (App.CrashActionType)0;
            var noAction = (App.CrashActionType)1;

            // Assert
            silentRestart.Should().Be(App.CrashActionType.SilentRestart);
            noAction.Should().Be(App.CrashActionType.NoAction);
        }

        [Fact]
        public void CrashActionType_ToString_ReturnsEnumName()
        {
            // Arrange & Act
            var silentRestartName = App.CrashActionType.SilentRestart.ToString();
            var noActionName = App.CrashActionType.NoAction.ToString();

            // Assert
            silentRestartName.Should().Be("SilentRestart");
            noActionName.Should().Be("NoAction");
        }

        [Theory]
        [InlineData(0, App.CrashActionType.SilentRestart)]
        [InlineData(1, App.CrashActionType.NoAction)]
        public void CrashActionType_IntegerMapping_IsCorrect(int intValue, App.CrashActionType expectedEnum)
        {
            // Arrange & Act
            var actualEnum = (App.CrashActionType)intValue;

            // Assert
            actualEnum.Should().Be(expectedEnum);
        }
    }

    // Edge case tests for App static properties
    public class AppEdgeCaseTests
    {
        [Fact]
        public void App_StartArgs_CanBeNull()
        {
            // Arrange
            var originalArgs = App.StartArgs;

            try
            {
                // Act
                App.StartArgs = null;

                // Assert
                App.StartArgs.Should().BeNull();
            }
            finally
            {
                // Cleanup
                App.StartArgs = originalArgs;
            }
        }

        [Fact]
        public void App_StartArgs_CanBeEmptyArray()
        {
            // Arrange
            var emptyArgs = new string[0];
            var originalArgs = App.StartArgs;

            try
            {
                // Act
                App.StartArgs = emptyArgs;

                // Assert
                App.StartArgs.Should().NotBeNull();
                App.StartArgs.Should().BeEmpty();
                App.StartArgs.Length.Should().Be(0);
            }
            finally
            {
                // Cleanup
                App.StartArgs = originalArgs;
            }
        }

        [Fact]
        public void App_RootPath_ContainsValidDirectorySeparator()
        {
            // Arrange & Act
            var rootPath = App.RootPath;

            // Assert
            rootPath.Should().NotBeNullOrWhiteSpace();
            // On Windows, paths typically end with backslash or are valid directory paths
            System.IO.Path.IsPathRooted(rootPath).Should().BeTrue();
        }

        [Fact]
        public void App_CrashAction_RoundTrip_PreservesValue()
        {
            // Arrange
            var originalValue = App.CrashAction;

            try
            {
                // Act - Set to SilentRestart
                App.CrashAction = App.CrashActionType.SilentRestart;
                var retrieved1 = App.CrashAction;

                // Act - Set to NoAction
                App.CrashAction = App.CrashActionType.NoAction;
                var retrieved2 = App.CrashAction;

                // Assert
                retrieved1.Should().Be(App.CrashActionType.SilentRestart);
                retrieved2.Should().Be(App.CrashActionType.NoAction);
            }
            finally
            {
                // Cleanup
                App.CrashAction = originalValue;
            }
        }

        [Fact]
        public void App_MultiplePropertyModifications_WorkCorrectly()
        {
            // Arrange
            var originalBoard = App.StartWithBoardMode;
            var originalShow = App.StartWithShowMode;
            var originalExit = App.IsAppExitByUser;
            var originalUIAccess = App.IsUIAccessTopMostEnabled;

            try
            {
                // Act
                App.StartWithBoardMode = true;
                App.StartWithShowMode = true;
                App.IsAppExitByUser = true;
                App.IsUIAccessTopMostEnabled = true;

                // Assert
                App.StartWithBoardMode.Should().BeTrue();
                App.StartWithShowMode.Should().BeTrue();
                App.IsAppExitByUser.Should().BeTrue();
                App.IsUIAccessTopMostEnabled.Should().BeTrue();

                // Act - Reset to false
                App.StartWithBoardMode = false;
                App.StartWithShowMode = false;
                App.IsAppExitByUser = false;
                App.IsUIAccessTopMostEnabled = false;

                // Assert
                App.StartWithBoardMode.Should().BeFalse();
                App.StartWithShowMode.Should().BeFalse();
                App.IsAppExitByUser.Should().BeFalse();
                App.IsUIAccessTopMostEnabled.Should().BeFalse();
            }
            finally
            {
                // Cleanup
                App.StartWithBoardMode = originalBoard;
                App.StartWithShowMode = originalShow;
                App.IsAppExitByUser = originalExit;
                App.IsUIAccessTopMostEnabled = originalUIAccess;
            }
        }
    }

    // Negative tests for boundary conditions
    public class AppNegativeTests
    {
        [Fact]
        public void App_CrashActionType_InvalidCast_ThrowsOrReturnsInvalid()
        {
            // Arrange
            int invalidValue = 999;

            // Act
            var result = (App.CrashActionType)invalidValue;

            // Assert - The cast doesn't throw, but the value is outside defined range
            Enum.IsDefined(typeof(App.CrashActionType), result).Should().BeFalse();
        }

        [Fact]
        public void App_StartArgs_WithSpecialCharacters_HandledCorrectly()
        {
            // Arrange
            var specialArgs = new[] { "--test=\"value with spaces\"", "--path=C:\\Program Files\\App", "--unicode=\u4E2D\u6587" };
            var originalArgs = App.StartArgs;

            try
            {
                // Act
                App.StartArgs = specialArgs;

                // Assert
                App.StartArgs.Should().NotBeNull();
                App.StartArgs[0].Should().Be("--test=\"value with spaces\"");
                App.StartArgs[1].Should().Be("--path=C:\\Program Files\\App");
                App.StartArgs[2].Should().Be("--unicode=\u4E2D\u6587");
            }
            finally
            {
                // Cleanup
                App.StartArgs = originalArgs;
            }
        }

        [Fact]
        public void App_StartArgs_WithEmptyStrings_HandledCorrectly()
        {
            // Arrange
            var argsWithEmpty = new[] { "", "--arg1", "", "value", "" };
            var originalArgs = App.StartArgs;

            try
            {
                // Act
                App.StartArgs = argsWithEmpty;

                // Assert
                App.StartArgs.Should().NotBeNull();
                App.StartArgs.Length.Should().Be(5);
                App.StartArgs[0].Should().BeEmpty();
                App.StartArgs[1].Should().Be("--arg1");
                App.StartArgs[2].Should().BeEmpty();
                App.StartArgs[3].Should().Be("value");
                App.StartArgs[4].Should().BeEmpty();
            }
            finally
            {
                // Cleanup
                App.StartArgs = originalArgs;
            }
        }

        [Fact]
        public void App_BooleanProperties_ToggleMultipleTimes_WorksCorrectly()
        {
            // Arrange
            var originalValue = App.StartWithBoardMode;

            try
            {
                // Act & Assert - Toggle 10 times
                for (int i = 0; i < 10; i++)
                {
                    App.StartWithBoardMode = (i % 2 == 0);
                    App.StartWithBoardMode.Should().Be(i % 2 == 0);
                }
            }
            finally
            {
                // Cleanup
                App.StartWithBoardMode = originalValue;
            }
        }
    }

    // Integration-style tests for combined property states
    public class AppIntegrationTests
    {
        [Fact]
        public void App_LaunchParameters_CanBeConfiguredTogether()
        {
            // Arrange
            var originalBoard = App.StartWithBoardMode;
            var originalShow = App.StartWithShowMode;
            var originalArgs = App.StartArgs;

            try
            {
                // Act - Simulate launch with board and show parameters
                App.StartWithBoardMode = true;
                App.StartWithShowMode = true;
                App.StartArgs = new[] { "--board", "--show" };

                // Assert
                App.StartWithBoardMode.Should().BeTrue("board mode should be enabled");
                App.StartWithShowMode.Should().BeTrue("show mode should be enabled");
                App.StartArgs.Should().Contain("--board");
                App.StartArgs.Should().Contain("--show");
            }
            finally
            {
                // Cleanup
                App.StartWithBoardMode = originalBoard;
                App.StartWithShowMode = originalShow;
                App.StartArgs = originalArgs;
            }
        }

        [Fact]
        public void App_ExitScenario_PropertiesSetCorrectly()
        {
            // Arrange
            var originalExit = App.IsAppExitByUser;
            var originalCrashAction = App.CrashAction;

            try
            {
                // Act - Simulate normal user exit
                App.IsAppExitByUser = true;
                App.CrashAction = App.CrashActionType.NoAction;

                // Assert
                App.IsAppExitByUser.Should().BeTrue("exit should be marked as user initiated");
                App.CrashAction.Should().Be(App.CrashActionType.NoAction, "crash action should not restart on user exit");
            }
            finally
            {
                // Cleanup
                App.IsAppExitByUser = originalExit;
                App.CrashAction = originalCrashAction;
            }
        }

        [Fact]
        public void App_CrashScenario_PropertiesSetCorrectly()
        {
            // Arrange
            var originalExit = App.IsAppExitByUser;
            var originalCrashAction = App.CrashAction;

            try
            {
                // Act - Simulate crash exit
                App.IsAppExitByUser = false;
                App.CrashAction = App.CrashActionType.SilentRestart;

                // Assert
                App.IsAppExitByUser.Should().BeFalse("exit should not be marked as user initiated");
                App.CrashAction.Should().Be(App.CrashActionType.SilentRestart, "crash action should allow restart");
            }
            finally
            {
                // Cleanup
                App.IsAppExitByUser = originalExit;
                App.CrashAction = originalCrashAction;
            }
        }

        [Fact]
        public void App_UIAccessScenario_CanBeEnabled()
        {
            // Arrange
            var originalUIAccess = App.IsUIAccessTopMostEnabled;

            try
            {
                // Act
                App.IsUIAccessTopMostEnabled = true;

                // Assert
                App.IsUIAccessTopMostEnabled.Should().BeTrue("UI access top most should be enabled");
            }
            finally
            {
                // Cleanup
                App.IsUIAccessTopMostEnabled = originalUIAccess;
            }
        }
    }
}