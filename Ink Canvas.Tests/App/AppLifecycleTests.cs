using System;
using FluentAssertions;
using Xunit;

namespace Ink_Canvas.Tests.App
{
    /// <summary>
    /// Tests for App.xaml.cs lifecycle and crash handling
    /// </summary>
    public class AppLifecycleTests
    {
        [Fact]
        public void CrashActionType_HasExpectedValues()
        {
            // Arrange & Act
            var silentRestart = Ink_Canvas.App.CrashActionType.SilentRestart;
            var noAction = Ink_Canvas.App.CrashActionType.NoAction;

            // Assert
            silentRestart.Should().NotBe(noAction, "crash action types should be distinct");
            ((int)silentRestart).Should().Be(0, "SilentRestart should be value 0");
            ((int)noAction).Should().Be(1, "NoAction should be value 1");
        }

        [Fact]
        public void RootPath_ShouldNotBeNull()
        {
            // Assert
            Ink_Canvas.App.RootPath.Should().NotBeNullOrEmpty("RootPath should be initialized");
        }

        [Fact]
        public void StartArgs_ShouldBeInitializable()
        {
            // Arrange
            var testArgs = new string[] { "--test", "--arg" };

            // Act
            Ink_Canvas.App.StartArgs = testArgs;

            // Assert
            Ink_Canvas.App.StartArgs.Should().Equal(testArgs, "StartArgs should store the provided arguments");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StartWithBoardMode_CanBeSet(bool value)
        {
            // Act
            Ink_Canvas.App.StartWithBoardMode = value;

            // Assert
            Ink_Canvas.App.StartWithBoardMode.Should().Be(value, "StartWithBoardMode should be settable");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StartWithShowMode_CanBeSet(bool value)
        {
            // Act
            Ink_Canvas.App.StartWithShowMode = value;

            // Assert
            Ink_Canvas.App.StartWithShowMode.Should().Be(value, "StartWithShowMode should be settable");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsAppExitByUser_CanBeSet(bool value)
        {
            // Act
            Ink_Canvas.App.IsAppExitByUser = value;

            // Assert
            Ink_Canvas.App.IsAppExitByUser.Should().Be(value, "IsAppExitByUser should be settable");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsUIAccessTopMostEnabled_CanBeSet(bool value)
        {
            // Act
            Ink_Canvas.App.IsUIAccessTopMostEnabled = value;

            // Assert
            Ink_Canvas.App.IsUIAccessTopMostEnabled.Should().Be(value, "IsUIAccessTopMostEnabled should be settable");
        }

        [Fact]
        public void CrashAction_DefaultValue_IsSilentRestart()
        {
            // Assert
            Ink_Canvas.App.CrashAction.Should().Be(Ink_Canvas.App.CrashActionType.SilentRestart,
                "default crash action should be SilentRestart");
        }

        [Theory]
        [InlineData(Ink_Canvas.App.CrashActionType.SilentRestart)]
        [InlineData(Ink_Canvas.App.CrashActionType.NoAction)]
        public void CrashAction_CanBeSet(Ink_Canvas.App.CrashActionType value)
        {
            // Act
            Ink_Canvas.App.CrashAction = value;

            // Assert
            Ink_Canvas.App.CrashAction.Should().Be(value, "CrashAction should be settable");
        }

        /// <summary>
        /// Test that the splash screen progress methods handle edge cases
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void SetSplashProgress_WithValidProgress_DoesNotThrow(int progress)
        {
            // Act
            Action act = () => Ink_Canvas.App.SetSplashProgress(progress);

            // Assert
            act.Should().NotThrow($"progress value {progress} should be valid");
        }

        /// <summary>
        /// Test that setting splash message handles various inputs
        /// </summary>
        [Theory]
        [InlineData("Loading...")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("正在加载中文消息")]
        public void SetSplashMessage_WithVariousMessages_DoesNotThrow(string message)
        {
            // Act
            Action act = () => Ink_Canvas.App.SetSplashMessage(message);

            // Assert
            act.Should().NotThrow($"message '{message}' should be handled gracefully");
        }

        /// <summary>
        /// Test splash screen lifecycle
        /// </summary>
        [Fact]
        public void ShowSplashScreen_CalledMultipleTimes_DoesNotThrow()
        {
            // Act
            Action act = () =>
            {
                Ink_Canvas.App.ShowSplashScreen();
                Ink_Canvas.App.ShowSplashScreen(); // Second call should be prevented
            };

            // Assert
            act.Should().NotThrow("multiple calls to ShowSplashScreen should be handled");
        }

        [Fact]
        public void CloseSplashScreen_WithoutShowingSplash_DoesNotThrow()
        {
            // Act
            Action act = () => Ink_Canvas.App.CloseSplashScreen();

            // Assert
            act.Should().NotThrow("closing splash when not shown should be safe");
        }

        /// <summary>
        /// Test crash action synchronization
        /// </summary>
        [Fact]
        public void SyncCrashActionFromSettings_DoesNotThrow()
        {
            // Act
            Action act = () => Ink_Canvas.App.SyncCrashActionFromSettings();

            // Assert
            act.Should().NotThrow("syncing crash action should not throw exceptions");
        }

        /// <summary>
        /// Boundary test: Verify extreme progress values
        /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void SetSplashProgress_WithBoundaryValues_DoesNotThrow(int progress)
        {
            // Act
            Action act = () => Ink_Canvas.App.SetSplashProgress(progress);

            // Assert
            act.Should().NotThrow($"boundary progress value {progress} should be handled");
        }

        /// <summary>
        /// Regression test: Verify watchdog process management
        /// </summary>
        [Fact]
        public void WatchdogProcess_CanBeAccessed()
        {
            // This test verifies the watchdogProcess field is accessible
            // and can be set to null without causing issues

            // Act
            Ink_Canvas.App.watchdogProcess = null;

            // Assert
            Ink_Canvas.App.watchdogProcess.Should().BeNull("watchdog process should be nullable");
        }

        /// <summary>
        /// Test command line argument parsing scenarios
        /// </summary>
        [Theory]
        [InlineData(new string[] { })]
        [InlineData(new string[] { "--board" })]
        [InlineData(new string[] { "--show" })]
        [InlineData(new string[] { "--board", "--show" })]
        [InlineData(new string[] { "--update-mode" })]
        [InlineData(new string[] { "--final-app" })]
        [InlineData(new string[] { "--watchdog", "12345", "path" })]
        public void StartArgs_HandlesVariousCommandLineArguments(string[] args)
        {
            // Act
            Ink_Canvas.App.StartArgs = args;

            // Assert
            Ink_Canvas.App.StartArgs.Should().BeEquivalentTo(args,
                "StartArgs should correctly store command line arguments");
        }

        /// <summary>
        /// Integration test: Verify app state consistency
        /// </summary>
        [Fact]
        public void AppState_RemainsConsistent_AfterMultipleChanges()
        {
            // Arrange
            var originalCrashAction = Ink_Canvas.App.CrashAction;

            // Act
            Ink_Canvas.App.CrashAction = Ink_Canvas.App.CrashActionType.NoAction;
            Ink_Canvas.App.IsAppExitByUser = true;
            Ink_Canvas.App.StartWithBoardMode = false;

            // Assert
            Ink_Canvas.App.CrashAction.Should().Be(Ink_Canvas.App.CrashActionType.NoAction);
            Ink_Canvas.App.IsAppExitByUser.Should().BeTrue();
            Ink_Canvas.App.StartWithBoardMode.Should().BeFalse();

            // Cleanup
            Ink_Canvas.App.CrashAction = originalCrashAction;
        }

        /// <summary>
        /// Negative test: Verify null string handling in splash messages
        /// </summary>
        [Fact]
        public void SetSplashMessage_WithNullMessage_DoesNotThrow()
        {
            // Act
            Action act = () => Ink_Canvas.App.SetSplashMessage(null);

            // Assert
            act.Should().NotThrow("null message should be handled gracefully");
        }

        /// <summary>
        /// Stress test: Rapid progress updates
        /// </summary>
        [Fact]
        public void SetSplashProgress_RapidUpdates_DoesNotFail()
        {
            // Act
            Action act = () =>
            {
                for (int i = 0; i <= 100; i += 10)
                {
                    Ink_Canvas.App.SetSplashProgress(i);
                }
            };

            // Assert
            act.Should().NotThrow("rapid progress updates should be handled");
        }
    }
}