# functional DependencyLicensing
- **id**: FR-011
- **title**: Permissive open source dependencies
- **priority**: medium
- **status**: approved
- **tags**: compliance, licensing

Framework dependencies should use permissive open source licenses.

## capabilities

### LicenseRequirements
- **id**: FR-011.1
- **title**: Permissive license requirements

Framework dependencies should use licenses that:
- Allow commercial use without fees
- Do not require per-developer or per-seat licensing
- Include at minimum: MIT, Apache 2.0, BSD, LGPL

Commercial/paid dependencies must be documented and approved.

### ProhibitedDependencies
- **id**: FR-011.2
- **title**: Prohibited libraries

The framework must not depend on FluentAssertions library due to commercial licensing requirements (post v6.x).

Alternatives:
- Built-in Assert methods on control objects (preferred)
- Shouldly (MIT license)
- xUnit assertions
- Custom assertion helpers
