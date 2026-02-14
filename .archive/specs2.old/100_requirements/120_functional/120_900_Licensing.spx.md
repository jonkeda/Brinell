# functional Licensing
- **id**: FR-900
- **title**: Dependency Licensing
- **priority**: medium
- **status**: draft
- **category**: Compliance

The framework must use only permissively licensed dependencies to allow unrestricted commercial use.

## capabilities

### PermissiveLicenses
- **id**: FR-900.1
- **title**: Permissive license requirement

All dependencies must use permissive licenses:

| License | Allowed |
|---------|---------|
| MIT | Yes |
| Apache 2.0 | Yes |
| BSD (2-clause, 3-clause) | Yes |
| LGPL | Yes (with restrictions) |
| ISC | Yes |
| Unlicense | Yes |
| GPL | No |
| AGPL | No |
| Commercial-only | No |

### CommercialUse
- **id**: FR-900.2
- **title**: Commercial use without fees

Dependencies must allow:
- Commercial use
- Modification
- Distribution
- Private use
- No per-seat licensing
- No runtime fees

### ProhibitedDependencies
- **id**: FR-900.3
- **title**: Prohibited dependencies

Specific prohibitions:

| Library | Reason | Alternative |
|---------|--------|-------------|
| FluentAssertions (v7+) | Commercial licensing | Framework assertions, Shouldly |

Prohibition list maintained and updated.

### LicenseAuditing
- **id**: FR-900.4
- **title**: License auditing

Framework releases should include:
- Dependency license audit
- Third-party license notices
- Attribution where required
- License compatibility verification

### TransitiveDependencies
- **id**: FR-900.5
- **title**: Transitive dependency compliance

License requirements apply to entire dependency tree:
- Direct dependencies
- Transitive dependencies
- Build-time dependencies (for distribution)

---

## relationships

- Applies to all framework packages
- Affects [FR-800 Extensibility](120_800_Extensibility.spx.md) choices

---

## constraints

- No runtime license checks
- No dependency on license servers
- License must allow CI/CD usage
- License must allow distribution in test assemblies
