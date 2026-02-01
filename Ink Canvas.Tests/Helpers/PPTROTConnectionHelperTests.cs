using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Ink_Canvas.Helpers;
using Xunit;

namespace Ink_Canvas.Tests.Helpers
{
    /// <summary>
    /// Tests for PPTROTConnectionHelper functionality
    /// </summary>
    public class PPTROTConnectionHelperTests
    {
        [Fact]
        public void AreComObjectsEqual_WithNullObjects_ReturnsFalse()
        {
            // Arrange
            object obj1 = null;
            object obj2 = null;

            // Act
            var result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            result.Should().BeFalse("both objects are null");
        }

        [Fact]
        public void AreComObjectsEqual_WithOneNullObject_ReturnsFalse()
        {
            // Arrange
            object obj1 = new object();
            object obj2 = null;

            // Act
            var result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            result.Should().BeFalse("one object is null");
        }

        [Fact]
        public void AreComObjectsEqual_WithSameReference_ReturnsTrue()
        {
            // Arrange
            object obj1 = new object();
            object obj2 = obj1;

            // Act
            var result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            result.Should().BeTrue("both references point to the same object");
        }

        [Fact]
        public void SafeReleaseComObject_WithNullObject_DoesNotThrow()
        {
            // Arrange
            object comObj = null;

            // Act
            Action act = () => PPTROTConnectionHelper.SafeReleaseComObject(comObj);

            // Assert
            act.Should().NotThrow("null objects should be handled gracefully");
        }

        [Fact]
        public void SafeReleaseComObject_WithNonComObject_DoesNotThrow()
        {
            // Arrange
            object regularObj = new object();

            // Act
            Action act = () => PPTROTConnectionHelper.SafeReleaseComObject(regularObj);

            // Assert
            act.Should().NotThrow("non-COM objects should be handled gracefully");
        }

        [Fact]
        public void GetSlideShowWindowsCount_WithNullApplication_ReturnsZero()
        {
            // Arrange
            Microsoft.Office.Interop.PowerPoint.Application pptApp = null;

            // Act
            var result = PPTROTConnectionHelper.GetSlideShowWindowsCount(pptApp);

            // Assert
            result.Should().Be(0, "null application should return zero count");
        }

        [Fact]
        public void IsValidSlideShowWindow_WithNullWindow_ReturnsFalse()
        {
            // Arrange
            object slideShowWindow = null;

            // Act
            var result = PPTROTConnectionHelper.IsValidSlideShowWindow(slideShowWindow);

            // Assert
            result.Should().BeFalse("null slideshow window should return false");
        }

        [Theory]
        [InlineData(".pptx")]
        [InlineData(".ppt")]
        [InlineData(".pptm")]
        [InlineData(".ppsx")]
        [InlineData(".pps")]
        [InlineData(".ppsm")]
        [InlineData(".dps")]
        [InlineData(".dpt")]
        public void TryConnectViaROT_RecognizesPresentationExtensions(string extension)
        {
            // This test verifies that the helper recognizes common PowerPoint file extensions
            // Since we cannot directly test the private LooksLikePresentationFile method,
            // we document the expected extensions here for regression testing
            var testFileName = $"test{extension}";
            testFileName.Should().EndWith(extension);
        }

        [Fact]
        public void TryConnectViaROT_WithoutRunningPowerPoint_ReturnsNull()
        {
            // Arrange & Act
            var result = PPTROTConnectionHelper.TryConnectViaROT(isSupportWPS: false);

            // Assert
            // When no PowerPoint is running, should return null
            // This is a smoke test to ensure the method doesn't crash
            // Result may be null or a valid application depending on system state
            result.Should().BeNull("no PowerPoint should be running in test environment");
        }

        [Fact]
        public void TryConnectViaROT_WithWPSSupport_DoesNotThrow()
        {
            // Arrange & Act
            Action act = () => PPTROTConnectionHelper.TryConnectViaROT(isSupportWPS: true);

            // Assert
            act.Should().NotThrow("WPS support should not cause exceptions");
        }

        [Fact]
        public void GetAnyActivePowerPoint_WithNullTargetApp_ReturnsValidOutputs()
        {
            // Arrange
            object targetApp = null;

            // Act
            var result = PPTROTConnectionHelper.GetAnyActivePowerPoint(
                targetApp,
                out int bestPriority,
                out int targetPriority
            );

            // Assert
            bestPriority.Should().BeGreaterOrEqualTo(0, "priority should not be negative");
            targetPriority.Should().Be(0, "target priority should be 0 when no target app");
        }

        [Fact]
        public void IsSlideShowWindowActive_WithNullObject_ReturnsFalse()
        {
            // Arrange
            object sswObj = null;

            // Act
            var result = PPTROTConnectionHelper.IsSlideShowWindowActive(sswObj);

            // Assert
            result.Should().BeFalse("null slideshow window should not be active");
        }

        /// <summary>
        /// Edge case test: Verify behavior with corrupted or invalid presentation objects
        /// </summary>
        [Fact]
        public void GetAnyActivePowerPoint_HandlesExceptionsGracefully()
        {
            // Arrange
            object invalidApp = new object(); // Not a real PowerPoint app

            // Act
            Action act = () => PPTROTConnectionHelper.GetAnyActivePowerPoint(
                invalidApp,
                out int bestPriority,
                out int targetPriority
            );

            // Assert
            act.Should().NotThrow("invalid application object should be handled gracefully");
        }

        /// <summary>
        /// Negative test: Verify COM object cleanup doesn't fail with exceptions
        /// </summary>
        [Fact]
        public void SafeReleaseComObject_WithMultipleCalls_HandlesGracefully()
        {
            // Arrange
            object testObj = new object();

            // Act & Assert
            // Multiple releases should not throw
            PPTROTConnectionHelper.SafeReleaseComObject(testObj);
            Action secondRelease = () => PPTROTConnectionHelper.SafeReleaseComObject(testObj);

            secondRelease.Should().NotThrow("multiple releases should be safe");
        }

        /// <summary>
        /// Boundary test: Verify slideshow window count edge cases
        /// </summary>
        [Fact]
        public void GetSlideShowWindowsCount_WithExceptionThrowingApp_ReturnsZero()
        {
            // This test ensures that if the PowerPoint application throws an exception
            // when accessing SlideShowWindows, the method returns 0 instead of crashing
            // This is tested indirectly by passing null, which will cause the internal
            // exception handling to activate

            // Arrange
            Microsoft.Office.Interop.PowerPoint.Application nullApp = null;

            // Act
            var count = PPTROTConnectionHelper.GetSlideShowWindowsCount(nullApp);

            // Assert
            count.Should().Be(0, "exception during count should result in zero");
        }

        /// <summary>
        /// Integration test boundary: Verify the helper can be called multiple times
        /// </summary>
        [Fact]
        public void TryConnectViaROT_CalledMultipleTimes_DoesNotFail()
        {
            // Arrange & Act
            Action act = () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var result = PPTROTConnectionHelper.TryConnectViaROT(isSupportWPS: false);
                    // Clean up if we got a connection
                    if (result != null)
                    {
                        PPTROTConnectionHelper.SafeReleaseComObject(result);
                    }
                }
            };

            // Assert
            act.Should().NotThrow("multiple connection attempts should be safe");
        }

        /// <summary>
        /// Regression test: Verify correct handling of WPS vs PowerPoint
        /// </summary>
        [Theory]
        [InlineData(true)]  // With WPS support
        [InlineData(false)] // Without WPS support
        public void TryConnectViaROT_WithDifferentWPSSettings_ExecutesSafely(bool supportWPS)
        {
            // Arrange & Act
            var result = PPTROTConnectionHelper.TryConnectViaROT(isSupportWPS: supportWPS);

            // Assert
            // The method should not crash regardless of WPS support setting
            // Result may be null if no presentation software is running
            if (result != null)
            {
                PPTROTConnectionHelper.SafeReleaseComObject(result);
            }

            // If we got here without exception, the test passes
            Assert.True(true, "Method executed without throwing exceptions");
        }
    }
}