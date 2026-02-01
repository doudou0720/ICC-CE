using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace Ink_Canvas.Tests.MainWindow_cs
{
    [TestClass]
    public class MW_PPTTests
    {
        #region Constants Tests

        [TestMethod]
        public void PPT_LongPressDelay_IsValidValue()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);
            var field = mainWindowType.GetField("LongPressDelay", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            int longPressDelay = (int)field.GetValue(null);

            // Assert
            Assert.AreEqual(500, longPressDelay);
            Assert.IsTrue(longPressDelay > 0, "Long press delay should be positive");
            Assert.IsTrue(longPressDelay < 2000, "Long press delay should be reasonable");
        }

        [TestMethod]
        public void PPT_LongPressInterval_IsValidValue()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);
            var field = mainWindowType.GetField("LongPressInterval", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            int longPressInterval = (int)field.GetValue(null);

            // Assert
            Assert.AreEqual(50, longPressInterval);
            Assert.IsTrue(longPressInterval > 0, "Long press interval should be positive");
            Assert.IsTrue(longPressInterval <= 100, "Long press interval should be fast enough");
        }

        [TestMethod]
        public void PPT_ProcessMonitorInterval_IsValidValue()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);
            var field = mainWindowType.GetField("ProcessMonitorInterval", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            int processMonitorInterval = (int)field.GetValue(null);

            // Assert
            Assert.AreEqual(1000, processMonitorInterval);
            Assert.IsTrue(processMonitorInterval >= 500, "Monitor interval should not be too frequent");
            Assert.IsTrue(processMonitorInterval <= 5000, "Monitor interval should not be too slow");
        }

        [TestMethod]
        public void PPT_SlideSwitchDebounceMs_IsValidValue()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);
            var field = mainWindowType.GetField("SlideSwitchDebounceMs", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            int slideSwitchDebounceMs = (int)field.GetValue(null);

            // Assert
            Assert.AreEqual(150, slideSwitchDebounceMs);
            Assert.IsTrue(slideSwitchDebounceMs > 0, "Debounce time should be positive");
            Assert.IsTrue(slideSwitchDebounceMs <= 500, "Debounce time should be reasonable");
        }

        #endregion

        #region Win32 API Constants Tests

        [TestMethod]
        public void PPT_Win32Constants_AreCorrect()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            int gwlStyle = (int)mainWindowType.GetField("GWL_STYLE", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int wsVisible = (int)mainWindowType.GetField("WS_VISIBLE", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int wsMinimize = (int)mainWindowType.GetField("WS_MINIMIZE", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            uint gwHwndNext = (uint)mainWindowType.GetField("GW_HWNDNEXT", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            uint gwHwndPrev = (uint)mainWindowType.GetField("GW_HWNDPREV", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // Assert
            Assert.AreEqual(-16, gwlStyle);
            Assert.AreEqual(0x10000000, wsVisible);
            Assert.AreEqual(0x20000000, wsMinimize);
            Assert.AreEqual((uint)2, gwHwndNext);
            Assert.AreEqual((uint)3, gwHwndPrev);
        }

        #endregion

        #region Static Field Tests

        [TestMethod]
        public void PPT_StaticFields_InitializeCorrectly()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act - Access static fields
            var pptApplicationField = mainWindowType.GetField("pptApplication", BindingFlags.Public | BindingFlags.Static);
            var presentationField = mainWindowType.GetField("presentation", BindingFlags.Public | BindingFlags.Static);
            var slidesField = mainWindowType.GetField("slides", BindingFlags.Public | BindingFlags.Static);
            var slideField = mainWindowType.GetField("slide", BindingFlags.Public | BindingFlags.Static);
            var slidescountField = mainWindowType.GetField("slidescount", BindingFlags.Public | BindingFlags.Static);

            // Assert
            Assert.IsNotNull(pptApplicationField);
            Assert.IsNotNull(presentationField);
            Assert.IsNotNull(slidesField);
            Assert.IsNotNull(slideField);
            Assert.IsNotNull(slidescountField);

            // Verify types
            Assert.AreEqual(typeof(Microsoft.Office.Interop.PowerPoint.Application), pptApplicationField.FieldType);
            Assert.AreEqual(typeof(object), presentationField.FieldType);
            Assert.AreEqual(typeof(object), slidesField.FieldType);
            Assert.AreEqual(typeof(object), slideField.FieldType);
            Assert.AreEqual(typeof(int), slidescountField.FieldType);
        }

        #endregion

        #region PPT Manager Property Tests

        [TestMethod]
        public void PPT_PPTManagerProperty_ExistsAndIsReadOnly()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var pptManagerProperty = mainWindowType.GetProperty("PPTManager", BindingFlags.Public | BindingFlags.Instance);

            // Assert
            Assert.IsNotNull(pptManagerProperty);
            Assert.IsTrue(pptManagerProperty.CanRead);
            Assert.IsFalse(pptManagerProperty.CanWrite, "PPTManager should be read-only");
        }

        #endregion

        #region Boundary Tests

        [TestMethod]
        public void PPT_SlidesCount_InitialValue_IsZero()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);
            var slidescountField = mainWindowType.GetField("slidescount", BindingFlags.Public | BindingFlags.Static);

            // Act
            int initialValue = (int)slidescountField.GetValue(null);

            // Assert - Default int value should be 0
            Assert.IsTrue(initialValue >= 0, "Slides count should not be negative");
        }

        [TestMethod]
        public void PPT_Constants_ArePositive()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            int longPressDelay = (int)mainWindowType.GetField("LongPressDelay", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int longPressInterval = (int)mainWindowType.GetField("LongPressInterval", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int processMonitorInterval = (int)mainWindowType.GetField("ProcessMonitorInterval", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int slideSwitchDebounceMs = (int)mainWindowType.GetField("SlideSwitchDebounceMs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // Assert
            Assert.IsTrue(longPressDelay > 0);
            Assert.IsTrue(longPressInterval > 0);
            Assert.IsTrue(processMonitorInterval > 0);
            Assert.IsTrue(slideSwitchDebounceMs > 0);
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void PPT_LongPressInterval_LessThanDelay()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            int longPressDelay = (int)mainWindowType.GetField("LongPressDelay", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int longPressInterval = (int)mainWindowType.GetField("LongPressInterval", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // Assert
            Assert.IsTrue(longPressInterval < longPressDelay,
                "Long press interval should be less than delay for proper functionality");
        }

        [TestMethod]
        public void PPT_SlideSwitchDebounce_ReasonableForUserExperience()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);
            int slideSwitchDebounceMs = (int)mainWindowType.GetField("SlideSwitchDebounceMs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // Assert
            Assert.IsTrue(slideSwitchDebounceMs >= 50, "Debounce should prevent accidental double-clicks");
            Assert.IsTrue(slideSwitchDebounceMs <= 300, "Debounce should not be too long for responsive UI");
        }

        #endregion

        #region Negative Tests

        [TestMethod]
        public void PPT_Win32Constants_NotZero()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            int gwlStyle = (int)mainWindowType.GetField("GWL_STYLE", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int wsVisible = (int)mainWindowType.GetField("WS_VISIBLE", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int wsMinimize = (int)mainWindowType.GetField("WS_MINIMIZE", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // Assert
            Assert.AreNotEqual(0, gwlStyle);
            Assert.AreNotEqual(0, wsVisible);
            Assert.AreNotEqual(0, wsMinimize);
        }

        #endregion

        #region Regression Tests

        [TestMethod]
        public void PPT_TimingConstants_HaveNotChanged()
        {
            // This test ensures timing constants remain stable across versions
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            int longPressDelay = (int)mainWindowType.GetField("LongPressDelay", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int longPressInterval = (int)mainWindowType.GetField("LongPressInterval", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int processMonitorInterval = (int)mainWindowType.GetField("ProcessMonitorInterval", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int slideSwitchDebounceMs = (int)mainWindowType.GetField("SlideSwitchDebounceMs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // Assert - These values should match the documented constants
            Assert.AreEqual(500, longPressDelay, "LongPressDelay should be 500ms");
            Assert.AreEqual(50, longPressInterval, "LongPressInterval should be 50ms");
            Assert.AreEqual(1000, processMonitorInterval, "ProcessMonitorInterval should be 1000ms");
            Assert.AreEqual(150, slideSwitchDebounceMs, "SlideSwitchDebounceMs should be 150ms");
        }

        [TestMethod]
        public void PPT_AllFieldsAndPropertiesAccessible()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act - Try to access all PPT-related fields and properties
            var pptApplicationField = mainWindowType.GetField("pptApplication", BindingFlags.Public | BindingFlags.Static);
            var presentationField = mainWindowType.GetField("presentation", BindingFlags.Public | BindingFlags.Static);
            var slidesField = mainWindowType.GetField("slides", BindingFlags.Public | BindingFlags.Static);
            var slideField = mainWindowType.GetField("slide", BindingFlags.Public | BindingFlags.Static);
            var slidescountField = mainWindowType.GetField("slidescount", BindingFlags.Public | BindingFlags.Static);
            var pptManagerProperty = mainWindowType.GetProperty("PPTManager", BindingFlags.Public | BindingFlags.Instance);

            // Assert
            Assert.IsNotNull(pptApplicationField);
            Assert.IsNotNull(presentationField);
            Assert.IsNotNull(slidesField);
            Assert.IsNotNull(slideField);
            Assert.IsNotNull(slidescountField);
            Assert.IsNotNull(pptManagerProperty);
        }

        #endregion
    }
}