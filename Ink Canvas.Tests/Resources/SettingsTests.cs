using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Ink_Canvas;
using Newtonsoft.Json;
using Xunit;

namespace Ink_Canvas.Tests.Resources
{
    public class SettingsTests
    {
        [Fact]
        public void Settings_DefaultConstructor_InitializesAllPropertiesWithDefaults()
        {
            // Arrange & Act
            var settings = new Settings();

            // Assert
            settings.Advanced.Should().NotBeNull();
            settings.Appearance.Should().NotBeNull();
            settings.Automation.Should().NotBeNull();
            settings.PowerPointSettings.Should().NotBeNull();
            settings.Canvas.Should().NotBeNull();
            settings.Gesture.Should().NotBeNull();
            settings.InkToShape.Should().NotBeNull();
            settings.Startup.Should().NotBeNull();
            settings.RandSettings.Should().NotBeNull();
            settings.ModeSettings.Should().NotBeNull();
            settings.Camera.Should().NotBeNull();
            settings.Dlass.Should().NotBeNull();
        }

        [Fact]
        public void Settings_SerializeToJson_ProducesValidJson()
        {
            // Arrange
            var settings = new Settings();

            // Act
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

            // Assert
            json.Should().NotBeNullOrWhiteSpace();
            json.Should().Contain("\"advanced\"");
            json.Should().Contain("\"appearance\"");
            json.Should().Contain("\"canvas\"");
        }

        [Fact]
        public void Settings_DeserializeFromJson_ReconstructsObject()
        {
            // Arrange
            var originalSettings = new Settings
            {
                Canvas = new Canvas { InkWidth = 5.0, InkAlpha = 128 },
                Startup = new Startup { IsAutoUpdate = false }
            };
            var json = JsonConvert.SerializeObject(originalSettings);

            // Act
            var deserializedSettings = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            deserializedSettings.Should().NotBeNull();
            deserializedSettings.Canvas.InkWidth.Should().Be(5.0);
            deserializedSettings.Canvas.InkAlpha.Should().Be(128);
            deserializedSettings.Startup.IsAutoUpdate.Should().BeFalse();
        }

        [Fact]
        public void Settings_RoundTripSerialization_PreservesValues()
        {
            // Arrange
            var settings = new Settings();
            settings.Canvas.InkWidth = 3.7;
            settings.Canvas.HighlighterWidth = 25.5;
            settings.Canvas.EnableInkFade = true;
            settings.Canvas.InkFadeTime = 5000;

            // Act
            var json = JsonConvert.SerializeObject(settings);
            var roundTripped = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            roundTripped.Canvas.InkWidth.Should().Be(3.7);
            roundTripped.Canvas.HighlighterWidth.Should().Be(25.5);
            roundTripped.Canvas.EnableInkFade.Should().BeTrue();
            roundTripped.Canvas.InkFadeTime.Should().Be(5000);
        }
    }

    public class CanvasTests
    {
        [Fact]
        public void Canvas_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var canvas = new Canvas();

            // Assert
            canvas.InkWidth.Should().Be(2.5);
            canvas.HighlighterWidth.Should().Be(20);
            canvas.InkAlpha.Should().Be(255);
            canvas.IsShowCursor.Should().BeFalse();
            canvas.EraserSize.Should().Be(2);
            canvas.HideStrokeWhenSelecting.Should().BeTrue();
            canvas.UseAdvancedBezierSmoothing.Should().BeTrue();
            canvas.UseAsyncInkSmoothing.Should().BeTrue();
            canvas.UseHardwareAcceleration.Should().BeTrue();
            canvas.InkSmoothingQuality.Should().Be(2);
            canvas.AutoStraightenLine.Should().BeTrue();
            canvas.AutoStraightenLineThreshold.Should().Be(80);
            canvas.HighPrecisionLineStraighten.Should().BeTrue();
            canvas.LineEndpointSnapping.Should().BeTrue();
            canvas.LineEndpointSnappingThreshold.Should().Be(15);
            canvas.CustomBackgroundColor.Should().Be("#162924");
            canvas.HyperbolaAsymptoteOption.Should().Be(OptionalOperation.Ask);
            canvas.EnablePalmEraser.Should().BeTrue();
            canvas.ClearCanvasAlsoClearImages.Should().BeTrue();
            canvas.EnableInkFade.Should().BeFalse();
            canvas.InkFadeTime.Should().Be(3000);
            canvas.HideInkFadeControlInPenMenu.Should().BeFalse();
        }

        [Fact]
        public void Canvas_InkWidth_CanBeModified()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.InkWidth = 10.0;

            // Assert
            canvas.InkWidth.Should().Be(10.0);
        }

        [Fact]
        public void Canvas_InkFadeTime_AcceptsValidValues()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.InkFadeTime = 5000;

            // Assert
            canvas.InkFadeTime.Should().Be(5000);
        }

        [Theory]
        [InlineData(0, 1, 2)] // Low, Medium, High sensitivity
        public void Canvas_PalmEraserSensitivity_AcceptsValidValues(params int[] validValues)
        {
            // Arrange
            var canvas = new Canvas();

            foreach (var value in validValues)
            {
                // Act
                canvas.PalmEraserSensitivity = value;

                // Assert
                canvas.PalmEraserSensitivity.Should().Be(value);
            }
        }

        [Theory]
        [InlineData(0)] // Area erase
        [InlineData(1)] // Line erase
        [InlineData(2)] // Icon toggle mode
        public void Canvas_EraserType_AcceptsValidValues(int eraserType)
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.EraserType = eraserType;

            // Assert
            canvas.EraserType.Should().Be(eraserType);
        }

        [Fact]
        public void Canvas_Serialization_PreservesAllProperties()
        {
            // Arrange
            var canvas = new Canvas
            {
                InkWidth = 4.5,
                HighlighterWidth = 30,
                InkAlpha = 200,
                IsShowCursor = true,
                InkStyle = 1,
                EraserSize = 5,
                EraserType = 1,
                EraserShapeType = 1,
                EnableInkFade = true,
                InkFadeTime = 4000,
                HideInkFadeControlInPenMenu = true
            };

            // Act
            var json = JsonConvert.SerializeObject(canvas);
            var deserialized = JsonConvert.DeserializeObject<Canvas>(json);

            // Assert
            deserialized.Should().BeEquivalentTo(canvas, options => options.ComparingByMembers<Canvas>());
        }
    }

    public class GestureTests
    {
        [Fact]
        public void Gesture_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var gesture = new Gesture();

            // Assert
            gesture.IsEnableMultiTouchMode.Should().BeFalse();
            gesture.IsEnableTwoFingerZoom.Should().BeTrue();
            gesture.IsEnableTwoFingerTranslate.Should().BeTrue();
            gesture.AutoSwitchTwoFingerGesture.Should().BeTrue();
            gesture.IsEnableTwoFingerRotation.Should().BeFalse();
            gesture.IsEnableTwoFingerRotationOnSelection.Should().BeFalse();
        }

        [Fact]
        public void Gesture_IsEnableTwoFingerGesture_ReturnsTrueWhenAnyGestureEnabled()
        {
            // Arrange
            var gesture = new Gesture
            {
                IsEnableTwoFingerZoom = true,
                IsEnableTwoFingerTranslate = false,
                IsEnableTwoFingerRotation = false
            };

            // Act
            var result = gesture.IsEnableTwoFingerGesture;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Gesture_IsEnableTwoFingerGesture_ReturnsFalseWhenNoGestureEnabled()
        {
            // Arrange
            var gesture = new Gesture
            {
                IsEnableTwoFingerZoom = false,
                IsEnableTwoFingerTranslate = false,
                IsEnableTwoFingerRotation = false
            };

            // Act
            var result = gesture.IsEnableTwoFingerGesture;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Gesture_IsEnableTwoFingerGestureTranslateOrRotation_ReturnsTrueWhenTranslateEnabled()
        {
            // Arrange
            var gesture = new Gesture
            {
                IsEnableTwoFingerTranslate = true,
                IsEnableTwoFingerRotation = false
            };

            // Act
            var result = gesture.IsEnableTwoFingerGestureTranslateOrRotation;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Gesture_IsEnableTwoFingerGestureTranslateOrRotation_ReturnsTrueWhenRotationEnabled()
        {
            // Arrange
            var gesture = new Gesture
            {
                IsEnableTwoFingerTranslate = false,
                IsEnableTwoFingerRotation = true
            };

            // Act
            var result = gesture.IsEnableTwoFingerGestureTranslateOrRotation;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Gesture_IsEnableTwoFingerGestureTranslateOrRotation_ReturnsFalseWhenBothDisabled()
        {
            // Arrange
            var gesture = new Gesture
            {
                IsEnableTwoFingerTranslate = false,
                IsEnableTwoFingerRotation = false
            };

            // Act
            var result = gesture.IsEnableTwoFingerGestureTranslateOrRotation;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Gesture_ComputedProperties_NotSerializedToJson()
        {
            // Arrange
            var gesture = new Gesture
            {
                IsEnableTwoFingerZoom = true,
                IsEnableTwoFingerTranslate = true
            };

            // Act
            var json = JsonConvert.SerializeObject(gesture);

            // Assert
            json.Should().NotContain("IsEnableTwoFingerGesture");
            json.Should().NotContain("IsEnableTwoFingerGestureTranslateOrRotation");
        }
    }

    public class StartupTests
    {
        [Fact]
        public void Startup_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var startup = new Startup();

            // Assert
            startup.IsAutoUpdate.Should().BeTrue();
            startup.IsAutoUpdateWithSilence.Should().BeFalse();
            startup.AutoUpdateWithSilenceStartTime.Should().Be("06:00");
            startup.AutoUpdateWithSilenceEndTime.Should().Be("22:00");
            startup.UpdateChannel.Should().Be(UpdateChannel.Release);
            startup.SkippedVersion.Should().Be("");
            startup.AutoUpdatePauseUntilDate.Should().Be("");
            startup.IsEnableNibMode.Should().BeFalse();
            startup.IsFoldAtStartup.Should().BeFalse();
            startup.CrashAction.Should().Be(0);
        }

        [Theory]
        [InlineData(UpdateChannel.Release)]
        [InlineData(UpdateChannel.Preview)]
        [InlineData(UpdateChannel.Beta)]
        public void Startup_UpdateChannel_AcceptsAllValidValues(UpdateChannel channel)
        {
            // Arrange
            var startup = new Startup();

            // Act
            startup.UpdateChannel = channel;

            // Assert
            startup.UpdateChannel.Should().Be(channel);
        }

        [Fact]
        public void Startup_SkippedVersion_CanBeSet()
        {
            // Arrange
            var startup = new Startup();

            // Act
            startup.SkippedVersion = "1.2.3";

            // Assert
            startup.SkippedVersion.Should().Be("1.2.3");
        }

        [Fact]
        public void Startup_AutoUpdateTimes_CanBeConfigured()
        {
            // Arrange
            var startup = new Startup();

            // Act
            startup.AutoUpdateWithSilenceStartTime = "08:00";
            startup.AutoUpdateWithSilenceEndTime = "20:00";

            // Assert
            startup.AutoUpdateWithSilenceStartTime.Should().Be("08:00");
            startup.AutoUpdateWithSilenceEndTime.Should().Be("20:00");
        }
    }

    public class PowerPointSettingsTests
    {
        [Fact]
        public void PowerPointSettings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var settings = new PowerPointSettings();

            // Assert
            settings.ShowPPTButton.Should().BeTrue();
            settings.PPTButtonsDisplayOption.Should().Be(2222);
            settings.PPTSButtonsOption.Should().Be(221);
            settings.PPTBButtonsOption.Should().Be(121);
            settings.EnablePPTButtonPageClickable.Should().BeTrue();
            settings.EnablePPTButtonLongPressPageTurn.Should().BeTrue();
            settings.PPTLSButtonOpacity.Should().Be(0.5);
            settings.PPTRSButtonOpacity.Should().Be(0.5);
            settings.PPTLBButtonOpacity.Should().Be(0.5);
            settings.PPTRBButtonOpacity.Should().Be(0.5);
            settings.PowerPointSupport.Should().BeTrue();
            settings.IsShowCanvasAtNewSlideShow.Should().BeTrue();
            settings.IsNoClearStrokeOnSelectWhenInPowerPoint.Should().BeTrue();
            settings.IsAutoSaveStrokesInPowerPoint.Should().BeTrue();
            settings.IsNotifyHiddenPage.Should().BeTrue();
            settings.IsNotifyAutoPlayPresentation.Should().BeTrue();
            settings.IsEnableFingerGestureSlideShowControl.Should().BeTrue();
            settings.EnableWppProcessKill.Should().BeTrue();
            settings.EnablePowerPointEnhancement.Should().BeFalse();
            settings.ShowGestureButtonInSlideShow.Should().BeFalse();
            settings.SkipAnimationsWhenGoNext.Should().BeFalse();
            settings.EnablePPTTimeCapsule.Should().BeTrue();
            settings.PPTTimeCapsulePosition.Should().Be(1);
        }

        [Fact]
        public void PowerPointSettings_ButtonOpacity_AcceptsValidRange()
        {
            // Arrange
            var settings = new PowerPointSettings();

            // Act
            settings.PPTLSButtonOpacity = 0.75;
            settings.PPTRSButtonOpacity = 1.0;
            settings.PPTLBButtonOpacity = 0.0;
            settings.PPTRBButtonOpacity = 0.25;

            // Assert
            settings.PPTLSButtonOpacity.Should().Be(0.75);
            settings.PPTRSButtonOpacity.Should().Be(1.0);
            settings.PPTLBButtonOpacity.Should().Be(0.0);
            settings.PPTRBButtonOpacity.Should().Be(0.25);
        }

        [Fact]
        public void PowerPointSettings_ButtonPosition_CanBeConfigured()
        {
            // Arrange
            var settings = new PowerPointSettings();

            // Act
            settings.PPTLSButtonPosition = 10;
            settings.PPTRSButtonPosition = -5;
            settings.PPTLBButtonPosition = 15;
            settings.PPTRBButtonPosition = -10;

            // Assert
            settings.PPTLSButtonPosition.Should().Be(10);
            settings.PPTRSButtonPosition.Should().Be(-5);
            settings.PPTLBButtonPosition.Should().Be(15);
            settings.PPTRBButtonPosition.Should().Be(-10);
        }
    }

    public class AutomationTests
    {
        [Fact]
        public void Automation_IsEnableAutoFold_ReturnsFalseWhenNoAutoFoldEnabled()
        {
            // Arrange
            var automation = new Automation();

            // Act
            var result = automation.IsEnableAutoFold;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Automation_IsEnableAutoFold_ReturnsTrueWhenAnyAutoFoldEnabled()
        {
            // Arrange
            var automation = new Automation
            {
                IsAutoFoldInEasiNote = true
            };

            // Act
            var result = automation.IsEnableAutoFold;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Automation_IsEnableAutoFold_ChecksAllAutoFoldOptions()
        {
            // Arrange & Act & Assert
            var automation = new Automation();

            automation.IsAutoFoldInEasiNote = true;
            automation.IsEnableAutoFold.Should().BeTrue();
            automation.IsAutoFoldInEasiNote = false;

            automation.IsAutoFoldInEasiCamera = true;
            automation.IsEnableAutoFold.Should().BeTrue();
            automation.IsAutoFoldInEasiCamera = false;

            automation.IsAutoFoldInPPTSlideShow = true;
            automation.IsEnableAutoFold.Should().BeTrue();
            automation.IsAutoFoldInPPTSlideShow = false;

            automation.IsAutoFoldInMSWhiteboard = true;
            automation.IsEnableAutoFold.Should().BeTrue();
        }

        [Fact]
        public void Automation_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var automation = new Automation();

            // Assert
            automation.MinimumAutomationStrokeNumber.Should().Be(0);
            automation.AutoSavedStrokesLocation.Should().Contain("Saves");
            automation.AutoDelSavedFiles.Should().BeFalse();
            automation.AutoDelSavedFilesDaysThreshold.Should().Be(15);
            automation.KeepFoldAfterSoftwareExit.Should().BeFalse();
            automation.IsSaveStrokesAsXML.Should().BeFalse();
            automation.IsEnableAutoSaveStrokes.Should().BeTrue();
            automation.AutoSaveStrokesIntervalMinutes.Should().Be(5);
        }

        [Fact]
        public void Automation_AutoSavedStrokesLocation_ContainsSavesFolder()
        {
            // Arrange & Act
            var automation = new Automation();

            // Assert
            automation.AutoSavedStrokesLocation.Should().EndWith("Saves");
            Path.IsPathRooted(automation.AutoSavedStrokesLocation).Should().BeTrue();
        }

        [Fact]
        public void Automation_FloatingWindowInterceptor_IsInitialized()
        {
            // Arrange & Act
            var automation = new Automation();

            // Assert
            automation.FloatingWindowInterceptor.Should().NotBeNull();
            automation.FloatingWindowInterceptor.IsEnabled.Should().BeFalse();
            automation.FloatingWindowInterceptor.ScanIntervalMs.Should().Be(5000);
        }
    }

    public class FloatingWindowInterceptorSettingsTests
    {
        [Fact]
        public void FloatingWindowInterceptorSettings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var settings = new FloatingWindowInterceptorSettings();

            // Assert
            settings.IsEnabled.Should().BeFalse();
            settings.ScanIntervalMs.Should().Be(5000);
            settings.AutoStart.Should().BeFalse();
            settings.ShowNotifications.Should().BeTrue();
        }

        [Fact]
        public void FloatingWindowInterceptorSettings_InterceptRules_AreInitialized()
        {
            // Arrange & Act
            var settings = new FloatingWindowInterceptorSettings();

            // Assert
            settings.InterceptRules.Should().NotBeNull();
            settings.InterceptRules.Should().NotBeEmpty();
            settings.InterceptRules.Should().ContainKey("SeewoWhiteboard3Floating");
            settings.InterceptRules.Should().ContainKey("SeewoWhiteboard5Floating");
            settings.InterceptRules.Should().ContainKey("HiteAnnotationFloating");
        }

        [Fact]
        public void FloatingWindowInterceptorSettings_InterceptRules_AllDefaultToTrue()
        {
            // Arrange & Act
            var settings = new FloatingWindowInterceptorSettings();

            // Assert
            foreach (var rule in settings.InterceptRules)
            {
                rule.Value.Should().BeTrue($"because {rule.Key} should default to enabled");
            }
        }

        [Fact]
        public void FloatingWindowInterceptorSettings_InterceptRules_CanBeModified()
        {
            // Arrange
            var settings = new FloatingWindowInterceptorSettings();

            // Act
            settings.InterceptRules["SeewoWhiteboard3Floating"] = false;

            // Assert
            settings.InterceptRules["SeewoWhiteboard3Floating"].Should().BeFalse();
        }
    }

    public class AdvancedTests
    {
        [Fact]
        public void Advanced_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var advanced = new Advanced();

            // Assert
            advanced.TouchMultiplier.Should().Be(0.25);
            advanced.NibModeBoundsWidth.Should().Be(10);
            advanced.FingerModeBoundsWidth.Should().Be(30);
            advanced.NibModeBoundsWidthThresholdValue.Should().Be(2.5);
            advanced.FingerModeBoundsWidthThresholdValue.Should().Be(2.5);
            advanced.NibModeBoundsWidthEraserSize.Should().Be(0.8);
            advanced.FingerModeBoundsWidthEraserSize.Should().Be(0.8);
            advanced.IsLogEnabled.Should().BeTrue();
            advanced.IsSaveLogByDate.Should().BeTrue();
            advanced.IsAutoBackupBeforeUpdate.Should().BeTrue();
            advanced.IsAutoBackupEnabled.Should().BeTrue();
            advanced.AutoBackupIntervalDays.Should().Be(7);
            advanced.LastAutoBackupTime.Should().Be(DateTime.MinValue);
            advanced.IsNoFocusMode.Should().BeTrue();
            advanced.IsAlwaysOnTop.Should().BeTrue();
            advanced.EnableUIAccessTopMost.Should().BeFalse();
            advanced.WindowMode.Should().BeTrue();
        }

        [Fact]
        public void Advanced_TouchMultiplier_CanBeModified()
        {
            // Arrange
            var advanced = new Advanced();

            // Act
            advanced.TouchMultiplier = 0.5;

            // Assert
            advanced.TouchMultiplier.Should().Be(0.5);
        }

        [Fact]
        public void Advanced_LastAutoBackupTime_CanBeSet()
        {
            // Arrange
            var advanced = new Advanced();
            var now = DateTime.Now;

            // Act
            advanced.LastAutoBackupTime = now;

            // Assert
            advanced.LastAutoBackupTime.Should().Be(now);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(30)]
        public void Advanced_AutoBackupIntervalDays_AcceptsValidValues(int days)
        {
            // Arrange
            var advanced = new Advanced();

            // Act
            advanced.AutoBackupIntervalDays = days;

            // Assert
            advanced.AutoBackupIntervalDays.Should().Be(days);
        }
    }

    public class InkToShapeTests
    {
        [Fact]
        public void InkToShape_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var inkToShape = new InkToShape();

            // Assert
            inkToShape.IsInkToShapeEnabled.Should().BeTrue();
            inkToShape.IsInkToShapeTriangle.Should().BeTrue();
            inkToShape.IsInkToShapeRectangle.Should().BeTrue();
            inkToShape.IsInkToShapeRounded.Should().BeTrue();
            inkToShape.LineStraightenSensitivity.Should().Be(0.20);
            inkToShape.LineNormalizationThreshold.Should().Be(0.5);
        }

        [Fact]
        public void InkToShape_Sensitivity_CanBeAdjusted()
        {
            // Arrange
            var inkToShape = new InkToShape();

            // Act
            inkToShape.LineStraightenSensitivity = 0.5;
            inkToShape.LineNormalizationThreshold = 0.75;

            // Assert
            inkToShape.LineStraightenSensitivity.Should().Be(0.5);
            inkToShape.LineNormalizationThreshold.Should().Be(0.75);
        }

        [Fact]
        public void InkToShape_ShapeOptions_CanBeDisabled()
        {
            // Arrange
            var inkToShape = new InkToShape();

            // Act
            inkToShape.IsInkToShapeEnabled = false;
            inkToShape.IsInkToShapeTriangle = false;
            inkToShape.IsInkToShapeRectangle = false;
            inkToShape.IsInkToShapeRounded = false;

            // Assert
            inkToShape.IsInkToShapeEnabled.Should().BeFalse();
            inkToShape.IsInkToShapeTriangle.Should().BeFalse();
            inkToShape.IsInkToShapeRectangle.Should().BeFalse();
            inkToShape.IsInkToShapeRounded.Should().BeFalse();
        }
    }

    public class RandSettingsTests
    {
        [Fact]
        public void RandSettings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var randSettings = new RandSettings();

            // Assert
            randSettings.RandWindowOnceCloseLatency.Should().Be(2.5);
            randSettings.RandWindowOnceMaxStudents.Should().Be(10);
            randSettings.ShowRandomAndSingleDraw.Should().BeTrue();
            randSettings.UseLegacyTimerUI.Should().BeFalse();
            randSettings.UseNewStyleUI.Should().BeTrue();
            randSettings.TimerVolume.Should().Be(1.0);
            randSettings.CustomTimerSoundPath.Should().Be("");
            randSettings.EnableOvertimeCountUp.Should().BeFalse();
            randSettings.EnableOvertimeRedText.Should().BeFalse();
            randSettings.EnableProgressiveReminder.Should().BeFalse();
            randSettings.ProgressiveReminderVolume.Should().Be(1.0);
            randSettings.UseNewRollCallUI.Should().BeTrue();
            randSettings.EnableMLAvoidance.Should().BeTrue();
            randSettings.MLAvoidanceHistoryCount.Should().Be(50);
            randSettings.MLAvoidanceWeight.Should().Be(1.0);
            randSettings.EnableQuickDraw.Should().BeTrue();
        }

        [Fact]
        public void RandSettings_CustomBackgrounds_IsInitialized()
        {
            // Arrange & Act
            var randSettings = new RandSettings();

            // Assert
            randSettings.CustomPickNameBackgrounds.Should().NotBeNull();
            randSettings.CustomPickNameBackgrounds.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void RandSettings_Volume_AcceptsValidRange(double volume)
        {
            // Arrange
            var randSettings = new RandSettings();

            // Act
            randSettings.TimerVolume = volume;
            randSettings.ProgressiveReminderVolume = volume;

            // Assert
            randSettings.TimerVolume.Should().Be(volume);
            randSettings.ProgressiveReminderVolume.Should().Be(volume);
        }
    }

    public class CustomPickNameBackgroundTests
    {
        [Fact]
        public void CustomPickNameBackground_Constructor_SetsProperties()
        {
            // Arrange & Act
            var background = new CustomPickNameBackground("Test", "/path/to/file.png");

            // Assert
            background.Name.Should().Be("Test");
            background.FilePath.Should().Be("/path/to/file.png");
        }

        [Fact]
        public void CustomPickNameBackground_DefaultConstructor_CreatesEmptyObject()
        {
            // Arrange & Act
            var background = new CustomPickNameBackground();

            // Assert
            background.Name.Should().BeNull();
            background.FilePath.Should().BeNull();
        }

        [Fact]
        public void CustomPickNameBackground_Serialization_PreservesProperties()
        {
            // Arrange
            var background = new CustomPickNameBackground("My Background", "C:\\Images\\bg.jpg");

            // Act
            var json = JsonConvert.SerializeObject(background);
            var deserialized = JsonConvert.DeserializeObject<CustomPickNameBackground>(json);

            // Assert
            deserialized.Name.Should().Be("My Background");
            deserialized.FilePath.Should().Be("C:\\Images\\bg.jpg");
        }
    }

    public class CustomFloatingBarIconTests
    {
        [Fact]
        public void CustomFloatingBarIcon_Constructor_SetsProperties()
        {
            // Arrange & Act
            var icon = new CustomFloatingBarIcon("TestIcon", "/path/to/icon.png");

            // Assert
            icon.Name.Should().Be("TestIcon");
            icon.FilePath.Should().Be("/path/to/icon.png");
        }

        [Fact]
        public void CustomFloatingBarIcon_DefaultConstructor_CreatesEmptyObject()
        {
            // Arrange & Act
            var icon = new CustomFloatingBarIcon();

            // Assert
            icon.Name.Should().BeNull();
            icon.FilePath.Should().BeNull();
        }

        [Fact]
        public void CustomFloatingBarIcon_Serialization_PreservesProperties()
        {
            // Arrange
            var icon = new CustomFloatingBarIcon("Custom Icon", "D:\\Icons\\custom.png");

            // Act
            var json = JsonConvert.SerializeObject(icon);
            var deserialized = JsonConvert.DeserializeObject<CustomFloatingBarIcon>(json);

            // Assert
            deserialized.Name.Should().Be("Custom Icon");
            deserialized.FilePath.Should().Be("D:\\Icons\\custom.png");
        }
    }

    public class ModeSettingsTests
    {
        [Fact]
        public void ModeSettings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var modeSettings = new ModeSettings();

            // Assert
            modeSettings.IsPPTOnlyMode.Should().BeFalse();
        }

        [Fact]
        public void ModeSettings_IsPPTOnlyMode_CanBeEnabled()
        {
            // Arrange
            var modeSettings = new ModeSettings();

            // Act
            modeSettings.IsPPTOnlyMode = true;

            // Assert
            modeSettings.IsPPTOnlyMode.Should().BeTrue();
        }
    }

    public class CameraSettingsTests
    {
        [Fact]
        public void CameraSettings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var cameraSettings = new CameraSettings();

            // Assert
            cameraSettings.RotationAngle.Should().Be(0);
            cameraSettings.ResolutionWidth.Should().Be(1920);
            cameraSettings.ResolutionHeight.Should().Be(1080);
            cameraSettings.SelectedCameraIndex.Should().Be(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(90)]
        [InlineData(180)]
        [InlineData(270)]
        public void CameraSettings_RotationAngle_AcceptsCommonValues(int angle)
        {
            // Arrange
            var cameraSettings = new CameraSettings();

            // Act
            cameraSettings.RotationAngle = angle;

            // Assert
            cameraSettings.RotationAngle.Should().Be(angle);
        }

        [Theory]
        [InlineData(640, 480)]
        [InlineData(1280, 720)]
        [InlineData(1920, 1080)]
        [InlineData(3840, 2160)]
        public void CameraSettings_Resolution_AcceptsCommonValues(int width, int height)
        {
            // Arrange
            var cameraSettings = new CameraSettings();

            // Act
            cameraSettings.ResolutionWidth = width;
            cameraSettings.ResolutionHeight = height;

            // Assert
            cameraSettings.ResolutionWidth.Should().Be(width);
            cameraSettings.ResolutionHeight.Should().Be(height);
        }
    }

    public class DlassSettingsTests
    {
        [Fact]
        public void DlassSettings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var dlassSettings = new DlassSettings();

            // Assert
            dlassSettings.UserToken.Should().Be(string.Empty);
            dlassSettings.SavedTokens.Should().NotBeNull();
            dlassSettings.SavedTokens.Should().BeEmpty();
            dlassSettings.SelectedClassName.Should().Be(string.Empty);
            dlassSettings.ApiBaseUrl.Should().Be("https://dlass.tech");
            dlassSettings.IsAutoUploadNotes.Should().BeFalse();
            dlassSettings.AutoUploadDelayMinutes.Should().Be(0);
        }

        [Fact]
        public void DlassSettings_UserToken_CanBeSet()
        {
            // Arrange
            var dlassSettings = new DlassSettings();

            // Act
            dlassSettings.UserToken = "test-token-123";

            // Assert
            dlassSettings.UserToken.Should().Be("test-token-123");
        }

        [Fact]
        public void DlassSettings_SavedTokens_CanAddMultipleTokens()
        {
            // Arrange
            var dlassSettings = new DlassSettings();

            // Act
            dlassSettings.SavedTokens.Add("token1");
            dlassSettings.SavedTokens.Add("token2");
            dlassSettings.SavedTokens.Add("token3");

            // Assert
            dlassSettings.SavedTokens.Should().HaveCount(3);
            dlassSettings.SavedTokens.Should().Contain("token1");
            dlassSettings.SavedTokens.Should().Contain("token2");
            dlassSettings.SavedTokens.Should().Contain("token3");
        }

        [Fact]
        public void DlassSettings_ApiBaseUrl_CanBeCustomized()
        {
            // Arrange
            var dlassSettings = new DlassSettings();

            // Act
            dlassSettings.ApiBaseUrl = "https://custom.api.url";

            // Assert
            dlassSettings.ApiBaseUrl.Should().Be("https://custom.api.url");
        }
    }

    public class AppearanceTests
    {
        [Fact]
        public void Appearance_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var appearance = new Appearance();

            // Assert
            appearance.IsEnableDisPlayNibModeToggler.Should().BeTrue();
            appearance.ViewboxFloatingBarScaleTransformValue.Should().Be(1.0);
            appearance.ViewboxFloatingBarOpacityValue.Should().Be(1.0);
            appearance.EnableTrayIcon.Should().BeTrue();
            appearance.ViewboxFloatingBarOpacityInPPTValue.Should().Be(0.5);
            appearance.IsTransparentButtonBackground.Should().BeTrue();
            appearance.IsShowExitButton.Should().BeTrue();
            appearance.IsShowEraserButton.Should().BeTrue();
            appearance.EnableTimeDisplayInWhiteboardMode.Should().BeTrue();
            appearance.EnableChickenSoupInWhiteboardMode.Should().BeTrue();
            appearance.EnableSplashScreen.Should().BeFalse();
            appearance.SplashScreenStyle.Should().Be(1);
            appearance.IsShowQuickPanel.Should().BeTrue();
            appearance.ChickenSoupSource.Should().Be(1);
            appearance.IsShowModeFingerToggleSwitch.Should().BeTrue();
            appearance.UseLegacyFloatingBarUI.Should().BeFalse();
            appearance.IsShowShapeButton.Should().BeTrue();
            appearance.IsShowUndoButton.Should().BeTrue();
            appearance.IsShowRedoButton.Should().BeTrue();
            appearance.IsShowClearButton.Should().BeTrue();
            appearance.IsShowWhiteboardButton.Should().BeTrue();
            appearance.IsShowHideButton.Should().BeTrue();
            appearance.IsShowLassoSelectButton.Should().BeTrue();
            appearance.IsShowClearAndMouseButton.Should().BeTrue();
            appearance.QuickColorPaletteDisplayMode.Should().Be(1);
            appearance.EnableHotkeysInMouseMode.Should().BeFalse();
        }

        [Fact]
        public void Appearance_CustomFloatingBarImgs_IsInitialized()
        {
            // Arrange & Act
            var appearance = new Appearance();

            // Assert
            appearance.CustomFloatingBarImgs.Should().NotBeNull();
            appearance.CustomFloatingBarImgs.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(1.5)]
        public void Appearance_FloatingBarScaleTransform_AcceptsValidRange(double scale)
        {
            // Arrange
            var appearance = new Appearance();

            // Act
            appearance.ViewboxFloatingBarScaleTransformValue = scale;

            // Assert
            appearance.ViewboxFloatingBarScaleTransformValue.Should().Be(scale);
        }

        [Theory]
        [InlineData(0, 1, 2, 3, 4, 5, 6)] // Random, Seasonal, Spring, Summer, Autumn, Winter, Horse Year
        public void Appearance_SplashScreenStyle_AcceptsValidValues(params int[] validStyles)
        {
            // Arrange
            var appearance = new Appearance();

            foreach (var style in validStyles)
            {
                // Act
                appearance.SplashScreenStyle = style;

                // Assert
                appearance.SplashScreenStyle.Should().Be(style);
            }
        }
    }

    public class OptionalOperationEnumTests
    {
        [Fact]
        public void OptionalOperation_HasThreeValues()
        {
            // Arrange & Act
            var values = Enum.GetValues(typeof(OptionalOperation));

            // Assert
            values.Length.Should().Be(3);
        }

        [Fact]
        public void OptionalOperation_ContainsExpectedValues()
        {
            // Assert
            Enum.IsDefined(typeof(OptionalOperation), OptionalOperation.Yes).Should().BeTrue();
            Enum.IsDefined(typeof(OptionalOperation), OptionalOperation.No).Should().BeTrue();
            Enum.IsDefined(typeof(OptionalOperation), OptionalOperation.Ask).Should().BeTrue();
        }
    }

    public class UpdateChannelEnumTests
    {
        [Fact]
        public void UpdateChannel_HasThreeValues()
        {
            // Arrange & Act
            var values = Enum.GetValues(typeof(UpdateChannel));

            // Assert
            values.Length.Should().Be(3);
        }

        [Fact]
        public void UpdateChannel_ContainsExpectedValues()
        {
            // Assert
            Enum.IsDefined(typeof(UpdateChannel), UpdateChannel.Release).Should().BeTrue();
            Enum.IsDefined(typeof(UpdateChannel), UpdateChannel.Preview).Should().BeTrue();
            Enum.IsDefined(typeof(UpdateChannel), UpdateChannel.Beta).Should().BeTrue();
        }
    }

    // Edge case and negative tests
    public class SettingsEdgeCaseTests
    {
        [Fact]
        public void Settings_DeserializeWithMissingProperties_UsesDefaults()
        {
            // Arrange
            var json = "{}";

            // Act
            var settings = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            settings.Should().NotBeNull();
            settings.Canvas.Should().NotBeNull();
            settings.Canvas.InkWidth.Should().Be(2.5);
        }

        [Fact]
        public void Canvas_InkWidth_CanBeZero()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.InkWidth = 0.0;

            // Assert
            canvas.InkWidth.Should().Be(0.0);
        }

        [Fact]
        public void Canvas_InkAlpha_AcceptsBoundaryValues()
        {
            // Arrange
            var canvas = new Canvas();

            // Act & Assert - Min boundary
            canvas.InkAlpha = 0;
            canvas.InkAlpha.Should().Be(0);

            // Act & Assert - Max boundary
            canvas.InkAlpha = 255;
            canvas.InkAlpha.Should().Be(255);
        }

        [Fact]
        public void Automation_AutoSavedStrokesLocation_CanBeModified()
        {
            // Arrange
            var automation = new Automation();
            var customPath = "C:\\CustomPath\\Saves";

            // Act
            automation.AutoSavedStrokesLocation = customPath;

            // Assert
            automation.AutoSavedStrokesLocation.Should().Be(customPath);
        }

        [Fact]
        public void FloatingWindowInterceptorSettings_InterceptRules_CanAddNewRule()
        {
            // Arrange
            var settings = new FloatingWindowInterceptorSettings();

            // Act
            settings.InterceptRules.Add("CustomFloatingWindow", false);

            // Assert
            settings.InterceptRules.Should().ContainKey("CustomFloatingWindow");
            settings.InterceptRules["CustomFloatingWindow"].Should().BeFalse();
        }

        [Fact]
        public void Settings_ComplexObjectSerialization_PreservesNestedStructures()
        {
            // Arrange
            var settings = new Settings();
            settings.Canvas.InkWidth = 7.5;
            settings.Gesture.IsEnableTwoFingerZoom = false;
            settings.Startup.UpdateChannel = UpdateChannel.Beta;
            settings.Automation.FloatingWindowInterceptor.IsEnabled = true;
            settings.Automation.FloatingWindowInterceptor.InterceptRules["CustomRule"] = false;

            // Act
            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            deserialized.Canvas.InkWidth.Should().Be(7.5);
            deserialized.Gesture.IsEnableTwoFingerZoom.Should().BeFalse();
            deserialized.Startup.UpdateChannel.Should().Be(UpdateChannel.Beta);
            deserialized.Automation.FloatingWindowInterceptor.IsEnabled.Should().BeTrue();
            deserialized.Automation.FloatingWindowInterceptor.InterceptRules.Should().ContainKey("CustomRule");
        }
    }
}