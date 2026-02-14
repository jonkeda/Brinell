# goal DebugFriendly
- **id**: G-005
- **title**: Make test failures easy to diagnose
- **priority**: high
- **success**: When a test fails, the developer can identify the root cause within 5 minutes using logs, screenshots, and error messages without needing to reproduce the failure locally

Turn test failures into actionable insights, not frustrating mysteries.

## rationale

The value of a test is not just in catching bugs, but in helping fix them quickly. Comprehensive logging, automatic screenshots, and detailed error messages transform test failures from "something broke" into "here's exactly what went wrong and why."

## achievedBy

- FR-006: Logging and Diagnostics (structured logging, CSV format)
- FR-010: Error Handling (contextual error messages, exception logging)
- FR-004: State Verification (expected vs actual in assertions)
