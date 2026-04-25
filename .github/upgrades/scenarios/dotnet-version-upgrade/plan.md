# dotnet-version-upgrade Plan

## Overview

**Target**: Migrar proyecto `TEST_IDENNA` de `.NET Framework 4.8` a `net10.0-windows`.
**Scope**: 1 proyecto (WPF clásico), ~1.1k LOC. Requiere convertir proyecto a SDK-style, actualizar paquetes NuGet y adaptar APIs WPF/Configuration.

## Tasks

### 01-convert-project-to-sdk

Convert the existing classic `.csproj` to SDK-style preserving WPF settings and target `net10.0-windows`.

This includes:
- Replace legacy project XML with SDK-style properties
- Preserve AssemblyInfo, embedded resources, and WPF build actions
- Ensure `<UseWindowsDesktop>true</UseWindowsDesktop>` or appropriate `TargetFramework` suffix is present

**Done when**: Project file is in SDK-style format, builds for `net10.0-windows` (even if compilation errors remain in source-incompatible code sections), and project is loadable in the IDE.

---

### 02-update-nuget-packages

Update NuGet package references to versions compatible with `net10.0`.

This includes:
- Replace obsolete packages included in Framework with framework references
- Update EF Core packages to 10.x where applicable
- Replace or remove packages that have no compatible version and document alternatives

**Done when**: All package references in `TEST_IDENNA.csproj` reference versions compatible with `net10.0` or documented as blockers in `assessment.md`.

---

### 03-implement-api-fixes

Apply source changes required by API breaking changes identified in the assessment (WPF binary incompatibilities, behavioral changes, configuration API migration).

This includes:
- Fix XAML/Code-behind issues caused by changed WPF types or constructors
- Migrate configuration usage from `System.Configuration` to `Microsoft.Extensions.Configuration` or add `System.Configuration.ConfigurationManager` as interim
- Address Uri and other behavioral changes flagged in assessment.md

**Done when**: Project compiles targeting `net10.0-windows` and unit/integration smoke tests run without crashing.

---

### 04-validation-and-testing

Run a full build and manual smoke test of the WPF UI. Verify runtime behavior for key scenarios.

**Done when**: Solution builds with `dotnet build -f net10.0-windows` and manual verification of the UI completes for main flows.

---

### 05-documentation-and-cleanup

Update README and scenario-instructions.md with decisions taken (target framework, commit strategy: manual since no git) and any remaining blockers.

**Done when**: `scenario-instructions.md` updated and `assessment.md` linked with remaining issues.
