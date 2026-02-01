# Test Execution Guide

## Overview
This document provides instructions for running the comprehensive test suite created for Ink Canvas For Class.

## Prerequisites

### Required Software
1. **Visual Studio 2019 or later** (with .NET desktop development workload)
   - OR **Visual Studio Build Tools 2019+**
2. **NuGet Package Manager** (included with Visual Studio)
3. **.NET Framework 4.7.2** (included with Visual Studio)
4. **Optional**: JetBrains ReSharper (for enhanced test runner)

### Verifying Prerequisites
Open a Developer Command Prompt for Visual Studio and run:
```cmd
msbuild -version
nuget help
```

## Building the Solution

### Option 1: Visual Studio GUI
1. Open `Ink Canvas.sln` in Visual Studio
2. Right-click on the solution in Solution Explorer
3. Select "Restore NuGet Packages"
4. Build the solution: **Build > Build Solution** (Ctrl+Shift+B)

### Option 2: Command Line
```cmd
cd "path\to\Ink Canvas"

REM Restore NuGet packages
nuget restore "Ink Canvas.sln"

REM Build the solution
msbuild "Ink Canvas.sln" /p:Configuration=Debug /p:Platform="Any CPU"

REM Or build just the test project
msbuild "Ink Canvas.Tests\Ink Canvas.Tests.csproj" /p:Configuration=Debug
```

### Option 3: Using MSBuild Restore
```cmd
msbuild "Ink Canvas.sln" /t:Restore
msbuild "Ink Canvas.sln" /p:Configuration=Debug
```

## Running Tests

### Method 1: Visual Studio Test Explorer
1. Open Visual Studio
2. Go to **Test > Test Explorer** (Ctrl+E, T)
3. Click **Run All** button
4. View test results in the Test Explorer window

![Test Explorer Screenshot Placeholder]

### Method 2: Command Line with vstest.console
```cmd
REM Find vstest.console.exe location (typically in Visual Studio installation)
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" ^
  "Ink Canvas.Tests\bin\Debug\net472\InkCanvasForClass.Tests.dll"
```

### Method 3: Using dotnet CLI (if SDK is installed)
```cmd
dotnet test "Ink Canvas.Tests\Ink Canvas.Tests.csproj" --configuration Debug --verbosity detailed
```

### Method 4: ReSharper (if installed)
1. Right-click on the test project in Solution Explorer
2. Select **Run Unit Tests**
3. View results in the ReSharper test runner window

## Test Execution Results

### Expected Output
```
Starting test execution, please wait...
A total of 78 test files matched the specified pattern.

Test Run Successful.
Total tests: 78
     Passed: 78
     Failed: 0
   Skipped: 0
Total time: 4.5678 Seconds
```

### Understanding Test Results

#### ✅ Passed Tests
- All assertions were successful
- No exceptions were thrown
- Expected behavior was observed

#### ❌ Failed Tests
- At least one assertion failed
- Unexpected exception was thrown
- Actual behavior didn't match expected

#### ⚠️ Skipped Tests
- Test has `[Fact(Skip = "reason")]` attribute
- Test is conditionally executed and conditions not met
- Test requires specific environment setup

## Troubleshooting

### Build Errors

#### Error: Cannot find project reference
```
Solution: Ensure the main project is building successfully first
1. Build "Ink Canvas\InkCanvasForClass.csproj" first
2. Then build the test project
```

#### Error: NuGet packages not restored
```
Solution: Manually restore packages
1. Right-click solution > Restore NuGet Packages
2. Or run: nuget restore "Ink Canvas.sln"
```

#### Error: Missing assembly references
```
Solution: Check .NET Framework 4.7.2 is installed
1. Visual Studio Installer > Modify
2. Individual components > .NET Framework 4.7.2
```

### Test Execution Errors

#### Error: PowerPoint COM tests failing
```
Reason: Some tests require PowerPoint to be installed
Solution:
- Install Microsoft Office PowerPoint
- Or skip COM-dependent tests (they handle absence gracefully)
```

#### Error: File access denied in WorkflowValidationTests
```
Reason: Tests need to read .github/workflows/*.yml files
Solution: Ensure the repository is cloned with all files
```

#### Error: TestHost.exe crashed
```
Reason: Possible WPF threading issues
Solution: Run tests individually to identify problematic test
```

## Running Specific Tests

### Run a Single Test Class
```cmd
vstest.console.exe "InkCanvasForClass.Tests.dll" /Tests:PPTROTConnectionHelperTests
```

### Run Tests Matching Pattern
```cmd
vstest.console.exe "InkCanvasForClass.Tests.dll" /TestCaseFilter:"FullyQualifiedName~Settings"
```

### Run Only Fast Tests (exclude integration tests)
```cmd
vstest.console.exe "InkCanvasForClass.Tests.dll" /TestCaseFilter:"TestCategory!=Integration"
```

## Test Coverage Report (Future Enhancement)

### Install Coverage Tools
```cmd
dotnet tool install --global dotnet-coverage
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Generate Coverage Report
```cmd
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**\coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

## Continuous Integration

### GitHub Actions Integration

Add this step to `.github/workflows/dotnet-desktop.yml` after the build step:

```yaml
- name: Run Unit Tests
  run: |
    Write-Host "📋 Running unit tests..." -ForegroundColor Cyan

    # Find vstest.console.exe
    $vstestPath = Get-ChildItem -Path "C:\Program Files*\Microsoft Visual Studio" -Recurse -Filter "vstest.console.exe" | Select-Object -First 1 -ExpandProperty FullName

    if ($vstestPath) {
      & "$vstestPath" "Ink Canvas.Tests\bin\Debug\net472\InkCanvasForClass.Tests.dll" /Logger:trx

      if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed!" -ForegroundColor Red
        exit 1
      }

      Write-Host "✅ All tests passed!" -ForegroundColor Green
    } else {
      Write-Host "⚠️ vstest.console.exe not found, skipping tests" -ForegroundColor Yellow
    }
```

### Alternative: Using dotnet test in CI
```yaml
- name: Install .NET SDK
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '6.0.x'

- name: Run Tests
  run: dotnet test "Ink Canvas.Tests/Ink Canvas.Tests.csproj" --configuration Debug --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Test Results
    path: '**/test-results.trx'
    reporter: dotnet-trx
```

## Best Practices

### Before Committing
1. **Run all tests locally**: `dotnet test` or Test Explorer > Run All
2. **Fix all failing tests**: Don't commit with failing tests
3. **Add tests for new features**: Maintain test coverage
4. **Update documentation**: If adding new test categories

### Test Development Guidelines
1. **Follow AAA pattern**: Arrange, Act, Assert
2. **Use descriptive names**: `MethodName_Scenario_ExpectedBehavior`
3. **One assertion per test**: Keep tests focused
4. **Use FluentAssertions**: More readable assertions
5. **Test edge cases**: Null, empty, boundary values
6. **Document complex tests**: Add XML comments explaining intent

## Performance Benchmarks

### Typical Test Execution Times
- **AppLifecycleTests**: ~0.5 seconds (20 tests)
- **PPTROTConnectionHelperTests**: ~2.0 seconds (25 tests) *may vary with PowerPoint installed*
- **SettingsTests**: ~0.3 seconds (18 tests)
- **WorkflowValidationTests**: ~0.4 seconds (15 tests)

**Total Estimated Time**: 3-5 seconds for full test suite

### Optimization Tips
- Run tests in parallel (Visual Studio does this by default)
- Skip expensive integration tests during development
- Use test categories to run only relevant tests
- Cache test assemblies to speed up repeated runs

## Common Test Scenarios

### Testing New PowerPoint Feature
1. Create test file in `Helpers/` folder
2. Name test class with `Tests` suffix
3. Add `[Fact]` attribute to test methods
4. Test both success and failure paths
5. Include boundary cases (null, empty, extreme values)

### Testing Settings Changes
1. Create test in `Resources/SettingsTests.cs`
2. Test serialization/deserialization
3. Verify default values
4. Test backwards compatibility

### Testing UI Changes (Future)
1. Add UI test project with FlaUI/TestStack.White
2. Use Page Object pattern for XAML windows
3. Test user workflows, not individual controls
4. Keep UI tests separate from unit tests

## Support

### Getting Help
- **GitHub Issues**: https://github.com/InkCanvasForClass/community/issues
- **Documentation**: See `Ink Canvas.Tests/README.md`
- **Community**: Check project discussions

### Reporting Test Failures
When reporting a test failure, include:
1. Test name and full error message
2. Operating System version
3. Visual Studio version
4. Whether PowerPoint is installed
5. Test execution method used

---

**Last Updated**: 2026-02-01
**Test Framework Version**: xUnit 2.6.5
**Target Framework**: .NET Framework 4.7.2