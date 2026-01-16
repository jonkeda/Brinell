# SPEC-016: Screenshot Evidence on Failure - Tasks

**Spec ID:** 016  
**Phase:** Tasks  
**Created:** 2025-01-20  
**Status:** Ready for Implementation  

---

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Each task includes File, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Core Infrastructure

### [ ] 1. Create ScreenshotSettings configuration class
- **File:** `srcnew/Brinell.Core/Configuration/ScreenshotSettings.cs`
- **Purpose:** Define configuration options for screenshot capture behavior
- **_Leverage:** `srcnew/Brinell.Core/Configuration/TimeoutSettings.cs` (pattern reference)
- **_Requirements:** FR-502.4
- **_Prompt:** Role: C# Developer specializing in configuration patterns | Task: Create ScreenshotSettings class with OutputDirectory, Format, JpegQuality, CaptureOnFailure, IncludeTimestamp properties following TimeoutSettings pattern | Restrictions: Use same style as TimeoutSettings, add static Default property | Success: Class compiles, follows existing configuration patterns, has XML docs

### [ ] 2. Create ScreenshotFormat enum
- **File:** `srcnew/Brinell.Core/Configuration/ScreenshotFormat.cs`
- **Purpose:** Define supported screenshot image formats
- **_Leverage:** None (new file)
- **_Requirements:** FR-502.4
- **_Prompt:** Role: C# Developer | Task: Create simple enum with Png and Jpeg values | Restrictions: Keep minimal | Success: Enum compiles, has XML doc summary

### [ ] 3. Create ScreenshotReason enum
- **File:** `srcnew/Brinell.Core/Logging/ScreenshotReason.cs`
- **Purpose:** Categorize why a screenshot was captured
- **_Leverage:** `srcnew/Brinell.Core/Logging/LogResult.cs` (pattern reference)
- **_Requirements:** FR-502.1, FR-502.2
- **_Prompt:** Role: C# Developer | Task: Create enum with Manual, AssertionFailure, Exception, Timeout, ElementNotFound values | Restrictions: Follow LogResult pattern | Success: Enum compiles, values cover all failure types in requirements

### [ ] 4. Add LogScreenshot method to ITestLogger
- **File:** `srcnew/Brinell.Core/Logging/ITestLogger.cs` (modify)
- **Purpose:** Enable logging of screenshot capture events
- **_Leverage:** Existing ITestLogger methods
- **_Requirements:** FR-502.5
- **_Prompt:** Role: C# Developer | Task: Add LogScreenshot(string testName, string pageName, string screenshotPath, ScreenshotReason reason) method signature with XML doc | Restrictions: Add to existing interface, don't change other methods | Success: Method added, builds successfully

### [ ] 5. Implement LogScreenshot in logger implementations
- **Files:** 
  - `srcnew/Brinell.Core/Logging/ConsoleTestLogger.cs`
  - `srcnew/Brinell.Core/Logging/CsvTestLogger.cs`
  - `srcnew/Brinell.Core/Logging/NullTestLogger.cs`
- **Purpose:** Implement the new LogScreenshot method in all loggers
- **_Leverage:** Existing Log method implementations
- **_Requirements:** FR-502.5
- **_Prompt:** Role: C# Developer | Task: Implement LogScreenshot in all three logger classes - ConsoleTestLogger writes to console, CsvTestLogger writes to CSV, NullTestLogger does nothing | Restrictions: Follow existing patterns in each class | Success: All implementations compile, no breaking changes

---

## Phase 2: Screenshot Service

### [ ] 6. Create IScreenshotService interface
- **File:** `srcnew/Brinell.Core/Interfaces/IScreenshotService.cs`
- **Purpose:** Define screenshot capture contract
- **_Leverage:** Design document section 5.2
- **_Requirements:** FR-502.1, FR-502.2
- **_Prompt:** Role: C# Developer specializing in interface design | Task: Create IScreenshotService with Capture(description), Capture(testClass, testMethod, description), CaptureOnFailure(testClass, testMethod, exception), and Settings property | Restrictions: Methods return string (filepath), follow existing interface patterns | Success: Interface compiles, covers all required capture scenarios

### [ ] 7. Create ScreenshotService implementation
- **File:** `srcnew/Brinell.Core/Services/ScreenshotService.cs`
- **Purpose:** Implement screenshot capture with naming and error handling
- **_Leverage:** 
  - `srcnew/Brinell.Core/Interfaces/ITestContext.cs` (TakeScreenshot, SaveScreenshot)
  - Design document section 5.3
- **_Requirements:** FR-502.1, FR-502.2, FR-502.3
- **_Prompt:** Role: C# Developer with error handling expertise | Task: Implement ScreenshotService with constructor taking ITestContext, ITestLogger, ScreenshotSettings; implement GenerateFilename with pattern {TestClass}_{TestMethod}_{Timestamp}_{Description}.{ext}; wrap capture in try-catch returning empty string on failure | Restrictions: Screenshot failures must not throw, use Path.Combine for paths, sanitize filenames | Success: All methods implemented, handles errors gracefully, generates correct filenames

### [ ] 8. Add ScreenshotInfo record
- **File:** `srcnew/Brinell.Core/Models/ScreenshotInfo.cs`
- **Purpose:** Data transfer object for screenshot metadata
- **_Leverage:** Design document section 6
- **_Requirements:** FR-502.3
- **_Prompt:** Role: C# Developer | Task: Create record with FilePath, TestClass, TestMethod, Timestamp, Reason, ExceptionMessage properties | Restrictions: Use C# record type, all properties init-only | Success: Record compiles, has all required properties

---

## Phase 3: xUnit Integration

### [ ] 9. Create ScreenshotTestAttribute
- **File:** `srcnew/Brinell.Core/Testing/ScreenshotTestAttribute.cs`
- **Purpose:** xUnit hook for automatic failure capture
- **_Leverage:** Design document section 5.4, xUnit BeforeAfterTestAttribute
- **_Requirements:** FR-502.1
- **_Prompt:** Role: C# Developer with xUnit expertise | Task: Create ScreenshotTestAttribute extending BeforeAfterTestAttribute with AsyncLocal storage for service and test info; implement Before/After methods; add static SetService and CaptureIfFailed methods | Restrictions: Use AsyncLocal for thread safety, Before stores test info, After clears it | Success: Attribute compiles, can be applied to test class/method

### [ ] 10. Update AppiumFixture to wire screenshot service
- **File:** `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` (modify)
- **Purpose:** Initialize and register screenshot service with test context
- **_Leverage:** Existing AppiumFixture constructor pattern
- **_Requirements:** FR-502.1
- **_Prompt:** Role: C# Developer | Task: Add ScreenshotService field, create in constructor with context and settings, call ScreenshotTestAttribute.SetService | Restrictions: Add ScreenshotSettings with OutputDirectory pointing to TestResults/Screenshots, ensure directory exists | Success: Service initialized on fixture creation, directory auto-created

---

## Phase 4: Testing

### [ ] 11. Create unit tests for ScreenshotService
- **File:** `testsnew/Brinell.Core.UnitTests/Services/ScreenshotServiceTests.cs`
- **Purpose:** Verify filename generation and error handling
- **_Leverage:** Existing test patterns in testsnew
- **_Requirements:** All
- **_Prompt:** Role: C# Test Developer | Task: Create tests for GenerateFilename pattern, SanitizeFilename with invalid chars, CaptureOnFailure with CaptureOnFailure=false returns empty, exception mapping to description | Restrictions: Mock ITestContext and ITestLogger, use xUnit and FluentAssertions | Success: Tests cover happy path and edge cases, all pass

### [ ] 12. Create integration test for screenshot capture
- **File:** `testsnew/Brinell.Maui.UITests/Tests/ScreenshotEvidenceTests.cs`
- **Purpose:** Verify screenshots are captured on actual failure
- **_Leverage:** Existing UI test patterns
- **_Requirements:** FR-502.1, AC-1
- **_Prompt:** Role: UI Test Developer | Task: Create test that intentionally fails an assertion, verify screenshot file exists in TestResults/Screenshots with correct naming pattern | Restrictions: Test should be marked with [ScreenshotTest], clean up screenshot after verification | Success: Screenshot file created on failure, filename matches pattern

---

## Summary

| Phase | Tasks | Est. Time |
|-------|-------|-----------|
| Phase 1: Core Infrastructure | 5 tasks | 1-2 hours |
| Phase 2: Screenshot Service | 3 tasks | 1 hour |
| Phase 3: xUnit Integration | 2 tasks | 30 min |
| Phase 4: Testing | 2 tasks | 1 hour |
| **Total** | **12 tasks** | **~4 hours** |

---

**Next Phase:** Implementation (say "implement" to continue)
