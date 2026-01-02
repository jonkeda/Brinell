namespace Brinell.Testing.Accessibility;

/// <summary>
/// Accessibility testing helpers for WCAG 2.1 compliance.
/// Checks ARIA attributes, color contrast, keyboard navigation, and screen reader support.
/// </summary>
public class AccessibilityTester
{
    private readonly List<AccessibilityIssue> _issues = new();

    /// <summary>
    /// Check if element has accessible name (ARIA label or text).
    /// </summary>
    public void AssertAccessibleName(bool hasName, string selector)
    {
        if (!hasName)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Selector = selector,
                Rule = "ARIA-1",
                Message = $"Element {selector} must have accessible name (aria-label or text content)"
            });
        }
    }

    /// <summary>
    /// Check if interactive element has proper role.
    /// </summary>
    public void AssertProperRole(string selector, string expectedRole, string? actualRole)
    {
        if (actualRole != expectedRole)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Selector = selector,
                Rule = "ARIA-2",
                Message = $"Element {selector} should have role='{expectedRole}' but found '{actualRole}'"
            });
        }
    }

    /// <summary>
    /// Check color contrast ratio (WCAG AA requires 4.5:1 for text).
    /// </summary>
    public void AssertColorContrast(double contrastRatio, string selector, WCAGLevel level = WCAGLevel.AA)
    {
        var minimumRatio = level switch
        {
            WCAGLevel.A => 3.0,
            WCAGLevel.AA => 4.5,
            WCAGLevel.AAA => 7.0,
            _ => 4.5
        };

        if (contrastRatio < minimumRatio)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Selector = selector,
                Rule = "COLOR-1",
                Message = $"Element {selector} has contrast ratio {contrastRatio:F2}:1 but needs {minimumRatio}:1 for WCAG {level}"
            });
        }
    }

    /// <summary>
    /// Check if form field has associated label.
    /// </summary>
    public void AssertFormFieldLabel(bool hasLabel, string fieldId)
    {
        if (!hasLabel)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Selector = $"[id='{fieldId}']",
                Rule = "FORM-1",
                Message = $"Form field {fieldId} must have associated <label> element"
            });
        }
    }

    /// <summary>
    /// Check if keyboard navigation works (tabindex, focus visible).
    /// </summary>
    public void AssertKeyboardNavigable(bool isNavigable, string selector)
    {
        if (!isNavigable)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Selector = selector,
                Rule = "KEYBOARD-1",
                Message = $"Element {selector} must be keyboard accessible (proper tabindex and focus styles)"
            });
        }
    }

    /// <summary>
    /// Check if focus is visible (not hidden by CSS).
    /// </summary>
    public void AssertFocusVisible(bool isVisible, string selector)
    {
        if (!isVisible)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Warning,
                Selector = selector,
                Rule = "FOCUS-1",
                Message = $"Element {selector} should have visible focus indicator (e.g., outline or border)"
            });
        }
    }

    /// <summary>
    /// Check if image has alt text.
    /// </summary>
    public void AssertImageAltText(bool hasAlt, string imagePath)
    {
        if (!hasAlt)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Selector = $"img[src='{imagePath}']",
                Rule = "IMAGE-1",
                Message = $"Image {imagePath} must have descriptive alt text"
            });
        }
    }

    /// <summary>
    /// Check ARIA live region configuration.
    /// </summary>
    public void AssertLiveRegion(bool isConfigured, string selector, string? actualPolite)
    {
        if (!isConfigured || actualPolite == null)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Warning,
                Selector = selector,
                Rule = "ARIA-3",
                Message = $"Dynamic content at {selector} should use aria-live='polite' for screen reader announcement"
            });
        }
    }

    /// <summary>
    /// Check heading hierarchy (h1 -> h2/h3, no skipped levels).
    /// </summary>
    public void AssertHeadingHierarchy(int currentLevel, int? previousLevel, string selector)
    {
        if (previousLevel.HasValue && currentLevel > previousLevel.Value + 1)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Warning,
                Selector = selector,
                Rule = "HEADING-1",
                Message = $"Heading hierarchy skipped from h{previousLevel} to h{currentLevel} at {selector}"
            });
        }
    }

    /// <summary>
    /// Check if page has skip link.
    /// </summary>
    public void AssertSkipLink(bool hasSkipLink)
    {
        if (!hasSkipLink)
        {
            _issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Warning,
                Selector = "body",
                Rule = "PAGE-1",
                Message = "Page should have skip-to-main-content link at top"
            });
        }
    }

    /// <summary>
    /// Check ARIA attributes validity.
    /// </summary>
    public void AssertValidAriaAttributes(Dictionary<string, string> attributes, string selector)
    {
        var validRoles = new[] { "button", "link", "navigation", "main", "region", "complementary", "contentinfo" };
        
        if (attributes.TryGetValue("role", out var role))
        {
            if (!validRoles.Contains(role))
            {
                _issues.Add(new AccessibilityIssue
                {
                    Severity = IssueSeverity.Error,
                    Selector = selector,
                    Rule = "ARIA-4",
                    Message = $"Invalid ARIA role '{role}' on {selector}"
                });
            }
        }
    }

    /// <summary>
    /// Get all accessibility issues found.
    /// </summary>
    public List<AccessibilityIssue> GetIssues() => _issues.ToList();

    /// <summary>
    /// Get summary of issues.
    /// </summary>
    public AccessibilitySummary GetSummary()
    {
        var errors = _issues.Count(i => i.Severity == IssueSeverity.Error);
        var warnings = _issues.Count(i => i.Severity == IssueSeverity.Warning);

        return new AccessibilitySummary
        {
            TotalIssues = _issues.Count,
            Errors = errors,
            Warnings = warnings,
            IsAccessible = errors == 0,
            Issues = _issues.ToList()
        };
    }

    /// <summary>
    /// Assert no critical accessibility issues.
    /// </summary>
    public void AssertAccessible()
    {
        var errors = _issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
        if (errors.Any())
        {
            var message = $"Accessibility check failed with {errors.Count} error(s):\n" +
                string.Join("\n", errors.Select(e => $"  [{e.Rule}] {e.Message}"));
            throw new AccessibilityException(message);
        }
    }

    /// <summary>
    /// Assert accessibility meets WCAG level.
    /// </summary>
    public void AssertWCAGCompliance(WCAGLevel level)
    {
        var summary = GetSummary();
        if (!summary.IsAccessible)
        {
            throw new AccessibilityException(
                $"Page does not meet WCAG {level} compliance. Found {summary.Errors} critical error(s).");
        }
    }

    /// <summary>
    /// Clear issues for next test.
    /// </summary>
    public void Reset() => _issues.Clear();

    /// <summary>
    /// Generate accessibility report.
    /// </summary>
    public string GenerateReport()
    {
        var summary = GetSummary();
        var report = $"""
Accessibility Report
====================
Total Issues: {summary.TotalIssues}
Errors: {summary.Errors}
Warnings: {summary.Warnings}
Accessible: {(summary.IsAccessible ? "Yes" : "No")}

Issues:
{string.Join("\n", summary.Issues.Select(i => $"  [{i.Severity}] {i.Rule}: {i.Message} ({i.Selector})"))}
""";
        return report;
    }
}

/// <summary>
/// Accessibility issue found during testing.
/// </summary>
public class AccessibilityIssue
{
    public required IssueSeverity Severity { get; set; }
    public required string Selector { get; set; }
    public required string Rule { get; set; }
    public required string Message { get; set; }
}

/// <summary>
/// Summary of accessibility testing results.
/// </summary>
public class AccessibilitySummary
{
    public required int TotalIssues { get; set; }
    public required int Errors { get; set; }
    public required int Warnings { get; set; }
    public required bool IsAccessible { get; set; }
    public required List<AccessibilityIssue> Issues { get; set; }
}

/// <summary>
/// Severity of accessibility issue.
/// </summary>
public enum IssueSeverity
{
    Error,      // Blocks accessibility
    Warning     // Should be fixed but not critical
}

/// <summary>
/// WCAG conformance level.
/// </summary>
public enum WCAGLevel
{
    A,          // Basic accessibility
    AA,         // Enhanced accessibility (recommended)
    AAA         // Advanced accessibility
}

/// <summary>
/// Exception for accessibility violations.
/// </summary>
public class AccessibilityException : Exception
{
    public AccessibilityException(string message) : base(message) { }
}

/// <summary>
/// Extension methods for accessibility testing.
/// </summary>
public static class AccessibilityExtensions
{
    /// <summary>
    /// Create accessibility tester instance.
    /// </summary>
    public static AccessibilityTester CreateAccessibilityTester() => new();

    /// <summary>
    /// Check accessibility and throw if issues found.
    /// </summary>
    public static void CheckAccessibility(this AccessibilityTester tester)
    {
        tester.AssertAccessible();
    }

    /// <summary>
    /// Get formatted accessibility report.
    /// </summary>
    public static string Report(this AccessibilityTester tester) => tester.GenerateReport();
}
