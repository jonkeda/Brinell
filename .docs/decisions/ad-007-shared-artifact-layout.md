# AD-007: Shared Artifact Layout

Screenshots, logs, traces, UAT output, and runner reports should use the shared
`TestResults/<run-id>/suites/<suite>/` layout.

**Ruled out:** ad-hoc artifact paths per test or per project.

**Broken when:** a suite writes screenshots or logs outside the shared run-id
layout.
