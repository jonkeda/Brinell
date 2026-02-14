# goal NativePerformance
- **id**: G-008
- **title**: Leverage native automation capabilities without abstraction overhead
- **priority**: medium
- **success**: Framework operations perform within 10% of direct automation driver calls. No adapter layers that hide platform capabilities.

Abstraction should simplify, not slow down or limit.

## rationale

Many automation frameworks introduce adapter layers that add overhead and hide platform-specific capabilities. By accessing automation drivers directly and exposing platform extensions when needed, the framework provides convenience without sacrificing performance or capability.

## achievedBy

- FR-007: Platform-Specific Automation (direct driver access)
- FR-001: Multi-Platform Support (independent implementations)
