# goal OpenSourceFriendly
- **id**: G-006
- **title**: Maintain open source compatibility with permissive licensing
- **priority**: medium
- **success**: All framework dependencies use permissive open source licenses (MIT, Apache 2.0, BSD). No commercial license fees for users.

Keep the framework free and open for all users, from hobbyists to enterprises.

## rationale

Commercial licensing requirements create barriers to adoption and legal complexities for users. By using only permissively licensed dependencies and avoiding libraries with commercial licensing (like FluentAssertions post-v6), the framework remains truly open source and free to use.

## achievedBy

- FR-011: Dependency Licensing
