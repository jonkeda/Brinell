# functional Extensibility
- **id**: FR-008
- **title**: Extension and customization support
- **priority**: medium
- **status**: approved
- **tags**: extensibility, customization

The framework should support extension and customization by users.

## capabilities

### VirtualMethods
- **id**: FR-008.1
- **title**: Virtual methods for override

All base class methods should be virtual. Virtual methods must allow override in derived classes. Overrides must be able to call base implementation.

### CustomControls
- **id**: FR-008.2
- **title**: Custom control types

Users must be able to create custom control types. Custom controls must be able to inherit from framework base classes. Custom controls must be able to add platform-specific functionality.

### CustomPages
- **id**: FR-008.3
- **title**: Custom page object bases

Users must be able to create custom page object base classes. Custom page objects must be able to override default behaviors. Custom page objects must maintain framework patterns.

### ThirdPartyControls
- **id**: FR-008.4
- **title**: Third-party control library support

For third-party control libraries (Telerik, DevExpress, Syncfusion), separate NuGet packages should be created:
- Brinell.Wpf.Telerik
- Brinell.Wpf.DevExpress
- Brinell.Maui.Syncfusion

These packages must reference the base platform package and follow the same interface patterns.
