# InkCanvasForClass Test Suite

## Overview
This test project contains comprehensive unit tests for the InkCanvasForClass application. The tests cover core functionality, settings management, PowerPoint integration, and GitHub workflow validation.

## Test Structure

### 1. Settings Tests (`Resources/SettingsTests.cs`)
Comprehensive tests for the settings system covering:

#### Settings Class Tests
- Default constructor initialization
- Serialization/deserialization with JSON
- File-based persistence

#### Canvas Tests
- Default values validation
- Ink width range acceptance
- Eraser size enumeration
- Ink fade timing
- Custom background colors
- Boundary value testing

#### Gesture Tests
- Default values validation
- Computed property validation (`IsEnableTwoFingerGesture`, `IsEnableTwoFingerGestureTranslateOrRotation`)
- Multi-touch mode behavior

#### Startup Tests
- Auto-update settings
- Update channel enumeration
- Time string storage
- Version skipping

#### Appearance Tests
- Default values for all appearance settings
- Floating bar configuration
- Opacity values
- UI visibility toggles
- Custom floating bar images

#### PowerPoint Settings Tests
- PPT button configuration
- Button positioning (negative and positive values)
- Opacity settings
- Auto-save behaviors
- Slide show controls

#### Automation Tests
- Auto-fold computed property
- Auto-save configuration
- File management settings
- Floating window interceptor

#### Floating Window Interceptor Tests
- Default values
- Intercept rules validation
- Rule modification

#### Advanced Tests
- Touch multiplier settings
- Bounds width configuration
- Logging settings
- Backup configuration
- Window mode settings

#### InkToShape Tests
- Shape recognition settings
- Line straightening sensitivity
- Pressure simulation settings

#### RandSettings Tests
- Random name picker configuration
- Timer settings
- Volume controls
- ML avoidance settings

#### Custom Classes Tests
- `CustomPickNameBackground` constructor and serialization
- `CustomFloatingBarIcon` constructor and serialization

#### Mode Settings Tests
- PPT-only mode configuration

#### Camera Settings Tests
- Resolution configuration
- Rotation angle
- Camera selection

#### Dlass Settings Tests
- Token management
- API configuration
- Auto-upload settings

#### Edge Cases and Negative Tests
- Empty JSON deserialization
- Partial JSON deserialization
- Negative value handling
- Enum validation
- Large number of operations

**Total Tests: 55+**

### 2. PPT ROT Connection Helper Tests (`Helpers/PPTROTConnectionHelperTests.cs`)
Tests for PowerPoint COM interop and Running Object Table functionality:

#### SafeReleaseComObject Tests
- Null object handling
- Non-COM object handling
- Multiple release attempts
- Various object types (strings, primitives, arrays)

#### AreComObjectsEqual Tests
- Null handling (both, first, second)
- Same reference comparison
- Different object comparison
- String comparison (identical vs equal)
- Array comparison

#### TryConnectViaROT Tests
- No PowerPoint running scenario
- WPS support enabled/disabled
- Multiple connection attempts

#### GetAnyActivePowerPoint Tests
- Null target handling
- Priority output validation (0-3 range)
- Multiple concurrent calls

#### GetSlideShowWindowsCount Tests
- Null application handling
- Valid application handling
- Consistent results across multiple calls

#### IsValidSlideShowWindow Tests
- Null window handling
- Non-slideshow object handling
- Invalid object types

#### IsSlideShowWindowActive Tests
- Null window handling
- Non-slideshow object handling
- Invalid object types

#### Edge Cases and Boundary Tests
- Primitive types handling
- DateTime handling
- Reflexive property validation
- Symmetric property validation
- Large number of operations (1000+ calls)

#### Regression Tests
- Multiple connection attempts
- Concurrent call handling
- Delegate object handling

**Total Tests: 31+**

### 3. MainWindow PPT Tests (`MainWindow_cs/MW_PPTTests.cs`)
Tests for MainWindow PowerPoint-related functionality:

#### Constants Tests
- LongPressDelay validation (500ms)
- LongPressInterval validation (50ms)
- ProcessMonitorInterval validation (1000ms)
- SlideSwitchDebounceMs validation (150ms)

#### Win32 API Constants Tests
- GWL_STYLE constant
- WS_VISIBLE constant
- WS_MINIMIZE constant
- GW_HWNDNEXT constant
- GW_HWNDPREV constant

#### Static Fields Tests
- Field existence validation
- Type validation
- Initial values

#### PPT Manager Property Tests
- Property existence
- Read-only validation

#### Boundary Tests
- Slides count initial value
- Positive constants validation
- Timing relationships (interval < delay)

#### Edge Case Tests
- Debounce timing for user experience
- Constant relationships

#### Negative Tests
- Non-zero Win32 constants

#### Regression Tests
- Timing constants stability
- Field/property accessibility

**Total Tests: 14+**

### 4. GitHub Workflow Validation Tests (`GitHub/WorkflowValidationTests.cs`)
Tests for GitHub Actions workflows and issue templates:

#### Workflow File Structure Tests
- Directory existence
- dotnet-desktop.yml file existence
- YAML syntax validation
- Required fields validation
- Content non-empty validation

#### Issue Template Tests
- Directory existence
- Bug report YAML/Markdown existence
- Feature request YAML/Markdown existence
- YAML syntax validation
- Required fields validation
- Front matter validation

#### File Naming Tests
- Naming convention compliance
- File extension validation (.yml, .yaml)

#### Content Validation Tests
- GitHub Actions usage
- Bug report sections
- Feature request sections

#### Edge Case Tests
- No empty files
- File size validation

#### Negative Tests
- No syntax errors (tabs vs spaces)
- YAML colon spacing
- Common YAML issues

**Total Tests: 20+**

## Test Coverage Summary

| Component | Test File | # of Tests | Coverage Areas |
|-----------|-----------|------------|----------------|
| Settings System | SettingsTests.cs | 55+ | All settings classes, serialization, validation |
| PPT COM Interop | PPTROTConnectionHelperTests.cs | 31+ | COM object lifecycle, ROT connection, window detection |
| PPT UI Integration | MW_PPTTests.cs | 14+ | Constants, timing, Win32 API, field validation |
| GitHub Workflows | WorkflowValidationTests.cs | 20+ | YAML validation, template structure, naming |
| **TOTAL** | **4 test files** | **120+** | **Core functionality, edge cases, regressions** |

## Running the Tests

### Prerequisites
- .NET Framework 4.7.2 or higher
- Visual Studio 2019 or higher (recommended)
- MSTest test runner

### Using Visual Studio
1. Open the solution in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test > Test Explorer)
4. Click "Run All" to execute all tests

### Using Command Line
```bash
# Restore packages
dotnet restore InkCanvasForClass.Tests/InkCanvasForClass.Tests.csproj

# Build the test project
dotnet build InkCanvasForClass.Tests/InkCanvasForClass.Tests.csproj

# Run all tests
dotnet test InkCanvasForClass.Tests/InkCanvasForClass.Tests.csproj

# Run with detailed output
dotnet test InkCanvasForClass.Tests/InkCanvasForClass.Tests.csproj --logger "console;verbosity=detailed"
```

### Using MSBuild
```bash
# Build
msbuild InkCanvasForClass.Tests/InkCanvasForClass.Tests.csproj /p:Configuration=Debug

# Run (using vstest.console.exe)
vstest.console.exe InkCanvasForClass.Tests/bin/Debug/net472/InkCanvasForClass.Tests.dll
```

## Test Methodology

### Unit Tests
- Test individual components in isolation
- Mock external dependencies where necessary
- Focus on single responsibility

### Integration Tests
- Test interactions between components (e.g., Settings serialization to file)
- Verify correct data flow

### Validation Tests
- Verify configuration files (YAML, JSON)
- Check for structural correctness
- Validate naming conventions

### Edge Case Tests
- Boundary values
- Null/empty inputs
- Extreme values
- Error conditions

### Regression Tests
- Ensure constants remain stable
- Verify backward compatibility
- Prevent re-introduction of bugs

## Test Naming Convention

Tests follow the pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `Canvas_DefaultValues_AreSetCorrectly`
- `SafeReleaseComObject_NullObject_DoesNotThrow`
- `Workflows_DotNetDesktop_IsValidYaml`

## Dependencies

### Test Framework
- MSTest.TestFramework 2.2.10
- MSTest.TestAdapter 2.2.10
- Microsoft.NET.Test.Sdk 17.3.2

### Mocking
- Moq 4.18.4

### Serialization
- Newtonsoft.Json 13.0.3
- YamlDotNet 13.0.2

### Code Coverage
- coverlet.collector 3.1.2

## Notes

### PowerPoint Tests
Some tests may be inconclusive if PowerPoint is not installed or not running:
- `TryConnectViaROT_*`
- `GetSlideShowWindowsCount_WithValidApplication_*`

These tests will report as "Inconclusive" rather than "Failed".

### GitHub Workflow Tests
Tests for GitHub workflows require the repository structure to be intact:
- `.github/workflows/` directory
- `.github/ISSUE_TEMPLATE/` directory

If these directories are missing, tests will report as "Inconclusive".

### COM Interop Tests
Tests involving COM objects may behave differently depending on:
- Whether PowerPoint is installed
- Whether PowerPoint is currently running
- System permissions for COM automation

## Additional Test Categories

The tests also provide coverage for:

1. **Boundary Conditions**: Min/max values, edge cases
2. **Negative Scenarios**: Invalid inputs, error conditions
3. **Regression Prevention**: Ensuring known issues don't recur
4. **Performance**: Large number of operations (1000+ iterations)
5. **Thread Safety**: Concurrent operations where applicable

## Future Test Additions

Potential areas for additional test coverage:
1. UI component tests for WPF controls (requires UI testing framework)
2. End-to-end integration tests with real PowerPoint instances
3. Performance benchmarks for ink rendering
4. Accessibility tests for UI elements
5. Localization tests for multi-language support
6. Security tests for file operations and COM automation

## Contributing

When adding new tests:
1. Follow the existing naming conventions
2. Add tests to the appropriate test class
3. Include edge cases and negative scenarios
4. Update this README with new test counts
5. Ensure tests are independent and can run in any order
6. Use `[TestInitialize]` and `[TestCleanup]` for setup/teardown

## License

This test suite is part of the InkCanvasForClass project.