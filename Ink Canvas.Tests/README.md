# Ink Canvas For Class - Test Suite

This project contains comprehensive unit and integration tests for Ink Canvas For Class.

## Test Framework

- **Test Framework**: xUnit 2.6.5
- **Mocking Framework**: Moq 4.20.70
- **Assertion Library**: FluentAssertions 6.12.0
- **Target Framework**: .NET Framework 4.7.2

## Test Structure

```
Ink Canvas.Tests/
├── App/
│   └── AppLifecycleTests.cs          # Tests for application lifecycle and crash handling
├── Helpers/
│   └── PPTROTConnectionHelperTests.cs # Tests for PowerPoint ROT connection helper
├── Resources/
│   └── SettingsTests.cs              # Tests for settings serialization and structure
└── CI/
    └── WorkflowValidationTests.cs    # Tests for GitHub Actions workflow validation
```

## Running Tests

### Using Visual Studio
1. Open the solution in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test > Test Explorer)
4. Click "Run All" to execute all tests

### Using Command Line
```bash
# Restore NuGet packages
nuget restore "Ink Canvas.sln"

# Build the solution
msbuild "Ink Canvas.sln" /p:Configuration=Debug

# Run tests (requires vstest.console.exe or dotnet CLI)
dotnet test "Ink Canvas.Tests\Ink Canvas.Tests.csproj"
```

### Using dotnet CLI (if available)
```bash
dotnet test "Ink Canvas.Tests/Ink Canvas.Tests.csproj" --logger "console;verbosity=detailed"
```

## Test Coverage

### Application Lifecycle (AppLifecycleTests.cs)
- **Crash action handling**: Tests for `CrashActionType` enum values and behavior
- **Splash screen**: Tests for splash screen lifecycle and progress updates
- **Command line arguments**: Tests for various startup arguments (--board, --show, --watchdog, etc.)
- **Application state**: Tests for app state consistency and persistence
- **Boundary cases**: Tests for edge cases like extreme progress values and rapid updates

### PowerPoint Integration (PPTROTConnectionHelperTests.cs)
- **COM object management**: Tests for COM object equality and safe release
- **ROT connection**: Tests for Running Object Table connection attempts
- **Slideshow validation**: Tests for slideshow window validation
- **WPS support**: Tests for WPS Office compatibility
- **Exception handling**: Tests for graceful error handling in PowerPoint operations
- **Edge cases**: Multiple connection attempts, null handling, and cleanup

### Settings Management (SettingsTests.cs)
- **Settings structure**: Tests for all settings sections (Appearance, Advanced, etc.)
- **JSON serialization**: Tests for settings serialization and deserialization
- **Property access**: Tests for accessibility of all settings properties
- **Roundtrip consistency**: Tests for settings data integrity through save/load cycles
- **Invalid data handling**: Tests for handling of invalid JSON input

### CI/CD Workflows (WorkflowValidationTests.cs)
- **Workflow existence**: Verifies GitHub Actions workflow files exist
- **YAML validity**: Tests for valid YAML syntax
- **Build configuration**: Tests for proper MSBuild and NuGet setup
- **Artifact generation**: Tests for build artifact upload configuration
- **PR features**: Tests for pull request comment and status features
- **Security**: Tests for proper permissions and no hardcoded secrets

## Test Categories

### Unit Tests
- Individual class and method testing
- Isolated functionality verification
- Mock external dependencies

### Integration Tests
- Multi-component interaction testing
- Settings serialization/deserialization
- Workflow configuration validation

### Boundary Tests
- Edge case handling
- Extreme value testing
- Null and empty input handling

### Regression Tests
- Prevent previously fixed bugs from reappearing
- Verify critical functionality remains intact
- Document expected behaviors

### Negative Tests
- Invalid input handling
- Exception scenarios
- Error recovery mechanisms

## Writing New Tests

### Test Naming Convention
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

### Example Test
```csharp
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
```

### Fluent Assertions Examples
```csharp
// Boolean assertions
result.Should().BeTrue();
result.Should().BeFalse("custom failure message");

// Numeric assertions
count.Should().BeGreaterThan(0);
value.Should().BeLessThanOrEqualTo(100);

// String assertions
name.Should().NotBeNullOrEmpty();
text.Should().Contain("expected");

// Exception assertions
action.Should().NotThrow();
action.Should().Throw<ArgumentException>();

// Collection assertions
list.Should().BeEmpty();
list.Should().HaveCount(5);
list.Should().Contain(item);
```

## CI Integration

These tests are designed to be integrated into the GitHub Actions CI/CD pipeline. To add test execution to the build workflow:

```yaml
- name: Run Unit Tests
  run: dotnet test "Ink Canvas.Tests/Ink Canvas.Tests.csproj" --no-build --verbosity normal
```

## Known Limitations

1. **WPF UI Testing**: UI-specific tests require additional frameworks (e.g., TestStack.White, FlaUI)
2. **PowerPoint COM**: Requires PowerPoint to be installed for full integration testing
3. **File System**: Some tests may require file system access permissions
4. **Threading**: Tests involving WPF Dispatcher may require STA thread apartment

## Future Enhancements

- [ ] Add UI automation tests for XAML windows
- [ ] Add performance benchmarks
- [ ] Add code coverage reporting
- [ ] Integrate with SonarQube for code quality analysis
- [ ] Add mutation testing with Stryker.NET
- [ ] Add snapshot testing for settings schemas

## Contributing

When adding new features to Ink Canvas For Class:

1. Write tests first (TDD approach recommended)
2. Ensure all existing tests pass
3. Add tests for edge cases and error conditions
4. Update this README if adding new test categories
5. Maintain minimum 70% code coverage for new code

## Contact

For questions about the test suite, please refer to the main project documentation or open an issue on GitHub.