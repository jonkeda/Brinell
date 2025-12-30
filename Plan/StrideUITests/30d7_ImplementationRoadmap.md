# 7. Implementation Roadmap

**Parent:** [Documentation Index](30d0_StrideUITestFramework_Index.md)  
**Previous:** [Test Patterns](30d6_TestPatterns.md)  
**Version:** 1.0 (Proposal - January 2025)

---

## 7.1 Implementation Phases

### Phase Overview

| Phase | Focus | Duration | Dependencies |
|-------|-------|----------|--------------|
| **Phase 1** | Core Infrastructure | 2-3 weeks | None |
| **Phase 2** | Game Automation Hooks | 1-2 weeks | Phase 1 |
| **Phase 3** | Control Objects | 2 weeks | Phase 2 |
| **Phase 4** | Page Objects | 1-2 weeks | Phase 3 |
| **Phase 5** | Test Suite | 2-3 weeks | Phase 4 |
| **Phase 6** | CI/CD Integration | 1 week | Phase 5 |

**Total Estimated Duration:** 9-13 weeks

---

## 7.2 Phase 1: Core Infrastructure

### 7.2.1 Goals

- Create `Oravey.UITestFramework.Stride` project
- Implement communication channel between test and game
- Implement input simulation

### 7.2.2 Deliverables

| Item | Description | Priority |
|------|-------------|----------|
| Project setup | Create .csproj with dependencies | High |
| `IAutomationChannel` | Channel abstraction interface | High |
| `NamedPipeChannel` | Named pipe implementation | High |
| `StrideInputSimulator` | Keyboard/mouse simulation | High |
| `StrideTestOptions` | Configuration class | Medium |
| `StrideGameDriver` | Game lifecycle management | High |
| Unit tests | Tests for infrastructure components | Medium |

### 7.2.3 Tasks

```
1. Create Oravey.UITestFramework.Stride project
   - Add package references (xUnit, FluentAssertions, InputSimulatorStandard)
   - Add project reference to Oravey.UITestFramework.Core
   
2. Implement IAutomationChannel interface
   - Define command/response types
   - Create AutomationCommand class
   - Create AutomationResponse class

3. Implement NamedPipeChannel
   - Client-side pipe connection
   - JSON serialization for commands
   - Async send/receive methods
   - Connection retry logic

4. Implement StrideInputSimulator
   - Mouse movement and clicks
   - Keyboard input
   - Key combinations

5. Implement StrideGameDriver
   - Start game process
   - Wait for game ready
   - Stop game process
   - Attach to running game
```

### 7.2.4 Acceptance Criteria

- [ ] Can connect to a running game via named pipe
- [ ] Can send commands and receive responses
- [ ] Can simulate mouse clicks at screen coordinates
- [ ] Can simulate keyboard input
- [ ] Can start and stop game process

---

## 7.3 Phase 2: Game Automation Hooks

### 7.3.1 Goals

- Add automation support to Oravey.Game
- Implement UI element registry
- Enable conditional compilation

### 7.3.2 Deliverables

| Item | Description | Priority |
|------|-------------|----------|
| `AutomationService` | UI element registry | High |
| `AutomationHost` | Named pipe server | High |
| `UIElementProvider` | Element state queries | High |
| `AutomationExtensions` | SetAutomationId helper | High |
| Conditional compilation | ENABLE_AUTOMATION flag | High |
| Game startup integration | Initialize automation on start | High |

### 7.3.3 Tasks

```
1. Add Automation folder to Oravey.Game
   - Create AutomationService.cs
   - Create AutomationHost.cs
   - Create UIElementProvider.cs
   - Create AutomationExtensions.cs

2. Implement AutomationService
   - Element registration (WeakReference)
   - Element lookup by automation ID
   - Thread-safe dictionary

3. Implement AutomationHost
   - Named pipe server setup
   - Command processing loop
   - Execute commands on game thread
   - Response serialization

4. Implement UIElementProvider
   - GetElementState method
   - Bounds calculation (screen coordinates)
   - Text extraction
   - Toggle state extraction
   - List item extraction

5. Configure conditional compilation
   - Add ENABLE_AUTOMATION to Debug configuration
   - Add UITest configuration
   - Exclude automation code from Release

6. Integrate with game startup
   - Initialize AutomationService in OraveyGame
   - Start AutomationHost if enabled
   - Register service with Script.Scheduler
```

### 7.3.4 Acceptance Criteria

- [ ] Game starts with automation enabled in Debug
- [ ] Game accepts automation connections
- [ ] Can query element state via named pipe
- [ ] Element registration works with existing UI code
- [ ] Automation code excluded from Release build

---

## 7.4 Phase 3: Control Objects

### 7.4.1 Goals

- Implement StrideControlBase and derived classes
- Implement Wait/Check/Is/Assert pattern
- Cover all Stride UI control types

### 7.4.2 Deliverables

| Item | Description | Priority |
|------|-------------|----------|
| `StrideTestContext` | ITestContext implementation | High |
| `StrideControlBase` | Base control class | High |
| `StrideButtonControl` | Button wrapper | High |
| `StrideTextBlockControl` | TextBlock wrapper | High |
| `StrideEditTextControl` | EditText wrapper | High |
| `StrideCheckBoxControl` | CheckBox wrapper | Medium |
| `StrideSliderControl` | Slider wrapper | Medium |
| `StrideListBoxControl` | ListBox wrapper | Medium |
| Control unit tests | Tests for control behaviors | Medium |

### 7.4.3 Tasks

```
1. Implement StrideTestContext
   - ITestContext properties
   - Element operation methods
   - Input simulation integration
   - Logging support

2. Implement StrideControlBase
   - Is* methods (IsExists, IsVisible, IsEnabled)
   - Wait* methods with polling
   - Check* methods with throw
   - Assert* methods with logging

3. Implement content controls
   - StrideContentControlBase (Click, DoubleClick)
   - StrideButtonControl

4. Implement text controls
   - StrideTextControlBase
   - StrideTextBlockControl (display)
   - StrideEditTextControl (input)

5. Implement toggle controls
   - StrideToggleControlBase
   - StrideCheckBoxControl

6. Implement range controls
   - StrideSliderControl

7. Implement list controls
   - StrideListBoxControl
```

### 7.4.4 Acceptance Criteria

- [ ] All controls implement IControlObject
- [ ] Wait/Check/Is/Assert pattern works correctly
- [ ] Input simulation clicks buttons correctly
- [ ] Text input works with EditText
- [ ] All control methods are virtual for extension

---

## 7.5 Phase 4: Page Objects

### 7.5.1 Goals

- Implement StridePageBase
- Create page objects for all game screens
- Add automation IDs to existing UI code

### 7.5.2 Deliverables

| Item | Description | Priority |
|------|-------------|----------|
| `StridePageBase` | Base page class | High |
| `StrideBusyPageBase` | Loading-aware pages | High |
| `MainMenuPage` | Main menu page object | High |
| `NameInputPage` | Name input page object | High |
| `LoadGamePage` | Load game page object | High |
| `PauseMenuPage` | Pause menu page object | High |
| `LoadingScreenPage` | Loading screen page object | Medium |
| `InGameUIPage` | HUD page object | Medium |
| Automation ID updates | Add IDs to existing UI code | High |

### 7.5.3 Tasks

```
1. Implement page base classes
   - StridePageBase
   - StrideBusyPageBase
   - Display detection
   - Page ready detection

2. Update MainMenuUI with automation IDs
   - Add .WithAutomationId() to all buttons
   - Register with AutomationService

3. Create MainMenuPage
   - All button controls
   - Navigation methods
   - Keyboard navigation

4. Update NameInputDialog with automation IDs
   - Add IDs to input field and buttons

5. Create NameInputPage
   - Input field control
   - Start/Cancel buttons
   - Name validation

6. Continue for other pages...
   - LoadGamePage
   - PauseMenuPage
   - LoadingScreenPage
   - InGameUIPage
```

### 7.5.4 Acceptance Criteria

- [ ] All pages implement IPageObject
- [ ] Pages correctly detect display state
- [ ] Navigation methods work correctly
- [ ] All game UI screens have corresponding page objects
- [ ] Existing game code has automation IDs

---

## 7.6 Phase 5: Test Suite

### 7.6.1 Goals

- Create test project
- Implement smoke tests
- Implement feature tests
- Document test patterns

### 7.6.2 Deliverables

| Item | Description | Priority |
|------|-------------|----------|
| Test project | Oravey.Game.StrideUITests | High |
| `StrideGameFixture` | Test collection fixture | High |
| `StrideUITestBase` | Base test class | High |
| Smoke tests | Critical path tests | High |
| Main menu tests | Menu functionality | High |
| New game tests | New game flow | High |
| Load game tests | Load game flow | Medium |
| In-game tests | Pause menu, HUD | Medium |
| Test documentation | Patterns and examples | Medium |

### 7.6.3 Tasks

```
1. Create test project
   - Add references to framework and game
   - Configure test settings
   - Set up test categories

2. Implement test infrastructure
   - StrideGameFixture
   - StrideUITestBase
   - Screenshot capture
   - CSV logging

3. Implement smoke tests
   - Game starts and shows main menu
   - All main menu buttons visible
   - Basic navigation works

4. Implement main menu tests
   - Button states
   - Keyboard navigation
   - Navigation to other screens

5. Implement new game tests
   - Name input validation
   - Game start flow
   - Loading screen

6. Implement load game tests
   - Save list population
   - Save selection
   - Load flow

7. Implement in-game tests
   - Pause menu
   - Debug overlay
   - Mini map (if applicable)
```

### 7.6.4 Acceptance Criteria

- [ ] All smoke tests pass
- [ ] Tests run reliably in isolation
- [ ] Tests clean up after themselves
- [ ] CSV logging works correctly
- [ ] Screenshot capture on failure works

---

## 7.7 Phase 6: CI/CD Integration

### 7.7.1 Goals

- Integrate UI tests with build pipeline
- Configure headless/windowed execution
- Set up test reporting

### 7.7.2 Deliverables

| Item | Description | Priority |
|------|-------------|----------|
| Build configuration | UITest configuration | High |
| CI pipeline updates | Run UI tests in pipeline | High |
| Test filtering | Run smoke tests on PR | High |
| Test reporting | Publish results and screenshots | Medium |
| Documentation | CI/CD setup guide | Medium |

### 7.7.3 Tasks

```
1. Create UITest build configuration
   - Enable ENABLE_AUTOMATION
   - Optimize for testing

2. Update CI pipeline
   - Build with UITest configuration
   - Install required dependencies
   - Configure display (virtual or real)

3. Configure test execution
   - Run smoke tests on every PR
   - Run full suite nightly
   - Configure timeouts

4. Set up test reporting
   - Publish test results
   - Upload failure screenshots
   - Generate coverage reports

5. Document CI/CD setup
   - Local test execution
   - Pipeline configuration
   - Troubleshooting guide
```

### 7.7.4 Acceptance Criteria

- [ ] UI tests run in CI/CD pipeline
- [ ] Smoke tests block PR merges on failure
- [ ] Test results published and visible
- [ ] Screenshots uploaded on failure

---

## 7.8 Risk Assessment

### 7.8.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Named pipe reliability | Medium | High | Implement retry logic, fallback to in-process |
| Input simulation accuracy | Medium | Medium | Calibrate delays, use direct automation for critical actions |
| Game thread synchronization | High | High | Use Script.Scheduler, implement proper async patterns |
| Screen resolution differences | Medium | Medium | Use relative coordinates, test at fixed resolution |
| Test flakiness | High | High | Implement robust waits, retry mechanism |

### 7.8.2 Schedule Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Underestimated complexity | Medium | Medium | Build incrementally, prioritize core features |
| Dependency on game changes | Medium | High | Coordinate with game development, buffer time |
| CI/CD integration issues | Medium | Medium | Test locally first, involve DevOps early |

---

## 7.9 Success Metrics

### 7.9.1 Phase Completion Metrics

| Phase | Key Metric |
|-------|------------|
| Phase 1 | Can connect and send commands to game |
| Phase 2 | Can query element state from game |
| Phase 3 | All control wrappers functional |
| Phase 4 | All page objects created |
| Phase 5 | 90% test pass rate |
| Phase 6 | Tests run in CI/CD pipeline |

### 7.9.2 Quality Metrics

| Metric | Target |
|--------|--------|
| Test reliability | > 95% pass rate on same code |
| Test execution time | < 5 minutes for smoke tests |
| Code coverage (UI code) | > 80% of UI paths |
| Defect detection | Catch UI regressions before release |

---

## 7.10 Recommended First Steps

### 7.10.1 Week 1 Tasks

1. **Create project structure**
   - Create `Oravey.UITestFramework.Stride` project
   - Add basic dependencies

2. **Implement proof-of-concept**
   - Simple named pipe communication
   - Basic element query
   - One working button click test

3. **Validate approach**
   - Test with real game
   - Measure timing reliability
   - Identify any blockers

### 7.10.2 Quick Win: Minimal Viable Test

```csharp
// Target: Get this test passing within first 2 weeks
[Fact]
public void MinimalViableTest()
{
    // Start game
    var driver = new StrideGameDriver();
    driver.StartGame();
    
    // Connect automation
    var channel = new NamedPipeChannel();
    channel.Connect(TimeSpan.FromSeconds(10));
    
    // Query main menu button
    var response = channel.SendCommand(new AutomationCommand
    {
        Type = "Query",
        Target = "MainMenu.NewGame",
        Method = "GetState"
    });
    
    // Verify button is visible
    var state = JsonSerializer.Deserialize<ElementState>(response.Result);
    Assert.True(state.IsVisible);
    
    // Click button
    var simulator = new StrideInputSimulator();
    simulator.Click(state.Bounds.Center());
    
    // Verify navigation occurred
    var nameInputResponse = channel.SendCommand(new AutomationCommand
    {
        Type = "Query",
        Target = "NameInput.NameField",
        Method = "GetState"
    });
    var nameInputState = JsonSerializer.Deserialize<ElementState>(nameInputResponse.Result);
    Assert.True(nameInputState.IsVisible);
    
    // Cleanup
    driver.StopGame();
}
```

---

## 7.11 Future Considerations

### 7.11.1 Potential Enhancements

| Enhancement | Description | Priority |
|-------------|-------------|----------|
| Visual regression testing | Compare screenshots between runs | Low |
| Performance testing | Measure UI response times | Low |
| Accessibility testing | Verify keyboard navigation | Medium |
| Cross-platform testing | Test on different Windows versions | Low |
| Parallel test execution | Run tests concurrently | Medium |

### 7.11.2 Alternative Approaches

If named pipe approach proves problematic:

1. **Shared memory** - Faster but more complex
2. **HTTP server** - More debuggable but more overhead
3. **Direct in-process** - Simplest but less isolation

---

*Document Version: 1.0*  
*Last Updated: January 2025*
