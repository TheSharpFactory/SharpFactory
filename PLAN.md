# TheSharpFactory — Avalonia UI and file-based Sync

Decided by the user (2026-11-05):

- The UI moves to **Avalonia**. Only the UI changes: the generator, the schema reader and the `.edmj` format stay as they are.
- The app runs on **Windows, macOS and Linux**.
- **Visual Studio automation is removed.** Sync writes the files, project files and solution file itself.
- Sync keeps the ability to **choose an open solution**. It supports **`.sln` and `.slnx`**; `.slnx` is preferred for new solutions.
- **No parallel WPF app.** The WPF app is deleted once the new UI covers every field (the checklist below).

## Architecture

```
SF.Models, SF.Db, SF.CSharpGenerator, SF.CodeGenerator   (unchanged generator and schema reader)
SF.Utils      model loading/saving, schema reading, generation helpers (Visual Studio code removed)
SF.Engine     NEW: everything the UI used to do itself
                 - ModelBuilder        build/update a model from the database (was in the WPF model screen)
                 - ModelValidation     model name and connection rules
                 - GenerationRunner    the one generation order, shared by Sync and GenCheck
                 - SolutionSync        file-based Sync: projects, references, solution file, stale files, preview
                 - TargetSolutionFinder open/remembered/nearby solutions for the Sync target list
                 - AppSettingsStore    per-user settings (recent files, Sync targets, theme)
SF.App        NEW: the Avalonia app (MVVM, CommunityToolkit.Mvvm, Fluent theme, AvaloniaEdit)
SF.Cli        NEW: `sharpfactory sync <model.edmj> --solution <file>`, `generate`, `preview`
Tests/SF.Tests          engine tests (Sync into temporary folders)
Tests/SF.App.Tests      headless Avalonia tests (no window, no screen takeover)
```

Rules that apply throughout: the C# rules in `BACKLOG.md`; generated `.cs` output stays byte-identical
(checked with GenCheck against the reference solution and the test baseline); generated code is never built or tested
inside the repo (only in a folder outside it).

## Steps

All done in 1.1.0.0 – 1.1.0.3 (see `RELEASE_NOTES.md`). CI and the release workflow run for the first time once
the repository is on GitHub.

1. Field inventory (this file).
2. Engine: move the logic out of the WPF screens; one generation order for Sync and GenCheck; generated
   files always use CRLF (no change on Windows, identical output on macOS and Linux).
3. File-based Sync: project files, project references, `.sln`/`.slnx` through
   `Microsoft.VisualStudio.SolutionPersistence`, stale-file removal (Sharp Factory files only), preview with
   diffs, the target-solution list, and the CLI.
4. Remove the Visual Studio automation and the Windows-only code (EnvDTE, COM message filter, vswhere,
   registry, admin checks); per-OS app-data folders for logs and settings.
5. The Avalonia app: every screen in the checklist.
6. Headless UI tests, and the parity check: a model built and edited through the new UI equals one built
   by the old app.
7. Delete the WPF app; CI on Windows, macOS and Linux; packaging per OS.

## Parity checklist

Every field and action of the WPF app, and where it lives in the new UI. A row is done when the new UI
has it and a test covers it.

**Status (1.1.0.2): every row is done.** The tests are in `Tests/SF.App.Tests`: `ParityTests` (named after
the rows: `A1_…`, `C1_C4_…`, `M1_M7_…`, `D1_…`, `B1_…` to `B7_…`, `S1_S4_…`, `L1_L11_…`, `Y1_Y5_…`, `U1_…`,
`H1_H2_…`) and `DatabaseParityTests` (M8–M10, U2–U3: builds and updates the test model through the new UI
against SharpFactoryTest and compares the saved `.edmj` with the one the WPF app built; runs when
`SHARPFACTORY_TEST_EDMJ` names that file). A2 (the `.edmj` on the command line) is `App.FindModelFileArgument`
feeding `OpenFile`, the same path A1 tests.

### App

| # | WPF app | Model field / behavior | New UI |
|---|---|---|---|
| A1 | Load (only when nothing is loaded) | open `.edmj` | Welcome page **Open**, toolbar **Open**, recent files |
| A2 | `.edmj` passed on the command line | open at start | same |
| A3 | Save (Save As dialog when the file is new) | `ModelUtils.Save` | **Save** / **Save As**, unsaved-changes marker, confirm on close |
| A4 | Title shows the file path | — | title bar and toolbar |
| A5 | Version in the corner | `Versionable` | toolbar and About |
| A6 | Model combo box (pick the current model) | `ModelContainer.Models` | model list on the Models page and in the toolbar |
| A7 | Global error handler, error log | `LogManager` | same, log folder in the per-OS app-data folder |

### New model container (first Add, when nothing is loaded)

| # | WPF app | Model field / behavior | New UI |
|---|---|---|---|
| C1 | Root Namespace | `CommonSettings.RootNamespace` (trimmed, spaces → `_`) | New wizard step 1 |
| C2 | Dependencies Folder (default `Dependencies`) | `CommonSettings.DependenciesFolder` (trimmed, spaces → `_`) | New wizard step 1 |
| C3 | Indent Size (default 4, number > 0) | `CommonSettings.IndentSize` | New wizard step 1 |
| C4 | Use Root Namespace in Project Name (default on) | `CommonSettings.UseRootNSInProjectName` | New wizard step 1 |
| C5 | — | `CommonLayers` = `Defaults.CommonLayers()` | same |

### Add model

| # | WPF app | Model field / behavior | New UI |
|---|---|---|---|
| M1 | Model Name | `Model.Name`: required, unique, not equal to the root namespace | Connect step |
| M2 | Server (default: machine name) | `ConnectionInfo.Server` | Connect step |
| M3 | Enable Integrated Security (default on) | `ConnectionInfo.DisableIntegratedSecurity` (inverted) | Connect step, with a note for macOS/Linux |
| M4 | User (required without integrated security) | `ConnectionInfo.User` | Connect step |
| M5 | Password (required without integrated security) | `ConnectionInfo.Password` | Connect step (masked) |
| M6 | Database drop-down, loaded from the server | `ConnectionInfo.Database` | Connect step (**Load databases**) |
| M7 | Validation and connection errors | `ValidateDbInfo`, `ValidateConnectionInfo` | inline errors |
| M8 | Object tree: category, schema and object check boxes; schemas/objects found; counts | `DbObject.IsSelected`, `DbObjectSchema.IsSelected` | Choose step: tree with search, counts, select all/none |
| M9 | Progress console, procedure/function/sequence warnings | `PopulateModelFromDbModel` | Progress step: step log, warnings |
| M10 | Model layers | `Defaults.ModelLayers()` | same |

### Update model

| # | WPF app | Model field / behavior | New UI |
|---|---|---|---|
| U1 | Connection summary (name, server, database, user, integrated security), read-only | `Model.ConInfo` | Update step 1 |
| U2 | Object tree with the current selection | `DbModelUtils.OverlayCurrentModel` | Choose step |
| U3 | Progress, then the updated model replaces the old one | swap in `ModelContainer.Models` | Progress step |

### Delete model

| # | WPF app | Model field / behavior | New UI |
|---|---|---|---|
| D1 | Confirm, delete, select the first remaining model | `ModelContainer.Models.Remove` | Models page **Delete** with confirm |

### Model browser (read-only)

| # | WPF app | Model field | New UI |
|---|---|---|---|
| B1 | Tree: category (count) → schema (count) → table/view (property count) → properties; procedures and functions (parameter count) → parameters; sequences | `Model.Categories` | Models page tree, with search |
| B2 | Table/view: name, store name, primary key, navigation properties, unique keys (expand for columns) | `Entity.N`, `SN`, `PK.Ps`, `NPs`, `UKs` | details pane |
| B3 | Property: name, store name, .NET type, store type, nullable, identity, computed, SQL generated, read-only, default, max length, parameter mode | `Property.N`, `SN`, `T`, `ST`, `Nul`, `Ide`, `IsComp`, `IsGen`, `RO`, `Def`, `S`, `M` | details pane |
| B4 | Procedure: name, store name, returns; result column picker → name, store name, .NET type, store type, nullable, max length | `StoredProc.N`, `SN`, `En.N`, `En.Ps` | details pane |
| B5 | Function: name, store name, returns (entity name or scalar type); return columns or scalar → same 6 fields | `Function.N`, `SN`, `REn`, `En`, `Sc` | details pane |
| B6 | Sequence: name, store name; returns .NET type, store type | `Sequence.N`, `SN`, `Sc.T`, `Sc.ST` | details pane |
| B7 | Sync button hidden when the model is marked synced | `Model.Synced` | Sync disabled with a note |

### Settings (all models)

| # | WPF app | Model field | New UI |
|---|---|---|---|
| S1 | Use Root Namespace in Project Name | `CommonSettings.UseRootNSInProjectName` | Settings page, with live naming preview |
| S2 | Root Namespace | `CommonSettings.RootNamespace` | Settings page |
| S3 | Dependencies Folder | `CommonSettings.DependenciesFolder` | Settings page |
| S4 | Indent Size | `CommonSettings.IndentSize` | Settings page |

### Layers (project name and namespace for each)

| # | WPF app | Model field | New UI |
|---|---|---|---|
| L1 | Entities | `Model.Layers.Entity.Project`, `.Namespace` | Layers page, model section |
| L2 | Nav Property Enumerations | `Model.Layers.NavPropEnum` | same |
| L3 | Property Enumerations | `Model.Layers.PropEnum` | same |
| L4 | Entity Utilities | `Model.Layers.EntityUtil` | same |
| L5 | Repositories | `Model.Layers.Repository` | same |
| L6 | Repository Interfaces | `Model.Layers.IRepository` | same |
| L7 | Sorting, Filtering and Pagination (Query) | `CommonLayers.Query` | Layers page, common section |
| L8 | Exceptions | `CommonLayers.Exceptions` | same |
| L9 | Repository Common | `CommonLayers.Repository` | same |
| L10 | Repository Container | `CommonLayers.RepoContainer` | same |
| L11 | Repository Container Interface | `CommonLayers.IRepoContainer` | same |

### Sync

| # | WPF app | Model field / behavior | New UI |
|---|---|---|---|
| Y1 | Choose a Visual Studio instance | target solution | target list: open in an IDE, synced before, near the `.edmj`, Browse, New `.slnx` |
| Y2 | Choose a .NET version | `CommonSettings.TargetFramework` (.NET 10) | Sync page |
| Y3 | Progress console, final report (files per project), errors | — | progress step log, report |
| Y4 | Administrator mismatch checks | Visual Studio only | dropped with Visual Studio |
| Y5 | — | — | Preview: projects to create/move, references, files added/changed/deleted, diffs |

### Help

| # | WPF app | New UI |
|---|---|---|
| H1 | Documentation: 3 sections, 21 links to thesharpfactory.com | Documentation page, opens the browser |
| H2 | About: product name, edition, version, notice | About page |
