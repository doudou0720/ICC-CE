using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Ink_Canvas;

namespace Ink_Canvas.Tests.Resources
{
    [TestClass]
    public class SettingsTests
    {
        private string _testSettingsDirectory;
        private string _testSettingsPath;

        [TestInitialize]
        public void Setup()
        {
            _testSettingsDirectory = Path.Combine(Path.GetTempPath(), "InkCanvasTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSettingsDirectory);
            _testSettingsPath = Path.Combine(_testSettingsDirectory, "settings.json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testSettingsDirectory))
            {
                Directory.Delete(_testSettingsDirectory, true);
            }
        }

        #region Settings Class Tests

        [TestMethod]
        public void Settings_DefaultConstructor_InitializesAllProperties()
        {
            // Arrange & Act
            var settings = new Settings();

            // Assert
            Assert.IsNotNull(settings.Advanced);
            Assert.IsNotNull(settings.Appearance);
            Assert.IsNotNull(settings.Automation);
            Assert.IsNotNull(settings.PowerPointSettings);
            Assert.IsNotNull(settings.Canvas);
            Assert.IsNotNull(settings.Gesture);
            Assert.IsNotNull(settings.InkToShape);
            Assert.IsNotNull(settings.Startup);
            Assert.IsNotNull(settings.RandSettings);
            Assert.IsNotNull(settings.ModeSettings);
            Assert.IsNotNull(settings.Camera);
            Assert.IsNotNull(settings.Dlass);
        }

        [TestMethod]
        public void Settings_Serialization_PreservesAllProperties()
        {
            // Arrange
            var settings = new Settings();
            settings.Canvas.InkWidth = 5.0;
            settings.Canvas.HighlighterWidth = 25.0;
            settings.Startup.IsAutoUpdate = false;
            settings.Appearance.Theme = 1;

            // Act
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            var deserializedSettings = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            Assert.AreEqual(5.0, deserializedSettings.Canvas.InkWidth);
            Assert.AreEqual(25.0, deserializedSettings.Canvas.HighlighterWidth);
            Assert.AreEqual(false, deserializedSettings.Startup.IsAutoUpdate);
            Assert.AreEqual(1, deserializedSettings.Appearance.Theme);
        }

        [TestMethod]
        public void Settings_SerializationToFile_WorksCorrectly()
        {
            // Arrange
            var settings = new Settings();
            settings.Canvas.InkWidth = 3.5;
            settings.PowerPointSettings.PowerPointSupport = false;

            // Act
            File.WriteAllText(_testSettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            var loadedSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_testSettingsPath));

            // Assert
            Assert.IsNotNull(loadedSettings);
            Assert.AreEqual(3.5, loadedSettings.Canvas.InkWidth);
            Assert.AreEqual(false, loadedSettings.PowerPointSettings.PowerPointSupport);
        }

        #endregion

        #region Canvas Tests

        [TestMethod]
        public void Canvas_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var canvas = new Canvas();

            // Assert
            Assert.AreEqual(2.5, canvas.InkWidth);
            Assert.AreEqual(20, canvas.HighlighterWidth);
            Assert.AreEqual(255, canvas.InkAlpha);
            Assert.AreEqual(2, canvas.EraserSize);
            Assert.AreEqual(true, canvas.HideStrokeWhenSelecting);
            Assert.AreEqual(true, canvas.UseAdvancedBezierSmoothing);
            Assert.AreEqual(true, canvas.UseAsyncInkSmoothing);
            Assert.AreEqual(true, canvas.UseHardwareAcceleration);
            Assert.AreEqual(2, canvas.InkSmoothingQuality);
            Assert.AreEqual(true, canvas.AutoStraightenLine);
            Assert.AreEqual(80, canvas.AutoStraightenLineThreshold);
            Assert.AreEqual(true, canvas.HighPrecisionLineStraighten);
            Assert.AreEqual(true, canvas.LineEndpointSnapping);
            Assert.AreEqual(15, canvas.LineEndpointSnappingThreshold);
            Assert.AreEqual("#162924", canvas.CustomBackgroundColor);
            Assert.AreEqual(OptionalOperation.Ask, canvas.HyperbolaAsymptoteOption);
            Assert.AreEqual(true, canvas.EnablePalmEraser);
            Assert.AreEqual(0, canvas.PalmEraserSensitivity);
            Assert.AreEqual(true, canvas.ClearCanvasAlsoClearImages);
            Assert.AreEqual(false, canvas.EnableInkFade);
            Assert.AreEqual(3000, canvas.InkFadeTime);
            Assert.AreEqual(false, canvas.HideInkFadeControlInPenMenu);
        }

        [TestMethod]
        public void Canvas_InkWidthRange_AcceptsValidValues()
        {
            // Arrange
            var canvas = new Canvas();

            // Act & Assert
            canvas.InkWidth = 1.0;
            Assert.AreEqual(1.0, canvas.InkWidth);

            canvas.InkWidth = 10.0;
            Assert.AreEqual(10.0, canvas.InkWidth);

            canvas.InkWidth = 0.5;
            Assert.AreEqual(0.5, canvas.InkWidth);
        }

        [TestMethod]
        public void Canvas_EraserSizeEnum_WorksCorrectly()
        {
            // Arrange
            var canvas = new Canvas();

            // Act & Assert - Test all eraser sizes (0-4 typically: VerySmall, Small, Medium, Large, VeryLarge)
            for (int i = 0; i <= 4; i++)
            {
                canvas.EraserSize = i;
                Assert.AreEqual(i, canvas.EraserSize);
            }
        }

        [TestMethod]
        public void Canvas_InkFadeTime_BoundaryValues()
        {
            // Arrange
            var canvas = new Canvas();

            // Act & Assert
            canvas.InkFadeTime = 1000;
            Assert.AreEqual(1000, canvas.InkFadeTime);

            canvas.InkFadeTime = 10000;
            Assert.AreEqual(10000, canvas.InkFadeTime);

            canvas.InkFadeTime = 0;
            Assert.AreEqual(0, canvas.InkFadeTime);
        }

        [TestMethod]
        public void Canvas_CustomBackgroundColor_HandlesHexColors()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.CustomBackgroundColor = "#FF5733";

            // Assert
            Assert.AreEqual("#FF5733", canvas.CustomBackgroundColor);
        }

        #endregion

        #region Gesture Tests

        [TestMethod]
        public void Gesture_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var gesture = new Gesture();

            // Assert
            Assert.AreEqual(false, gesture.IsEnableMultiTouchMode);
            Assert.AreEqual(true, gesture.IsEnableTwoFingerZoom);
            Assert.AreEqual(true, gesture.IsEnableTwoFingerTranslate);
            Assert.AreEqual(true, gesture.AutoSwitchTwoFingerGesture);
            Assert.AreEqual(false, gesture.IsEnableTwoFingerRotation);
            Assert.AreEqual(false, gesture.IsEnableTwoFingerRotationOnSelection);
        }

        [TestMethod]
        public void Gesture_IsEnableTwoFingerGesture_ComputedProperty()
        {
            // Arrange
            var gesture = new Gesture();

            // Act & Assert - All disabled
            gesture.IsEnableTwoFingerZoom = false;
            gesture.IsEnableTwoFingerTranslate = false;
            gesture.IsEnableTwoFingerRotation = false;
            Assert.IsFalse(gesture.IsEnableTwoFingerGesture);

            // Only zoom enabled
            gesture.IsEnableTwoFingerZoom = true;
            Assert.IsTrue(gesture.IsEnableTwoFingerGesture);

            // Only translate enabled
            gesture.IsEnableTwoFingerZoom = false;
            gesture.IsEnableTwoFingerTranslate = true;
            Assert.IsTrue(gesture.IsEnableTwoFingerGesture);

            // Only rotation enabled
            gesture.IsEnableTwoFingerTranslate = false;
            gesture.IsEnableTwoFingerRotation = true;
            Assert.IsTrue(gesture.IsEnableTwoFingerGesture);
        }

        [TestMethod]
        public void Gesture_IsEnableTwoFingerGestureTranslateOrRotation_ComputedProperty()
        {
            // Arrange
            var gesture = new Gesture();

            // Act & Assert
            gesture.IsEnableTwoFingerTranslate = false;
            gesture.IsEnableTwoFingerRotation = false;
            Assert.IsFalse(gesture.IsEnableTwoFingerGestureTranslateOrRotation);

            gesture.IsEnableTwoFingerTranslate = true;
            Assert.IsTrue(gesture.IsEnableTwoFingerGestureTranslateOrRotation);

            gesture.IsEnableTwoFingerTranslate = false;
            gesture.IsEnableTwoFingerRotation = true;
            Assert.IsTrue(gesture.IsEnableTwoFingerGestureTranslateOrRotation);

            gesture.IsEnableTwoFingerTranslate = true;
            gesture.IsEnableTwoFingerRotation = true;
            Assert.IsTrue(gesture.IsEnableTwoFingerGestureTranslateOrRotation);
        }

        #endregion

        #region Startup Tests

        [TestMethod]
        public void Startup_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var startup = new Startup();

            // Assert
            Assert.AreEqual(true, startup.IsAutoUpdate);
            Assert.AreEqual(false, startup.IsAutoUpdateWithSilence);
            Assert.AreEqual("06:00", startup.AutoUpdateWithSilenceStartTime);
            Assert.AreEqual("22:00", startup.AutoUpdateWithSilenceEndTime);
            Assert.AreEqual(UpdateChannel.Release, startup.UpdateChannel);
            Assert.AreEqual("", startup.SkippedVersion);
            Assert.AreEqual("", startup.AutoUpdatePauseUntilDate);
            Assert.AreEqual(false, startup.IsEnableNibMode);
            Assert.AreEqual(false, startup.IsFoldAtStartup);
            Assert.AreEqual(0, startup.CrashAction);
        }

        [TestMethod]
        public void Startup_UpdateChannel_AllValues()
        {
            // Arrange
            var startup = new Startup();

            // Act & Assert
            startup.UpdateChannel = UpdateChannel.Release;
            Assert.AreEqual(UpdateChannel.Release, startup.UpdateChannel);

            startup.UpdateChannel = UpdateChannel.Preview;
            Assert.AreEqual(UpdateChannel.Preview, startup.UpdateChannel);

            startup.UpdateChannel = UpdateChannel.Beta;
            Assert.AreEqual(UpdateChannel.Beta, startup.UpdateChannel);
        }

        [TestMethod]
        public void Startup_TimeStrings_StoreCorrectly()
        {
            // Arrange
            var startup = new Startup();

            // Act
            startup.AutoUpdateWithSilenceStartTime = "08:30";
            startup.AutoUpdateWithSilenceEndTime = "23:45";

            // Assert
            Assert.AreEqual("08:30", startup.AutoUpdateWithSilenceStartTime);
            Assert.AreEqual("23:45", startup.AutoUpdateWithSilenceEndTime);
        }

        #endregion

        #region Appearance Tests

        [TestMethod]
        public void Appearance_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var appearance = new Appearance();

            // Assert
            Assert.AreEqual(true, appearance.IsEnableDisPlayNibModeToggler);
            Assert.AreEqual(false, appearance.IsColorfulViewboxFloatingBar);
            Assert.AreEqual(1.0, appearance.ViewboxFloatingBarScaleTransformValue);
            Assert.AreEqual(0, appearance.FloatingBarImg);
            Assert.IsNotNull(appearance.CustomFloatingBarImgs);
            Assert.AreEqual(1.0, appearance.ViewboxFloatingBarOpacityValue);
            Assert.AreEqual(true, appearance.EnableTrayIcon);
            Assert.AreEqual(0.5, appearance.ViewboxFloatingBarOpacityInPPTValue);
            Assert.AreEqual(false, appearance.EnableViewboxBlackBoardScaleTransform);
            Assert.AreEqual(true, appearance.IsTransparentButtonBackground);
            Assert.AreEqual(true, appearance.IsShowExitButton);
            Assert.AreEqual(true, appearance.IsShowEraserButton);
            Assert.AreEqual(true, appearance.EnableTimeDisplayInWhiteboardMode);
            Assert.AreEqual(true, appearance.EnableChickenSoupInWhiteboardMode);
            Assert.AreEqual(false, appearance.IsShowHideControlButton);
            Assert.AreEqual(0, appearance.UnFoldButtonImageType);
            Assert.AreEqual(false, appearance.IsShowLRSwitchButton);
            Assert.AreEqual(false, appearance.EnableSplashScreen);
            Assert.AreEqual(1, appearance.SplashScreenStyle);
            Assert.AreEqual(true, appearance.IsShowQuickPanel);
            Assert.AreEqual(1, appearance.ChickenSoupSource);
            Assert.AreEqual(true, appearance.IsShowModeFingerToggleSwitch);
            Assert.AreEqual(0, appearance.Theme);
            Assert.AreEqual(false, appearance.UseLegacyFloatingBarUI);
            Assert.AreEqual(true, appearance.IsShowShapeButton);
            Assert.AreEqual(true, appearance.IsShowUndoButton);
            Assert.AreEqual(true, appearance.IsShowRedoButton);
            Assert.AreEqual(true, appearance.IsShowClearButton);
            Assert.AreEqual(true, appearance.IsShowWhiteboardButton);
            Assert.AreEqual(true, appearance.IsShowHideButton);
            Assert.AreEqual(true, appearance.IsShowLassoSelectButton);
            Assert.AreEqual(true, appearance.IsShowClearAndMouseButton);
            Assert.AreEqual(0, appearance.EraserDisplayOption);
            Assert.AreEqual(false, appearance.IsShowQuickColorPalette);
            Assert.AreEqual(1, appearance.QuickColorPaletteDisplayMode);
            Assert.AreEqual(false, appearance.EnableHotkeysInMouseMode);
        }

        [TestMethod]
        public void Appearance_CustomFloatingBarImgs_InitializesAsEmptyList()
        {
            // Arrange & Act
            var appearance = new Appearance();

            // Assert
            Assert.IsNotNull(appearance.CustomFloatingBarImgs);
            Assert.AreEqual(0, appearance.CustomFloatingBarImgs.Count);
        }

        [TestMethod]
        public void Appearance_OpacityValues_WithinValidRange()
        {
            // Arrange
            var appearance = new Appearance();

            // Act
            appearance.ViewboxFloatingBarOpacityValue = 0.5;
            appearance.ViewboxFloatingBarOpacityInPPTValue = 0.75;

            // Assert
            Assert.AreEqual(0.5, appearance.ViewboxFloatingBarOpacityValue);
            Assert.AreEqual(0.75, appearance.ViewboxFloatingBarOpacityInPPTValue);
        }

        #endregion

        #region PowerPointSettings Tests

        [TestMethod]
        public void PowerPointSettings_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var pptSettings = new PowerPointSettings();

            // Assert
            Assert.AreEqual(true, pptSettings.ShowPPTButton);
            Assert.AreEqual(2222, pptSettings.PPTButtonsDisplayOption);
            Assert.AreEqual(0, pptSettings.PPTLSButtonPosition);
            Assert.AreEqual(0, pptSettings.PPTRSButtonPosition);
            Assert.AreEqual(0, pptSettings.PPTLBButtonPosition);
            Assert.AreEqual(0, pptSettings.PPTRBButtonPosition);
            Assert.AreEqual(221, pptSettings.PPTSButtonsOption);
            Assert.AreEqual(121, pptSettings.PPTBButtonsOption);
            Assert.AreEqual(true, pptSettings.EnablePPTButtonPageClickable);
            Assert.AreEqual(true, pptSettings.EnablePPTButtonLongPressPageTurn);
            Assert.AreEqual(0.5, pptSettings.PPTLSButtonOpacity);
            Assert.AreEqual(0.5, pptSettings.PPTRSButtonOpacity);
            Assert.AreEqual(0.5, pptSettings.PPTLBButtonOpacity);
            Assert.AreEqual(0.5, pptSettings.PPTRBButtonOpacity);
            Assert.AreEqual(true, pptSettings.PowerPointSupport);
            Assert.AreEqual(true, pptSettings.IsShowCanvasAtNewSlideShow);
            Assert.AreEqual(true, pptSettings.IsNoClearStrokeOnSelectWhenInPowerPoint);
            Assert.AreEqual(false, pptSettings.IsShowStrokeOnSelectInPowerPoint);
            Assert.AreEqual(true, pptSettings.IsAutoSaveStrokesInPowerPoint);
            Assert.AreEqual(false, pptSettings.IsAutoSaveScreenShotInPowerPoint);
            Assert.AreEqual(false, pptSettings.IsNotifyPreviousPage);
            Assert.AreEqual(true, pptSettings.IsNotifyHiddenPage);
            Assert.AreEqual(true, pptSettings.IsNotifyAutoPlayPresentation);
            Assert.AreEqual(false, pptSettings.IsEnableTwoFingerGestureInPresentationMode);
            Assert.AreEqual(true, pptSettings.IsEnableFingerGestureSlideShowControl);
            Assert.AreEqual(false, pptSettings.IsSupportWPS);
            Assert.AreEqual(true, pptSettings.EnableWppProcessKill);
            Assert.AreEqual(false, pptSettings.IsAlwaysGoToFirstPageOnReenter);
            Assert.AreEqual(false, pptSettings.EnablePowerPointEnhancement);
            Assert.AreEqual(false, pptSettings.ShowGestureButtonInSlideShow);
            Assert.AreEqual(false, pptSettings.SkipAnimationsWhenGoNext);
            Assert.AreEqual(true, pptSettings.EnablePPTTimeCapsule);
            Assert.AreEqual(1, pptSettings.PPTTimeCapsulePosition);
        }

        [TestMethod]
        public void PowerPointSettings_ButtonPositions_AcceptNegativeAndPositiveValues()
        {
            // Arrange
            var pptSettings = new PowerPointSettings();

            // Act
            pptSettings.PPTLSButtonPosition = -50;
            pptSettings.PPTRSButtonPosition = 100;
            pptSettings.PPTLBButtonPosition = -25;
            pptSettings.PPTRBButtonPosition = 75;

            // Assert
            Assert.AreEqual(-50, pptSettings.PPTLSButtonPosition);
            Assert.AreEqual(100, pptSettings.PPTRSButtonPosition);
            Assert.AreEqual(-25, pptSettings.PPTLBButtonPosition);
            Assert.AreEqual(75, pptSettings.PPTRBButtonPosition);
        }

        #endregion

        #region Automation Tests

        [TestMethod]
        public void Automation_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var automation = new Automation();

            // Assert
            Assert.AreEqual(false, automation.IsAutoEnterAnnotationModeWhenExitFoldMode);
            Assert.AreEqual(false, automation.IsAutoFoldWhenExitWhiteboard);
            Assert.AreEqual(0, automation.MinimumAutomationStrokeNumber);
            Assert.AreEqual(false, automation.AutoDelSavedFiles);
            Assert.AreEqual(15, automation.AutoDelSavedFilesDaysThreshold);
            Assert.AreEqual(false, automation.KeepFoldAfterSoftwareExit);
            Assert.AreEqual(false, automation.IsSaveFullPageStrokes);
            Assert.AreEqual(false, automation.IsSaveStrokesAsXML);
            Assert.AreEqual(false, automation.IsAutoEnterAnnotationAfterKillHite);
            Assert.AreEqual(true, automation.IsEnableAutoSaveStrokes);
            Assert.AreEqual(5, automation.AutoSaveStrokesIntervalMinutes);
            Assert.IsNotNull(automation.FloatingWindowInterceptor);
        }

        [TestMethod]
        public void Automation_IsEnableAutoFold_ComputedProperty()
        {
            // Arrange
            var automation = new Automation();

            // Act & Assert - All disabled
            Assert.IsFalse(automation.IsEnableAutoFold);

            // Enable one option
            automation.IsAutoFoldInPPTSlideShow = true;
            Assert.IsTrue(automation.IsEnableAutoFold);

            // Enable another option
            automation.IsAutoFoldInPPTSlideShow = false;
            automation.IsAutoFoldInEasiNote = true;
            Assert.IsTrue(automation.IsEnableAutoFold);

            // Enable multiple options
            automation.IsAutoFoldInPPTSlideShow = true;
            automation.IsAutoFoldInMSWhiteboard = true;
            Assert.IsTrue(automation.IsEnableAutoFold);
        }

        [TestMethod]
        public void Automation_AutoSavedStrokesLocation_HasDefaultPath()
        {
            // Arrange & Act
            var automation = new Automation();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(automation.AutoSavedStrokesLocation));
            Assert.IsTrue(automation.AutoSavedStrokesLocation.Contains("Saves"));
        }

        #endregion

        #region FloatingWindowInterceptorSettings Tests

        [TestMethod]
        public void FloatingWindowInterceptorSettings_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var interceptor = new FloatingWindowInterceptorSettings();

            // Assert
            Assert.AreEqual(false, interceptor.IsEnabled);
            Assert.AreEqual(5000, interceptor.ScanIntervalMs);
            Assert.AreEqual(false, interceptor.AutoStart);
            Assert.AreEqual(true, interceptor.ShowNotifications);
            Assert.IsNotNull(interceptor.InterceptRules);
            Assert.IsTrue(interceptor.InterceptRules.Count > 0);
        }

        [TestMethod]
        public void FloatingWindowInterceptorSettings_InterceptRules_ContainsExpectedKeys()
        {
            // Arrange & Act
            var interceptor = new FloatingWindowInterceptorSettings();

            // Assert
            Assert.IsTrue(interceptor.InterceptRules.ContainsKey("SeewoWhiteboard3Floating"));
            Assert.IsTrue(interceptor.InterceptRules.ContainsKey("SeewoPPTFloating"));
            Assert.IsTrue(interceptor.InterceptRules.ContainsKey("HiteAnnotationFloating"));
            Assert.IsTrue(interceptor.InterceptRules.ContainsKey("ChangYanFloating"));
        }

        #endregion

        #region Advanced Tests

        [TestMethod]
        public void Advanced_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var advanced = new Advanced();

            // Assert
            Assert.AreEqual(false, advanced.IsSpecialScreen);
            Assert.AreEqual(false, advanced.IsQuadIR);
            Assert.AreEqual(0.25, advanced.TouchMultiplier);
            Assert.AreEqual(10, advanced.NibModeBoundsWidth);
            Assert.AreEqual(30, advanced.FingerModeBoundsWidth);
            Assert.AreEqual(2.5, advanced.NibModeBoundsWidthThresholdValue);
            Assert.AreEqual(2.5, advanced.FingerModeBoundsWidthThresholdValue);
            Assert.AreEqual(0.8, advanced.NibModeBoundsWidthEraserSize);
            Assert.AreEqual(0.8, advanced.FingerModeBoundsWidthEraserSize);
            Assert.AreEqual(false, advanced.EraserBindTouchMultiplier);
            Assert.AreEqual(true, advanced.IsLogEnabled);
            Assert.AreEqual(true, advanced.IsSaveLogByDate);
            Assert.AreEqual(false, advanced.IsEnableFullScreenHelper);
            Assert.AreEqual(false, advanced.IsEnableEdgeGestureUtil);
            Assert.AreEqual(false, advanced.EdgeGestureUtilOnlyAffectBlackboardMode);
            Assert.AreEqual(false, advanced.IsEnableForceFullScreen);
            Assert.AreEqual(false, advanced.IsEnableResolutionChangeDetection);
            Assert.AreEqual(false, advanced.IsEnableDPIChangeDetection);
            Assert.AreEqual(false, advanced.IsSecondConfirmWhenShutdownApp);
            Assert.AreEqual(false, advanced.IsEnableAvoidFullScreenHelper);
            Assert.AreEqual(true, advanced.IsAutoBackupBeforeUpdate);
            Assert.AreEqual(true, advanced.IsAutoBackupEnabled);
            Assert.AreEqual(7, advanced.AutoBackupIntervalDays);
            Assert.AreEqual(DateTime.MinValue, advanced.LastAutoBackupTime);
            Assert.AreEqual(true, advanced.IsNoFocusMode);
            Assert.AreEqual(true, advanced.IsAlwaysOnTop);
            Assert.AreEqual(false, advanced.EnableUIAccessTopMost);
            Assert.AreEqual(true, advanced.WindowMode);
        }

        #endregion

        #region InkToShape Tests

        [TestMethod]
        public void InkToShape_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var inkToShape = new InkToShape();

            // Assert
            Assert.AreEqual(true, inkToShape.IsInkToShapeEnabled);
            Assert.AreEqual(false, inkToShape.IsInkToShapeNoFakePressureRectangle);
            Assert.AreEqual(false, inkToShape.IsInkToShapeNoFakePressureTriangle);
            Assert.AreEqual(true, inkToShape.IsInkToShapeTriangle);
            Assert.AreEqual(true, inkToShape.IsInkToShapeRectangle);
            Assert.AreEqual(true, inkToShape.IsInkToShapeRounded);
            Assert.AreEqual(0.20, inkToShape.LineStraightenSensitivity);
            Assert.AreEqual(0.5, inkToShape.LineNormalizationThreshold);
        }

        #endregion

        #region RandSettings Tests

        [TestMethod]
        public void RandSettings_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var randSettings = new RandSettings();

            // Assert
            Assert.AreEqual(false, randSettings.DisplayRandWindowNamesInputBtn);
            Assert.AreEqual(2.5, randSettings.RandWindowOnceCloseLatency);
            Assert.AreEqual(10, randSettings.RandWindowOnceMaxStudents);
            Assert.AreEqual(true, randSettings.ShowRandomAndSingleDraw);
            Assert.AreEqual(false, randSettings.DirectCallCiRand);
            Assert.AreEqual(0, randSettings.ExternalCallerType);
            Assert.AreEqual(0, randSettings.SelectedBackgroundIndex);
            Assert.IsNotNull(randSettings.CustomPickNameBackgrounds);
            Assert.AreEqual(false, randSettings.UseLegacyTimerUI);
            Assert.AreEqual(true, randSettings.UseNewStyleUI);
            Assert.AreEqual(1.0, randSettings.TimerVolume);
            Assert.AreEqual("", randSettings.CustomTimerSoundPath);
            Assert.AreEqual(false, randSettings.EnableOvertimeCountUp);
            Assert.AreEqual(false, randSettings.EnableOvertimeRedText);
            Assert.AreEqual(false, randSettings.EnableProgressiveReminder);
            Assert.AreEqual(1.0, randSettings.ProgressiveReminderVolume);
            Assert.AreEqual("", randSettings.ProgressiveReminderSoundPath);
            Assert.AreEqual(true, randSettings.UseNewRollCallUI);
            Assert.AreEqual(true, randSettings.EnableMLAvoidance);
            Assert.AreEqual(50, randSettings.MLAvoidanceHistoryCount);
            Assert.AreEqual(1.0, randSettings.MLAvoidanceWeight);
            Assert.AreEqual(true, randSettings.EnableQuickDraw);
        }

        #endregion

        #region CustomPickNameBackground Tests

        [TestMethod]
        public void CustomPickNameBackground_Constructor_SetsProperties()
        {
            // Arrange & Act
            var background = new CustomPickNameBackground("Test Background", "/path/to/image.png");

            // Assert
            Assert.AreEqual("Test Background", background.Name);
            Assert.AreEqual("/path/to/image.png", background.FilePath);
        }

        [TestMethod]
        public void CustomPickNameBackground_DefaultConstructor_ForJsonSerialization()
        {
            // Arrange & Act
            var background = new CustomPickNameBackground();

            // Assert
            Assert.IsNull(background.Name);
            Assert.IsNull(background.FilePath);
        }

        #endregion

        #region CustomFloatingBarIcon Tests

        [TestMethod]
        public void CustomFloatingBarIcon_Constructor_SetsProperties()
        {
            // Arrange & Act
            var icon = new CustomFloatingBarIcon("Custom Icon", "/path/to/icon.png");

            // Assert
            Assert.AreEqual("Custom Icon", icon.Name);
            Assert.AreEqual("/path/to/icon.png", icon.FilePath);
        }

        [TestMethod]
        public void CustomFloatingBarIcon_DefaultConstructor_ForJsonSerialization()
        {
            // Arrange & Act
            var icon = new CustomFloatingBarIcon();

            // Assert
            Assert.IsNull(icon.Name);
            Assert.IsNull(icon.FilePath);
        }

        #endregion

        #region ModeSettings Tests

        [TestMethod]
        public void ModeSettings_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var modeSettings = new ModeSettings();

            // Assert
            Assert.AreEqual(false, modeSettings.IsPPTOnlyMode);
        }

        #endregion

        #region CameraSettings Tests

        [TestMethod]
        public void CameraSettings_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var cameraSettings = new CameraSettings();

            // Assert
            Assert.AreEqual(0, cameraSettings.RotationAngle);
            Assert.AreEqual(1920, cameraSettings.ResolutionWidth);
            Assert.AreEqual(1080, cameraSettings.ResolutionHeight);
            Assert.AreEqual(0, cameraSettings.SelectedCameraIndex);
        }

        #endregion

        #region DlassSettings Tests

        [TestMethod]
        public void DlassSettings_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var dlassSettings = new DlassSettings();

            // Assert
            Assert.AreEqual(string.Empty, dlassSettings.UserToken);
            Assert.IsNotNull(dlassSettings.SavedTokens);
            Assert.AreEqual(0, dlassSettings.SavedTokens.Count);
            Assert.AreEqual(string.Empty, dlassSettings.SelectedClassName);
            Assert.AreEqual("https://dlass.tech", dlassSettings.ApiBaseUrl);
            Assert.AreEqual(false, dlassSettings.IsAutoUploadNotes);
            Assert.AreEqual(0, dlassSettings.AutoUploadDelayMinutes);
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void Settings_EmptyJsonDeserialization_UsesDefaultValues()
        {
            // Arrange
            var json = "{}";

            // Act
            var settings = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.Canvas);
            Assert.IsNotNull(settings.Gesture);
        }

        [TestMethod]
        public void Settings_PartialJsonDeserialization_FillsMissingWithDefaults()
        {
            // Arrange
            var json = @"{""canvas"":{""inkWidth"":5.0}}";

            // Act
            var settings = JsonConvert.DeserializeObject<Settings>(json);

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(5.0, settings.Canvas.InkWidth);
            Assert.AreEqual(20, settings.Canvas.HighlighterWidth); // Default value
        }

        [TestMethod]
        public void Canvas_InkAlphaMaxValue_IsValid()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.InkAlpha = 255;

            // Assert
            Assert.AreEqual(255, canvas.InkAlpha);
        }

        [TestMethod]
        public void Canvas_InkAlphaMinValue_IsValid()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.InkAlpha = 0;

            // Assert
            Assert.AreEqual(0, canvas.InkAlpha);
        }

        [TestMethod]
        public void OptionalOperation_Enum_AllValues()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0, (int)OptionalOperation.Yes);
            Assert.AreEqual(1, (int)OptionalOperation.No);
            Assert.AreEqual(2, (int)OptionalOperation.Ask);
        }

        [TestMethod]
        public void UpdateChannel_Enum_AllValues()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0, (int)UpdateChannel.Release);
            Assert.AreEqual(1, (int)UpdateChannel.Preview);
            Assert.AreEqual(2, (int)UpdateChannel.Beta);
        }

        #endregion

        #region Negative Test Cases

        [TestMethod]
        public void Canvas_InkWidth_NegativeValue_StillStored()
        {
            // Arrange
            var canvas = new Canvas();

            // Act
            canvas.InkWidth = -1.0;

            // Assert - No validation in the model, so negative values are stored
            Assert.AreEqual(-1.0, canvas.InkWidth);
        }

        [TestMethod]
        public void FloatingWindowInterceptorSettings_ModifyInterceptRules()
        {
            // Arrange
            var interceptor = new FloatingWindowInterceptorSettings();

            // Act
            interceptor.InterceptRules["TestWindow"] = true;
            interceptor.InterceptRules["SeewoWhiteboard3Floating"] = false;

            // Assert
            Assert.IsTrue(interceptor.InterceptRules.ContainsKey("TestWindow"));
            Assert.AreEqual(true, interceptor.InterceptRules["TestWindow"]);
            Assert.AreEqual(false, interceptor.InterceptRules["SeewoWhiteboard3Floating"]);
        }

        #endregion
    }
}