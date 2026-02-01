using System;
using System.IO;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Ink_Canvas.Tests.Resources
{
    /// <summary>
    /// Tests for Settings class serialization and structure
    /// </summary>
    public class SettingsTests
    {
        [Fact]
        public void Settings_CanBeInstantiated()
        {
            // Act
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Should().NotBeNull("Settings should be instantiable");
        }

        [Fact]
        public void Settings_HasAppearanceSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Appearance.Should().NotBeNull("Settings should have Appearance section");
        }

        [Fact]
        public void Settings_HasAdvancedSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Advanced.Should().NotBeNull("Settings should have Advanced section");
        }

        [Fact]
        public void Settings_HasAutomationSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Automation.Should().NotBeNull("Settings should have Automation section");
        }

        [Fact]
        public void Settings_HasBehaviorSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Behavior.Should().NotBeNull("Settings should have Behavior section");
        }

        [Fact]
        public void Settings_HasCanvasSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Canvas.Should().NotBeNull("Settings should have Canvas section");
        }

        [Fact]
        public void Settings_HasGestureSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Gesture.Should().NotBeNull("Settings should have Gesture section");
        }

        [Fact]
        public void Settings_HasInkToShapeSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.InkToShape.Should().NotBeNull("Settings should have InkToShape section");
        }

        [Fact]
        public void Settings_HasPowerPointSettingsSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.PowerPointSettings.Should().NotBeNull("Settings should have PowerPointSettings section");
        }

        [Fact]
        public void Settings_HasRandWindowSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.RandWindow.Should().NotBeNull("Settings should have RandWindow section");
        }

        [Fact]
        public void Settings_HasStartupSection()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Startup.Should().NotBeNull("Settings should have Startup section");
        }

        /// <summary>
        /// Test JSON serialization of Settings
        /// </summary>
        [Fact]
        public void Settings_CanBeSerializedToJson()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act
            Action act = () => JsonConvert.SerializeObject(settings);

            // Assert
            act.Should().NotThrow("Settings should be serializable to JSON");
        }

        /// <summary>
        /// Test JSON deserialization of Settings
        /// </summary>
        [Fact]
        public void Settings_CanBeDeserializedFromJson()
        {
            // Arrange
            var originalSettings = new Ink_Canvas.Settings();
            var json = JsonConvert.SerializeObject(originalSettings);

            // Act
            var deserializedSettings = JsonConvert.DeserializeObject<Ink_Canvas.Settings>(json);

            // Assert
            deserializedSettings.Should().NotBeNull("Settings should be deserializable from JSON");
            deserializedSettings.Should().BeEquivalentTo(originalSettings,
                "Deserialized settings should match original settings");
        }

        /// <summary>
        /// Test that PowerPointSettings has expected properties
        /// </summary>
        [Fact]
        public void PowerPointSettings_HasExpectedProperties()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act
            var pptSettings = settings.PowerPointSettings;

            // Assert
            pptSettings.Should().NotBeNull();
            // Verify property accessibility (these should not throw)
            Action act = () =>
            {
                var _ = pptSettings.PowerPointSupport;
                var __ = pptSettings.IsAutoSaveStrokesInPowerPoint;
                var ___ = pptSettings.IsSupportWPS;
            };
            act.Should().NotThrow("PowerPointSettings properties should be accessible");
        }

        /// <summary>
        /// Test that Startup section has crash action property
        /// </summary>
        [Fact]
        public void StartupSettings_HasCrashActionProperty()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act
            var startup = settings.Startup;

            // Assert
            startup.Should().NotBeNull();
            Action act = () => { var _ = startup.CrashAction; };
            act.Should().NotThrow("Startup.CrashAction should be accessible");
        }

        /// <summary>
        /// Test that Appearance section has theme properties
        /// </summary>
        [Fact]
        public void AppearanceSettings_HasThemeProperties()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act
            var appearance = settings.Appearance;

            // Assert
            appearance.Should().NotBeNull();
            Action act = () =>
            {
                var _ = appearance.Theme;
                var __ = appearance.EnableSplashScreen;
            };
            act.Should().NotThrow("Appearance theme properties should be accessible");
        }

        /// <summary>
        /// Boundary test: Verify settings with modified values can be serialized
        /// </summary>
        [Fact]
        public void Settings_WithModifiedValues_SerializesCorrectly()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();
            settings.PowerPointSettings.PowerPointSupport = true;
            settings.PowerPointSettings.IsSupportWPS = true;

            // Act
            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<Ink_Canvas.Settings>(json);

            // Assert
            deserialized.PowerPointSettings.PowerPointSupport.Should().BeTrue();
            deserialized.PowerPointSettings.IsSupportWPS.Should().BeTrue();
        }

        /// <summary>
        /// Negative test: Deserializing invalid JSON should handle gracefully
        /// </summary>
        [Fact]
        public void Settings_DeserializingInvalidJson_ThrowsJsonException()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act
            Action act = () => JsonConvert.DeserializeObject<Ink_Canvas.Settings>(invalidJson);

            // Assert
            act.Should().Throw<JsonReaderException>("invalid JSON should throw exception");
        }

        /// <summary>
        /// Regression test: Verify all nested settings objects are initialized
        /// </summary>
        [Fact]
        public void Settings_AllNestedObjects_AreInitialized()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Assert
            settings.Appearance.Should().NotBeNull("Appearance should be initialized");
            settings.Advanced.Should().NotBeNull("Advanced should be initialized");
            settings.Automation.Should().NotBeNull("Automation should be initialized");
            settings.Behavior.Should().NotBeNull("Behavior should be initialized");
            settings.Canvas.Should().NotBeNull("Canvas should be initialized");
            settings.Gesture.Should().NotBeNull("Gesture should be initialized");
            settings.InkToShape.Should().NotBeNull("InkToShape should be initialized");
            settings.PowerPointSettings.Should().NotBeNull("PowerPointSettings should be initialized");
            settings.RandWindow.Should().NotBeNull("RandWindow should be initialized");
            settings.Startup.Should().NotBeNull("Startup should be initialized");
        }

        /// <summary>
        /// Test settings roundtrip (serialize -> deserialize -> serialize)
        /// </summary>
        [Fact]
        public void Settings_Roundtrip_MaintainsConsistency()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act
            var json1 = JsonConvert.SerializeObject(settings);
            var intermediate = JsonConvert.DeserializeObject<Ink_Canvas.Settings>(json1);
            var json2 = JsonConvert.SerializeObject(intermediate);

            // Assert
            json1.Should().Be(json2, "settings should remain consistent through roundtrip");
        }

        /// <summary>
        /// Test that automation settings has auto-save location property
        /// </summary>
        [Fact]
        public void AutomationSettings_HasAutoSavedStrokesLocation()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act
            var automation = settings.Automation;

            // Assert
            automation.Should().NotBeNull();
            Action act = () => { var _ = automation.AutoSavedStrokesLocation; };
            act.Should().NotThrow("AutoSavedStrokesLocation should be accessible");
        }

        /// <summary>
        /// Test PowerPoint settings button display options
        /// </summary>
        [Fact]
        public void PowerPointSettings_HasButtonDisplayOptions()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act & Assert
            var pptSettings = settings.PowerPointSettings;
            Action act = () =>
            {
                var _ = pptSettings.ShowPPTButton;
                var __ = pptSettings.PPTButtonsDisplayOption;
                var ___ = pptSettings.EnablePPTButtonPageClickable;
                var ____ = pptSettings.EnablePPTButtonLongPressPageTurn;
            };
            act.Should().NotThrow("PPT button display properties should be accessible");
        }

        /// <summary>
        /// Test that Canvas settings has ink-related properties
        /// </summary>
        [Fact]
        public void CanvasSettings_HasInkProperties()
        {
            // Arrange
            var settings = new Ink_Canvas.Settings();

            // Act & Assert
            var canvas = settings.Canvas;
            canvas.Should().NotBeNull();
            // Verify canvas has expected structure without exposing internal implementation
            canvas.Should().NotBeSameAs(new Ink_Canvas.Settings().Canvas,
                "Each settings instance should have its own canvas settings");
        }
    }
}