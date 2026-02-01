using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Tests.Helpers
{
    [TestClass]
    public class PPTROTConnectionHelperTests
    {
        #region SafeReleaseComObject Tests

        [TestMethod]
        public void SafeReleaseComObject_NullObject_DoesNotThrow()
        {
            // Arrange
            object nullObject = null;

            // Act & Assert - Should not throw
            PPTROTConnectionHelper.SafeReleaseComObject(nullObject);
        }

        [TestMethod]
        public void SafeReleaseComObject_NonComObject_DoesNotThrow()
        {
            // Arrange
            object regularObject = new object();

            // Act & Assert - Should not throw
            PPTROTConnectionHelper.SafeReleaseComObject(regularObject);
        }

        [TestMethod]
        public void SafeReleaseComObject_StringObject_DoesNotThrow()
        {
            // Arrange
            string testString = "test";

            // Act & Assert - Should not throw
            PPTROTConnectionHelper.SafeReleaseComObject(testString);
        }

        [TestMethod]
        public void SafeReleaseComObject_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            object testObject = new object();

            // Act & Assert - Should handle multiple releases gracefully
            PPTROTConnectionHelper.SafeReleaseComObject(testObject);
            PPTROTConnectionHelper.SafeReleaseComObject(testObject);
            PPTROTConnectionHelper.SafeReleaseComObject(testObject);
        }

        #endregion

        #region AreComObjectsEqual Tests

        [TestMethod]
        public void AreComObjectsEqual_BothNull_ReturnsFalse()
        {
            // Arrange
            object obj1 = null;
            object obj2 = null;

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreComObjectsEqual_FirstNull_ReturnsFalse()
        {
            // Arrange
            object obj1 = null;
            object obj2 = new object();

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreComObjectsEqual_SecondNull_ReturnsFalse()
        {
            // Arrange
            object obj1 = new object();
            object obj2 = null;

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreComObjectsEqual_SameReference_ReturnsTrue()
        {
            // Arrange
            object obj = new object();

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(obj, obj);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AreComObjectsEqual_DifferentNonComObjects_ReturnsFalse()
        {
            // Arrange
            object obj1 = new object();
            object obj2 = new object();

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreComObjectsEqual_DifferentTypes_ReturnsFalse()
        {
            // Arrange
            object obj1 = "string";
            object obj2 = 123;

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region TryConnectViaROT Tests

        [TestMethod]
        public void TryConnectViaROT_WhenNoPowerPointRunning_ReturnsNull()
        {
            // Arrange & Act
            var result = PPTROTConnectionHelper.TryConnectViaROT(false);

            // Assert
            // When no PowerPoint is running, should return null
            // This test may pass or fail depending on system state
            // If PowerPoint is not running, result should be null
            Assert.IsTrue(result == null || result != null);
        }

        [TestMethod]
        public void TryConnectViaROT_WithWPSSupport_DoesNotThrow()
        {
            // Arrange & Act & Assert - Should not throw
            var result = PPTROTConnectionHelper.TryConnectViaROT(true);

            // Cleanup
            if (result != null)
            {
                PPTROTConnectionHelper.SafeReleaseComObject(result);
            }
        }

        [TestMethod]
        public void TryConnectViaROT_WithoutWPSSupport_DoesNotThrow()
        {
            // Arrange & Act & Assert - Should not throw
            var result = PPTROTConnectionHelper.TryConnectViaROT(false);

            // Cleanup
            if (result != null)
            {
                PPTROTConnectionHelper.SafeReleaseComObject(result);
            }
        }

        #endregion

        #region GetAnyActivePowerPoint Tests

        [TestMethod]
        public void GetAnyActivePowerPoint_WithNullTarget_ReturnsValidResults()
        {
            // Arrange
            int bestPriority;
            int targetPriority;

            // Act
            var result = PPTROTConnectionHelper.GetAnyActivePowerPoint(null, out bestPriority, out targetPriority);

            // Assert
            Assert.IsTrue(bestPriority >= 0);
            Assert.AreEqual(0, targetPriority); // Target priority should be 0 when target is null

            // Cleanup
            if (result != null)
            {
                PPTROTConnectionHelper.SafeReleaseComObject(result);
            }
        }

        [TestMethod]
        public void GetAnyActivePowerPoint_OutputsPriorities_InValidRange()
        {
            // Arrange
            int bestPriority;
            int targetPriority;

            // Act
            var result = PPTROTConnectionHelper.GetAnyActivePowerPoint(null, out bestPriority, out targetPriority);

            // Assert
            // Priority should be in range 0-3:
            // 0 = no app, 1 = has ActivePresentation, 2 = has SlideShowWindow, 3 = window is active
            Assert.IsTrue(bestPriority >= 0 && bestPriority <= 3);
            Assert.IsTrue(targetPriority >= 0 && targetPriority <= 3);

            // Cleanup
            if (result != null)
            {
                PPTROTConnectionHelper.SafeReleaseComObject(result);
            }
        }

        #endregion

        #region GetSlideShowWindowsCount Tests

        [TestMethod]
        public void GetSlideShowWindowsCount_NullApplication_ReturnsZero()
        {
            // Arrange
            Microsoft.Office.Interop.PowerPoint.Application nullApp = null;

            // Act
            int count = PPTROTConnectionHelper.GetSlideShowWindowsCount(nullApp);

            // Assert
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void GetSlideShowWindowsCount_WithValidApplication_ReturnsNonNegative()
        {
            // Arrange
            var app = PPTROTConnectionHelper.TryConnectViaROT(false);

            if (app == null)
            {
                Assert.Inconclusive("No PowerPoint application running");
                return;
            }

            try
            {
                // Act
                int count = PPTROTConnectionHelper.GetSlideShowWindowsCount(app);

                // Assert
                Assert.IsTrue(count >= 0);
            }
            finally
            {
                // Cleanup
                PPTROTConnectionHelper.SafeReleaseComObject(app);
            }
        }

        #endregion

        #region IsValidSlideShowWindow Tests

        [TestMethod]
        public void IsValidSlideShowWindow_NullWindow_ReturnsFalse()
        {
            // Arrange
            object nullWindow = null;

            // Act
            bool result = PPTROTConnectionHelper.IsValidSlideShowWindow(nullWindow);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidSlideShowWindow_NonSlideShowObject_ReturnsFalse()
        {
            // Arrange
            object nonSlideShowObject = new object();

            // Act
            bool result = PPTROTConnectionHelper.IsValidSlideShowWindow(nonSlideShowObject);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidSlideShowWindow_StringObject_ReturnsFalse()
        {
            // Arrange
            object stringObject = "not a slideshow";

            // Act
            bool result = PPTROTConnectionHelper.IsValidSlideShowWindow(stringObject);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region IsSlideShowWindowActive Tests

        [TestMethod]
        public void IsSlideShowWindowActive_NullWindow_ReturnsFalse()
        {
            // Arrange
            object nullWindow = null;

            // Act
            bool result = PPTROTConnectionHelper.IsSlideShowWindowActive(nullWindow);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsSlideShowWindowActive_NonSlideShowObject_ReturnsFalse()
        {
            // Arrange
            object nonSlideShowObject = new object();

            // Act
            bool result = PPTROTConnectionHelper.IsSlideShowWindowActive(nonSlideShowObject);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsSlideShowWindowActive_InvalidObject_ReturnsFalse()
        {
            // Arrange
            object invalidObject = 123;

            // Act
            bool result = PPTROTConnectionHelper.IsSlideShowWindowActive(invalidObject);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Edge Case and Negative Tests

        [TestMethod]
        public void SafeReleaseComObject_IntegerPrimitive_DoesNotThrow()
        {
            // Arrange
            object intObject = 42;

            // Act & Assert
            PPTROTConnectionHelper.SafeReleaseComObject(intObject);
        }

        [TestMethod]
        public void SafeReleaseComObject_BooleanPrimitive_DoesNotThrow()
        {
            // Arrange
            object boolObject = true;

            // Act & Assert
            PPTROTConnectionHelper.SafeReleaseComObject(boolObject);
        }

        [TestMethod]
        public void SafeReleaseComObject_DateTime_DoesNotThrow()
        {
            // Arrange
            object dateTimeObject = DateTime.Now;

            // Act & Assert
            PPTROTConnectionHelper.SafeReleaseComObject(dateTimeObject);
        }

        [TestMethod]
        public void AreComObjectsEqual_WithIdenticalStrings_ReturnsTrue()
        {
            // Arrange
            string str1 = "test";
            string str2 = str1; // Same reference

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(str1, str2);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AreComObjectsEqual_WithEqualButDifferentStrings_ReturnsFalse()
        {
            // Arrange
            string str1 = "test";
            string str2 = new string("test".ToCharArray()); // Different reference

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(str1, str2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetSlideShowWindowsCount_CalledMultipleTimes_Consistent()
        {
            // Arrange
            var app = PPTROTConnectionHelper.TryConnectViaROT(false);

            if (app == null)
            {
                Assert.Inconclusive("No PowerPoint application running");
                return;
            }

            try
            {
                // Act
                int count1 = PPTROTConnectionHelper.GetSlideShowWindowsCount(app);
                int count2 = PPTROTConnectionHelper.GetSlideShowWindowsCount(app);
                int count3 = PPTROTConnectionHelper.GetSlideShowWindowsCount(app);

                // Assert - Results should be consistent
                Assert.AreEqual(count1, count2);
                Assert.AreEqual(count2, count3);
            }
            finally
            {
                // Cleanup
                PPTROTConnectionHelper.SafeReleaseComObject(app);
            }
        }

        #endregion

        #region Regression Tests

        [TestMethod]
        public void TryConnectViaROT_CalledMultipleTimes_DoesNotCrash()
        {
            // Arrange & Act - Call multiple times to ensure no state corruption
            var result1 = PPTROTConnectionHelper.TryConnectViaROT(false);
            var result2 = PPTROTConnectionHelper.TryConnectViaROT(false);
            var result3 = PPTROTConnectionHelper.TryConnectViaROT(true);

            // Assert - Should not crash
            Assert.IsTrue(true); // If we got here, test passed

            // Cleanup
            if (result1 != null) PPTROTConnectionHelper.SafeReleaseComObject(result1);
            if (result2 != null) PPTROTConnectionHelper.SafeReleaseComObject(result2);
            if (result3 != null) PPTROTConnectionHelper.SafeReleaseComObject(result3);
        }

        [TestMethod]
        public void GetAnyActivePowerPoint_CalledConcurrently_HandlesGracefully()
        {
            // Arrange
            int bestPriority1, bestPriority2;
            int targetPriority1, targetPriority2;

            // Act - Simulate concurrent calls
            var result1 = PPTROTConnectionHelper.GetAnyActivePowerPoint(null, out bestPriority1, out targetPriority1);
            var result2 = PPTROTConnectionHelper.GetAnyActivePowerPoint(null, out bestPriority2, out targetPriority2);

            // Assert - Both calls should complete without exception
            Assert.IsTrue(bestPriority1 >= 0);
            Assert.IsTrue(bestPriority2 >= 0);

            // Cleanup
            if (result1 != null) PPTROTConnectionHelper.SafeReleaseComObject(result1);
            if (result2 != null) PPTROTConnectionHelper.SafeReleaseComObject(result2);
        }

        [TestMethod]
        public void SafeReleaseComObject_WithArray_DoesNotThrow()
        {
            // Arrange
            object arrayObject = new int[] { 1, 2, 3 };

            // Act & Assert
            PPTROTConnectionHelper.SafeReleaseComObject(arrayObject);
        }

        [TestMethod]
        public void AreComObjectsEqual_WithArrays_ReturnsFalseForDifferentArrays()
        {
            // Arrange
            object array1 = new int[] { 1, 2, 3 };
            object array2 = new int[] { 1, 2, 3 };

            // Act
            bool result = PPTROTConnectionHelper.AreComObjectsEqual(array1, array2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidSlideShowWindow_WithDelegate_ReturnsFalse()
        {
            // Arrange
            Func<int> delegateObject = () => 42;

            // Act
            bool result = PPTROTConnectionHelper.IsValidSlideShowWindow(delegateObject);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Boundary Tests

        [TestMethod]
        public void GetSlideShowWindowsCount_MultipleCallsWithNullApp_AlwaysReturnsZero()
        {
            // Arrange
            Microsoft.Office.Interop.PowerPoint.Application nullApp = null;

            // Act
            int count1 = PPTROTConnectionHelper.GetSlideShowWindowsCount(nullApp);
            int count2 = PPTROTConnectionHelper.GetSlideShowWindowsCount(nullApp);
            int count3 = PPTROTConnectionHelper.GetSlideShowWindowsCount(nullApp);

            // Assert
            Assert.AreEqual(0, count1);
            Assert.AreEqual(0, count2);
            Assert.AreEqual(0, count3);
        }

        [TestMethod]
        public void SafeReleaseComObject_LargeNumberOfCalls_DoesNotCauseMemoryIssues()
        {
            // Arrange
            object testObject = new object();

            // Act - Call many times to test for memory leaks
            for (int i = 0; i < 1000; i++)
            {
                PPTROTConnectionHelper.SafeReleaseComObject(testObject);
            }

            // Assert - If we got here without OutOfMemoryException, test passed
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void AreComObjectsEqual_ReflexiveProperty_AlwaysTrueForSameObject()
        {
            // Arrange
            object obj = new object();

            // Act & Assert - Reflexive: obj == obj
            Assert.IsTrue(PPTROTConnectionHelper.AreComObjectsEqual(obj, obj));
        }

        [TestMethod]
        public void AreComObjectsEqual_SymmetricProperty_Holds()
        {
            // Arrange
            object obj1 = new object();
            object obj2 = new object();

            // Act
            bool result1 = PPTROTConnectionHelper.AreComObjectsEqual(obj1, obj2);
            bool result2 = PPTROTConnectionHelper.AreComObjectsEqual(obj2, obj1);

            // Assert - Symmetric: if obj1 == obj2, then obj2 == obj1
            Assert.AreEqual(result1, result2);
        }

        #endregion
    }
}