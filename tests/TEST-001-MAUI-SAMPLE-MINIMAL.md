# TEST-001: MAUI Sample App - Minimal Test Set

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Overview

Minimal test coverage for MAUI sample app validating core functionality of each page type.

---

## Dashboard Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-MIN-001 | Page loads | Dashboard displays KPI cards and statistics |
| MAUI-MIN-002 | Navigation link to Form | Clicking "Go to Form" navigates to User Form page |
| MAUI-MIN-003 | Navigation link to Data | Clicking "View Data" navigates to Data Grid page |

---

## User Form Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-MIN-004 | Text input | Entry field accepts and displays text |
| MAUI-MIN-005 | Select control | Picker displays options and selection updates |
| MAUI-MIN-006 | Date input | DatePicker selects and displays date |
| MAUI-MIN-007 | Toggle control | Switch toggles on/off correctly |
| MAUI-MIN-008 | Checkbox | CheckBox toggles and value reflects |
| MAUI-MIN-009 | Radio button | RadioButton group selects one option |
| MAUI-MIN-010 | Form submission | Submit button submits valid form |
| MAUI-MIN-011 | Form validation | Invalid form shows validation errors |

---

## Data Grid Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-MIN-012 | Grid displays | CollectionView renders list of items |
| MAUI-MIN-013 | Search filter | SearchBar filters list results |
| MAUI-MIN-014 | Sorting | Dropdown sort column changes order |
| MAUI-MIN-015 | Pagination | Next/Previous buttons navigate pages |

---

## File Upload Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-MIN-016 | File selection | Tapping file input opens file picker |
| MAUI-MIN-017 | File upload | File uploads successfully |
| MAUI-MIN-018 | Progress display | Progress bar shows upload progress |
| MAUI-MIN-019 | File list | Uploaded file appears in file list |

---

## Navigation Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-MIN-020 | Menu navigation | Navigation menu items navigate to pages |
| MAUI-MIN-021 | Back navigation | Platform back button returns to previous page |

---

## Gesture Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-MIN-022 | Tap gesture | Tapping control triggers action |
| MAUI-MIN-023 | Swipe gesture | Swiping reveals hidden content |

---

## Summary

- **Total Tests:** 23
- **Coverage:** Core functionality per page
- **Execution Time:** ~5-10 minutes
- **Focus:** Happy path scenarios

