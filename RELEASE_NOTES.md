# TheSharpFactory — Release Notes

Newest first. One entry per change set. Version rules: see [BACKLOG.md](BACKLOG.md).

---

## 1.2.1.3 — 2026-11-13

Changes
- SF-184: `QueryFilterGenerator` adds `IQueryFilter<TProps> : IQueryFilter where TProps : PropertyContainer` to the `IQueryFilter` file and makes `QueryFilter.New` return it. `PropertyFilterGenerator` drops `sealed` from `PropertyFilter` and adds `internal sealed class PropertyFilter<TProps> : PropertyFilter, IQueryFilter<TProps>` (constructor only); `PropGenerator` makes `Prop.CreateFilter` build and return it, and `CreateChange` return `IQueryFilter<XProperty>`. `EntityUtilGenerator`/`EntityUtilsMethodSignatures`: `GetChanges` returns `List<IQueryFilter<XProperty>>`. `EntityRepoGenerator`: the repository's `GetChanges` copies the typed list into a `List<IQueryFilter>` for the engine (null when nothing changed, as before). `UpdateValuesBuilderGenerator`: `Set(IQueryFilter<TProps> change)`. `CSharpFile` writes interface inheritance as ` : X, Y` (no generated interface inherited before).
- Nothing in the SQL engine or the repository base classes changes, so the SQL is the same.

Verification
- The reference model regenerated: 1,790 files, of which the utils (246), the containers (246), the repositories and five query files change, in the lines above.
- A copy of the reference solution's sources with the regenerated code: the two business-logic projects that use `GetChanges`, `Set(change)` and `CreateChange` build with 0 errors and 0 warnings, unchanged.
- `SF.Tests` 66 passed with the database tests on (the SQL Server run compiles the generated code and updates through `GetChanges` and `CreateChange`); `GeneratedChangesTests` checks the typed signatures. `SF.App.Tests` 106 passed.

Commit details

```
Type changes by entity: IQueryFilter<TProps>

GetChanges, CreateChange and QueryFilter.New return IQueryFilter<TProps>
and an update's Set(change) takes only its own entity's changes, so a
change of another entity does not compile. PropertyFilter<TProps> adds the
interface to the filter the SQL engine reads; the repository copies the
typed changes into the engine's plain list. Same SQL.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.1.2 — 2026-11-13

Changes
- SF-147: `StoreUtils.GetRoutineTableResultType` skipped every result column whose `IsHidden` was empty, and SQL Server leaves it empty (DBNull, with Microsoft.Data.SqlClient 5.1.1 and 7.1.1 alike) for a column computed from an expression. `Ticker.GetClientBets`, which returns `CAST(0 AS BIGINT) AS [PlatformBetId]` when called with NULLs, lost its only column on Update and its generated `_Result` lost `PlatformBetId`. Now only a column SQL Server marks hidden is skipped: those are the key columns `CommandBehavior.KeyInfo` adds (checked: `SELECT [Name] FROM [Coverage].[Region]` is described as `Name` plus hidden `CountryCode` and `RegionCode`, and a normal call returns one column). An empty `AllowDBNull` reads as nullable instead of throwing. The column building moved to `ComposeRoutineResult(routineName, DataTable)`; `SF.Utils` lets `SF.Tests` see its internals.
- SF-183: `CreateChange(int propertyId, object value)` switches on `(XProp)propertyId` (one `case XProp.Column:` per property) instead of the property name; `Set(IQueryFilter change)` adds the change as it is, without the owner check, so `PropertyFilter.Owner` is gone and `PropertyFilter.cs` and `Prop.cs` generate exactly as in 1.2.0.9.

Verification
- New `RoutineResultTests.OnlyColumnsMarkedHiddenAreSkipped` with SQL Server's description as it comes back for an expression column, a hidden key column and an empty `AllowDBNull`. `GeneratedChangesTests` checks the enum switch and the unchecked `Set(change)`; `GeneratedPagingTests` updates through `GetChanges` and `CreateChange((int)RegionProp.Name, …)` on SQL Server.
- The user updated the reference model with the Debug build and `GetClientBets_Result.PlatformBetId` came back as `long?`.
- `SF.Tests` 66 passed with the database tests on; `SF.App.Tests` 106 passed.

Commit details

```
Keep expression result columns; CreateChange switches on the enum

The schema reader skipped result columns with an empty IsHidden, which is
how SQL Server describes a column computed from an expression, so a
procedure such as Ticker.GetClientBets lost its result columns on Update.
Only columns marked hidden (the key columns KeyInfo adds) are skipped now.
CreateChange takes the property id and switches on the XProp enum, and
Set(change) adds the change without a check.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.1.1 — 2026-11-13

Changes
- SF-182: `UpdateValuesBuilderGenerator` writes `Set(IQueryFilter change)`: it adds the change to the Set values as it is (the filter `Set(prop, value)` would have made), after checking it is a `PropertyFilter` of an Equals condition whose `Owner` is `TProps` (`ArgumentException` otherwise). `PropertyFilterGenerator` gives `PropertyFilter` an internal `Owner` (the property container's type, first constructor parameter) and `PropGenerator` passes `typeof(TProps)` from `Prop.CreateFilter`. `PropGenerator` writes `public static IQueryFilter CreateChange(string propertyName, object value)` on every entity container: a switch with one case per property returning `QueryFilter.New(prop, FilterConditions.Equals, (T)value)`, `ArgumentException` for any other name.
- Nothing that writes SQL changed: the reference model generated before and after differs in the 246 property containers (`CreateChange` added), `UpdateValuesBuilder.cs` (`Set(change)` added), `PropertyFilter.cs` (`Owner` added, one constructor parameter) and `Prop.cs` (that parameter passed).

Verification
- `GeneratedPagingTests` (SQL Server): an update from `RegionUtils.GetChanges` through `Set(change)` and one from `RegionProperty.CreateChange("Name", …)` both write the new name; `Set(StoreProperty.CreateChange(…))` on a Region update throws. New `GeneratedChangesTests.AnUpdateTakesAChangeOfItsOwnEntity` checks the generated code.
- `SF.Tests` 65 passed with the database tests on (one needs its own database); `SF.App.Tests` 106 passed.

Commit details

```
Generate Set(change) and CreateChange for updates from changes

UpdateValuesBuilder.Set(IQueryFilter) takes a change made by GetChanges
or by the new XProperty.CreateChange(name, value), refusing a change of
another entity (PropertyFilter knows its container). Code that set changed
columns in a loop with the old untyped Set now has a typed equivalent.
Same Set clause and SQL.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.1.0 — 2026-11-13

Changes
- SF-180 (what happened): the reference model's Sync went into a solution that used its generated projects from another folder, in a solution folder of its own. The Sync page had picked that solution as "open in an IDE": `RunningIdeScanner` read Visual Studio's command line (WMI), which keeps the solution Visual Studio was started with. Sync then moved the five generated projects out of their own solution's folder into the target's (`MoveProject` moved any project whose solution folder was not the dependencies folder), rewired the references of 22 project files to them and dropped their BOM. Everything was restorable from git.
- SF-180 (target): `RunningIdeScanner`, `RecentSolutionsReader` and the `System.Management` package are removed, with COM interop in the app. `TargetSolutionFinder` orders the solutions synced before (most recent first), then the ones near the model file (the nearest first); `FindSuggested` picks the first of those, otherwise none. The Sync page suggests it and says "Synced …" or "Near the model file".
- SF-180 (confirmation): a Sync (not a preview) into another solution than the model was last synced into, or, for a model never synced, into a solution whose folder does not have the model file, asks "Sync into another solution?" with both paths.
- SF-180 (safety, also on the command line): a generated project of the solution outside the solution's folder is used where it is (`ProjectsOutsideSolution` in the report and the CLI summary): never moved, no project file outside the solution's folder is written (no references added or rewired there), and `RemoveStaleFiles` deletes nothing in it. Moving projects inside the solution's folder when the dependencies folder changes works as before. Project files Sync edits keep their BOM.
- SF-181: the update question shows a short message and the release notes in a details box (`DialogViewModel.Details`/`DetailsTitle`, scrolling, 260 px at most); `ReleaseNotes.Summarize` joins the lines a paragraph or list item was wrapped over, starts list items with "•", keeps headings on their own line and shows a link as its text.

Verification
- `SolutionSyncTests.AnotherSolutionsGeneratedProjectsAreUsedWhereTheyAre` rebuilds the production case: a shared solution synced, a second solution with those projects in a folder named after the shared one and its own BOM project file referencing them; the Sync into the second moves nothing, creates nothing, changes no project file (bytes and BOM equal), deletes nothing in the shared projects; the shared solution still syncs normally. `AProjectFileWithABomKeepsIt`.
- `EngineUnitTests`: the list order (synced, then nearest), nothing suggested five folders up, `IsModelInSolutionFolder`. App: the suggestion (near, then last synced), the confirmation (Cancel writes nothing, Sync goes ahead, a preview never asks), the question for a solution away from the model; the update question's notes box with the 1.2.0.8 release text, screenshotted.
- `SF.Tests` 64 passed with the database tests on; `SF.App.Tests` 106 passed.

Commit details

```
Never move another solution's projects; pick the right Sync target

Sync used a generated project outside the solution's folder by moving it
into the solution and rewiring every reference to it. Such projects are
now used where they are: never moved, their project files never changed,
nothing deleted in them; project files keep their BOM. The Sync page no
longer guesses a solution open in an IDE (Visual Studio's command line
keeps the solution it was started with): it suggests the solution last
synced, then the one nearest the model file, and asks before a Sync into
another solution. The update question shows the release notes in their
own box.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.9 — 2026-11-13

Changes
- SF-179: the generated `NavProps<TProps>` implements `IEnumerable<NavProp>` (`GetEnumerator` over its items, plus the explicit non-generic one), so `new NavProps<CustomerProperty> { CustomerNavProperty.Orders }` compiles. Production code written for the old API used `new NavProps<XNavProperty> { ... }` in 71 places; with this it only needs the type argument changed.
- `CSharpFile.MemberSignature` writes each modifier with its own trailing space and nothing for `AccessModifiers.None`, so an explicit interface member has no modifier and no stray space. No existing member used `None`: the rest of the generated code is byte-identical.

Verification
- The Coverage model and the reference model generated with 1.2.0.8 and with this version: only `<Root>.Query/Query/NavProps/NavProps.cs` differs, by the interface and the two methods.
- `SF.Tests` 64 passed with the database tests on (the quoted-names test skips without its database); `GeneratedPagingTests` now also compiles and runs `ByPKAsync(..., new NavProps<RegionProperty> { RegionNavProperty.Stores })` against SQL Server and checks the stores are loaded. New `GeneratedNavPropsTests.NavPropsOfAContainerIsACollection` checks the generated declaration.

Commit details

```
Let NavProps<T> take a collection initializer

The generated NavProps<TProps> implements IEnumerable<NavProp>, so
new NavProps<CustomerProperty> { CustomerNavProperty.Orders } compiles,
as code written for the old query API did with NavProps<XNavProperty>.
The C# writer writes no access modifier for explicit interface members.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.8 — 2026-11-13

Changes
- SF-178 (1): the CHANGELOG heading said 1.2.0.6 while VERSION was 1.2.0.7, which would have stopped the release tag. The cause: every change set bumps VERSION, but the CHANGELOG section kept the number it was written with, and only release.yml (at tag time) compared them. The heading is now `## 1.2.0.8`, and `ReleaseFilesTests.TheChangelogHasASectionForTheCurrentVersion` fails on every test run (locally and in CI) when the newest CHANGELOG section is not VERSION's, naming the heading to rename.
- SF-178 (2): updates are signed. release.yml signs `SHA256SUMS.txt` with the private key in the `UPDATE_SIGNING_KEY` secret (`openssl dgst -sha256 -sign`, ECDSA P-256) and publishes `SHA256SUMS.txt.sig`; before publishing it verifies that signature against `App/SF.App/Packaging/update-signing-key.pem`, so a missing or wrong key stops the release. The app embeds that public key (`UpdateSignature`) and installs a download only when the checksums' signature matches it; a release without the signature is offered as a page only. Nothing beyond the checksums is downloaded when the signature fails.

Verification
- `dotnet build SF.slnx -c Release`: no warnings. `SF.App.Tests` 102 passed and `SF.Tests` 60 passed (the database tests skip locally, as before; CI runs them on Linux).
- New tests: an update signed with another key, checksums changed after signing and a missing signature all fail with nothing changed; a signature made by openssl is read; the release key is built in and is what the installer trusts; a release without `SHA256SUMS.txt.sig` cannot be installed in place. The changelog test was seen failing with the old heading.
- The release key pair signed and verified with openssl, including with the public key file in CRLF.

Commit details

```
Sign updates and keep the changelog heading on VERSION

The release workflow signs SHA256SUMS.txt (ECDSA P-256, key in the
UPDATE_SIGNING_KEY secret) and checks the signature against the public
key built into the app; the app installs an update only when the
signature matches. A test fails whenever the newest CHANGELOG section is
not the version in VERSION.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.7 — 2026-11-13

Changes
- SF-177: CI failed for the push of 1.2.0.3 to 1.2.0.6 in the UI tests on Linux and macOS (Windows passed; the engine tests, including the SQL Server paging test, passed everywhere). The job logs need a GitHub sign-in, so the causes come from the code:
  - **macOS:** `AReleaseDownloadCanUpdateItselfButABuildFromTheSourceCannot` asked `InstallLocation.Find` for the current system since 1.2.0.6, which on macOS expects an `.app` bundle around the program; the test checks a program in a plain folder. It now asks for the Windows or the Linux layout (the macOS bundle has its own test).
  - **Linux:** `WhenAFileCannotBeReplacedTheOthersArePutBack` and `ARunningProgramIsReplacedByRenamingIt` were Windows-only and skipped elsewhere, and CI's Linux job fails on a skipped test (`xUnit.FailSkips`). Both run on every system now: the replacement is blocked by a file open without sharing on Windows and by a read-only left-over `LICENSE.sf-old` folder on Linux and macOS; the running program is a copy of `cmd.exe` on Windows and of `sleep` on Linux and macOS (which also rename and delete a running program, so the clean-up succeeds there at once).

Verification
- `dotnet test SF.slnx -c Release -- xUnit.FailSkips=true` with the database tests on (Windows): `SF.Tests` 63 passed, `SF.App.Tests` 103 passed, none skipped. The Linux and macOS branches run only in CI.

Commit details

```
Run the update tests on Linux and macOS too

The folder-layout test asks for the Windows or Linux layout instead of
the current system (macOS expects a bundle). The roll-back and
running-program tests no longer skip outside Windows, which CI's Linux
job counts as failures.

Bump version to 1.2.0.7.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.6 — 2026-11-12

Changes
- SF-176: the full code review of 1.2.0.3 to 1.2.0.5 (user, 2026-11-12: "full code review and test again", then "Fix all 7"; the eighth, the 1.2.0.4 notes' walk result, was filled in when the walk finished).
  - **Cancel cannot be ignored any more.** Cancel and the start of the file swap go through one atomic state (`UpdateCancellation`): either the update stops with the app unchanged, or Cancel says "Too late to cancel: the files are being replaced." The installer reports when the swap starts (`UpdateProgress`, stage `Replacing`; the download percentage is stage `Downloading`) and the About page hides Cancel then (`UpdatesViewModel.CanCancel`). Before, a Cancel clicked after unpacking said "Cancelling the update…" and the app restarted as the new version anyway.
  - **An update in progress is left alone by another copy of the app.** The download folder holds a lock file, open and unshared, for the whole update (`.sharpfactory-update/.lock`; on Linux and macOS .NET takes an advisory lock for that). A second copy's clean-up when it starts skips a locked download folder, and its own update stops with "Another copy of the app is updating this folder right now." Before, it deleted the first copy's download.
  - `InstallLocation.Find` takes the system (`AppPlatform`: Windows, Linux, macOS) instead of deciding Windows or Linux itself, so all three layouts are tested on every system.
  - `WindowAppHost` moved next to the window (`Views`), so the services no longer depend on a view.
  - The update check's `IOException` catch is gone: `HttpClient` reports a connection lost while reading the answer as `HttpRequestException`, which was already handled.
  - Tests: the paging test also checks the total of a paged query with a to-many navigation property (25 records, 3 pages); the app tests use the engine tests' `TestFolder` (linked; `Fixtures` moved to its own file) instead of a copy.
  - The 1.2.0.4 notes' verification has the walk's result: no paged read fails any more.
  - From the second full review (user, 2026-11-12: "fix the two minor issues"): `CHANGELOG.md` has the 1.2.0.6 section the Release workflow needs before a tag, for users: regenerate (paged reads were broken in 1.1.8.8), automatic updates, the command line in the app downloads, the fixes since 1.1.8.8, and the downloads table with `SHA256SUMS.txt`. The install stage is a type (`UpdateProgress`) instead of the progress value 101 sent with the download percentage.
- Tests: `UpdateTests` +5 (39): each system's program name, an update in progress left alone by another copy, Cancel and the swap cannot both win, a cancelled update stops before replacing anything, the swap is reported once Cancel no longer works; Update now also checks that Cancel is gone after the swap.

Verification
- `dotnet test SF.slnx -c Release` with the database tests on: `SF.Tests` 63 passed, `SF.App.Tests` 103 passed, none skipped. 0 build warnings.
- Against the reference solution (outside the repository), with the 1.2.0.6 command line: the current reference model (it gained `Trader.BettingEventProfile.SapMarketControl` today) generates 1,790 files that differ from the published 1.1.9.8's output only in `RepositoryBase.cs` (the paging fix); the test model (1,424) and Coverage (174) the same. Against the reference code: 374 identical, 719 changed by the query redesign, 436 renamed enum files, 261 new containers, 3 retired, as before. A re-sync changes 1 file, a fresh sync is identical, both compile with 0 warnings. SchemaCheck: 0 differences on the test model. The 16 business-layer cases: byte-identical results and identical SQL.
- Paired benchmark, reference code against 1.2.0.6 code, 16 real queries, two sessions (results byte-identical in both): CPU per call -12.4% (11 queries less, none more), memory per call -67.0% (12,034 KB to 3,967 KB), garbage collections -54.5%, wall-clock -22.3% (9 faster, none slower). The last benchmark (1.1.9.5) had CPU -7.1%, memory -65.1%, wall-clock +4.7%.
- The real-package check again (outside the repository): a published single-file 1.2.0.3 in an install folder updated itself from a local feed on `127.0.0.1` to a throwaway test build stamped 9.9.9.9 (a made-up version, higher than any release, built only for this check); the bundled `sharpfactory.exe --version` then printed `sharpfactory 9.9.9.9`.

Commit details

```
Make Cancel atomic and protect an update in progress

Cancel and the start of the file swap share one atomic state, and
Cancel is hidden once the swap starts. The download folder holds a
lock so another copy of the app leaves it alone. Find takes the
platform; WindowAppHost moves to Views; the app tests share TestFolder.
The install stage is a type instead of progress value 101, and
CHANGELOG.md has the 1.2.0.6 section for the next release.

Bump version to 1.2.0.6.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.5 — 2026-11-12

Changes
- SF-175: automatic updates (user, 2026-11-12: "it should check automatically once a day, it should check on button click inside the about tab and should update once the user accepts").
  - **The check:** once the window is up, and every hour while the app is open, the app asks GitHub for the latest release (`api.github.com/repos/TheSharpFactory/SharpFactory/releases/latest`, which never returns drafts or pre-releases) when the last successful check was a day ago or more. The request sends only `User-Agent: TheSharpFactory/<version>`. A failed automatic check says nothing and is tried again later. The About page has **Check for updates** and a "Check for updates once a day" box (`AppSettings.CheckForUpdates`, on by default; `LastUpdateCheck` holds the time of the last successful check).
  - **The question:** "An update is available", with the release text shortened to plain text (`ReleaseNotes.Summarize`: what comes before its Downloads section, at most about 700 characters), and **Later** or **Update now**. An automatic check never interrupts a dialog, a wizard or a running Sync: it asks at the next hourly tick. Nothing is downloaded before **Update now**.
  - **The install:** asks about unsaved changes first, then downloads this system's package into `.sharpfactory-update` next to the app, checks it against the release's new `SHA256SUMS.txt` (`release.yml` now publishes it), unpacks it (.zip, or .tar.gz on Linux), renames each file and folder it replaces (the app, `sharpfactory`, the licenses; on macOS the whole `TheSharpFactory.app`) to `*.sf-old`, moves the new ones in and starts the new version with the model file that was open. A running program cannot be overwritten on Windows but can be renamed, which is what makes this work. The new version deletes the `*.sf-old` files when it starts (trying again for a few seconds while the old one exits). If a step fails, the files already moved are put back and the user is told; if even that fails, the user is told to download the release again. **Cancel** on the About page stops the download; the app stays as it was.
  - **Where it cannot update itself** (a build from the source, a renamed program, a folder it cannot write to, macOS running the app from its temporary copy, a release without checksums or without this system's download), it offers to open the release page instead.
  - The comment that the app "connects to nothing but the database" now says it also connects to GitHub for the update check; the close message while busy says "A model build, Sync or update is running".
- Tests: `UpdateTests` (34): reading GitHub's answer (versions, downloads, https only, errors, a dropped connection, the User-Agent), the once-a-day rule, the release text, the checksums file, where the app runs from (Windows/Linux layout, the macOS bundle and its temporary copy, a build from the source), the zip and tar.gz installs, a damaged download, a dropped download, a download without the program, putting the files back when one cannot be replaced (Windows), replacing a running program (Windows), and in the headless app: up to date, Later, a failed check, the daily rule and the check box, not interrupting a dialog, Update now with the restart, unsaved changes asked first, a failed update, Cancel, and the release page offer. No test uses the network (a fake HTTP handler answers).
- Code review (two rounds, 10 findings, all fixed): the swap and the clean-up take the folder's list before changing it; dropped connections and the time limit while reading become messages ("Nothing was changed"); Cancel; a failed roll-back says so; the running-program test waits for the clean-up; code rules (`var`, verb-first names); the release-text summary moved to `ReleaseNotes`.

Verification
- `dotnet test SF.slnx -c Release` with the database tests on (`SHARPFACTORY_TEST_EDMJ`, `SHARPFACTORY_QUOTED_NAMES_DB`, `SHARPFACTORY_DOCS_DB`): `SF.Tests` 63 passed, `SF.App.Tests` 98 passed, none skipped. 0 build warnings.
- End to end outside the repository: a published single-file 1.2.0.3 build in an install folder and a 9.9.9.9 package (made like `release.yml` makes them) served from `127.0.0.1` with a real HTTP client: the app found 9.9.9.9, downloaded and checked the 84 MB zip against a `sha256sum` file, and swapped the folder; the bundled `sharpfactory.exe --version` then printed `sharpfactory 9.9.9.9`. Not run: starting the new version (it opens the app window), macOS and Linux, and the real GitHub check (it needs a release made after this change).

Commit details

```
Check for updates and update the app in place

Once a day (and from the About page) the app asks GitHub for the latest
release; after the user accepts it downloads the package, checks it
against the release's SHA256SUMS.txt, swaps the files by renaming the
running ones and restarts. Where it cannot update itself it offers the
release page. release.yml publishes SHA256SUMS.txt.

Bump version to 1.2.0.5.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.4 — 2026-11-12

Changes
- SF-174: **every paged read in the generated code failed on SQL Server** with `InvalidOperationException: Invalid attempt to read from column ordinal '0'. With CommandBehavior.SequentialAccess, you may only read from column ordinal 'N' or greater.` The generated readers are sequential (`CommandBehavior.SequentialAccess`), so columns must be read left to right, and a paged read returns the total row count in the last column; `RepositoryBase.MaterializeEntityResult` read that column before the first row's own columns. It now reads the first row, then the total, then the other rows. Found by the full repository walk of the reference model against its test database (937 paged reads failed, with and without the total and with every navigation property; the reference code read them all). The walk of 1.1.4.5 was clean; the first commit in the repository (1.1.8.7) already had it, so the published 1.1.8.8 has it. Of the 1,790 files generated for the reference model only `RepositoryBase.cs` changes (5 lines); the SQL text and parameters are unchanged.
- Tests, so it cannot come back:
  - `GeneratedPagingTests.APagedReadReadsTheRowBeforeTheTotalCount` (runs everywhere): the generated readers are sequential and `MaterializeEntityResult` reads the first row before the total.
  - `GeneratedPagingTests.PagedReadsWorkOnSqlServer` (with `SHARPFACTORY_TEST_EDMJ`; in CI's Linux job, where a skip fails the build): compiles the code generated for the Coverage model with a small program (`Fixtures/PagingRunner/Program.cs.txt`) and pages through rows it adds to `Coverage.Region` and `Coverage.Store` (and deletes): with and without the total, the last page, a to-many and a to-one navigation property, sync and async. Until now no test ran generated code against a database.
  - Both fail on the old template (the database test with exactly the error above) and pass now.
  - `GeneratedResultListTests.AResultListIsCreatedOnItsFirstRow` checks the new loop instead of the old `do/while`.
  - TESTING.md says what the database paging test needs and that it writes (and deletes) `'ZZ'` rows.

Verification
- `dotnet test Tests/SF.Tests/SF.Tests.csproj -c Release` with `SHARPFACTORY_TEST_EDMJ` set: 63 passed, none skipped.
- The full repository walk of the reference model against its test database, the fixed code against the reference code: of 7,879 operations, no paged read fails any more (937 did in 1.2.0.2). 7,337 give the same result; 347 that threw in the reference code now work (the SF-121 and SF-122 fixes); 187 differ only in the async task type the walk prints (the await pass-through); 6 paged reads with navigation properties on the very large `Grading.PeriodSummary` table that the reference code rejected ("Incorrect syntax near ORDER") now run and hit the 30-second timeout; 1 paged read hits a column missing from the test database, as other reads do in both runs.

Commit details

```
Read the first row before the total count in paged reads

The generated readers use CommandBehavior.SequentialAccess, so the
total row count in the last column has to be read after the row's own
columns. Every paged read threw on SQL Server. Adds a test that
compiles the generated code and pages through it on a real database.

Bump version to 1.2.0.4.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.3 — 2026-11-12

Changes
- SF-173: every app download includes the command line (user, 2026-11-12: "CLI should be deployed with the desktop app. always"). `release.yml` copies `sharpfactory.exe` (Windows) or `sharpfactory` (Linux, macOS), from the same build and the same version, next to `TheSharpFactory` in each app download. On macOS it is `TheSharpFactory.app/Contents/MacOS/sharpfactory`, marked executable like the app. The app's `licenses` folder now covers the packages of both programs. The separate `sharpfactory-cli-*` downloads stay, for build servers that don't want the app.
- This comes before automatic updates: the updater will replace both programs together.

Verification
- The workflow's publish step, run locally into a scratch folder for all five systems: each app download holds `TheSharpFactory`, `sharpfactory`, `LICENSE`, `THIRD-PARTY-NOTICES.md` and `licenses`; the macOS bundle has both programs in `Contents/MacOS`; the bundled `sharpfactory.exe` is byte-identical to the one in `sharpfactory-cli-win-x64.zip` and runs from the app folder (`--version`, usage); every package in the command line's `licenses/INDEX.txt` is also in the app's. Each app download grows by the size of the command line, about 33–35 MB (now 80–84 MB).
- No code changed.

Commit details

```
Ship the command line in every app download

The release workflow copies sharpfactory next to the app in each
app download (inside the .app bundle on macOS) and collects the
licenses of both programs. The separate command-line downloads stay.

Bump version to 1.2.0.3.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.2 — 2026-11-12

Changes
- SF-172: Sync is right under Home in the navigation rail (Home, Sync, Models, Layers, Settings, Documentation, About). Home and Sync are the pages used most: open the model file, Update it on Models (where opening a file lands), then Sync. Sync stays off until a model file is open, as before.
- Tests: `SyncIsRightUnderHomeInTheRail` checks the order in the rail and that Sync still needs an open model file.

Verification
- `dotnet test SF.slnx -c Release`: `SF.App.Tests` 60 passed, `SF.Tests` 58 passed (database tests skip locally). 0 build errors.

Commit details

```
Put Sync right under Home in the navigation rail

Home and Sync are the pages used most, so Sync moves up from fifth
to second place in the rail.

Bump version to 1.2.0.2.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.1 — 2026-11-12

Changes
- SF-171: four fixes from the UI review (items 9, 10, 11 and 13).
  - **Sync counts (9):** a zero added, changed or deleted count is a plain grey pill. Only a count above zero is green, amber or red (`SyncPageViewModel.HasAdded`, `HasChanged`, `HasDeleted`).
  - **Message signs (10):** a warning (the Delete model question) shows an amber sign, an error a red one. `DialogViewModel.HasSign` is replaced by `HasErrorSign` and `HasWarningSign`.
  - **Status line (11):** the message clears when the model goes from saved to unsaved, so "Opened" or "Saved" no longer sits next to the unsaved-changes dot. Every message is set after the change it reports, so "Added model", "Deleted model" and "Updated model" still show.
  - **Cleanups (13):** the `badge` style was the same as `pill` and is gone (the Models kind label uses `pill`). The added, changed and deleted colours are one set of rules for pills and Sync's status chips. The Layers column header is one control (`LayerTableHeader`) used by both tables. A wrong comment in Settings is corrected. Settings' "Names this gives" stays: it shows the result while you type the root namespace.
- Tests: `UiReviewFixesTests` (5): grey zero counts, amber and red signs, the status line clearing, a message about the change itself staying, and the merged styles giving the same colours as before.

Verification
- `dotnet test SF.slnx -c Release`: `SF.App.Tests` 59 passed, `SF.Tests` 58 passed (database tests skip locally). 0 build errors.

Commit details

```
Grey zero Sync counts, amber warning sign, clear stale status

From the UI review: a zero added/changed/deleted count on Sync is a
plain pill; a warning question shows an amber sign and an error a red
one; the status line clears when the model gets unsaved changes. Also
merges the duplicate badge/pill and chip colour styles and shares the
Layers column header.

Bump version to 1.2.0.1.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.2.0.0 — 2026-11-12

Changes
- SF-170: the Sync page's solution list follows the settings. This replaces the 1.1.9.8 settings copy (SF-168, item 6), which only stopped the crash.
  - **The bug:** the list was built once per scan. Each row's "Synced" date was fixed then, and nothing rebuilt the list after a Sync. A Sync left the badge saying "Synced 3 days ago", and a solution the Sync had just created was not in the list, until a refresh. The 1.1.9.8 fix had also left a stale date when a Sync finished during a scan.
  - **The fix:** the background job only does the slow searches (open IDEs, the IDEs' recent lists, the solutions near the model file) and never reads the settings. The list is built on the UI thread, where the settings change, from those searches and the settings as they are now. That happens when a scan ends, and again right after a Sync records its target. Choosing the same solution again while the list is rebuilt is no new target, so the Sync's report stays.
  - `TargetSolutionFinder.Find` has an overload that takes the near-model list. `AppSettingsStore.Copy` is gone.
- Tests: `ASyncShowsInTheSolutionListAtOnce`, which fails on 1.1.9.9, and `ASyncDateRecordedAfterTheScanIsShownWhenTheListIsBuilt`.

Verification
- `dotnet test SF.slnx`: `SF.Tests` 58 passed, `SF.App.Tests` 54 passed (database tests skip locally). `dotnet build SF.slnx`: 0 errors.

Commit details

```
Rebuild the Sync page's solution list when the settings change

The list was built once per scan, so a Sync left its "Synced" date
stale and a solution it created out of the list until a refresh. The
scan now only searches (off the UI thread, without the settings); the
list is built on the UI thread from the settings as they are, after
each scan and after each Sync.

Bump version to 1.2.0.0.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.9 — 2026-11-12

Changes
- SF-169: the schema-reader log rolls over at 150 KB instead of 1 MB (`IOUtils.LogFileMaxBytes`), so each file opens quickly in Notepad. Five files are still kept per model and source.

Verification
- `SF.Tests` 59 passed; `SchemaReaderLogsAppendAndRollOver` checks the rollover against `LogFileMaxBytes`.

Commit details

```
Roll the schema-reader log over at 150 KB

A 1 MB log is slow to open in Notepad; 150 KB files open at once. Five
files are still kept.

Bump version to 1.1.9.9.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.8 — 2026-11-12

Changes
- SF-168: three fixes from the code review of 1.1.9.7 (items 8, 6 and 4).
  - **Schema-reader logs roll over (item 8).** `IOUtils.WriteLog` replaced the whole file on every call, so only the last entry per model and source survived. The file name was fixed at process start from the date and the minute, with no hour, so sessions an hour apart overwrote each other. It now appends a dated entry to `<model>Logs\<source>\log.txt`. Past 1 MB the file rolls over (`log.txt` becomes `log.1.txt`, and so on), and 5 files are kept (`LogFileMaxBytes`, `LogFilesKept`).
  - **The Sync page's scan works on a copy of the settings (item 6).** The background solution scan read the shared `AppSettings.SyncTargets` while a Sync that finished meanwhile added to it on the UI thread ("Collection was modified", an empty solution list). `FindTargetsJob` now takes `AppSettingsStore.Copy(settings)` on the UI thread.
  - **String IN lists on nvarchar and nchar columns are Unicode literals (item 4).** The generated SQL engine wrote IN / NOT IN strings as `'value'`. Against an nvarchar or nchar column, characters outside the database's code page became `?` and matched nothing. For those two column types the values are now `N'value'`; every other column's SQL is unchanged. In the generated `Database.cs` / `DatabaseSelect.cs`: `AppendQuoted(value, unicode)` and `WriteConditionWhenIsArray(..., prop)`.
- Tests: `SchemaReaderLogsAppendAndRollOver`, `ACopyOfTheSettingsDoesNotChangeWithTheOriginal`, `StringInListsOnUnicodeColumnsAreUnicodeLiterals`.

Verification
- The 1.1.9.8 CLI (published like the release workflow) synced a copy of a real model's generated code: only `Database.cs` and `DatabaseSelect.cs` changed. A read-only probe against a local test database sends `[PlayerName] IN (N'Malla',N'O''Neil',N'Łukasz')` for an nvarchar column and `[Alias] IN ('Hector','O''Neil')` for a varchar column, with the expected rows.
- `dotnet test SF.slnx`: `SF.Tests` 59 passed, `SF.App.Tests` 52 passed (database tests skip locally). `dotnet build SF.slnx`: 0 errors.

Commit details

```
Roll over schema logs, copy settings for the scan, N'' for nvarchar IN

Code review fixes: the schema reader's log replaced itself on every
write, so it now appends and rolls over at 1 MB (5 files kept); the Sync
page's background scan read the settings while a finished Sync changed
them, so it gets a copy; string IN lists on nvarchar and nchar columns
are written as N'...' so no character is lost to the code page.

Bump version to 1.1.9.8.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.7 — 2026-11-12

Changes
- SF-167: generated result lists are created on their first row, and a to-one navigation property's list is sized by its parent's row count.
  - **First row:** every result set's list used to be created at 500 (or the TOP, page size, or 1 for key lookups) before any row was read, so an empty result set cost a 4 KB array that was thrown away. `MaterializeEntityResult` now reads the first row before creating the list, and returns `null` without allocating when there is none. A list that gets rows starts at the same capacity as before.
  - **To-one sizing:** a to-one navigation property (one-to-one or many-to-one) returns at most one row per row of its parent result set, and the parent is always read first. Its list now starts at the parent's row count (1 for a key lookup) instead of 500, with the same 10,000 cap, and never grows. The to-one decision is the one `PopulateNavProperties` already makes (assigned rather than added to a list).
  - **How:** `GetNavPropMaterializers` (protected, overridden by each repository) and `ComposeNavPropMaterializers` (internal) fill the materializer array directly and record each to-one result set's parent; `MaterializeResult` uses the parent's row count. The temporary list of materializers is gone. The code writer has `Do()` / `DoWhile(condition)` for the loop.
- SQL text and parameters are byte-identical; results are unchanged. The signature of the protected `GetNavPropMaterializers` changes; hand-written partial classes are not affected.
- Tests: `AResultListIsCreatedOnItsFirstRow`, and `ToOneNavigationPropertiesAreTheOnesThatAreAssigned` (every generated repository of the Coverage model: each navigation property marked to-one is exactly one that `PopulateNavProperties` assigns). Both fail on 1.1.9.6's generator.

Verification
- Measured on a real business layer's queries against a local test database (paired benchmark outside the repository, 5 sessions plus swapped, A/A and single-query checks; results byte-identical in every run): 112 KB less per call for a 35-navigation-property query (17 of its 36 result sets empty, 22 to-one), 32 KB for two 9-property queries, 16 KB for three queries with nested navigation properties; queries without navigation properties are unchanged. CPU over the 16 queries: +0.3% and +0.4%, no measurable change.
- The 1.1.9.7 CLI (published like the release workflow) synced a copy of that generated code: 191 files changed, the same code as the measured prototype (only comment wording differs), results byte-identical to 1.1.9.6 and the same memory per call.
- `dotnet test SF.slnx`: `SF.Tests` 56 passed, `SF.App.Tests` 52 passed (database tests skip locally). `dotnet build SF.slnx`: 0 errors.

Commit details

```
Create result lists on the first row, size to-one lists by the parent

Every result set's list was created at 500 before a row was read, so an
empty result set cost a 4 KB array. Lists are now created on the first
row, at the same capacity. A to-one navigation property returns at most
one row per parent row, so its list starts at the parent's row count.
Same SQL and results; 16-112 KB less per call for queries with
navigation properties.

Bump version to 1.1.9.7.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.6 — 2026-11-12

Changes
- SF-166: generated async methods that only hand a call on return the inner `Task` instead of awaiting it. Each such method used to cost one async state machine per call (one heap object of about 140 bytes, plus a resume step when the result came back), with no work of its own to do after the `await`.
  - Changed (625 methods in a 270-table test model): `ByPKAsync`, `ByIdAsync`, `ByUK*Async`; the base classes' `ToListAsync`, `TopAsync`, `SelectQueryAsync` (base and navigation-property override), `SelectSingleAsync(NavProps<T>)`, `CreateAsync`, the four `UpdateAsync`, `UpdateOrCreateAsync`, `DeleteAsync(query)`, `DeleteAnyAsync`, `UpdateOptimisticEntityAsync`, `StoredProcedureAsync`, `FunctionAsync`, `SequenceAsync` and each sequence's method; the engine's `Database.SelectAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `FunctionAsync`, `SequenceAsync`, `StoredProcAsync`, the three `GetStore*Async`, and `SelectQuery`/`DeleteQuery`/`FunctionQuery.ExecuteAsync`.
  - Kept async: every method that does work after its `await` (`DeleteBy*Async`'s `> 0`, `SelectSingleAsync`'s row checks, `SelectManyAsync`, `First`/`Single` and their `OrDefault` forms, the entity insert/update helpers, stored-procedure wrappers, `SqlExecute`).
  - The code writer has a new `MethodDeclaration.SkipAwait`: an async method declared with it is written without the `async` keyword, and `GenerateCall(true)` writes a call without `await`. The repository generator decides which methods use it.
- SQL text and parameters are byte-identical. The public API is unchanged: `async` is not part of a method's signature, so interfaces, hand-written partials and mocks compile as they are.
- One behaviour difference: an exception thrown before the database call (while building the query) now comes out of the method call instead of out of the `await`. Code that awaits on the same line sees no difference. Database errors (timeouts, deadlocks, connection failures) still come out of the `await`, from `SqlExecute`, which stays async.
- Tests: `AsyncMethodsThatOnlyHandACallOnReturnTheTask` (named methods on both sides of the line) and `NoGeneratedAsyncMethodOnlyReturnsAnAwaitedCall` (every generated async method of the Coverage model). Both fail on 1.1.9.5's generator.

Verification
- Measured on a real business layer's queries against a local test database (paired benchmark outside the repository, 2 sessions plus a 3-session recheck, results byte-identical): every async query allocates 430–680 bytes less per call (4–6% for key lookups and short lists, under 1% for large results); the synchronous control query is unchanged. CPU and wall-clock differences were below what the machine can measure.
- The 1.1.9.6 CLI (published like the release workflow) synced a copy of that generated code: 255 files changed, byte-identical to the hand-made copy that was benchmarked and parity-checked. A scan of the business layer found nothing that behaves differently: its generated async calls are awaited where they are made, except two fire-and-forget updates whose only code before the database call is `Query.Prepare()`, which returns a field.
- `dotnet test SF.slnx`: `SF.Tests` 54 passed, `SF.App.Tests` 52 passed (database tests skip locally). `dotnet build SF.slnx`: 0 errors.

Commit details

```
Return the inner task from pass-through async methods

Generated async methods that only hand a call on (ByPKAsync, ToListAsync,
the engine's SelectAsync and others) awaited it, costing an async state
machine per layer per call. They now return the inner task: 430-680 bytes
less per async query, same SQL, same public API. Methods that do work
after the await keep it. The code writer gets SkipAwait for this.

Bump version to 1.1.9.6.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.5 — 2026-11-10

Changes
- SF-164: `BACKLOG.md` matches what happened when the project went public. Sprint proposals are built from the backlog, and five rows still said "Open" or "Ready, not run yet" for finished work:
  - SF-007 (source control): Done in 1.1.8.7, a fresh public repository `TheSharpFactory/SharpFactory` seeded from the cleaned code; `.gitignore` in place, no binaries or `.vs` tracked.
  - SF-040 (download address): Done in 1.1.8.8, the website's Download button goes to the latest release (website 1.0.0.4).
  - SF-041 (release packaging): Done in 1.1.8.8, the Release workflow published `v1.1.8.8` with all ten downloads.
  - SF-099 (CI on three systems): Done in 1.1.8.8, green on GitHub at 1.1.8.8 and 1.1.8.9.
  - SF-112 (addresses for the website): Done in 1.1.8.8, the website links to the repository, Issues, Discussions and the latest release.
- The versioning rule's stale `WPF/SharpFactory/Versionable.cs` path was already replaced in 1.1.9.1 (SF-160: `VERSION` is the only place to edit).

Verification
- The facts come from the product owner's check of GitHub's public API and the live website (2026-11-10) and match this project's own records (the 1.1.8.8 release and CI runs, SF-158, SF-159). `git ls-files` lists no `.vs`, `bin`, `obj`, `.dll` or `.exe` entries.
- Backlog only: no code changed.

Commit details

```
Bring the backlog up to date

Five rows still called finished work open or not run yet: the public
repository, the download address, the release workflow, CI on three
systems and the addresses for the website. They are marked done with
what was done.

Bump version to 1.1.9.5.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.4 — 2026-11-10

Changes
- SF-163: the About page links to the releases and to the issue tracker, so a user who hits a problem has a way from the app to the release notes and to a bug report. Next to "thesharpfactory.com" there are now "Releases" (`https://github.com/TheSharpFactory/SharpFactory/releases`, tooltip "Release notes and downloads on GitHub") and "Report an issue" (`.../issues`, tooltip "Report a problem or ask for a feature on GitHub"), in the same link style with the external-link icon, on one row. `AboutPageViewModel` has the two `Uri` properties next to `Website`.
- The links open the browser only when clicked: the app still makes no connection except to the database, and there is no update check. SF-112's description says so.
- Test: `H1_H2_DocumentationAndAbout` checks the three links' addresses.

Verification
- UI review: three judges graded the headless screenshot of the About page. Round 1: Look 8/8/8, Usability 8/8/7, Comfort 8/7/8, Appeal 7/8/7, all signed off; two asked for more room between the links and above the row (gap 20 -> 28 px, 4 px above), which round 2 has. One asked to rename "Releases" to "Release notes"; kept, because the page holds the downloads as well and matches GitHub's own name (the tooltip says "Release notes and downloads"). Round 2: Look 9/9/8, Usability 8/8/8, Comfort 9/9/8, Appeal 8/8/7, all "keep" and signed off, the third judge agreeing with "Releases".
- `SF.App.Tests` 52 passed (database tests skip locally). `dotnet build SF.slnx`: 0 errors, 0 warnings.

Commit details

```
Link the About page to the releases and the issues

A user who hits a problem had no way from the app to the release notes
or the issue tracker. The About page links to both next to the website,
in the same style; they open the browser only when clicked.

Bump version to 1.1.9.4.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.3 — 2026-11-10

Changes
- SF-162: the macOS first-run instruction is correct for current macOS. Since macOS 15 (Sequoia), right-click → Open no longer gets an unsigned app past Gatekeeper: the user opens the app once (macOS refuses), then chooses **Open Anyway** in System Settings > Privacy & Security. Right-click → Open still works on macOS 14 and earlier. `README.md` (Releases), `CHANGELOG.md` (the 1.1.8.8 downloads paragraph) and the comment in `.github/workflows/release.yml` say so; SF-102's description too.
- The published 1.1.8.8 release page on GitHub still has the old sentence: editing `CHANGELOG.md` does not change a published release. The next release's text comes from the corrected file.

Verification
- No other place in the repository gives the old instruction (searched for "right-click" and "Control-click"; only the release-notes history has it).
- `.github/scripts/release_notes.py 1.1.8.8` extracts the corrected paragraph.

Commit details

```
Correct the macOS first-run instruction

Right-click and Open no longer gets an unsigned app past Gatekeeper
since macOS 15. The README, the changelog and the release workflow now
point to Open Anyway in Privacy & Security, with right-click and Open
for macOS 14 and earlier.

Bump version to 1.1.9.3.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.2 — 2026-11-10

Changes
- SF-161: `sharpfactory --version` (or `sharpfactory version`) prints one line to standard output, `sharpfactory 1.1.9.2`, and exits with 0, so a build pipeline can log which version it runs. The number is the assembly's version, which the build takes from `VERSION` (SF-160). Before, `--version` was an unknown command: usage and exit code 2.
- The usage's first line shows the version (`The Sharp Factory 1.1.9.2`) and lists `--version`.
- `README.md` (the command list) and the summary comment of `App/SF.Cli/Program.cs` list the new option. The website's command list was handed to the website project (docs request #2).

Verification
- `dotnet run --project App/SF.Cli -- --version`: `sharpfactory 1.1.9.2`, exit code 0; `version` the same. A misspelled `--verison` still prints the usage and exits with 2; `--help` shows the version in its first line and exits with 0.
- `dotnet build SF.slnx`: 0 errors, 0 warnings.

Commit details

```
Add sharpfactory --version

The command line prints its version, taken from the assembly that the
build stamps from VERSION, and the usage shows it in its first line.

Bump version to 1.1.9.2.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.1 — 2026-11-10

Changes
- SF-160: the binaries carry the real version. No project set a version, so every assembly said 1.0.0.0 (file version) and 1.0.0 (product version), in local builds and in the downloads. `Directory.Build.props` next to `SF.slnx` reads `VERSION` into `<Version>`, so every project, in every build and in the release workflow alike, gets it as its assembly, file and product version (the SDK adds `+<commit>` to the product version).
- `VERSION` is now the only place to edit: `App/SF.App/Versionable.cs` returns the assembly's version instead of a constant, and `Common/SF.Common/Versioning/VersionManager.cs` (internal, unused, left from the old website, still saying 1.0.7.13) is deleted. The versioning rule in `BACKLOG.md` says so.
- Test: `TheAppHasTheVersionInTheVersionFile` (what the About page and the toolbar show equals `VERSION`).

Verification
- `TheSharpFactory.dll`, `sharpfactory.dll` and `SF.Engine.dll`: FileVersion 1.1.9.0 (the version before this bump; it follows `VERSION`), ProductVersion `1.1.9.0+c60a852...`. A release-style publish (`dotnet publish App/SF.Cli -c Release -r win-x64 --self-contained`) gives `sharpfactory.exe` the same.
- Generated code unchanged: the 174 files generated from `Coverage.edmj` before and after are byte-identical (`SF.GenCheck`: 174 identical, exit code 0).
- `SF.Tests` 52 passed, `SF.App.Tests` 52 passed (database tests skip locally). `dotnet build SF.slnx`: 0 errors, 0 warnings.

Commit details

```
Stamp the version into the binaries

Every assembly said 1.0.0.0. Directory.Build.props now reads VERSION
into every project, the app reads its version from its assembly, and
the unused VersionManager with its stale number is gone, so VERSION is
the only place the version is kept.

Bump version to 1.1.9.1.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.9.0 — 2026-11-10

Changes
- SF-142: Sync shows the solution open in Visual Studio again. The app's project had `BuiltInComInteropSupport` set to `false`, which writes `System.Runtime.InteropServices.BuiltInComInterop.IsSupported: false` into `TheSharpFactory.runtimeconfig.json`. `System.Management` (WMI) is built on COM interop, so `RunningIdeScanner.ScanWindows` threw `TypeInitializationException` (`System.Management.ManagementPath`) on every call, and `Scan`'s empty `catch` returned an empty list: no candidate was ever labelled "Open in ...". The switch is now `true` (the value of Avalonia's own template). The command line was never affected (it does not scan).
- `RunningIdeScanner.Scan` writes the exception to the error log instead of swallowing it, so a scan that fails can be found.
- The window-title fallback (used when the IDE was started without a solution on its command line, e.g. from the Start menu) is `RunningIdeScanner.GetSolutionNameFromWindowTitle`: a title with no separator, such as `Microsoft Visual Studio` with nothing open, names no solution instead of a solution called "Microsoft Visual Studio".
- Tests: `WindowTitlesGiveTheSolutionName` (engine) and `TheAppCanUseComToFindOpenSolutions` (app: the app's runtime config must not switch COM interop off).

Verification
- `TheSharpFactory.runtimeconfig.json` now has the switch `true`.
- A scratch program with the real `RunningIdeScanner` and the same WMI query: with the switch `false` it fails with the `ManagementPath` exception from the bug; with `true` it returns the running `devenv.exe` (Visual Studio 18) and its command line. With no solution open the scan returns no hint, as it should.
- `SF.Tests` 52 passed, `SF.App.Tests` 51 passed (the database tests skip locally without their settings; CI runs them). `dotnet build SF.slnx`: 0 errors, 0 warnings.
- Not yet verified: the Sync page with a real solution open in Visual Studio started from the Start menu (the window-title path); that needs the user.

Commit details

```
Let Sync see the solutions open in Visual Studio

The app switched COM interop off, so WMI threw on every call and the
scan for open solutions always came back empty without a trace. COM
interop is on again, a failed scan goes to the error log, and a window
title with no solution is no longer read as one.

Bump version to 1.1.9.0.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.9 — 2026-11-10

Changes
- SF-159: the database tests run in CI (user: "this is 100% needed"). On the Linux leg, `.github/scripts/setup_test_databases.py` starts SQL Server 2022 in Docker (the `sa` login with a random password per run, masked in the log), creates `SharpFactoryTest` and `SharpFactoryDocs` from `Coverage.sql` and `SharpFactoryQuote's` from `QuotedNames.sql`, and sets the three test settings. Both test projects run there with `-- xUnit.FailSkips=true`, so a database test that skips fails the build. The same script works on any machine with Docker (`--port` when 1433 is taken, `--remove` to clean up).
- The database tests accept a SQL Server login as well as Windows sign-in (user: "fix the tests to use a user and password ... that would work on linux"): `SHARPFACTORY_QUOTED_NAMES_DB` and `SHARPFACTORY_DOCS_DB` take `server;database` (Windows) or `server;database;user;password`; the parity tests fill the wizard's user and password from the model's connection; the engine update test uses the connection of `SHARPFACTORY_TEST_EDMJ` instead of the fixture's hard-coded `localhost`.
- The parity tests compare with a model file, and a model file only matches a database with the same history (foreign key numbers, object order). In CI the reference is built from the fresh database by `.github/scripts/build_reference_model.cs` (every object, the default layers, the fixture's settings), so the tests check that the wizard builds exactly what the engine builds and that an update leaves the file unchanged. Locally they compare with your own reference as before.
- `Tests/SF.Tests/Fixtures/Coverage.edmj` is saved the way the app saves model files (tables, columns, keys, layers and the connection on one line each; it was fully indented, so the app's output never matched it). Same JSON, and the 174 files generated from it are byte-identical. `AModelWithATableNameThatCannotBecomeAnIdentifierStillOpens` edits the fixture in the new layout.
- `TESTING.md` describes the login format and the Docker setup; `README.md` points at it.
- The website's query composition pages still show the query API from before SF-141; the changes they need were handed to the website project.

Verification
- Rehearsed locally with SQL Server 2022 in Docker, exactly as CI runs it: `SF.Tests` 54 passed, 0 skipped; `SF.App.Tests` 54 passed, 0 skipped (all 7 database tests ran). Without the settings and with `xUnit.FailSkips=true`, the skipped tests fail; without it they skip as before.
- `dotnet build SF.slnx`: 0 errors, 0 warnings. `VERSION` and `Versionable.cs` say 1.1.8.9.

Commit details

```
Run the database tests in CI

On Linux, CI starts SQL Server 2022 in Docker, creates the test
databases from the fixtures and runs both test projects with skips
counted as failures. The database tests accept a SQL Server login as
well as Windows sign-in, the parity tests compare with a reference
model built from the fresh database, and the Coverage fixture is saved
the way the app saves model files (same content, same generated code).

Bump version to 1.1.8.9.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.8 — 2026-11-10

Changes
- SF-158: the first runs on GitHub failed, so 1.1.8.7 published no release; two fixes.
  - `.gitattributes`: `* text=auto eol=crlf`. The repository stores LF and Linux and macOS checked out LF, while `.editorconfig` requires CRLF; the build (which treats formatting as an error) failed with IDE0055 in `FKeyPart.cs` on Linux and macOS, and the Release workflow (on Linux) with it. Every checkout now gets CRLF.
  - `PolishTests.TheSolutionListSaysItIsLooking` failed on Windows: opening the Sync page starts the real search for solutions, which on the build machine ended during the next `Pump` and turned `IsScanning` off right after the test turned it on. The test now waits for that search first, with the new `AppHarness.PumpUntil`.
- `CHANGELOG.md`: the first public release is 1.1.8.8 (the 1.1.8.7 section is now 1.1.8.8; 1.1.8.7 was never released).

Verification
- The failures were read from the GitHub job logs. A clone checked out the Linux way (LF) reproduced the 12 IDE0055 errors; with the new `.gitattributes` the same checkout gets CRLF and builds with 0 errors.
- `dotnet build SF.slnx`: 0 errors, 0 warnings. `SF.Tests`: 51 passed, 3 skipped. `SF.App.Tests`: 50 passed, 4 skipped. `VERSION` and `Versionable.cs` say 1.1.8.8.

Commit details

```
Fix the CI and release builds

Check out every file with CRLF, as .editorconfig requires: Linux and
macOS got LF and the build failed IDE0055. The Sync page test now
waits for the page's own search for solutions before it sets
IsScanning, so the search cannot reset it on a fast machine. The first
public release is 1.1.8.8.

Bump version to 1.1.8.8.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.7 — 2026-11-10

Changes
- SF-157: GitHub releases show real release notes. New `CHANGELOG.md`: one section per published release, written for users, newest first; its first section, 1.1.8.7, introduces the first open-source release and explains the downloads. `release.yml` now checks that the tag equals `v` + `VERSION` and that `CHANGELOG.md` has that version's section before it builds anything, and publishes the release as "The Sharp Factory <version>" with that section as its text (`.github/scripts/release_notes.py`); GitHub's generated commit list is off. `README.md` has a Releases section that says how to publish one.
- SF-156: the third-party notices cover everything in the downloads. `THIRD-PARTY-NOTICES.md` lists every component the self-contained downloads include (the .NET runtime, Avalonia, AvaloniaEdit, ANGLE, the Inter typeface, SkiaSharp and HarfBuzzSharp, TextMateSharp, Onigwrap, CommunityToolkit.Mvvm, DiffPlex, Microsoft.Data.SqlClient and its SNI library, Microsoft.IdentityModel, SolutionPersistence, Newtonsoft.Json, MicroCom, Tmds.DBus) with license and copyright, and carries the MIT, Apache 2.0, BSD 3-Clause (ANGLE) and SIL Open Font License 1.1 (Inter) texts. The Octicons entry is gone (it covered a website icon).
  - Every download gets a `licenses` folder with the license and notice files the packages ship themselves (among them the .NET runtime's, Skia's, HarfBuzz's and Oniguruma's notices), collected from the restore by `.github/scripts/collect_licenses.py`; identical files are copied once and `INDEX.txt` lists every package.
  - `Microsoft.Data.SqlClient.SNI.runtime` (Windows only) is not open source; the notices say so and point at its license terms in `licenses`.
- `release.yml`: the macOS bundle copies the published folder with `cp -R`, so the `licenses` folder comes along (moved to `Contents/Resources` with `LICENSE` and the notices).

Verification
- `collect_licenses.py` on the local restore of the app and the command line: 12 files (408 KB) for the 48 license and notice entries of the packages.
- `release_notes.py 1.1.8.7` prints the new section; for a version without a section it fails with a message.
- `dotnet build SF.slnx`: 0 errors, 0 warnings. `SF.Tests` and `SF.App.Tests` pass as before. `release.yml` itself runs only on a tag push and was not run. `VERSION` and `Versionable.cs` say 1.1.8.7.

Commit details

```
Release notes from CHANGELOG.md and complete third-party notices

GitHub releases now take their text from the version's section of
the new CHANGELOG.md; the release workflow checks that the tag matches
VERSION and that the section exists before it builds. The third-party
notices list every component of the self-contained downloads with its
license, and every download carries a licenses folder with the
packages' own license and notice files.

Bump version to 1.1.8.7.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.6 — 2026-11-10

Changes
- SF-155: The Sharp Factory is released under the MIT License, written by Jonathan Alfaro (JAM) (user, 2026-11-10).
  - New `LICENSE`: the MIT License, "Copyright (c) Jonathan Alfaro (JAM)".
  - `README.md`: a License section naming the author and the license, pointing at `THIRD-PARTY-NOTICES.md`, and saying the code the app generates belongs to the user.
  - `THIRD-PARTY-NOTICES.md`: `Inflector.cs` is listed with its MIT License, "Copyright (c) 2013 Scott Kirkland" (from Inflector-Portable, a fork of Scott Kirkland's Inflector). `Inflector.cs` carries the same copyright line under its "based on" comment.
  - `.github/workflows/release.yml`: every download carries `LICENSE` and `THIRD-PARTY-NOTICES.md` (next to the executable; inside `Contents/Resources` of the macOS app).
- SF-156 (new, open): the release downloads also contain the NuGet packages the app uses; their notices go into `THIRD-PARTY-NOTICES.md` before the first public release.
- No change to the generator's output: `Inflector.cs` is used while generating, its source is not written into generated files.

Verification
- `dotnet build SF.slnx`: 0 errors, 0 warnings. `SF.Tests` and `SF.App.Tests` pass as before.
- The Inflector license text and copyright line were read from Inflector-Portable's `LICENSE.txt`; the original repository is MIT as well.
- `release.yml` was read through, not run (it runs on a tag push). `VERSION` and `Versionable.cs` say 1.1.8.6.

Commit details

```
Release under the MIT License

Add LICENSE (MIT, Jonathan Alfaro) and a License section in the
README that also says the generated code belongs to its user. List
Inflector's MIT License (Scott Kirkland) in THIRD-PARTY-NOTICES.md
and in the header of Inflector.cs, and ship LICENSE and the notices
in every release download.

Bump version to 1.1.8.6.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.5 — 2026-11-10

Changes
- SF-154: the notes no longer name private projects or folders on the development machine, ahead of the public repository. Older release-notes entries and backlog row SF-141 now say "the reference solution", "the reference model", "the test baseline", "the reference baseline", "the prototype" or "a folder outside the repository" instead. What each entry says happened is unchanged.
- No code changes.

Verification
- A search of every file that would be published finds no private project name and no local folder path. `VERSION` and `Versionable.cs` say 1.1.8.5.

Commit details

```
Remove private references from the notes

Older release-notes entries and one backlog row named private
projects and folders on the development machine. They now use neutral
names, so the repository can go public. What each entry records is
unchanged.

Bump version to 1.1.8.5.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.4 — 2026-11-10

Changes
- SF-153: website entries that were added to this repository by mistake are removed. They came from website work (turning the website into a static site, letting its Cloudflare Web Analytics run, and giving it its own repository) and are not app changes; the website keeps its own version and release notes in its own repository.
  - `RELEASE_NOTES.md`: the entries 1.1.8.0, 1.1.8.1 and 1.1.8.2 are gone. Those three numbers moved `VERSION` and `Versionable.cs` from 1.1.7.9 to 1.1.8.2 without any app change; they are not reused, so the history goes from 1.1.7.9 to 1.1.8.3.
  - `BACKLOG.md`: the guiding principle "The website is static" and rows SF-150 and SF-151 are gone. SF-040 and SF-112 are app-side items again: once the repository exists and is public, its addresses (repository, Issues, Discussions, latest release) go to the website project, which tracks its own links. The app links only to thesharpfactory.com and needs no change for it.
  - The 1.1.8.3 entry no longer leans on the removed 1.1.8.2.
- No code changes.

Verification
- `RELEASE_NOTES.md` goes 1.1.8.4, 1.1.8.3, 1.1.7.9; no entry, backlog row or principle describes website work as an app change. `VERSION` and `Versionable.cs` say 1.1.8.4.

Commit details

```
Remove website entries added by mistake

Website work (the static site, its analytics and its own repository)
was recorded here as app versions 1.1.8.0 to 1.1.8.2 without any app
change. Remove those release-notes entries, the backlog rows SF-150
and SF-151 and the "website is static" principle, and turn SF-040 and
SF-112 back into app-side items: hand the repository and release
addresses to the website project once the repository is public. The
numbers 1.1.8.0 to 1.1.8.2 are not reused.

Bump version to 1.1.8.4.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.8.3 — 2026-11-10

Changes
- SF-152: everything that belongs to the app moved into a new folder, `Apps`, which is now the root of this repository: `App`, `Common`, `Dependencies`, `Tests`, `Tools`, `.github`, `SF.slnx`, `VERSION`, the docs (`README.md`, `PLAN.md`, `TESTING.md`, `BACKLOG.md`, these notes, `THIRD-PARTY-NOTICES.md`) and the build files (`.editorconfig`, `.gitattributes`, `.gitignore`, `.dockerignore`) (user, 2026-11-10). The website stays beside it in its own repository.
- No file inside the projects changed: every project, solution and workflow path is relative to the solution, so `SF.slnx` and both workflows work unchanged from the new root.
- `README.md`: the website is no longer a row of the folder table; a line says it is its own repository. `.gitignore` has no `Websites/` entry. `TESTING.md` and `THIRD-PARTY-NOTICES.md` name the website's files by their place in the website repository.

Verification
- `dotnet build SF.slnx`: 0 errors, 0 warnings.
- `SF.Tests`: 51 passed, 3 skipped (need a database). `SF.App.Tests`: 50 passed, 4 skipped (need a database or a screenshot folder).
- `sharpfactory generate` of `Tests/SF.Tests/Fixtures/Coverage.edmj` into a folder outside the repo: 174 files.
- `VERSION` and `Versionable.cs` say 1.1.8.3.

Commit details

```
Move the app into the Apps folder

The app, its tests, tools, docs and build files now live in Apps,
the root of this repository; the website stays beside it in its own
repository. Paths inside the solution are relative, so nothing in the
projects changed. README, .gitignore, TESTING and the third-party
notices no longer treat the website as part of this repository.

Bump version to 1.1.8.3.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.9 — 2026-11-09

Changes
- `BACKLOG.md` SF-046 is Done: it still asked to retire the old licensing database (SCFDb) "on the server", but that database is gone and the solution has no database or server of its own; it only reads the schema of the database the user points it at (user, 2026-11-09: "we removed the database. the whole solution no longre needs a db").
- Correction to 1.1.7.7: its entry said `SCFDb` was not renamed because it was "the real database on the server". That was wrong (read from the stale SF-046 row); the sentence now lists only the history that keeps the old name. The SF-148 backlog row is corrected the same way.
- No code change.

Verification
- No open backlog row and no release-notes entry from 1.1.7.7 on says SF has a database or a server. `VERSION` and `Versionable.cs` say 1.1.7.9; CRLF check OK on the changed files.

Commit details

```
Close SF-046: the old licensing database is already gone

SF has no database or server of its own. The 1.1.7.7 entry and the
SF-148 row no longer call SCFDb a real database on the server.

Bump version to 1.1.7.9.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.8 — 2026-11-09

Changes
- SF-149, the four warnings of the Visual Studio build (user, 2026-11-09: "i see these warnings when i build"):
  - CS0618 in `Tests\SF.App.Tests\TestSupport.cs`: `Bitmap.Save(string, int?)` is obsolete in Avalonia 12.1.6; the screenshot is saved with `Save(path, PngBitmapEncoderOptions.Default)` (`using Avalonia.Media.Imaging;`), still PNG.
  - `Websites\SF.Web.CoreWebsite\wwwroot\css\site.v2.css`, `"none" is not a valid value for the "font-variant-ligatures" property`: `code, kbd, pre` now turn ligatures off with `font-feature-settings: "liga" 0, "clig" 0, "calt" 0;`, the same three features `font-variant-ligatures: none` turns off (discretionary and historical ligatures are off by default).
  - `site.v2.css`, `"1.5" is not a valid value for the "stroke-width" property`: `.step-badge svg` uses `stroke-width: 1.5px`; inside an SVG a CSS pixel is one user unit, so the line is as thick as before.
  - `docs.v2.css`, `"::-webkit-details-marker" is not a valid pseudo-element`: the rule is removed. `.doc-tree summary` already has `display: flex` and `list-style: none`, which hide the disclosure triangle in Chrome, Edge, Firefox and Safari 18.4 and later; only older Safari versions show it again.

Verification
- `dotnet build SF.slnx --no-incremental`: 0 errors, 0 warnings. The CSS warnings come from Visual Studio's CSS editor, not from `dotnet build`; the three rules they named are gone.
- `SF.Tests`: 51 passed, 3 skipped, 0 failed. `SF.App.Tests`: 49 passed, 4 skipped, 1 failed — the known flaky `PolishTests.TheSolutionListSaysItIsLooking`, which passes alone (1 passed).
- Screenshots: `H1_H2_DocumentationAndAbout` with `SF_SCREENSHOTS` set writes `08-documentation.png` and `09-about.png`, both PNG files.
- GenCheck against 1.1.7.7: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both).
- CRLF: the changed files OK.

Commit details

```
Fix the four build warnings (SF-149)

The test screenshots use the Bitmap.Save overload with PNG encoder
options; the website's CSS turns ligatures off with
font-feature-settings, gives the step badge a 1.5px stroke and drops
the ::-webkit-details-marker rule that list-style already covers.

Bump version to 1.1.7.8.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.7 — 2026-11-09

Changes
- SF-148, SCF is renamed SF everywhere (user, 2026-11-09: "rename everything SCF to SF ... i want it renamed to just SF"):
  - The solution `SCF.slnx` is `SF.slnx`; the 14 projects, their folders and `.csproj` files are `SF.App`, `SF.Cli`, `SF.Common`, `SF.CodeGenerator`, `SF.CSharpGenerator`, `SF.Db`, `SF.Engine`, `SF.Models`, `SF.Utils`, `SF.App.Tests`, `SF.Tests`, `SF.GenCheck`, `SF.SchemaCheck` and `SF.Web.CoreWebsite`, with their project references, root namespaces and `InternalsVisibleTo`.
  - Every namespace and `using` (`SF.Models`, `SF.CodeGeneration`, `SF.Engine`, ...), the app's `x:Class` and `using:` names in the `.axaml` files, and the website's `_ViewImports.cshtml`.
  - The eight embedded SQL engine templates: their resource names (`SF.CodeGeneration.Database*.cs`, in the `.csproj` and in `SQLEngineGenerator`), and their namespaces `SF.Query` / `SF.Repository.Common`, which `SQLEngineGenerator` replaces with the model's namespaces as before. Generated code never held "SCF", so no generated file changes.
  - Environment variables of the app tests: `SF_SCREENSHOTS` and `SF_TIMINGS` (were `SCF_...`). Website keys: `SF_NONCE`, `SF_DOC_TOC`. Website log folders renamed the same way.
  - The CI and release workflows, `.gitignore`, `.dockerignore`, `.claude/launch.json`, README, TESTING, PLAN, THIRD-PARTY-NOTICES and the open backlog rows SF-099 and SF-144.
  - Not renamed: earlier release-notes entries and Done backlog rows (history). The assembly names `TheSharpFactory` (app) and `sharpfactory` (CLI) were not SCF and are unchanged.
  - Outside the repository: the helper scripts `routine.sh` and `bump.py`, and the engine-check capture script (the fixture path).
- NuGet packages, upgraded by the user to the latest versions: Avalonia 12.1.6, Avalonia.AvaloniaEdit and AvaloniaEdit.TextMate 12.0.0, CommunityToolkit.Mvvm 8.4.2, DiffPlex 1.9.0, Microsoft.Data.SqlClient 7.1.1, Microsoft.AspNetCore.Mvc.NewtonsoftJson, System.Management and System.Security.Cryptography.Pkcs 10.0.14, Newtonsoft.Json 13.0.4, Microsoft.VisualStudio.SolutionPersistence 1.0.52, Microsoft.NET.Test.Sdk 18.11.0, xunit.runner.visualstudio 4.0.1.
  - `xunit.v3` goes back from 4.0.3 to 3.2.2 (user: "lets go back to 3.2.2"). With 4.0.3, `dotnet test` refused to run on .NET 10 ("Testing with VSTest target is no longer supported by Microsoft.Testing.Platform"), and `Avalonia.Headless.XUnit` 12.1.6, built for xunit.v3 3.2.2, failed all 54 app tests at discovery (`MissingMethodException` in `TestIntrospectionHelper.GetTestCaseDetails`). Move to xunit 4 when the official Avalonia package supports it.
  - `Tests\SF.Tests\TestFolder.cs` and `Tests\SF.App.Tests\TestSupport.cs`: the 4.x attribute `[assembly: Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]` is back to the 3.x `[assembly: CollectionBehavior(DisableTestParallelization = true)]` (`using Xunit;` instead of `using Xunit.v3;`); tests still run one at a time.
- Every `bin` and `obj` folder was removed before building (user: "delete the obj and bin folders in each project before building"). A local hook blocks deleting files, so they were moved to a folder outside the repository.

Verification
- Clean build, `dotnet build SF.slnx`: 0 errors, 1 warning (the known CS0618 in `Tests\SF.App.Tests\TestSupport.cs:132`, fixed in the next entry). The outputs are `SF.*.dll`, `TheSharpFactory.exe` and `sharpfactory.exe`.
- `dotnet test Tests/SF.Tests/SF.Tests.csproj`: 51 passed, 3 skipped, 0 failed. `dotnet test Tests/SF.App.Tests/SF.App.Tests.csproj`: 50 passed, 4 skipped, 0 failed.
- GenCheck against 1.1.7.5 (1.1.7.6 changed no code): test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both).
- No file outside the release notes and the backlog history holds "SCF". The replacement worked on bytes, so line endings and BOMs are unchanged (CRLF check OK on the changed files sampled).

Commit details

```
Rename SCF to SF everywhere (SF-148)

The solution, the 14 projects, their folders, namespaces, the axaml
classes, the embedded engine template names, the test environment
variables, the website keys and log folders, the workflows and the
docs now say SF. Generated code is unchanged. xunit.v3 goes back to
3.2.2 after the NuGet upgrade: 4.0.3 broke dotnet test and the
Avalonia headless tests.

Bump version to 1.1.7.7.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.6 — 2026-11-09

Changes
- SF-146, baselines and full verification, no code change. SF-146 is Done.
- The test baseline: the 6 files that changed with the renamed `Order Line` columns (1.1.7.0; the user agreed to update the baselines for that table) were replaced: `Order_Line.cs`, `Order_LineProp.cs`, `Order_LineProperty.cs`, `Order_LineUtils.cs`, `Order_LineRepository.cs` and `RepoLookup.cs`. The old files moved to a backup folder next to it (6 files). The reference baseline needed no change.

Verification
- GenCheck against the baselines before the replacement: test `1418 identical, 6 different` (exactly the 6 files); ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`. After: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0).
- SchemaCheck on the test model: `185 tables, 2 views, 22 procedures, 4 functions, 2 sequences, 177 foreign keys in the model; 0 differences` (exit 0).
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`). `SCF.Tests`: 51 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — the known flaky `PolishTests.TheSolutionListSaysItIsLooking`, which passes alone (1.1.7.5 run).
- Sync into new solutions under `_scratch\fixes\review2\build\` and build (with `Microsoft.SqlServer.Types` added for the geography/geometry columns): the Coverage model, 174 files, 0 errors; the SharpFactoryTest model, 1,424 files, 2 errors, both the documented SF-083 case ("'Coverage' is a namespace but is used like a type").
- Engine check, `compare.py capture-1.1.4.7.txt capture-1.1.7.0.txt`: commands or parameters differ in 33 operations and results in 32: the 30 / 29 `AllTypes` operations of item #3 and the 3 `Order Line` operations of 1.1.7.0; no other operation differs. `capture-1.1.7.0.txt` is the last capture: no later change set changes the SQL text.

Commit details

```
Update the Order Line baseline files and verify everything (SF-146)

The six test-model files of the renamed Order Line columns replace the
old ones (kept in Baseline.Old-1.1.6.8). GenCheck, SchemaCheck, the
tests, the generated solutions and the engine check confirm the result.
SF-146 is done.

Bump version to 1.1.7.6.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.5 — 2026-11-09

Changes
- SF-146, older bugs found by the review of 1.1.6.8 (they were there before SF-143):
  - `Dependencies\SCF.Models\Utils.cs` `GenerateEntityName`: a table named `s`, `S` or `_s` singularized to an empty name and `Inflector.Capitalize` threw `IndexOutOfRangeException`, so the whole Add or Update failed. When nothing is left after singularizing, the cleaned name is kept (`s` → `S`). Every other name gives the same result as before.
  - `Dependencies\SCF.Utils\StoreUtils.cs` `GetRoutineTableResultType`: the number added to a repeated result column name was never checked against the names already taken, so `Id, Id1, Id` gave `Id1` twice and the property collection threw (the routine was left out). It now counts on until the name is free (`Id, Id1, Id, id` → `Id, Id1, Id2, id3`). A name repeated once still gets `1`, as before.
  - 57 source files ended with a lone `\r` (no line feed), which the CRLF check could not see: each now ends with `\r\n` (generator sources, `SCF.CSharpGenerator`, `SCF.Models`, `SCF.Db\Sql\SqlSchemaColumn.cs`, the website's `ErrorViewModel.cs`). None of them is an SQL engine template, so no generated file changes. No other file in the repository has a lone `\r`.
- `BACKLOG.md`: new row SF-147 (`Open`): result columns that do not come from a table (empty `IsHidden`) are left out without a word; reading them changes the result types of models that read fine today, so it needs a decision.
- `EngineUnitTests.NamesThatCannotBecomeIdentifiersGiveNull` also checks `s`, `_s` and `Orders`.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 51 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- Scratch database `SFFixesReview2`: a table `dbo.s` becomes the entity `S` (the build threw before); a procedure `dbo.Dups` (`SELECT a.Id, a.Id AS Id1, b.Id, c.Id AS id ...`) gets the result columns `Id, Id1, Id2, id3`.
- SchemaCheck on the test model: `0 differences` (exit 0).
- GenCheck against 1.1.7.4: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both).
- Line endings: a scan of every `.cs`, `.md`, `.csproj`, `.sql`, `.edmj`, `.json`, `.axaml`, `.cshtml`, `.py`, `.yml`, `.js` and `.css` file outside `bin`, `obj` and `.vs` finds no lone `\r` and the CRLF check passes for the changed files; BOMs unchanged.

Commit details

```
Fix one-letter table names, repeated result names and stray CRs (SF-146)

A table named s no longer throws; a repeated result column name counts
on until it is free; 57 files that ended with a lone CR end with CRLF.
SF-147 records the result columns the reader skips silently.

Bump version to 1.1.7.5.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.4 — 2026-11-09

Changes
- SF-146, reporting of objects left out as not supported (items #12 and #20 follow-up):
  - `Dependencies\SCF.Engine\ModelBuilder.cs`: `Populate` no longer adds the builder's records to the chooser's `DBObjectCategoryCollection.NotSupported`; it works on its own copy. In the app, going Back and building again (the wizard's retry) logged the records of the first attempt again, also for objects the user had deselected. New overloads `Populate(..., IProgressLog log, List<string> notSupported)` and `Update(..., IProgressLog log, List<string> notSupported)` hand the records to the caller instead of the log; the old signatures log them as before (the app).
  - `Dependencies\SCF.Engine\ModelUpdater.cs` (CLI `update`) uses the new overload, so each record is printed once (it was printed by the log and again by the CLI). It reports only the model's own objects: the database's other unsupported objects were never in the model and were listed on every run. A model object is matched to its record by category, schema and name (`FindNotSupportedRecord`; "Procedures" in the model's names is "Stored procedures" in the records); before, a table that left the database was not reported as gone when a procedure with the same schema and name was not supported.
  - `App\SCF.Cli\Program.cs`: the `not supported:` lines are indented like the other result lines.
  - `Dependencies\SCF.Engine\ModelFileCheck.cs`: the `SyncedModelNames` doc comment no longer says "(as in the old app)".
- Corrections to earlier entries (from the review of 1.1.6.8):
  - 1.1.5.8 said "as in the old app" was nowhere else; it was still in that doc comment (removed here).
  - 1.1.6.6: the first try of item #3 also set `En = null` on stored procedures whose result entity was already empty, which changed 11 generated files of the test model (for example `GetClientBets` returned `int` instead of `StoredProcResult<GetClientBets_Result>`). It was found by the snapshot comparison and fixed (`removed > 0 && Ps.Count == 0`) before the entry was written; that snapshot is kept as `_scratch\fixes\1.1.6.6-firstrun`. The `1.1.6.1b` snapshot is a second GenCheck run of 1.1.6.1, identical to it.
  - The GenCheck lines in 1.1.4.8–1.1.6.8 compare each version with the previous snapshot, not with the baselines.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 51 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- Scratch app `_scratch\fixes\review2\Retry` on `SFFixesReview2`: two builds from the same chooser objects log 6 records each and leave the chooser's list unchanged (it grew before).
- CLI `update --check` on a model of `SFFixesReview2` after renaming `dbo.Good.Name` to `Na.me` (renamed back afterwards): one line `   not supported: Tables: dbo.Good (column 'Na.me' has characters that are not supported)`; the database's other unsupported objects are not listed. (`dbo.FineProc` reads that column, so it failed to read and was listed as left out, as expected.)
- GenCheck against 1.1.7.3: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both).
- CRLF: the four changed files OK.

Commit details

```
Report each unsupported object once, and only the model's (SF-146)

ModelBuilder keeps its records apart from the chooser's list, so a
retry does not repeat them; the CLI update prints the model's own
unsupported objects once, matched by category. Corrections to the
1.1.5.8 and 1.1.6.6 entries.

Bump version to 1.1.7.4.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.3 — 2026-11-09

Changes
- SF-146, item #22e follow-up, `Dependencies\SCF.Utils\IOUtils.cs` `SaveFile` (the `.edmj` and the settings file):
  - When `File.Replace` failed, the error handler always deleted the temporary file. Windows' `ReplaceFile` can fail after the old file is already gone (`ERROR_UNABLE_TO_MOVE_REPLACEMENT`): the temporary file was then the only copy, and deleting it lost both the old and the new model. Now, when the old file is gone, the temporary file is moved into its place; if even that fails, the error names the file that holds the new content and nothing is deleted.
  - When the old file is still there (another program holds it open without delete sharing, it is read-only, or the file system cannot replace files), the content is written in place as before 1.1.5.7 and the temporary file is removed. Saving a file another program holds open works again, and the read-only error names the file again.
  - A file that appears between the check and the move is replaced instead of failing.
  - The steps are in the new private methods `MoveInPlace`, `ReplaceWith` and `DeleteIfExists`.
- New test `EngineUnitTests.SavingAFileAnotherProgramHoldsOpenStillWorks` (the file is open with `FileShare.ReadWrite`: the save writes the new content and leaves no temporary file).

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 51 passed, 3 skipped, 0 failed (the new test, `SavingAModelTwiceLeavesNoTemporaryFile` and `SettingsRoundTripInTheirOwnFolder` pass). `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- The `ERROR_UNABLE_TO_MOVE_REPLACEMENT` path cannot be produced on demand; it was checked by reading the code against Microsoft's `ReplaceFileW` documentation.
- GenCheck against 1.1.7.2: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both).
- CRLF: both changed files OK; `IOUtils.cs` keeps its BOM.

Commit details

```
Never lose the model file when it cannot be replaced (SF-146)

If ReplaceFile fails after the old file is gone, the temporary file is
moved into place instead of deleted; if the old file is still there
(open in another program, read-only), it is written in place as before.

Bump version to 1.1.7.3.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.2 — 2026-11-09

Changes
- SF-146, item #15 follow-up:
  - `Dependencies\SCF.Engine\ModelValidation.cs` `ValidatePathNames`: an empty `DependenciesFolder` is valid again (the projects go at the solution root, as `SolutionProjectHost` has always done); 1.1.6.5 rejected it, which stopped every Sync and CLI `generate` of such a model. Empty parts from a trailing or doubled separator (`src/Generated/`) are ignored; the folder is trimmed as Sync trims it, and a rooted folder (`C:\x`, `/x`) is still rejected as a whole.
  - A hand-written file at a generated path now shows in the preview, not only when the real Sync refuses it: `SolutionSync.Run` logs `"Not a Sharp Factory file, Sync will stop: <path>"` for each; `SyncReport.IsUpToDate` is false while there is one; the app's preview summary says `Sync will stop: <n> file(s) at generated paths not written by Sharp Factory (the Log tab lists them)`; the CLI summary prints `not a Sharp Factory file (Sync will stop): <path>`.
  - The refusal message no longer tells the user only to "Close them or clear read-only" when a blocked file is hand-written: it ends with `Move or rename the files that are not Sharp Factory files, close the others or clear read-only, then Sync again.` (the label is the constant `_notASharpFactoryFile`). The message for locked or read-only files only is unchanged.
- Tests: `SolutionSyncTests.AHandWrittenFileAtAGeneratedPathIsNotOverwritten` also checks the preview and the new advice; `PathNamesThatEscapeTheSolutionStopSync` also checks `/x` (rejected), an empty folder and `src/Generated/` (accepted).

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 50 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- By hand (`_scratch\fixes\review2\item15`): a copy of `Coverage.edmj` with `"DependenciesFolder": ""` — `sharpfactory generate` writes 174 files (exit 0) and `sharpfactory sync` creates the solution with the 5 projects at its root. With a hand-written `Region.cs` at its generated path, `sync --preview` lists it and exits 0; `sync` refuses with the new advice.
- GenCheck against 1.1.7.1: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both). (The first snapshot of this version, made before the message change, is kept as `_scratch\fixes\1.1.7.2-firstrun`; it is identical too.)
- CRLF: the six changed files OK.

Commit details

```
Allow no dependencies folder and show hand-written files in the preview (SF-146)

An empty or trailing-separator DependenciesFolder no longer blocks Sync
and generate; the preview, the app summary and the CLI list hand-written
files at generated paths, and the refusal tells the user to move them.

Bump version to 1.1.7.2.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.1 — 2026-11-09

Changes
- SF-146, item #3 follow-up (user: drop the routine), `Dependencies\SCF.Engine\SqlVariantFilter.cs`:
  - A stored procedure or table function whose result has a sql_variant column is now dropped as a whole and logged (`"Skipped stored procedure <schema>.<name>: its result has a sql_variant column"`). 1.1.6.6 removed only the column, but the generated materializer reads result columns by position and the database still returns the column, so every later column was read from the wrong position (wrong data or `InvalidCastException`; harmless only when the column was the last one).
  - A table or view left with no columns after its sql_variant columns are removed is dropped and logged (`"Skipped <schema>.<name>: it has only sql_variant columns"`). Before, its repository, interface and utils were generated without its `...Property` and `...Prop` types and did not compile (for example a view of `SERVERPROPERTY(...)`). Its foreign keys were already dropped with their sql_variant columns.
  - The skipped columns of a table are logged in column order (they were logged backwards).
- New tests `EngineUnitTests.SqlVariantResultColumnsRemoveTheRoutine` (`GetEmployees` and `ShipmentsFrom` with a sql_variant first result column are gone; `GetShipments` and `EmployeesUnder` stay) and `ATableWithOnlySqlVariantColumnsIsRemoved` (`AuditLog`; the source keeps it).

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 50 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- Scratch model `_scratch\fixes\review2\item3\Probe.edmj` (the reviewer's probe: `AuditLog` and the view `EmployeeDirectory` all sql_variant, `GetShipments` and `ShipmentsFrom` with a sql_variant first result column; the four Order Line columns renamed as in 1.1.7.0) synced into a new solution: the log lists the four objects as skipped; 158 files; built with 0 errors (after adding `Microsoft.SqlServer.Types` to the Entity and Repository projects for the geography/geometry columns); no generated file mentions the four objects.
- GenCheck against 1.1.7.0: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both). No routine of either model has a sql_variant result column, so the output is unchanged; the engine check is not needed (no generated file changed).
- CRLF: both changed files OK.

Commit details

```
Drop routines with a sql_variant result column and empty entities (SF-146)

Results are read by position, so removing only the column shifted every
later column; such a procedure or function is now skipped as a whole. A
table or view left with no columns is skipped instead of generating code
that does not compile.

Bump version to 1.1.7.1.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.7.0 — 2026-11-09

Changes
- SF-146, the name rule (user, 2026-11-09: "Table, schema, database, column names should be AZ, az, 09, spaces, underscore"; schemas keep no space and no leading digit, because they are written as they are into namespaces and folders):
  - `Dependencies\SCF.Models\NameRules.cs`: `IsSupportedObjectName` accepts only ASCII letters, digits, `_` and spaces (it used `char.IsLetterOrDigit`, which accepts any Unicode letter); new `IsSupportedColumnName` (the same rule); `IsSupportedSchemaName` and `IsValidIdentifier` accept only ASCII letters, digits and `_`, an ASCII letter or `_` first. Every name of the three models keeps its C# name (they are all ASCII).
  - `Dependencies\SCF.Utils\ModelUtils.cs`: a table or view with a column whose name breaks the rule is left out as a whole and recorded (`"Tables: dbo.Dotted (column 'A.B' has characters that are not supported)"`); the same for a procedure or table function with such a result column. A procedure, function or sequence whose C# name is not an identifier (e.g. `2Proc`) is left out and recorded too (it generated code that does not compile). `PopulateSequences` takes the `NotSupported` list; `ModelBuilder.Populate` passes it.
  - `Dependencies\SCF.Engine\ModelNameCheck.cs` also reports, in a saved model, column and result column names that break the rule and procedure, function and sequence names that are not C# identifiers.
  - Test material (user: rename them): the four `Coverage.[Order Line]` columns `Unit-Price`, `Item#`, `Size(cm)`, `Weight.Kg` are `Unit_Price`, `Item_No`, `Size_cm`, `Weight_Kg` in `Tests\SCF.Tests\Fixtures\Coverage.sql`, `Fixtures\Coverage.edmj`, a model file outside the repository and the databases `SharpFactoryTest` and `SharpFactoryDocs` on localhost (`sp_rename`; nothing else in those databases referenced them). The engine-check harness outside the repository uses the new property names `Item_No` and `Weight_Kg`.
  - `TESTING.md`: the test database description states the rule.
  - `BACKLOG.md`: SF-124 (invalid parameter names from such columns) is done: those column names are not supported.
- Tests: `EngineUnitTests.NameRulesKnowWhatTheAppSupports` checks non-ASCII and column names; `ModelNameCheckFindsNamesTheAppDoesNotSupport` checks a column named `Item#`.

Breaking change for users
- A table, view, procedure or function with a non-ASCII name or a column name with other characters than `A-Z`, `a-z`, `0-9`, spaces and `_` is left out on Add and Update and reported; a saved model that has one is refused by Sync and `generate` until it is updated.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 48 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- `ModelNameCheck.Find` (scratch app `_scratch\fixes\review2\NameCheck`): `SharpFactoryTest.edmj` 0, `Fixtures\Coverage.edmj` 0, the reference model 0.
- Scratch database `SFFixesReview2` with `dbo.Dotted` (column `A.B`), `dbo.HashCol` (result column `x#`) and `dbo.[2Proc]`: all three logged as `Not supported, left out: ...`; the model builds.
- SchemaCheck on the test model after the rename: `185 tables, 2 views, 22 procedures, 4 functions, 2 sequences, 177 foreign keys in the model; 0 differences` (exit 0).
- GenCheck against 1.1.6.9: test `1418 identical, 6 different, 0 differ only in line endings or BOM, 0 not in the reference` — exactly the `Order_Line` files (`Order_Line.cs`, `Order_LineProp.cs`, `Order_LineProperty.cs`, `Order_LineUtils.cs`, `Order_LineRepository.cs`) and `RepoLookup.cs`: `ItemNumber` → `Item_No`, `WeightDotKg` → `Weight_Kg` and the four column names in the SQL. Ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0). No file disappeared.
- Engine check (`compare.py capture-1.1.6.6.txt capture-new.txt`): 852 operations, 889 commands; differences only in `15 order line odd names`, `34 order lines all` and `847 delete by query`. The insert now works (before: `SqlException: Incorrect syntax near '-'`, SF-124). Saved as `capture-1.1.7.0.txt`.
- CRLF: every changed file OK; `NameRules.cs` has no BOM, `ModelUtils.cs` keeps its BOM.

Commit details

```
Support only ASCII letters, digits, spaces and _ in names (SF-146)

Database, object and column names may have only A-Z, a-z, 0-9, spaces
and _; schemas no space and no leading digit. Objects with other column
names, and routines or sequences whose name is not a C# identifier, are
left out and reported; ModelNameCheck reports them in saved models. The
four odd Order Line test columns are renamed.

Bump version to 1.1.7.0.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.6.9 — 2026-11-09

Changes
- SF-146, from the code review of 1.1.6.8: the item #20 paths that still crashed on a name that cannot become a C# name.
  - `Dependencies\SCF.Utils\ModelUtils.cs` `ApplyIdentityComputedValues` looked up every selected table with `GetByName`, including the tables #20 had left out, so Add or Update failed with `KeyNotFoundException` as soon as such a table had an identity column (`NullReferenceException` for a null name). It now skips a table that is not in the model. `PopulateForeignKeysAndNavProps` skips a foreign key whose child or parent entity name is null (it threw in `Contains(null)`); a foreign key to a left-out table was already skipped.
  - `Dependencies\SCF.Models\EntityDataModel\Entities\Entity.cs` `EntityCollection` keeps an entity with a null `N` without indexing it (as `PropertyCollection` does since 1.1.6.4), and `Contains(null)` is false. A saved model with a table such as `_1` opens again (it said "It is not a model file"), so it can be updated; `ModelNameCheck` reports it.
  - `App\SCF.App\ViewModels\ModelTree.cs`: table, view and column nodes show the store name when there is no C# name, so the tree search no longer throws on a null title.
  - `Dependencies\SCF.Utils\StoreUtils.cs` `GetRoutineTableResultType` renamed a result column with a null C# name to `Result` (the line was unreachable before #20), so the procedure stayed in the model with a column the database does not have. The rename is gone; the null name reaches the model builder, which leaves the routine out and reports it.
- New test `EngineUnitTests.AModelWithATableNameThatCannotBecomeAnIdentifierStillOpens`.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 48 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- Scratch database `SFFixesReview2` on localhost (`_scratch\fixes\review2\db\r2.sql`), every object selected, `ModelBuilder.Populate` through `_scratch\fixes\review2\Probe`: the model builds; `dbo._1` (identity, foreign key to `Good`), `dbo.Odd` (identity, column `_`) and `dbo.UnderscoreCol` (result column `_`) are logged as `Not supported, left out: ...`; `Child` keeps its identity and only its foreign key to `Good`. Before the change the same run failed in `ApplyIdentityComputedValues`.
- GenCheck against the `final` snapshot: test `1424 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference`; ref `1783 identical, 0 different, 0 differ only in line endings or BOM, 0 not in the reference` (exit 0 both).
- CRLF: the five changed files OK.

Commit details

```
Stop the left-out-name paths from crashing (SF-146)

The identity and foreign-key steps skip tables that were left out as
not supported; an entity with no C# name is kept but not indexed, so
such a model opens; the tree shows the store name; a result column
with no C# name is no longer renamed to Result.

Bump version to 1.1.6.9.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.6.8 — 2026-11-09

Changes
- SF-143, section 6 (baselines and full verification), no code change. SF-143 is Done.
- The two GenCheck baselines were replaced, as agreed after the differences were listed by item: 195 files in the test baseline and 251 in the reference baseline. The old files moved to backup folders next to them; the hand-written `Extended` files and project files in the reference baseline were not touched, and nothing was written to the reference solution.
- The differences were exactly the expected output of review items #1 (1 file per model), #17 (2 per model), #19 (the same `Database.cs`, so no extra file), #7 (186 / 247 files), #8 (1 per model) and #3 (6 files, test model only; `AllTypeRepository.cs` is also one of #7's). Every other item produced no generated-output change.

Verification
- GenCheck after the baseline replacement: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).
- SchemaCheck on the test model: 0 differences (exit 0).
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`). `SCF.Tests`: 47 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- Sync and build of the final solutions under `_scratch\fixes\final\`: the Coverage model builds with 0 errors; the SharpFactoryTest model builds with 2 errors, both the documented SF-083 case (the table `Coverage` in the schema `Coverage`: "'Coverage' is a namespace but is used like a type").
- Engine check, final: `compare.py capture-1.1.4.7.txt capture-1.1.6.6.txt` reports 30 operations with command/parameter differences and 29 with result differences, all the `AllTypes` operations of item #3 (listed there); no other table differs. `capture-1.1.6.6.txt` is the last capture: no later item changes the SQL text.
- CRLF: every file changed in this work passes the check; the new `.cs` files (`NameRules.cs`, `DbObjectOrder.cs`, `ModelNameCheck.cs`, `SqlVariantFilter.cs`) have no BOM.

Commit details

```
Replace the GenCheck baselines after the review fixes (SF-143, section 6)

The 195 + 251 differing files are exactly the expected output of items
#1, #17, #19, #7, #8 and #3; the old files are kept in the Old-1.1.4.7
folders. SchemaCheck, the tests, the generated solutions and the engine
check confirm the result. SF-143 is done.

Bump version to 1.1.6.8.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.7 — 2026-11-09

Changes
- SF-143, review items #16 and #18: no code change. Two backlog rows, both `Open`:
  - SF-144 (#16): IN/NOT IN lists of strings are written as `'value'` literals, not parameters and without the `N` prefix (`DatabaseSelect.cs` about 674–683, `Database.cs` about 251): a non-ASCII value on an nvarchar column matches nothing, and a null element throws. Changing it changes the generated SQL text, so it needs a decision.
  - SF-145 (#18): `UpdateOptimisticBatch` sends every entry as one command, so a few hundred entities exceed SQL Server's 2,100 parameters. Splitting it changes transaction and result behaviour, so it needs a design decision.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 47 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Add the two backlog rows from the review (SF-143, items #16 and #18)

SF-144: string IN lists are literals without N. SF-145: the optimistic
batch is never split at the 2,100-parameter limit. Both need a decision.

Bump version to 1.1.6.7.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.6 — 2026-11-09

Changes
- SF-143, review item #3 (user: "sql variant columns should be skipped from any sql composition as if they did not exist"): new `Dependencies\SCF.Engine\SqlVariantFilter.cs`. `Apply` returns the same container when no model has a sql_variant anywhere; otherwise it clones the container with the existing `Clone()` methods, filters the copy and leaves the `.edmj` and the model the app shows unchanged.
  - A foreign key with a sql_variant column on either end is dropped, with the navigation properties that point at it on both entities (an empty `NPs` becomes null). A primary key with one of the columns is dropped (the table has no primary key); a unique key with one is dropped (an empty `UKs` becomes null). The sql_variant columns are then removed from the entity, each logged once.
  - A stored procedure or function with a sql_variant parameter is removed; a function with a sql_variant scalar return is removed. A result entity loses its sql_variant columns, and when nothing else remains it becomes null (procedure) or the function is removed.
  - `GenerationRunner.Run` applies the filter first, so Sync, the CLI, GenCheck and the tests all generate from the filtered model. `App\SCF.App\Services\CodePreview.cs` applies it too and finds the same model, schema and item in the copy (an empty preview when the item was removed).
  - `Repository\EntityRepoGenerator.cs`: the `IsPK` test now checks `keyInfo.KeyEntity.PK != null` before reading `PK.N`, for a parent that lost its primary key.
  - The engine-check harness outside the repository sets `SqlVariantCol` only for the old engine (`#if OLD_ENGINE`).
  - The `object?` Prop of a nullable sql_variant column (review item #4) is gone with this item: there is no sql_variant column left to give a Prop to.
- New tests in `EngineUnitTests`: the Coverage `AllTypes` loses only `SqlVariantCol` (same order, the source keeps it); a `Region` whose `RegionCode` is sql_variant loses `PK_Region`, `FK_Store_Region` with both navigation properties, and keeps `UK_Region_Name`; a procedure and a function with a sql_variant parameter are removed; a model without sql_variant is returned as the same instance.

Breaking change for users
- A user with sql_variant columns gets a different generated layer: the property disappears from the entity and its utils, the container, the property enum, the repository and the lookup. Reads no longer return the column. Inserting into a table with a NOT NULL sql_variant column that has no default now fails. A key that contains a sql_variant column disappears (a dropped primary key means the table has no primary key), and a stored procedure or function with a sql_variant parameter is not generated.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 47 passed, 3 skipped, 0 failed (the four new tests pass). `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- Scratch model (`_scratch\fixes\item3\Coverage.edmj`, `Region.RegionCode` with `"TId": 24`) synced into a new solution and built: 0 errors (after adding `Microsoft.SqlServer.Types`).
- GenCheck, test model: 1,418 identical, 6 different — exactly `SFTest.Entity\TestDb\Coverage\AllType.cs`, `...\PropertyEnums\AllTypeProp.cs`, `SFTest.Query\Props\TestDb\Coverage\AllTypeProperty.cs`, `SFTest.Entity.Utils\Utils\TestDb\Coverage\AllTypeUtils.cs`, `SFTest.Repository\TestDb\Coverage\AllTypeRepository.cs` and `SFTest.Repository\Lookups\RepoLookup.cs`. The differences are the property, enum member, container field, compare/copy/changes lines (the `GetChanges` list capacity is 40 instead of 41), the materializer line (later reader indexes one lower) and the lookup entries. `grep -rni "sqlvariant" 1.1.6.6/test`: nothing.
- GenCheck, reference model: 1,783 identical, 0 different (it has no sql_variant).
- Engine check against `capture-1.1.6.0.txt`: 852 operations, 889 commands, 1,767 parameters; commands/parameters differ in 30 operations and results in 29 — all on `AllTypes`: 16 alltype full, 17 alltype nulls, 29 alltype all, 30 alltype filled ByPK, 31 alltype nulls ByPK, 32 alltype top 2, 33 alltype all async, 93 alltype short in, 94 alltype short notin, 95 alltype short gt, 96 alltype byte in, 97 alltype byte le, 98 alltype decimal ge, 99 alltype money lt, 100 alltype float gt, 101 alltype bigint eq, 102 alltype bit eq, 103 alltype guid eq, 104 alltype char eq, 105 alltype nchar like, 106 alltype varchar starts, 107 alltype nvarcharmax like, 108 alltype varcharmax ends, 111 alltype dto gt, 112 alltype date col dateeq, 113 alltype smalldt year, 114 alltype dt2 le, 115 alltype intnull null, 811 alltype ToListAsync (async), 836 update alltype computed+rowversion. The result list is the same without 103 alltype guid eq. No other table differs. Saved as `capture-1.1.6.6.txt`.

Commit details

```
Skip sql_variant columns from every generated composition (SF-143, item #3)

SqlVariantFilter works on a copy just before generation: columns are
removed, and the keys, foreign keys, procedures and functions that
cannot exist without them are dropped. The model file and the app's
model are unchanged.

Bump version to 1.1.6.6.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.5 — 2026-11-09

Changes
- SF-143, review item #15:
  - `Dependencies\SCF.Engine\Sync\SolutionProjectHost.cs` `WriteFile` no longer overwrites an existing file at a generated path that has no `/*SFF*/` marker (it is the only writer of generated `.cs` files; project and solution files go elsewhere). It records the path in the new `SyncReport.UnmarkedFiles`.
  - `SolutionSync.FindBlockedFiles` adds each as `"<path> (not a Sharp Factory file)"`, so the existing "Nothing was written. This file cannot be written: ..." message stops Sync before anything is written.
  - New `ModelValidation.ValidatePathNames(ModelContainer)`: every model name and full project name (common layers and each model's layers) must not be rooted, contain `..`, a path separator or an invalid file name character; `DependenciesFolder` is split on `/` and `\` and each part must not be empty, `.` or `..` and must not contain those characters (a nested folder such as `src/Generated` still works). Called at the start of `SolutionSync.Run` and the CLI `generate`; the failure message is `"Nothing was written. These names cannot be used in file paths: <lines>"`.
  - `SolutionSync.Run`: when the preview run in `FindBlockedFiles` fails, the real run now returns that failure instead of going ahead.
  - New tests `SolutionSyncTests.AHandWrittenFileAtAGeneratedPathIsNotOverwritten` and `PathNamesThatEscapeTheSolutionStopSync`.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 43 passed, 3 skipped, 0 failed (the new tests and the existing `StaleSharpFactoryFilesAreDeletedAndUserFilesKept`, `AReadOnlyFileStopsSyncBeforeAnythingIsWritten`, `NewSlnxGetsEveryProjectReferenceAndFile`, `SecondSyncChangesNothing` and `ANestedDependenciesFolderIsUpToDateOnTheSecondSync` pass). `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- Reference baseline check (read only): of the 1,783 generated `.cs` files of the ref snapshot whose path exists in the reference baseline, 1,783 start with `/*SFF*/` and 0 do not.
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Refuse to overwrite hand-written files and names that leave the solution (SF-143, item #15)

WriteFile skips a file at a generated path without the Sharp Factory
marker and Sync stops with it in the blocked list. Model, project and
dependencies-folder names are validated as path parts before a preview
or a write, and a failed preview now stops the real run.

Bump version to 1.1.6.5.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.4 — 2026-11-09

Changes
- SF-143, review item #20: a column named `_` or `@` became an empty C# name and `clean[clean.Length - 1]` threw `IndexOutOfRangeException` (also while loading an `.edmj`, which then said "It is not a model file"); `_1` became `1`, not an identifier. Such names are not supported: they are reported, never throw and never generate invalid code.
  - `Dependencies\SCF.Models\NameRules.cs`: new `IsValidIdentifier` (not empty, a letter or '_' first, then letters, digits and '_').
  - `Dependencies\SCF.Models\Utils.cs` `CleanIdentifier`: returns null when the cleaned name is empty and when the result is not a valid identifier. `GenerateEntityName` and `GeneratePropertyName` pass null through. Every name that is valid today keeps exactly the same result.
  - `Dependencies\SCF.Models\EntityDataModel\Properties\Property.cs`: `PropertyCollection` tolerates a null `N` (it is not indexed by name), so the reader can build an object and let the caller drop it.
  - `Dependencies\SCF.Utils\ModelUtils.cs`: `PopulateEntitiesPropsAndKeys`, `PopulateStoredProcsAndParams` and `PopulateFunctionsAndParams` take the `NotSupported` list; a table, view, procedure or function whose own name, column, parameter or result column gives a null identifier is left out as a whole and recorded (`"Tables: dbo.Odd (column '_' cannot become a C# name)"`). `ModelBuilder.Populate` writes the records after the model is built.
  - `Dependencies\SCF.Engine\ModelNameCheck.cs` `Find` also reports every entity, property, parameter and result column whose `N` is null.
  - A function's scalar return is not checked: the saved model has no C# name for it (the item does not list it, and the Coverage model's functions have none).
- New test `EngineUnitTests.NamesThatCannotBecomeIdentifiersGiveNull`; `ModelNameCheckFindsNamesTheAppDoesNotSupport` also covers a null property name.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 41 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- `ModelNameCheck.Find` on the three models (scratch app under `_scratch\fixes\item12`): 0, 0, 0.
- SchemaCheck on the test model: 0 differences (exit 0).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both): every name that is valid today is unchanged.

Commit details

```
Report names that cannot become C# identifiers (SF-143, item #20)

CleanIdentifier returns null instead of throwing or producing an
invalid name; the model builder leaves such an object out as a whole
and records it, and ModelNameCheck reports null names in a saved model.

Bump version to 1.1.6.4.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.3 — 2026-11-09

Changes
- SF-143, review item #12: database, schema and object names with characters the app cannot use are not supported (the schema reader embeds a full object name in its column queries, so a '.' or another special character breaks them silently). The reader's SQL is unchanged.
  - New `Dependencies\SCF.Models\NameRules.cs`: `IsSupportedObjectName` (letters, digits, '_' and spaces) for databases, tables, views, procedures, functions and sequences; `IsSupportedSchemaName` (letter or '_' first, then letters, digits and '_') for schema names.
  - `Dependencies\SCF.Utils\DbModelUtils.cs` leaves such objects out of `PopulateDbCategory` and records `"<category>: <schema>.<object> (the schema name has characters that are not supported)"` or `"(the name has characters that are not supported)"` in the new `DBObjectCategoryCollection.NotSupported`. `ModelBuilder.Populate` writes each as `Not supported, left out: <record>`; `ModelUpdater` returns them in the new `ModelUpdateResult.NotSupported` and no longer lists them as "no longer in the database"; the CLI `update` prints `not supported: <record>`.
  - `ModelValidation.ValidateDbInfo` rejects an unsupported database name with the new constant `DatabaseNameNotSupported`.
  - New `Dependencies\SCF.Engine\ModelNameCheck.cs` `Find(ModelContainer)` reports every unsupported database, schema and object name in a saved model (one line each). `SolutionSync.Run` and the CLI `generate` refuse the model before writing anything: `"Nothing was written. These objects are not supported: <lines>. Update the model to remove them."` (Sync failure / exit code 1). It is not in `ModelFileCheck`, so the app still opens the file for an update.
  - `Tests\SCF.Tests\QuotedNamesTests.cs` is now `TheReaderLeavesOutNamesThatAreNotSupported`: the objects under the `Sales's` schema are left out and listed (4 records), and the categories are empty.
  - New tests `EngineUnitTests.NameRulesKnowWhatTheAppSupports` and `EngineUnitTests.ModelNameCheckFindsNamesTheAppDoesNotSupport`.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 40 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- `QuotedNamesTests` ran against `SharpFactoryQuote's` on localhost (the database already existed; the four objects were in place): 1 passed.
- `ModelNameCheck.Find` on the three models (scratch app under `_scratch\fixes\item12`): `SharpFactoryTest.edmj` 0, `Fixtures\Coverage.edmj` 0, the reference model 0.
- SchemaCheck on the test model: 0 differences (exit 0).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Support only names the schema reader can use (SF-143, item #12)

NameRules defines the allowed characters; the reader leaves other
objects out and reports them through ModelBuilder, the CLI update and
ModelNameCheck, which makes Sync and generate refuse such a model
before writing anything.

Bump version to 1.1.6.3.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.2 — 2026-11-09

Changes
- SF-143, review item #8: `Query\Builders\FilterBuilderGenerator.cs` `GenerateHiddenObjectEquals` documented the hidden `Equals(object)` as returning `PredicateBuilder`; it returns `bool`. Its `<returns>` text now says `bool. Never call this method: the compiler rejects every call to it (Obsolete as an error).` The summary, attribute and body are unchanged.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 38 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,423 identical, 1 different (`SFTest.Query\Query\Builders\FilterBuilder.cs`); reference model 1,782 identical, 1 different (the same file). The diff is only the `<returns>` line of `public new bool Equals(object val)`.

Commit details

```
Say what the hidden Equals returns and that it must not be called (SF-143, item #8)

The generated <returns> text says bool and that the compiler rejects
every call (Obsolete as an error).

Bump version to 1.1.6.2.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.1 — 2026-11-09

Changes
- SF-143, review item #7 (stale comments):
  - `Query\PropGenerator.cs`: the `FilterTypes` summary no longer mentions `Lookup.PropTypes`.
  - `Query\Props\PropertyFilterGenerator.cs`: the summary no longer mentions the Lookup tables.
  - `Repository\EntityInfoClassGenerator.cs`: the three default-sort comments say `List<ISortFilter>` instead of `SortFilters`.
  - Generator source comments in `Repository\BaseClassGenerator.cs` (3 places) and `Repository\InterfaceGenerator.cs` (6 places): `//NO NAV PROPERTIES, BASED ON QUERY` is now `//BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)`. `//NO NAV PROPERTIES, NO QUERY`, `//NO NAV PROPERTIES, DEFAULT SORTING` and `//NO NAV PROPERTIES, WITH SORTING` are unchanged.

Verification
- `grep -rn "Lookup.PropTypes\|Lookup tables\|SortFilters parameter" Dependencies --include=*.cs`: nothing.
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 38 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,238 identical, 186 different; reference model 1,536 identical, 247 different (exit code 1 as expected for changed output). Every changed line is a comment: test 372 changed lines in 186 files — the 2 query files (`SFTest.Query\Query\Props\Prop.cs`, `...\PropertyFilter.cs`) plus 184 repository files with the default-sorting comment; ref 494 changed lines in 247 files — the 2 query files plus 245 repository files. No non-comment line differs.

Commit details

```
Describe the current API in the generated comments (SF-143, item #7)

The FilterTypes and PropertyFilter summaries no longer mention the old
Lookup tables; the default-sort comments say List<ISortFilter>; the
generator's "NO NAV PROPERTIES, BASED ON QUERY" comments say nav
properties come from SelectQuery.

Bump version to 1.1.6.1.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.6.0 — 2026-11-09

Changes
- SF-143, review item #17: `Dependencies\SCF.CodeGenerator\Database.cs` `SqlText.AppendNumber` measured `NumberFormatInfo.CurrentInfo.NegativeSign.Length` and formatted with the current culture; under cultures whose minus sign is U+2212 (e.g. sv-SE) a negative number made invalid SQL. It now measures one character for the minus sign and formats with `CultureInfo.InvariantCulture`; its doc comment and the measuring comment say so. `DatabaseSelect.cs` `WriteList`'s summary says numbers are written with the invariant culture.
- The generated SQL text is identical on an en-US machine; the minus sign no longer depends on the machine's culture.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 38 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,422 identical, 2 different (`SFTest.Repository\SQLEngine\Database.cs`, `SFTest.Repository\SQLEngine\DatabaseSelect.cs`); reference model 1,781 identical, 2 different (the same two files). The diffs are only the two comment texts, the `_length++;` line and the `TryFormat` line in the item.
- Engine check: 852 operations, 889 commands, 1,767 parameters — 0 command/parameter differences, 0 result differences against `capture-1.1.4.7.txt` (exit 0); saved as `capture-1.1.6.0.txt`.

Commit details

```
Write numbers in the SQL text with the invariant culture (SF-143, item #17)

AppendNumber measures a one-character minus sign and formats with
CultureInfo.InvariantCulture, so a negative IN-list value is valid SQL
under any culture. On en-US the SQL text is unchanged.

Bump version to 1.1.6.0.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.9 — 2026-11-09

Changes
- SF-143, review item #19: `Dependencies\SCF.CodeGenerator\Database.cs` (the template written into every generated Repository project) documents `RegisterModelConnectionString` with the prescribed summary: call it once per model, at start-up, before any query; a second call for the same model throws and the registry must not change while queries read it. No code change; no lock, no override method (SF-138 stays as decided).

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 38 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,423 identical, 1 different (`SFTest.Repository\SQLEngine\Database.cs`); reference model 1,782 identical, 1 different (the same file). The diff is only the 4 comment lines of the summary in the item (its text says "3"; the prescribed block is 4 lines).
- Engine check: not run (comment only).

Commit details

```
Document RegisterModelConnectionString (SF-143, item #19)

The generated template says the method is called once per model at
start-up and that the registry does not change while queries read it.

Bump version to 1.1.5.9.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.8 — 2026-11-09

Changes
- SF-143, review item #21: the app turns Sync and Preview off when a model in the file has `Synced == YesOrNo.Yes`; the CLI never checked it. The check and its message moved to `Dependencies\SCF.Engine\ModelFileCheck.cs` as the public static methods `SyncedModelNames(ModelContainer)` and `SyncBlockText(ModelContainer)` (message without " (as in the old app)"). `App\SCF.App\ViewModels\SyncPageViewModel.cs` calls them, with the same behaviour. `App\SCF.Cli\Program.cs` `sync` prints the message to standard error and returns 1 before doing anything, including with `--preview`.
- New test `EngineUnitTests.ASyncedModelTurnsSyncOff`: the exact message for `Models[0].Synced = YesOrNo.Yes`, and null when no model is marked.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 38 passed, 3 skipped, 0 failed (the new test passes). `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- By hand: a scratch copy of `Coverage.edmj` with `"Synced": 1` next to `"Name"` — `sharpfactory sync <copy> <scratch>/App.slnx` prints `Model TestDb is marked as synced, so Sync is turned off for this file.` and exits 1; nothing is written; the same with `--preview` also exits 1. The unmarked fixture model syncs into a new solution (exit 0).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).
- Searched the source and docs for "as in the old app": no other place (only stale binaries under `obj` still contain the old string).

Commit details

```
The CLI refuses a file with a model marked as synced (SF-143, item #21)

SyncedModelNames and SyncBlockText live in ModelFileCheck; the app view
model and the CLI sync command both use them, and sync exits 1 before
writing anything, --preview included.

Bump version to 1.1.5.8.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.7 — 2026-11-09

Changes
- SF-143, review item #22 (five small fixes):
  - (a) `Dependencies\SCF.Models\EntityDataModel\Model\ModelContainer.cs`: `OnPropertyChanged(nameof(_commonSettings))` is `nameof(CommonSettings)`.
  - (b) `Dependencies\SCF.Models\EntityDataModel\Properties\Property.cs`: `PropertyCollection.ClearItems` also clears `_itemsByStoreName`.
  - (c) `Dependencies\SCF.Utils\DbModelUtils.cs` ordered database objects with the user's culture. The new `Dependencies\SCF.Utils\DbObjectOrder.cs` compares with `StringComparer.InvariantCulture` (the same order as en-US before), used at both places (`selected.Sort(DbObjectOrder.ByName)`; `dbObjects.Sort(DbObjectOrder.BySchemaThenName)`). `GetDbObjectName` and `GetDbObjectSchemaName` are gone (nothing else used them).
  - (d) `App\SCF.App\Services\CodePreview.cs` saves `IOUtils.CreateFolders` and restores that value in `finally` instead of always setting `true`.
  - (e) `Dependencies\SCF.Utils\IOUtils.cs` `SaveFile` writes a temporary file in the same folder and moves/replaces it, deleting the temporary file on failure. `Dependencies\SCF.Engine\Settings\AppSettingsStore.cs` saves through it.
- New test `EngineUnitTests.SavingAModelTwiceLeavesNoTemporaryFile`: two saves of the Coverage model to the same file leave exactly one file in the folder and the file loads.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 37 passed, 3 skipped, 0 failed (the new test and `SettingsRoundTripInTheirOwnFolder` pass). `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- SchemaCheck on the test model: 0 differences (exit 0), so the object order is the same as before.
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Five small fixes (SF-143, item #22)

The CommonSettings notification names the property; clearing a property
collection clears both indexes; database objects sort with the invariant
culture; the code preview restores CreateFolders; model and settings
files are written to a temporary file and moved into place.

Bump version to 1.1.5.7.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.6 — 2026-11-09

Changes
- SF-143, review item #11: `CommonSettings.UseRootNSInProjectName` has the initializer `YesOrNo.Yes`, and `JsonUtils` saves with `DefaultValueHandling.Ignore`; `No` is 0, so it was never written and read back as `Yes`. The custom contract resolver now sets `DefaultValueHandling.Include` for that one property, in `Dependencies\SCF.Utils\JsonUtils.cs`. No other setting has the problem.
- New test `EngineUnitTests.NotUsingTheRootNamespaceInProjectNamesIsSaved`: saving a model with `No` and loading it gives `No`, and a file without the setting reads as `Yes`.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 36 passed, 3 skipped, 0 failed (the new test passes; `ACloneOfAModelSavesExactlyLikeTheModel` still passes, so an unchanged model saves byte-identically). `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Always write UseRootNSInProjectName in the model file (SF-143, item #11)

Its default is Yes (1) and the saver writes only non-defaults, so No (0)
was lost on save. The contract resolver includes that one property.

Bump version to 1.1.5.6.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.5 — 2026-11-09

Changes
- SF-143, review item #14: `Dependencies\SCF.Utils\StoreUtils.cs` created the `repeatedProps` dictionary inside the per-column loop of `GetRoutineTableResultType`, so `SELECT a.Id, b.Id, c.Id` produced `Id, Id1, Id1` and the property collection threw. The dictionary is created once after `var props = new PropertyCollection(result);`, with `StringComparer.OrdinalIgnoreCase` because the collection compares names case-insensitively.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).
- SchemaCheck on the test model: 0 differences (exit 0).
- Scratch database `SFFixesScratch` on `localhost` (created for this item), read through `ModelBuilder.ReadDatabaseObjects` and `ModelBuilder.Populate` in a scratch console app under `_scratch\fixes\item14\Probe` (the CLI has no "add a model" command): `dbo.DupTable` (`SELECT a.Id, b.Id, c.Id FROM dbo.T a CROSS JOIN ...`) gives result columns `Id`, `Id1`, `Id2`.
- The review's own `dbo.Dup` (`SELECT 1 AS Id, 2 AS Id, 3 AS Id`) gives no result columns at all, before and after this change: SQL Server returns an empty `IsHidden` for columns that do not come from a table, and the existing reader skips those columns. That is a separate pre-existing limitation, not part of this item; the duplicate-name fix itself was verified with `dbo.DupTable`.

Commit details

```
Count repeated result column names per routine, not per column (SF-143, item #14)

repeatedProps is created once before the column loop and compares
names case-insensitively, so a third Id becomes Id2 instead of Id1.

Bump version to 1.1.5.5.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.4 — 2026-11-09

Changes
- SF-143, review item #13: `Repository\EntityRepoGenerator.cs` `Helpers.GenerateCaseBodyForSwitchForAssociateNavPropResult` built the null check of a composite foreign key with `=` and an `i > 0` separator, so its second nullable column generated `if(|| !c.X.HasValue)` (not compilable) and only the last nullable column was checked. `arePropsNull` now accumulates with `+=` and `" || "` between parts; the `conditionSeparator` local is gone. A key with one nullable column writes exactly what it wrote before.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).
- Scratch models (`_scratch\fixes\item13\a` and `\b`, `FK_Store_Region` on `Store`): in `a` only `RegionCode` nullable, in `b` both columns nullable. Both synced into new solutions and built: 0 errors. The generated `StoreRepository.cs` line 303 reads:
  - `a`: `if(string.IsNullOrWhiteSpace(c.RegionCode))`
  - `b`: `if(string.IsNullOrWhiteSpace(c.CountryCode) || string.IsNullOrWhiteSpace(c.RegionCode))`

Commit details

```
Fix the null check of a composite foreign key (SF-143, item #13)

arePropsNull accumulates every nullable child column with || instead of
overwriting the previous check. A single nullable column is unchanged.

Bump version to 1.1.5.4.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.3 — 2026-11-09

Changes
- SF-143, review item #2: `Query\PropGenerator.cs` `EntityProps.Generate` writes a container file in the prop enum's namespace that names the nav enum with its short name. When an entity has navigation properties and the prop enum and nav enum layers have different namespaces, the file now adds the nav enum namespace to its usings (the same check `EntityRepoGenerator` does). The using list capacity went from 3 to 4.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both): both models use one namespace for the two layers, so nothing changes there.
- Scratch model (`_scratch\fixes\item2\Coverage.edmj`, `NavPropEnum` namespace `Entity` changed to `Entity.NavEnums`) synced into a new solution and built: 0 errors (after adding `Microsoft.SqlServer.Types` for the geography/geometry columns). `Props\TestDb\Coverage\RegionProperty.cs` contains `using SFTest.Entity.NavEnums.TestDb.Coverage;`, the namespace the generated `RegionNavProp.cs` declares.

Commit details

```
Add the nav enum namespace to a container file when it differs (SF-143, item #2)

EntityProps.Generate adds the NavPropEnum layer namespace to the
container's usings when it is not the PropEnum namespace, so the
generated Query project compiles with split layers.

Bump version to 1.1.5.3.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.2 — 2026-11-09

Changes
- SF-143, review item #1: `ByPK(id, null)` threw `NullReferenceException`. `Repository\BaseClassGenerator.cs` `StoreQueries.GenerateSelectSingleWithContainerNavProps` now writes `navprops?.Items`, so a null `NavProps` reaches the internal overload (which accepts null) and returns the row without navigation properties, as before SF-141.

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,423 identical, 1 different (`SFTest.Repository\BaseClasses\RepositoryBase.cs`); reference model 1,782 identical, 1 different (the same file). The diff is exactly the two generated lines:
  `return SelectSingle(where, orderby, navprops?.Items);` and `return await SelectSingleAsync(where, orderby, navprops?.Items);`.
- Engine check: not run (the item says it does not change; this line never runs SQL differently).

Commit details

```
Let ByPK(id, null) return the row without nav props (SF-143, item #1)

The generated SelectSingle/SelectSingleAsync overloads pass
navprops?.Items to the internal overload, which accepts null.

Bump version to 1.1.5.2.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.1 — 2026-11-09

Changes
- SF-143, review item #10:
  - `Repository\EntityRepoGenerator.cs`: the unused `propEnum` and `navPropEnum` locals of `Generate` are gone.
  - The local `propEnum` that holds `Common.GeneratePropClassName(...)` (the container, not the enum) is `containerName` now, with every use in the same method: `EntityRepoGenerator.cs` (8 declarations with their uses), `EntityInfoClassGenerator.cs`, `Entity\EntityUtilGenerator.cs`, `Entity\EntityUtilsMethodSignatures.cs`, and the parameter of `RepoGenerator.Helpers.GenerateKeysForSelectSingle` and `GenerateKeysForSingleSelectHelper`. Variables holding `GeneratePropEnumName`/`GenerateNavPropEnumName` are unchanged.
  - `Repository\BaseClassGenerator.cs`: the five unused `var genericProp = Common.Names.GenericProp;` locals are gone.
  - `Repository\InterfaceGenerator.cs`: the unused `bool hasNavProps` parameter is gone from `GenerateGetTop`, `GenerateGetFirstOrDefault`, `GenerateGetFirst`, `GenerateGetSingleOrDefault`, `GenerateGetSingle` and `GenerateGetList`, and from their calls. `GenerateGetAll` keeps it (it uses it).

Verification
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both). A renamed variable writes the same text.

Commit details

```
Clean up unused and misnamed variables (SF-143, item #10)

The container locals are containerName; two unused locals in
EntityRepoGenerator, five unused genericProp locals and six unused
hasNavProps parameters are gone. Generated output unchanged.

Bump version to 1.1.5.1.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.5.0 — 2026-11-09

Changes
- SF-143, review item #6: the generator methods nobody calls are out.
  - `Repository\BaseClassGenerator.cs`: the six dead methods `GetTopMethods.GenerateWithNavProps`, `GetFirstOrDefaultMethods.GenerateWithNavProps`, `GetFirstMethods.GenerateWithNavProps`, `GetSingleOrDefaultMethods.GenerateWithNavProps`, `GetSingleMethods.GenerateWithNavProps` and `GetListMethods.GenerateGetListWithNavProps`.
  - `Repository\RepoMethodSignatures.cs`: `UpdateMethods.GenerateUpdateAny`, `DeleteMethods.GenerateDeleteAny`, their now-unused helpers `GenerateFilterListParam` and `GenerateValuesToSetParam`, the six commented-out `GenerateFilterListParam` calls, and the `suppressGeneration` parameter of `RetrieveMethods.GenerateGenericGetTopNoQuery` (it now declares the method unconditionally).
  - `Repository\InterfaceGenerator.cs`: the three commented-out blocks that called the removed methods.
  - The used methods with similar names stay: `StoreQueries.GenerateUpdateAny(bool)`/`GenerateDeleteAny(bool)`, the private `GenerateDeleteAny()`/`GenerateUpdateAny()`, every `...BasedOnQuery` and every `GenerateNoNavProps`.
- Removing the six commented calls left a double blank line in each place; the file was collapsed back to single blank lines (IDE2000 is an error here).

Verification
- `grep -rn "GenerateWithNavProps\|GenerateGetListWithNavProps\|GenerateFilterListParam\|GenerateValuesToSetParam\|suppressGeneration" Dependencies --include=*.cs`: nothing.
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 50 passed, 4 skipped, 0 failed.
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Remove the generator methods nobody calls (SF-143, item #6)

Six GenerateWithNavProps variants, GenerateUpdateAny/GenerateDeleteAny
(without a query), their helpers and the suppressGeneration flag are
gone, with the commented-out calls that mentioned them. Generated
output unchanged.

Bump version to 1.1.5.0.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.9 — 2026-11-09

Changes
- SF-143, review item #5: `Common.Names.GenericNavProp` (the old `TNavPropEnum` name) is removed. Its only readers were 8 variables in `Repository\RepoMethodSignatures.cs` that passed it to `GenerateQueryParam`, which ignored it; that overload is now `GenerateQueryParam(List<MethodParameter> methodParams, string propClass)`. The entity overload lost its ignored `hasNavProps` parameter too, at its 8 callers. The dead commented lines mentioning `genericNavProp` in `Repository\BaseClassGenerator.cs` are gone.
- Correction to the 1.1.4.7 entry: `GenericNavProp` was only read by the ignored parameter; no method had to go.
- `QueryConstants._genericNavProp` is a different thing (a generic parameter named `TProps` constrained to `NavPropertyContainer`) and is unchanged.

Verification
- `grep -rn "GenericNavProp\|genericNavProp" Dependencies --include=*.cs`: only `_genericNavProp` remains, in `QueryConstants.cs`, `NavPropsBuilderGenerator.cs`, `NavPropQueryExtensionsGenerator.cs` and `BaseClassGenerator.cs`, as expected.
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).

Commit details

```
Remove GenericNavProp and the ignored parameters (SF-143, item #5)

GenerateQueryParam takes the container name only; the entity overload
loses its ignored hasNavProps, and the old commented parameter lines in
BaseClassGenerator are gone. Generated output unchanged.

Bump version to 1.1.4.9.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.8 — 2026-11-09

Changes
- SF-143, review item #9 (from the code review of 1.1.4.7): `Dependencies\SCF.CodeGenerator\Repository\BaseClassGenerator.cs` and `Dependencies\SCF.CodeGenerator\Repository\StoredProcsGenerator.cs` had LF-only line endings (2,014 and 188 lines). They are converted to CRLF; nothing else in them changed. The byte counts grew by exactly the converted line counts (98,471 to 100,485 and 8,812 to 9,000) and both files keep their BOM.
- A scan of the repository (370 files: `.cs`, `.csproj`, `.md`, `.axaml`, `.cshtml`, `.json`, `.yml`, `.js`, `.css`, `.py`, `.sql`, `.edmj`, outside `bin`, `obj` and `.vs`) found no other LF-only file. The known ones listed in the review (`.gitignore`, `.gitattributes`, `.dockerignore`, `.claude\launch.json`, `.claude\settings.json`, `.claude\hooks\keep_line_endings.py`, `App\SCF.App\Packaging\macOS\Info.plist`) stay as they are.

Verification
- CRLF check: `OK` for both files.
- `dotnet build SCF.slnx --no-incremental`: 0 errors, 1 warning (the known CS0618 in `Tests\SCF.App.Tests\TestSupport.cs:132`).
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed in a full run — `PolishTests.TheSolutionListSaysItIsLooking`, the known flaky test; it passes alone (1 passed).
- GenCheck: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit code 0 both).
- Engine check: not run (line endings only; no generated text changes).

Commit details

```
Convert the two LF-only generator files to CRLF (SF-143, item #9)

BaseClassGenerator.cs (2,014 lines) and StoredProcsGenerator.cs
(188 lines) are CRLF now; no other text changed. A repository scan
found no other LF-only file outside the known list.

Bump version to 1.1.4.8.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.7 — 2026-11-09

Changes
- SF-141 follow-up (user: "files not called can be removed"): the three generator source files that no longer write anything are out of the repository — `Query\LookupGenerator.cs`, `Filters\QueryFilterCollectionGenerator.cs`, `Filters\SortFilterCollectionGenerator.cs`. They were moved to an archive folder outside the repository, not deleted.
- The `Names` entries nothing reads any more are gone: `FilterLookup`, `QueryFilterCollection`, `SortFilterCollection`, `TypedFilterBuilder`, `TypedFilterBuilderExtensions`, `QueryBase`, `QueryWrapperBase`, `QueryBuilderBase`, `GenericQuery`, `GenericQueryBuilder`. `GenericNavProp` stays until the unused generic navigation methods that still read it are cleaned.

Verification
- `dotnet build SCF.slnx`: 0 errors. `SCF.Tests`: 35 passed, 3 skipped, 0 failed.
- The generated output is unchanged: the removed files wrote nothing, and the removed names were read only by them.

Commit details

```
Remove the generator files that write nothing (SF-141)

LookupGenerator, QueryFilterCollectionGenerator and
SortFilterCollectionGenerator are out of the repository (moved to the
test archive), and the Names entries nothing reads are gone. Generated
output unchanged.

Bump version to 1.1.4.7.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.6 — 2026-11-09

Changes
- SF-141 verification (`GENERATOR_CHANGES.md` section 10.1), no code changes. SF-141 is Done.
- The two GenCheck baselines were replaced, as agreed after the expected differences were listed: the test baseline and the reference baseline now hold the new output; the stale generated files were moved to `Baseline.Old` and `ReferenceBaseline.Old`, and the hand-written `Extended` files and project files in the reference baseline are untouched. Nothing was written to the reference solution.
- The two harnesses outside the repository were adapted to the container API, each with its own `NEW_API` constant: `EngineCheck\Harness\NavApi.cs` aliases the one- and two-parameter query types and builds the `NavProps` collections; `RefCheck\Harness\Walk.cs` finds the containers and nav containers by reflection and calls the extension conditions by reflection (dynamic dispatch cannot see extension methods). The old builds of both harnesses still compile.

Verification
- `dotnet build SCF.slnx`: 0 errors. `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed (`PolishTests.TheSolutionListSaysItIsLooking`, a UI timing test that passes alone; unrelated).
- GenCheck, after the baseline change: test model 1,424 identical, 0 different; reference model 1,783 identical, 0 different (exit 0 both).
- SchemaCheck: 0 differences.
- Sync into a new solution outside the repository and build: 0 errors for the Coverage fixture model. The SharpFactoryTest model fails only on the documented SF-083 case (a table named like its schema).
- CompileChecks (copy, new API): 39 of 39 misuse lines fail to compile.
- FilterParity (copy, new API): 9,680 filter cases, 0 different; 132 typed condition methods, 116 exercised (no column of an exact type for the other 16), 0 different. The matrix uses only values the typed factory can hold; wrong types are compile errors, covered by CompileChecks.
- Engine check: 852 operations, 889 commands, 1,767 parameters — 0 differences in commands, parameters or results against `capture-1.1.3.6.txt`.
- RefCheck against `AppDbTest`: 7,936 operations, 7,722 commands; the ordered command/parameter stream is identical position for position, and every result is identical except one operation where the old recording hit `Execution Timeout Expired`. 18 operations have a command recorded under a neighbouring operation's label: the commands are in the same order and only the block marker differs (the diagnostic event of an async command arrives on another thread); the same artifact appears in the recording made before this work.

Commit details

```
Query API redesign: the full verification (SF-141)

GenCheck baselines replaced as agreed, SchemaCheck clean, the generated
solution compiles, 39/39 misuse lines fail, FilterParity 9,680 cases and
132 methods identical, engine check zero differences, RefCheck command
stream identical. Harnesses take the container API under NEW_API.

Bump version to 1.1.4.6.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.5 — 2026-11-09

Changes
- SF-141, step 7 of the query API redesign (`GENERATOR_CHANGES.md` section 9, step 7): the SF tests.
  - `TypedPropsTests` (renamed `TheCodePreviewShowsATablesContainer`) checks the new preview output: `WarehouseProperty.cs`, `public sealed class WarehouseProperty : NavPropertyContainer`, and the field `new Prop<WarehouseProperty, int>((int)WarehouseProp.Id, "Id", false)`.
  - Searched both test projects for `TypedFilterBuilder`, `QueryFilters`, `SortFilters`, `Lookup`, `NavProperty` and `Property>`; this is the only test that asserted generated text.

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 49 passed, 4 skipped, 1 failed — `PolishTests.TheSolutionListSaysItIsLooking`, a UI timing test that also failed and then passed on its own before step 1 (not related to the generated output).

Commit details

```
Query API redesign step 7: the tests (SF-141)

The code preview test checks the Warehouse container instead of the
old static typed class. No other test asserts generated text.

Bump version to 1.1.4.5.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.4 — 2026-11-09

Changes
- SF-141, step 6 of the query API redesign (`GENERATOR_CHANGES.md` 6.5 to 6.7): the entity info, the entity utils and the stored procedure repositories.
  - `EntityInfoClassGenerator`: the default sort and the unique key sorts are `new List<ISortFilter>(n){ SortFilter.New(XProperty.M), }`. The `PropertyInfo` dictionaries keep `(int)XProp.M`.
  - `EntityUtilGenerator` and `EntityUtilsMethodSignatures`: `GetChanges` returns `List<IQueryFilter>` and builds it with `QueryFilter.New(XProperty.M, FilterConditions.Equals, value)`, still null when nothing changed; the generated utils file gets `using System.Collections.Generic`.
  - `StoredProcsGenerator`: the parameters are a `List<IQueryFilter>` built with `QueryFilter.New(X_RequestProperty.P, ...)` and passed as they are (no `IFilters`); the output parameters keep `(int)X_RequestProp.P`.
- The sql_variant skip in the containers is removed: the container is now the only way the generated code builds a filter, so every column needs its `Prop`. `Prop<XProperty, object>` compiles; the old static typed class skipped those columns only because it was an optional second API.

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- Generated the Coverage model and compiled all five generated projects together outside the repository: **0 errors**. This is the first step where the generated code compiles again.
- Grepped the generated output: no `QueryFilters<`, `SortFilters<`, `IFilters`, `TPropEnum`, `TNavPropEnum` or `TypedFilterBuilder`; the only `Lookup.` is the word in a comment. No `Lookups\Lookup.cs`, `QueryFilters.cs`, `SortFilters.cs` or `TypedFilterBuilder*.cs` files are written.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed.

Commit details

```
Query API redesign step 6: entity info, utils and stored procedures (SF-141)

Default and unique key sorts are List<ISortFilter> from the containers;
GetChanges returns List<IQueryFilter>; stored procedure parameters are
List<IQueryFilter> built with QueryFilter.New on the request container.
The generated solution compiles again. sql_variant columns get a Prop
because the container is now the only filter API.

Bump version to 1.1.4.4.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.3 — 2026-11-09

Changes
- SF-141, step 5 of the query API redesign (`GENERATOR_CHANGES.md` 6.3, 6.4): the repository signatures, the interfaces and the entity repositories.
  - `RepoMethodSignatures`: `GenerateQueryParam` always writes `Query<XProperty>` (the container) and the generic overload `Query<TProps>`; `GenerateNavPropParam` writes `NavProps<XProperty>`; `GenerateFilterListParam` and `GenerateValuesToSetParam` write `List<IQueryFilter>`; `GenerateSortFilterListParam` writes `List<ISortFilter>`.
  - `InterfaceGenerator`: the second member per query-based method (`Query<X, XNav>`) is no longer written, so no duplicate signatures. `ById`, `ByPK` and `ByUK` keep their `NavProps<XProperty>` overloads for entities with navigation properties.
  - `EntityRepoGenerator`: the base class is `ReadableRepositoryBase`/`WritableRepositoryBase` plus its `Nav` variant, `<Entity, XProperty>`. The keys, insert predicates, changes and delete predicates build `List<IQueryFilter>` with `QueryFilter.New(XProperty.M, ...)`. The navigation property code keeps the enums (`(XNavProp)p.Value`, the `case` labels, the `PropertyInfo` key pairs).

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- Generated the Coverage model and compiled all five generated projects together outside the repository: only the entity utils fail (`QueryFilters<>`, step 6); the interfaces and the repositories compile. Spot-checked: `Query<EmployeeProperty>`, `NavProps<EmployeeProperty>` only on `ByPK` of an entity with nav props, keys as `new List<IQueryFilter>{ QueryFilter.New(CategoryProperty.Id, ...) }`.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed.

Commit details

```
Query API redesign step 5: repository signatures and interfaces (SF-141)

Query<XProperty> and NavProps<XProperty> everywhere, List<IQueryFilter>
and List<ISortFilter> parameters, no Query<X, XNav> interface members.
Repositories build their keys and predicates from the containers; the
navigation property code keeps the enums.

Bump version to 1.1.4.3.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.2 — 2026-11-09

Changes
- SF-141, step 4 of the query API redesign (`GENERATOR_CHANGES.md` 6.2): the four repository base classes.
  - `ReadableRepositoryBase<TEntity, TProps> where TProps : PropertyContainer`, `ReadableNavRepositoryBase<TEntity, TProps> where TProps : NavPropertyContainer` (derived from the readable one), `WritableRepositoryBase<TEntity, TProps>` and `WritableNavRepositoryBase<TEntity, TProps>` (derived from their readable counterparts). No enum type parameter remains; two new names in `Names`.
  - `DefaultSort` is a `List<ISortFilter>`. `ComposeKeys`, `ComposeInsertPredicate` and `GetChanges` are abstract and return `List<IQueryFilter>`.
  - Every `QueryFilters<TPropEnum>`/`SortFilters<TPropEnum>` parameter is a `List<IQueryFilter>`/`List<ISortFilter>`; the `?.IFilters` conversions are gone.
  - The public read/write methods take `Query<TProps>` and go through the new `protected virtual SelectQuery(QueryWrapper<TProps>, PaginationSettings, int)` and its async twin. The nav base overrides them: no navigation properties calls the base, otherwise the nav `SelectMany`. The duplicated nav method sets are no longer written.
  - The nav base adds `SelectSingle`/`SelectSingleAsync` overloads taking `NavProps<TProps>` (passing `Items` to the internal overload) and keeps the nav `SelectMany`.
  - Everything SF added beyond the reference solution stays: `CalculateListCapacity`, `listCapacity`, `MaterializeResult` with pagination, `Merge`, `HasTriggers`, `shareConnection`.

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- Generated the Coverage model: the four bases have the new names and signatures; `Query<TProps>` everywhere, `SelectQuery` virtual in the readable base and overridden in the nav base, `NavProps<TProps>` overloads present, no `IFilters` left.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed.
- The generated solution does not compile yet: the entity repositories and interfaces still pass the enums (steps 5 and 6).

Commit details

```
Query API redesign step 4: the repository base classes (SF-141)

The four bases take <TEntity, TProps>; DefaultSort, keys, insert
predicates and changes use List<IQueryFilter>/List<ISortFilter>; the
read methods take Query<TProps> and go through a virtual SelectQuery
the nav base overrides. The duplicate nav method sets are gone.

Bump version to 1.1.4.2.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.1 — 2026-11-09

Changes
- SF-141, step 3 of the query API redesign (`GENERATOR_CHANGES.md` 5.4): the containers replace the static `XProp` classes.
  - `PropGenerator.EntityProps` writes one sealed `XProperty : PropertyContainer` (or `: NavPropertyContainer` when the entity has navigation properties) with one `Prop<XProperty, T>` per column and a private constructor. Each field casts the enum: `new Prop<XProperty, T>((int)XProp.M, "M", isNullable)`. Nullability is `p.Nul != YesOrNo.No`; the sql_variant skip and `ComposeValueType` stay as they were.
  - When the entity has navigation properties, the same file also gets the sealed `XNavProperty` with one `NavProp<XProperty, TTarget>` per navigation property, in the nav enum's order and with the same member set (`EnumsGenerator.NavPropertyEnum`). The target is the container of the entity on the other end of the foreign key (`nl.End == NavPropertyEnd.Parent` → parent, else child), fully qualified in the target schema's PropEnum namespace.
  - Stored procedures: a sealed `{N}_RequestProperty : PropertyContainer` per procedure with parameters, one `Prop` per parameter, in the parameter enum's order, written next to the schema's entity containers.
  - `ProcessQuery` loops the stored procedures too; `CodePreview` shows the procedure's container.

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- Generated the Coverage model: 174 files, the containers written as `Props\<model>\<schema>\<Entity>Property.cs` (and `<Proc>_RequestProperty.cs`); the nav container appears only for entities with navigation properties; the enum file names follow the step-1 swap.
- Compiled the whole generated `SFTest.Query` + `SFTest.Entity` outside the repository: 0 errors, containers included.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: not run for this step (its generated-text tests change with step 7).

Commit details

```
Query API redesign step 3: the containers (SF-141)

XProperty is the sealed container with one typed Prop per column;
XNavProperty holds one typed NavProp per navigation property with the
target container from the foreign key. Stored procedures get
X_RequestProperty. The static XProp classes are no longer written.

Bump version to 1.1.4.1.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.4.0 — 2026-11-09

Changes
- SF-141, step 2 of the query API redesign (`GENERATOR_CHANGES.md` 5.1 to 5.3): the Query project's common files.
  - `Query<TProps>` is one sealed class (`QueryGenerator`); `QueryBase` and the `Query<TPropEnum, TNavPropEnum>` form are gone. `QueryWrapperGenerator` writes one `QueryWrapper<TProps>` with `List<IQueryFilter>`/`List<ISortFilter>` properties and the open/closed expression counts computed in the constructor; `QueryWrapperBase` is gone. `QueryBuilderGenerator` writes one `QueryBuilder<TProps>` with an internal constructor.
  - `PredicateBuilderGenerator`: the typed `Where<TValue>(Prop<TProps, TValue>)`/`Or` returning `FilterBuilder<TProps, TValue>`, the group overloads and `AddCondition` using `prop.CreateFilter`; the end-of-group mark is a `PropertyFilter` cast. The untyped parameter syntax is gone.
  - `FilterBuilderGenerator` now writes the typed `FilterBuilder<TProps, TValue>` (absorbing `PropGenerator`'s `TypedFilterBuilder`); the old untyped `FilterBuilder` is no longer written.
  - `SortBuilder`/`UpdateValuesBuilder`/`PaginationBuilder` take `Prop` and build `List<ISortFilter>`/`List<IQueryFilter>` lazily, with internal constructors.
  - `NavPropsBuilder` writes `NavPropsBuilder<TProps> where TProps : NavPropertyContainer` (`Append`, `Append(NavProps<TProps>)`, internal `Then`) plus `NavPathBuilder<TProps, TCurrent>` in the same file.
  - `Prop<TProps, TValue>` moved to `Query\Props\Prop.cs`, with the `FilterTypes` helper and the internal `PropertySortFilter`. `TypedFilterBuilderExtensions` became `FilterBuilderExtensions` in `Query\Props\`, extending `FilterBuilder<TProps, T>` and returning `PredicateBuilder<TProps>` (132 methods, checked against the prototype).
  - `QueryFilter` and `SortFilter` are the static factories only (`New` on a `Prop`); the enum-keyed classes and `New` overloads are gone.
  - `NavProp` gains the internal `SubNavProp(int, string, Type)` and `ContainerNavProp`; `NavProps<TProps>` is the minimal `Items`/`Add` class.
  - New files: `Query\Props\PropertyContainer.cs`, `Query\Props\PropertyFilter.cs`, `Query\NavProps\NavPropOfContainer.cs`, `Query\NavProps\NavPropQueryExtensions.cs`; `Query\Props` is a new subfolder in `ProcessQuery`.
  - `ProcessQuery` stops writing the Lookup, `QueryFilters`, `SortFilters`, `TypedFilterBuilder` and `TypedFilterBuilderExtensions`. Their generator source files are kept uncalled for now.
- `QueryConstants` uses `TProps` and the `PropertyContainer` constraints; `TQuery` and `TQueryBuilder` are gone. `Names` gains the new type and file names.

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- Generated the Coverage model: the Query tree matches `GENERATOR_CHANGES.md` 5.1/5.2 (12 rewritten files, 4 new, nothing stale). The 132 `FilterBuilderExtensions` signatures match the prototype's file one by one.
- Compiled the generated `SFTest.Query` + `SFTest.Entity` sources together outside the repository with the containers excluded: 0 errors. With the containers included it fails on purpose (they still emit `Prop<XProp, T>`); step 3 rewrites them.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: not run for this step (its generated-text tests change with step 7).

Commit details

```
Query API redesign step 2: the Query project's common files (SF-141)

Query, QueryWrapper, QueryBuilder, PredicateBuilder, FilterBuilder,
SortBuilder, UpdateValuesBuilder, PaginationBuilder and NavPropsBuilder
are written once on TProps; Prop, PropertyFilter, PropertyContainer,
FilterBuilderExtensions, NavPropOfContainer and NavPropQueryExtensions
are the new files. The Lookup, QueryFilters, SortFilters and the old
typed filter builder are no longer written.

Bump version to 1.1.4.0.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.3.9 — 2026-11-09

Changes
- SF-141, step 1 of the query API redesign (`GENERATOR_CHANGES.md` section 4, the design written with the prototype): the naming helpers.
  - `Common.GeneratePropEnumName(string)` returns `{N}Prop` instead of `{N}Property`; `GenerateNavPropEnumName` returns `{N}NavProp` instead of `{N}NavProperty`. The stored procedure override follows through `GenerateRequestTypeName`, so `X_RequestProperty` becomes `X_RequestProp`.
  - `GeneratePropClassName(Entity)` returns `{N}Property` (the container) instead of the old static `{N}Prop`. Added `GeneratePropClassName(StoredProc)` (`{N}_RequestProperty`), `GeneratePropClassName(string)` (for navigation targets known only by name) and `GenerateNavPropClassName(Entity)` (`{N}NavProperty`).
  - `Names.cs`: `GenericProp` is `TProps` instead of `TPropEnum`; added `PropertyContainer`, `NavPropertyContainer`, `PropertyFilter`, `PropertySortFilter`, `ContainerNavProp`, `FilterBuilderExtensions`, `NavPropQueryExtensions` and the `NavPropOfContainer` file name. The names of the generators that stop (Lookup, the enum filter collections, `TypedFilterBuilder`) stay until those generators stop reading them (steps 2 and 3).
  - Backlog SF-141 for the whole redesign, marked In progress.
- Generated code does not compile after this step alone; that is expected (`GENERATOR_CHANGES.md` section 9).

Verification
- `dotnet build SCF.slnx`: 0 errors, the same CS0618 warning.
- Generated the Coverage model: the enum files are now `PropertyEnums\<Entity>Prop.cs` and `NavPropertyEnums\<Entity>NavProp.cs`, and the typed props file is `Props\<Model>\<Schema>\<Entity>Property.cs` declaring `public static class <Entity>Property` with `Prop<<Entity>Prop, T>`. Step 3 turns it into the sealed container.
- `SCF.Tests`: 35 passed, 3 skipped, 0 failed. `SCF.App.Tests`: 48 passed, 4 skipped, 1 failed — `TypedPropsTests.TheCodePreviewShowsATablesTypedProps` still expects `public static class WarehouseProp`; it is updated to the new output in step 7, as the document says. `PolishTests.TheSolutionListSaysItIsLooking` failed once in the full run and passed alone on the re-run (a UI timing test, unrelated).
- GenCheck not run against the baselines: the output changes on purpose between steps 1 and 6.

Commit details

```
Query API redesign step 1: the container names (SF-141)

The property and navigation property enums take the Prop and NavProp
names; the property class helper returns the container name
(Property, RequestProperty, NavProperty). Names gains the container
api entries and GenericProp becomes TProps. Step 1 of
GENERATOR_CHANGES.md; generated code does not compile until step 6.

Bump version to 1.1.3.9.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.3.8 — 2026-11-09

Changes
- Fix the SF-137 test `SolutionSyncTests.OnlyTheCommonRepositoryProjectGetsTheSqlClientPackage`, which could never pass. It required the `Microsoft.Data.SqlClient` `ItemGroup` to be the last thing in the common Repository project file, but Sync writes the project file from the template first and then appends the project's `ProjectReference` group, so the package group is followed by a second `ItemGroup`. The test now asserts what it is named for: the package reference is in `SFTest.Repository` and no other generated project has one. The generated output itself is correct and unchanged.

Verification
- `dotnet test Tests/SCF.Tests/SCF.Tests.csproj`: 35 passed, 3 skipped, 0 failed.

Commit details

```
Fix the SqlClient package test from SF-137

The test required the package ItemGroup to be the last block in the
common Repository project file; Sync appends the project references
after it. It now checks that the package is in SFTest.Repository and
in no other generated project. Generated output is unchanged.

Bump version to 1.1.3.8.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.3.7 — 2026-11-09

Changes
- SF-142 (user: "add a backlog task... because thay WMI shit dont work. i tried and could not see an open visual studio solution."): a backlog entry for Sync's target list not showing a solution open in Visual Studio. Found while reviewing why the app references `System.Management`: the IDE detection runs a WMI query (`Win32_Process`) for the command lines and window titles of Visual Studio, Rider and VS Code, and the user tested it with a Visual Studio solution open and got no "Open in Visual Studio" candidate. The entry lists what to investigate and allows dropping WMI. No code changes.

Verification
- None needed: only `BACKLOG.md`, `VERSION`, `Versionable.cs` and these notes change. No generator output changes, so the recorded 1.1.3.6 "before" still applies.

Commit details

```
Backlog: Sync does not find a solution open in Visual Studio (SF-142)

The target list's IDE detection uses WMI over Win32_Process. The user
tested it with a Visual Studio solution open and no "Open in Visual
Studio" candidate appeared. Recorded as a bug to investigate and fix.

Bump version to 1.1.3.7.

Co-Authored-By: Claude Code <noreply@anthropic.com>
```

---

## 1.1.3.6 — 2026-11-08

Changes
- Two backlog entries for the query API redesign prototyped outside the repository. No code changes.
  - SF-139 (user: "We are going to ship the software to people that have been using it for a long time. So add this questions to the backlog for later revisit. The bahavior for today should be accept an array of 5 types and when the column is nullable, it has to be the non nullable undelying type"): the open questions on typed `In` / `NotIn`, one generic `In(TValue[])` on every column type, and nullable columns taking `TValue[]` with a rule for nulls in the list.
  - SF-140 (user: "what is the easiest solution right now?"; "do it"): hiding the property and navigation property enums from user code, internal in the Entity project with `InternalsVisibleTo` in a generated `.cs` file, since Sync never edits an existing project file. For now they stay public, as today.
- These two entries were added earlier without a version bump; this change set records them (user: "also you should have been keeping commit messages and version numbers").

Verification
- None needed: only `BACKLOG.md`, `VERSION`, `Versionable.cs` and these notes change.

Commit details

```
Backlog: typed In/NotIn questions and hiding the enums (SF-139, SF-140)

Records two open items from the query API redesign prototyped outside
the repository: the In/NotIn questions for later, and hiding the
property and navigation property enums from user code.

Bump version to 1.1.3.6.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.3.5 — 2026-11-08

Changes
- SF-138 (user: "those files use a wrapping namespace, i rather use one single line a the top of the class for namespace"; "public void CheckComplete() ... we need to make it readonly"; "the Length property should be marked readonly"; "System.Text is not needed as using statement"; "remove override method and change the first method to use TryAdd"; "should actually look like this"): more changes to the generated code.
  - The eight SQL engine files (`Database.cs`, `DatabaseDelete.cs`, `DatabaseFunction.cs`, `DatabaseInsert.cs`, `DatabaseSelect.cs`, `DatabaseSequence.cs`, `DatabaseStoredProc.cs`, `DatabaseUpdate.cs`) have a file-scoped namespace (`namespace ...Repository.Common;`) instead of a block namespace, so everything in them moves one level (4 spaces) to the left. No text other than the indentation changed; the files have no multi-line strings, so the SQL is unchanged.
  - `Database.cs`: `SqlText.Length` and `SqlText.CheckComplete()` are `readonly` (neither changes the struct); `using System.Text;` is removed (nothing used it).
  - `Database.cs`: `OverrideModelConnectionString` is removed, and with it the `_syncRoot` lock only it used. `RegisterModelConnectionString` adds with `TryAdd` and throws the same `ArgumentException` for an id that is already registered. Code of the user's that calls `OverrideModelConnectionString` no longer compiles.
  - `NavProps.CalculateCount` is `private static` (it uses no instance data; reverses SF-036), and its single-statement `if` and `foreach` bodies are indented, with a blank line after each.

Verification
- Not run yet: the build, both test projects, GenCheck against the saved baselines (expected on top of 1.1.3.4: the eight SQL engine files and `NavProps.cs` differ, nothing else), and compiling the generated code outside the repository.

Commit details

```
File-scoped namespace in the SQL engine, tidy Database and NavProps (SF-138)

The eight generated SQL engine files use a file-scoped namespace, one
level less indentation, same text and SQL. Database: SqlText.Length
and CheckComplete are readonly, the unused System.Text using is gone,
OverrideModelConnectionString and its lock are removed, and
RegisterModelConnectionString uses TryAdd. NavProps.CalculateCount is
static, with its if and foreach bodies indented.

Bump version to 1.1.3.5.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.3.4 — 2026-11-08

Changes
- SF-137 (user: "use Lock not object"; "remove ... Protected constructor to de-serialize data"; "\"public new\" instead of \"new public\""; "add this nugget to the Repository project when the generator creates it"; "ONLY the common Repository gets the nuget"; "Do not add System.Secutiry.Cryptography... microsoft will fix it soon enough"): four changes to the generated code. They reverse SF-052 and SF-057, dropped earlier because the output was not to change.
  - The generated SQL engine (`Database.cs`) locks on a `System.Threading.Lock` instead of an `object`: `private static readonly System.Threading.Lock _syncRoot = new System.Threading.Lock();`. The `lock` statement and the SQL are unchanged.
  - `DataExchangeException` no longer has the protected serialization constructor (`SerializationInfo`, `StreamingContext`), nor the `using System.Runtime.Serialization;` it needed. That also removes the SYSLIB0051 warning. A class of the user's that derives from it and calls `base(info, context)` no longer compiles.
  - Members that hide an inherited one are written `public new` instead of `new public` (`NavProps`, `FilterBuilder`, `TypedFilterBuilder`, `MultiKeyDictionary`): `new` now comes after the access modifier and `static`, before `readonly`.
  - Sync creates the common Repository project (where the SQL engine is) with the `Microsoft.Data.SqlClient` 7.1.1 package. The other projects get none. A project file already on disk is still kept as it is, so projects created by an earlier version do not get the package.
- The app, the engine and the website lock on `Lock` instead of `object`, each field `private static readonly` (user: "fix all our code to use Lock instead of object. and make sure its static readonly"): `AppSettingsStore`, `LogManager` and `SearchController`. The `lock` statements are unchanged.
- No project pins the C# version any more (user: "remove thos language pins from everywhere"): `<LangVersion>11.0</LangVersion>` is removed from all 14 project files, so each uses the default of its target framework (C# 14 for .NET 10).
- Tests: a new test checks that only the common Repository project file has the SqlClient package; the test that keeps a hand-edited project file now edits it with a `<Description>` instead of that same package.

Verification
- Not run yet: the build, both test projects, GenCheck against the saved baselines (expected: `Database.cs`, `DataExchangeException.cs`, `NavProps.cs`, `FilterBuilder.cs`, `TypedFilterBuilder.cs` and `MultiKeyDictionary.cs` differ, nothing else), and a Sync into a new solution built without adding a package by hand.

Commit details

```
Use Lock, drop the serialization constructor, add SqlClient (SF-137)

Generated code: the SQL engine locks on System.Threading.Lock instead
of object; DataExchangeException loses its protected serialization
constructor and the System.Runtime.Serialization using; hiding
members are written "public new" instead of "new public"; Sync
creates the common Repository project with the Microsoft.Data.SqlClient
7.1.1 package. The SQL is unchanged.

Our own code locks on Lock instead of object (AppSettingsStore,
LogManager, SearchController), and no project pins LangVersion any
more.

Bump version to 1.1.3.4.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.3.3 — 2026-11-07

Changes
- SF-136 (user: "Show me a more detailed plan on how you could do this typed API"; "Use PersonProp naming convention"; "wait but why cant we have the OrderBy?"): the generated query classes get a typed API next to the property enums. `.Where(PersonProp.Age).GreaterThan(30)` takes only an `int` when Age is an `int` column; a value of another type, or a condition the column's type does not have, does not compile.
  - `Prop<TPropEnum, TValue>`, new in the Query project: a property with the .NET type of its column.
  - `{Entity}Prop`, one per table and view (`Props/<model>/<schema>/`, in the property enum's namespace): a `static readonly Prop` for each column, typed as the entity declares the property (`int?` for a nullable int column). sql_variant columns (`object`) keep the enum API only.
  - `Where` and `Or` take a `Prop` and return a `TypedFilterBuilder`: `Equals` and `NotEquals` take the column's type. It hides `object.Equals(object)` with an obsolete-as-error `Equals(object)`, so a value of the wrong type does not reach `object.Equals` and quietly add no filter.
  - `TypedFilterBuilderExtensions` adds the conditions some types have, as the enum API offers them: the text filters on strings; `In` and `NotIn` on `int`, `long`, `short`, `byte` and strings; the comparisons on `int`, `long`, `short`, `byte`, `decimal`, `double` and `DateTime`; the date, year, month and day filters on `DateTime`; `IsNull` and `IsNotNull` on nullable value types and strings. The nullable versions of the value types take the same values.
  - `OrderBy`, `OrderByDescending` and `Set` take a `Prop` too; `Set` takes only a value of the column's type.
  - The enum API does not change, and the two can be mixed in one query. Every typed call goes through the same `QueryFilter`, so the SQL and the parameters are the same.
  - The C# code builder writes attributes on methods (it did on classes only).
- App: the Models page shows a table's or view's `{Entity}Prop` file in its code preview.
- Website: the Filtering page has a Typed Properties section.
- Removed a stray `_le_probe.cs` left in the repository root by the line endings work.

Verification
- 49 filters run against SQL Server once with the enum and once with the typed properties: equality on every kind of value, null checks, the text filters, IN and NOT IN, the comparisons, the date parts, AND and OR, nested groups, a query that mixes both, sorting, paging, nav props and updates. Each pair sent one command, and the command text, the parameters and the results are identical.
- The engine check of the existing 852 operations matches the 1.1.3.2 run: the commands and parameters are identical, and the results too once the values the database makes on each run (`GETDATE()` and `NEWID()` defaults) are left out.
- 18 mistakes compiled against the generated code all fail: a string or a double for an int or long column, null for a NOT NULL column (also as a statement on its own), IsNull on a NOT NULL column, StartsWith on a number, GreaterThan on a string or a bool, IN on a Guid column, an int[] for a short column, date parts on a string or a DateTimeOffset, a string year, a property of another entity in Where, Set with the wrong type, and OrderBy with another entity's property.
- The reference copy's regenerated code and the engine check's Coverage code compile with 0 errors. The test model's generated code has 190 new files (187 `{Entity}Prop` classes and 3 common ones) and 4 changed builders; the saved baselines were updated.
- SCF.slnx and the website build with 0 errors. SCF.Tests passes 36 of 37 and SCF.App.Tests 53 of 54, with 1 new test for the code preview (the two skipped tests need their own databases).

Commit details

```
Add a typed query API next to the property enums (SF-136)

Each table and view gets an {Entity}Prop class with a Prop<TPropEnum,
TValue> per column. Where, Or, OrderBy, OrderByDescending and Set take
it, and the filter methods only accept values of the column's type:
the text filters exist on strings, the comparisons on numbers and
dates, the date parts on DateTime, IsNull on nullable columns. A wrong
type is a compile error. The enum API is unchanged and the SQL is the
same: 49 enum/typed pairs sent identical commands, parameters and
results against SQL Server. The app's code preview shows the new
file; the website documents it on Filtering.

Bump version to 1.1.3.3.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.3.2 — 2026-11-07

Changes
- SF-135 (user: "i am pretty sure i said earlier that you need to upgrade to slnx"; "when i told you to upgrade to NET 10"): the solution is `SCF.slnx`, the XML solution format, made with `dotnet sln migrate`. `SCF.sln` is removed.
  - The same 14 projects in the same solution folders (App, Common, Dependencies, Tests, Tools, Websites), each built in every configuration, as in the old file.
  - `README.md`, `TESTING.md` and the CI workflow build `SCF.slnx`. The earlier backlog and release notes entries keep the old name: they describe the solution as it was then.

Verification
- `dotnet build SCF.slnx` builds with 0 errors in Debug and Release, the website included.
- `dotnet sln SCF.slnx list` lists the 14 projects.
- `dotnet test SCF.slnx`: SCF.Tests passes 36 of 37 and SCF.App.Tests 52 of 53 (the two skipped tests need their own databases).

Commit details

```
Move the solution to SCF.slnx (SF-135)

The solution is now SCF.slnx, the XML format, made with dotnet sln
migrate; SCF.sln is removed. Same projects and folders. README,
TESTING and the CI workflow build the new file.

Bump version to 1.1.3.2.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.3.1 — 2026-11-07

Changes
- SF-134 (user: "rename PackagedDependencies to just Dependencies. Both in the solution and on the disk"): the folder that holds SCF.CodeGenerator, SCF.CSharpGenerator, SCF.Db, SCF.Engine, SCF.Models and SCF.Utils is now `Dependencies`, on disk and as the solution folder.
  - Updated: `SCF.sln`, the project references of SCF.App, SCF.Cli, SCF.Tests, SCF.App.Tests, SCF.GenCheck and SCF.SchemaCheck, `.gitignore`, `README.md` and `TESTING.md`.
  - The earlier entries of these release notes keep the old name: they describe the folder as it was then.

Verification
- SCF.sln builds with 0 errors in Debug and Release, and so does the website.
- SCF.Tests passes 36 of 37 and SCF.App.Tests 52 of 53 (the two skipped tests need their own databases).
- GenCheck generates the test model from the renamed projects: 1216 of 1216 files identical to the saved baseline.
- The CLI runs (`info` on the test model, exit code 0).
- No file in the repo names `PackagedDependencies` any more, apart from the old release notes.

Commit details

```
Rename PackagedDependencies to Dependencies (SF-134)

The folder with the generator, engine, model and database projects is
now Dependencies, on disk and in the solution. The project
references, .gitignore, README and TESTING follow. Nothing else
changes.

Bump version to 1.1.3.1.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.3.0 — 2026-11-07

Changes
- SF-133 (user: "please use verbs on method names... like CalculateListCapacity"): in the generated `RepositoryBase.cs`, `ListCapacity(topCount, pagination)` is renamed `CalculateListCapacity`. It works as before: the TOP or the page size, whichever is smaller, up to 10,000; 0 when the query has neither.

Verification
- GenCheck: only `RepositoryBase.cs` changes in the regenerated code, and only the method's name and its four calls. The saved baseline (1216 files) and the reference copy (1508 files) were updated and match.
- The reference copy's regenerated code compiles with 0 errors, and so does the engine check harness.
- SCF.sln builds with 0 errors. SCF.Tests passes 36 of 37 and SCF.App.Tests 52 of 53 (the two skipped tests need their own databases).
- Earlier the same day, the list sizes were measured end to end against SQL Server, next to the same generated code with `new List<TEntity>()`. The lists start at 50 for Top(50), 100 for a page of 100, and 500 with neither. Sizing saves 24 bytes to 4.9 KB a call, 0.1% to 1.2% of what the call allocates. A small result with no TOP or page costs 3.7 KB more, and Top(50000) 151 KB more because of the 10,000 cap. With 50,000 rows, the list's growth is about 1 MB of a 58 MB call.

Commit details

```
Rename ListCapacity to CalculateListCapacity (SF-133)

The generated RepositoryBase's method that works out a result list's
starting size gets a verb in its name. Nothing else changes.

Bump version to 1.1.3.0.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.9 — 2026-11-07

Changes
- SF-131 (user: "please make those two private static readonly fields and use _ + camelCase"): in the generated `RepositoryBase.cs`, the two list sizes are `private static readonly int _unboundedListCapacity = 500` and `private static readonly int _maxListCapacity = 10000`. They were `private const int UnboundedListCapacity` and `MaxListCapacity`.
- SF-132 (user: "do something to permanently fix the line endings"; "just one for all files"): every text file in the repo uses Windows line endings (CRLF).
  - 88 files that had Unix line endings were converted: Razor views, project files, some app views and code, the markdown files and the two workflow files. Their content did not change.
  - `.editorconfig` now sets `end_of_line = crlf` for every file, not only C# files.
  - New `.claude/hooks/keep_line_endings.py`: it puts CRLF back in any file changed since its last run. It is meant to run after every shell and edit command, as a hook in `.claude/settings.json`; `--all` checks the whole repo.

Verification
- GenCheck: only `RepositoryBase.cs` changes in the regenerated code, and only those two fields and the three lines that use them. The saved baseline (1216 files) and the reference copy (1508 files) were updated and match.
- The reference copy's regenerated code compiles with 0 errors.
- Line endings: all 371 text files are CRLF. The hook script was tried on a scratch file: after `sed -i` changed it to Unix line endings, one run put CRLF back.
- SCF.sln and the website build with 0 errors. SCF.Tests passes 36 of 37 and SCF.App.Tests 52 of 53 (the two skipped tests need their own databases).

Commit details

```
Static readonly list sizes; CRLF for every file (SF-131/132)

The generated RepositoryBase keeps its two list sizes in private
static readonly fields, _unboundedListCapacity and _maxListCapacity,
instead of constants. Every text file in the repo now uses CRLF:
88 files were converted, .editorconfig sets it for all files, and
.claude/hooks/keep_line_endings.py puts CRLF back after any tool
writes LF.

Bump version to 1.1.2.9.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.8 — 2026-11-07

Changes
- SF-130 (user: "NO MORE HACKS"; "couldn't you just add an extra parameter?"): in the generated code, the list size is passed as a parameter all the way to the list. SF-125 (1.1.2.4) sent it through a `[ThreadStatic]` field that `MaterializeResult` set and `MaterializeEntityResult` read back. That field is gone.
  - The materializers take the size: `Func<SqlDataReader, int, object>` instead of `Func<SqlDataReader, object>`, in `MaterializeResult`, the nav prop materializer lists and the select and update methods.
  - `MaterializeEntity(SqlDataReader r, int listCapacity)` and each repository's `Materialize(SqlDataReader r, int listCapacity)` pass it on to `MaterializeEntityResult(r, MaterializeSingleEntity, fieldCount, listCapacity)`, which makes the list.
  - The sizes are the same as before: 1 for a key lookup, an insert and each updated row; the TOP or the page size, whichever is smaller, up to 10,000; 500 otherwise.
  - Breaking for hand-written code only: a class of its own that derives from `RepositoryBase` needs the new `listCapacity` parameter on `MaterializeEntity` and `GetNavPropMaterializers`. Regenerated code is consistent.
- Generated files that change: `RepositoryBase.cs` and every table and view repository (their materializers).

Verification
- The engine check against SQL Server, run on the 1.1.2.6 code and on the new code: 852 operations, 889 commands, 1,767 parameters.
  - The commands and the parameters are identical in every operation.
  - The returned entities are identical once the values the database makes on each run are left out: the `GETDATE()` and `NEWID()` defaults of the AllType and audit log rows (38 lines of 8,032).
- GenCheck: the regenerated code matches the updated saved baseline (1216 of 1216 files) and the reference copy (1508 of 1508). Only the materializer lines and their doc comments changed.
- The reference copy's regenerated code compiles with 0 errors. The test model's code has the two known errors of a table named like its schema (SF-124, "Nice to have"), in a file this change does not touch.
- SCF.sln builds with 0 errors. SCF.Tests passes 36 of 37 and SCF.App.Tests 52 of 53 (the two skipped tests need their own databases).

Commit details

```
Pass the result list size as a parameter (SF-130)

The generated code sent each result list's size through a
[ThreadStatic] field. The size is now a parameter of the
materializers (Func<SqlDataReader, int, object>), MaterializeEntity,
Materialize and MaterializeEntityResult, and the field is gone. The
sizes, the SQL and the results do not change.

Bump version to 1.1.2.8.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.7 — 2026-11-06

Changes
- SF-129 (user: "do one more round with your team"): a second review of the app and the CLI, by me and two independent experts. Nothing from 1.1.2.6 came back. Fixed:
- Model picker: opening a second model file (Open, a recent file, or a new file while one was open) left no model selected. The Models page showed an empty tree, Layers had no model, and Update asked for a model. The picker now gets the new file's list before its first model is selected.
- Wizards lock the top bar and the navigation for the keyboard too, as dialogs do. Before, Tab reached Open, New and the model picker behind an open wizard. A file the system hands over (macOS Finder) waits until the wizard is closed. Before, it could swap the file under the wizard, and the wizard's model then went into the wrong file.
- Models page: picking a column, schema or sequence while the Generated code tab was open left an empty editor. The Details tab now comes back.
- Sync page:
  - A preview is cleared once the model changes after it (Layers, Settings, the target framework, an added or updated model), so it never shows files Sync would no longer write.
  - Opening another model file starts the Log tab over.
  - Filtering the changed files no longer blanks the difference for a moment, so it keeps its place.
  - After a new model file's first save, the solutions next to it are looked for.
- CLI:
  - An argument a command does not take is a usage error (exit 2). Before, it was ignored: `sync ... --previw` synced for real and `update ... --chek` saved the file. `--model` needs a model name.
  - `update` no longer saves when the database could not describe an object (for example a procedure whose result type cannot be worked out). It exits with 1, and it lists those objects as "left out", not as "no longer in the database". Before, it dropped them from the file and exited with 0.
  - A model file that is there but cannot be read exits with 1 (a failure); a missing one still exits with 2.

Verification
- SCF.App.Tests: 52 of 53 pass with the test database (`SHARPFACTORY_TEST_EDMJ`). The docs screenshot test is skipped (it needs its own database).
  - 6 new tests in `SecondReviewTests`. The four app findings failed before their fixes: the empty model picker, the stuck code tab, the top bar behind a wizard and the Sync log.
- SCF.Tests: 36 of 37 pass. The quoted-names test is skipped (it needs its own database). The engine did not change.
- SCF.sln builds with 0 errors.
- CLI, run against copies in the test folder:
  - `sync ... --previw`, `update ... --chek`, `update m.edmj Sales`, `update ... --model`, `update ... --model --check` and `info m.edmj extra`: 2, with "Unexpected argument"
  - `info` on a file that is not a model: 1
  - `info` on a missing file: 2
  - `info`, `update --check` and `sync --preview`: 0
- The "could not be read" path of `update` was checked by reading the code, not run: the test database has no procedure the engine cannot describe.
- The style check finds nothing new in the app or the CLI. The one new hit is the new test class, which xUnit needs public.

Commit details

```
Fix the second app review findings (SF-129)

A second review of the app and the CLI. Opening a second model file
left no model selected. A wizard left the top bar usable from the
keyboard, and a file handed over by the system could replace the file
under it. Picking a column on the code tab showed an empty editor. A
Sync preview outlived model changes, and the Sync log outlived the
file. The CLI ignored misspelled options, so --previw synced for real.
update saved and exited 0 after dropping objects it could not read.

Bump version to 1.1.2.7.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.6 — 2026-11-06

Changes
- SF-128 (user: "do a full code review, everything in the App ... fire an coding expert to review it as well"; "Fix everything"): the findings of two independent reviews of the app and the CLI, all fixed.
- Object chooser:
  - Clicking a partly checked category or schema now checks every object under it. Before, the check box turned the click into "unchecked" and everything under it was unchecked; in the Update wizard that removed objects from the model. A click on a fully checked group unchecks it.
  - Checking a category or schema, Select all and None act on the objects the search shows. The objects it hides keep their check boxes.
- Sync page:
  - The search for solutions belongs to the model file it started for. Before, a file opened during the search received the previous file's solutions, and could get the previous file's target.
  - Any model of the file marked as synced turns Sync off, and the page names those models. Before, only the model picked in the top bar counted, although Sync generates every model.
  - The solutions are looked for once per model file (the refresh button looks again). The chosen solution stays highlighted, with its "Open in an IDE" or "Synced" badge, after a new search.
  - Filtering the changed files keeps the selected file and its difference. The list is filled once per run, not twice.
  - After a first Save As, the new solution's folder is the model file's folder.
- Models page:
  - The code preview is built again when the page is shown, so it follows Layers and Settings edits.
  - The tree search takes out and puts in only the rows that change: the rows that stay keep their state, and the highlight follows every key. The object chooser search does the same.
  - An Add or Update refreshes the pages once (it was three times).
- Dialogs:
  - Escape answers a dialog as Cancel.
  - The page and the wizard behind a dialog are locked.
  - The primary button takes the keyboard, so Enter chooses it.
  - Error messages show the warning sign.
  - A second dialog answers the first one as cancelled, so whatever waits for the first one goes on.
- Wizards: after a failed build, Back returns to the objects with the connection and the choices kept.
- Recent files:
  - A file that is there but cannot be read (locked, a share that is offline) stays on the recent list. Only a missing file leaves it.
  - A double click on a recent file opens one dialog.
- CLI: a failure the commands did not expect (for example `update` saving a read-only model file) prints `ERROR: ...`, goes to the error log and exits with 1; it crashed before. Errors and the usage go to standard error.
- Removed: the "The theme is in the top bar" sentence on Settings, unused styles, properties and usings, and the summary text kept only for tests.

Verification
- SCF.App.Tests: 46 of 47 pass with the test database (`SHARPFACTORY_TEST_EDMJ`), including the wizards' builds against SQL Server. The docs screenshot test is skipped (it needs its own database).
  - 9 new tests, one per finding, in `AppReviewTests`. The chooser tests click the real check boxes.
- SCF.Tests: 36 of 37 pass. The quoted-names test is skipped (it needs its own database). The engine did not change.
- SCF.sln builds with 0 errors.
- CLI exit codes:
  - no arguments: 2, usage on standard error
  - `help`: 0, usage on standard output
  - a missing or broken model file: 2
  - `info`: 0
  - `update` on a read-only model file that changed: 1, `ERROR: Access to the path ... is denied.`
- The style check finds nothing new in the app or the CLI. The one new hit is the new test class, which xUnit needs public.

Commit details

```
Fix the app code review findings (SF-128)

Two independent reviews of the app and the CLI. The object chooser
unchecked a partly checked group on click, and checked objects the
search hid. The Sync page could keep a previous model file's solutions
and target, and the synced-model block looked only at the picked
model. Also: a stale code preview, dialogs without Escape that left
the keyboard on the page behind, a dead end after a failed build, a
CLI crash on a read-only model file, the tree search rebuilding every
row on each key, and dead code removed.

Bump version to 1.1.2.6.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.5 — 2026-11-06

Changes
- SF-126 (user: "do this fix for sure"): the generated code reads nullable number and date columns with the typed getter: `r.IsDBNull(5) ? null : r.GetDecimal(5)` instead of `r.GetValue(5) as decimal?`. `GetValue` returns `object`, so the old read boxed every value of every row into a new heap object.
  - This covers entity repositories, stored procedure and function result sets, and scalar function results.
  - Strings, byte arrays, `object` (sql_variant), XML and spatial columns are read as before.
  - The SQL is unchanged.
- Generated files that change: entity repositories with a nullable number or date column, and the stored procedure and function repositories (only those reads).

Verification
- The engine check, run on the 1.1.2.4 code and on the new code: 852 operations, 889 commands, 1,767 parameters. The commands, the parameters and every returned entity are identical.
  - Every nullable column of every type is read holding a value and holding NULL. The reads cover a key lookup, a whole table, a TOP and an async read.
  - The script's insert of an all-columns-filled row now runs; its sql_variant value is now NULL.
- Reading 2,000 rows with 14 nullable number and date columns filled: 1,613 bytes per row before, 1,214 after; 17.2 ms per call before, 11.2 ms after.
- GenCheck: 1216 of 1216 files identical to the saved baseline, and 1508 of 1508 identical to the saved reference copy. Every changed line in the 43 and 42 changed files is one of these reads. The reference copy's regenerated code compiles with 0 errors.
- SCF.sln builds with 0 errors. SCF.Tests passes 37 of 37 and SCF.App.Tests 38 of 38. The style check finds 40, as before.

Commit details

```
Read nullable columns without boxing (SF-126)

The generated readers read nullable number and date columns with
GetValue, which boxes every value of every row. They now check
IsDBNull and use the typed getter. The SQL and the returned entities
are unchanged; reading 2,000 rows with 14 such columns allocates 25%
less and is about a third faster.

Bump version to 1.1.2.5.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.4 — 2026-11-06

Changes
- SF-125 (user: "size from bounds is good but start the unbounded list at 500 items"): in the generated code, each result list starts at the size the query can return.
  - A key lookup (`SelectSingle`), an insert, and each row of an update start at 1.
  - A query with a TOP or a page starts at the TOP or the page size, whichever is smaller, up to 10,000.
  - Anything else starts at 500 and grows from there. That covers queries with neither, `UpdateAny`, nav prop result sets, and stored procedure and function results.
  - The size reaches the list through a `[ThreadStatic]` field in `RepositoryBase`. The field is set right before the rows are read and read back when the list is made, both on the same call chain, on the async paths too.
  - `MaterializeResult` and `SelectMany` gain optional parameters at the end, so existing calls to them still compile.
- SF-122 (user: "do sf 122 and sf 123"): a paged query with a parent nav prop and no filter wrote `WHERE` with nothing after it, and SQL Server rejected it. The `WHERE` is now written only when there is a filter.
- SF-123: a self-referencing nav prop (Employee.ManagerId to Employee) gave both tables the same alias.
  - With paging, SQL Server rejected the query ("The correlation name 'D' is specified multiple times").
  - Without paging, the query ran but compared each row with itself, so the nav prop came back empty (an employee's manager was null).
  - A table that is already in the statement now gets an alias of its own: `[D]` becomes `[D_1]`, and `[D_2]` one level further down. A query without a repeated table writes the same text as before.
- SF-124 moved to a new "Nice to have" section at the end of the backlog (user: "if people name tables like that is not our problem").
- Generated files that change: `RepositoryBase.cs`, `DatabaseSelect.cs`, and the stored procedure and function repositories (only the line that makes the list).

Verification
- The engine check compared the generated code from before 1.1.2.2 with the new code: 848 operations, 884 commands, 1,729 parameters. Every result is recorded, with every property of every returned entity and its nav props.
  - The commands are identical except in the 54 operations of SF-122 and SF-123:
    - 30 SF-122 operations: SQL Server rejected them before; now they return their rows.
    - 8 SF-123 operations with paging: rejected before; now they return their rows.
    - 16 SF-123 operations without paging: the manager was missing before; now it is filled in.
  - The only other result changes are the 130 from SF-121 (1.1.2.3), whose SQL did not change.
  - Nothing else differs: a key lookup, a TOP, a page or a whole table returns the same entities as before.
- Bytes per call, end to end against the local database: key lookup 5,720; filters and sort 11,653; a page with 2 nav props 22,977; whole table 8,464.
- GenCheck: 1216 of 1216 files identical to the saved baseline, and 1508 of 1508 identical to the saved reference copy, after replacing the files listed above in each. The reference copy's regenerated code compiles with 0 errors.
- SCF.sln builds with 0 errors. SCF.Tests passes 37 of 37 and SCF.App.Tests 38 of 38. The style check finds 40, as before.

Commit details

```
Size result lists from the query; fix two nav prop bugs (SF-122/123/125)

Result lists now start at what the query can return: 1 for a key
lookup, an insert or an updated row, the TOP or page size (up to
10,000), and 500 otherwise; stored procedure and function lists start
at 500. A paged query with a parent nav prop and no filter no longer
writes an empty WHERE. A self-referencing nav prop gets its own alias,
so paged queries no longer fail and the others return the related row
instead of nothing. Every other query sends the same SQL as before.

Bump version to 1.1.2.4.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

---

## 1.1.2.3 — 2026-11-06

Changes
- SF-121 (user: "fix the SF 121"): in the generated code, a paged query returned null (and threw a NullReferenceException when it had nav props) unless the total count was asked for (`ReturnTotalCount(true)`): any page, the first one too. A paged query always reads the total row count with the rows, so the rows come back together with it; the repository only took them out when the total had been asked for. It now always takes them out, and fills in the total only when it was asked for.
  - The one generated file that changes is `RepositoryBase.cs` (that method).
  - The SQL and the parameters are the same as before (the engine check: 884 commands, 1,729 parameters, all identical). 130 results change, all of them paged queries without the total count that now return their rows (129 page-2 queries of the script, and a paged query sorted by the default order).
- Found by the same check, not changed here (they fail in SQL Server today, before and after this change): SF-122 (a paged query with a parent nav prop and no filter writes an empty WHERE), SF-123 (a self-referencing nav prop gives both tables the same alias) and SF-124 (parameter names are made from column names, so a column such as `Unit-Price`, `Size(cm)` or `Weight.Kg` gives an invalid parameter and the insert fails).
- Generated code changes only as described above.

Verification
- The engine check, old against new: the commands and parameters of all 848 operations identical; results different only in the 130 paged queries above.
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the saved reference copy, after replacing `RepositoryBase.cs` in each.
- SCF.sln builds with 0 errors. SCF.Tests 37/37, SCF.App.Tests 38/38. The style check finds 40, as before.

Commit details

```
Fix paged queries returning nothing without the total (SF-121)

A paged query reads the total row count with its rows, and the
generated repository only took the rows out when the total had been
asked for: otherwise it returned null, or threw with nav props. It
now always takes the rows out and fills in the total only when asked.
The SQL and parameters are unchanged.

Bump version to 1.1.2.3.
```

---

## 1.1.2.2 — 2026-11-06

Changes
- SF-120 (user: "the generated code does a lot of string building and concat when translating the Query class into a SQL string ... performance and memory improvements"; "use the string.Create"): the generated SQL engine builds its statements with far less memory. The SQL text and the parameters it sends are byte-identical, so SQL Server sees exactly the same queries and plans (the user's condition: nothing that could change a query plan).
  - Result lists start empty instead of pre-sized for 10,000 rows. That pre-size was about 80 KB on every call, even for one row, and three of them for a query with two nav props.
  - SELECT, INSERT, UPDATE and DELETE statements are written by a small two-pass writer (a `ref struct` over a `Span<char>`): the first pass measures, the second writes straight into the final string with `string.Create`. No intermediate strings, no `+=` loops, no pooled or shared buffers. Numbers (parameter ordinals, IN lists) are written with `TryFormat` in the current culture, as `ToString()` wrote them; IN-list strings have their quotes doubled while they are written (a vectorized span count first, so strings without quotes are copied whole); parentheses for nested expressions are written in place.
  - A SELECT without nav props is one string. With nav props, the parts that are repeated in each nav prop's statement (WHERE, ORDER BY, the select head) are built once, and the statements are joined at their exact length.
  - The parameter array is allocated at its exact size (it was a list copied into an array), and each parameter name is one concatenation.
  - Removed the helpers nothing uses any more (`FastStringJoin`, `GenerateParamName`, the old SET-clause builders).
  - Not done, measured not worth it: caching SQL per query shape (computing and comparing the key costs about as much as writing the text now), and the per-call closure, the paging-total box and the type-name checks (tens of bytes next to 80 KB).
- The generated files that change: the five SQL engine files (`Database.cs`, `DatabaseSelect.cs`, `DatabaseInsert.cs`, `DatabaseUpdate.cs`, `DatabaseDelete.cs`), `RepositoryBase.cs`, and the stored procedure and function repositories (the result list). Every other generated file is unchanged.
- TESTING.md: the engine check (how to prove the SQL does not change), and the reference comparison against a saved copy.

Measured (the Coverage model on the local SQL Server; bytes allocated per call)

| Call | End to end, before | After | Engine only, before | After | Engine time, before | After |
|---|---|---|---|---|---|---|
| ByPK | 86,652 | 5,736 | 1,448 | 568 | 0.60 µs | 0.66 µs |
| filters + sort | 89,509 | 7,656 | 2,888 | 1,072 | 1.72 µs | 1.26 µs |
| paging + 2 nav props | 258,155 | 14,960 | 8,608 | 5,488 | 4.40 µs | 3.02 µs |
| IN with 100 ids | 88,867 | 4,952 | 4,720 | 840 | 2.20 µs | 2.52 µs |
| nested + sort + nav props | 179,513 | 15,416 | 9,368 | 5,200 | 5.72 µs | 5.15 µs |

End-to-end times are dominated by the database (about 1 ms a call) and vary between runs on this machine; the engine times are the fastest of 15 rounds of 100,000 calls. The IN list is about 10% slower: the old code got cached strings for small numbers from .NET.

Verification
- The engine check (TESTING.md): one script through the generated repository API against a fresh Coverage database, old engine against new: 884 commands, 1,729 parameters and 848 results, identical. It covers every filter condition, IN lists (with quotes), nested expressions, ordering, Top, paging with and without the total count, and nav props (parent, child, two, nested, self-referencing, composite foreign key), with inserts, updates (whole, changed columns only, by query, to null), upserts and deletes on identity, composite, no-identity and primary-key-is-foreign-key tables.
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the saved reference copy, after replacing in each exactly the files listed above (10 and 7).
- The large model's generated code (172 tables) compiles with the new engine.
- SCF.sln builds with 0 errors. SCF.Tests 37/37, SCF.App.Tests 38/38. The style check finds 40, as before.

Found while testing (not changed here): a paged query for page 2 or later returns null (and throws with nav props) in the generated code, before and after this change (SF-121).

Commit details

```
Generated SQL engine: string.Create, far less memory (SF-120)

The generated repositories build their SQL with a two-pass writer:
measure, then write once into the final string with string.Create.
No intermediate strings, no += loops, no pooled buffers; numbers are
formatted in place and IN-list quotes doubled as they are written.
Result lists no longer pre-allocate 10,000 rows (80 KB a call).
Memory per call drops from 85-258 KB to 5-15 KB end to end. The SQL
text and parameters are byte-identical (884 commands compared), so
SQL Server plans do not change.

Bump version to 1.1.2.2.
```

---

## 1.1.2.1 — 2026-11-06

Changes
- SF-119 (user: "1 and 3"): the app timed with a large model (the test database's, 185 tables, 1,216 generated files), from opening the file to the Sync preview and its log. A new test, `BigModelTimings`, does it every run (when `SHARPFACTORY_TEST_EDMJ` is set) with generous limits, and writes the times to `SCF_TIMINGS` when set.
  - Clearing the tree search took 1.4 s: the search opened every group on the way to a match and left them open, so the whole tree was laid out again. Clearing now folds back what the search opened (groups the user opened stay open): 0.13 s.
  - Everything else was already quick: opening the file 0.4 s, a search keystroke 0.1 to 0.6 s, selecting a table and showing its code under 0.1 s, opening Sync 0.1 s, the preview of 1,216 files 0.3 s, selecting and filtering the changes under 0.1 s.
- The `DataAccess` and `WPF` folders at the root are gone: they held only build output left from removed projects, no source.
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 37/37 and SCF.App.Tests 38/38 (the new timing test included).
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the reference output.
- The style check finds 40: the 39 known, plus the new test class, which xUnit needs public like the other test classes.

Commit details

```
Time the app with a large model; fast search clear (SF-119)

A new test times what a user does with the 185-table test model,
from opening the file to the Sync preview of 1,216 files. Clearing
the tree search took 1.4 s because the search left every group it
had opened expanded; it now folds them back (0.13 s). The leftover
build-only DataAccess and WPF folders are removed.

Bump version to 1.1.2.1.
```

---

## 1.1.2.0 — 2026-11-06

Changes (sprint 9: the software)
- SF-116, the command line:
  - `sharpfactory update <model.edmj> [--model <name>] [--check]` reads each model's objects again from its database, as the app's Update does when every object is kept, and saves the file. Objects the database no longer has drop out and are listed; new objects are not added. With `--check` nothing is saved and the exit code is 1 when a model would change, so a build can stop when the model is behind the database. The comparison ignores the foreign keys' internal ids, which are numbered afresh on every read.
  - `sharpfactory info <model.edmj>` shows the file's settings and each model's connection and object counts.
  - The engine part is `ModelUpdater` (with `ModelUpdateResult`), so it is tested like the rest of the engine.
  - Replacing a model after an update (the app's Update too) keeps it in its place in the file; it used to move to the end.
- SF-117, robustness:
  - A model file that is not JSON, is empty, or is missing parts (common settings, layers, a model's name or connection) is refused with the reason, in the app and on the command line, instead of crashing on the first use. A model that lacks a whole category (no sequences, say) gets it back empty.
  - Sync checks first that every file it would change or delete, the project files and the solution can be written. If one is read-only or in use, it stops before writing anything and says which ("Nothing was written. This file cannot be written: …"). It used to stop half-way, after writing the files before it.
  - `generate` reports a file it cannot write instead of crashing.
  - App settings with a list saved as null (hand-edited) read as empty.
- SF-118, speed: reading a database's keys and foreign keys took one INFORMATION_SCHEMA query per schema, and each took most of a second to compile. They are now one `sys.*` catalog query each for the whole database, and each schema takes its rows. On the test database (16 schemas, 185 tables) the keys went from 10.7 s to 0.4 s and the foreign keys from 13.7 s to 0.6 s; a whole update from about 30 s to 10 s (1.4 s once SQL Server has its plans cached). The new queries return exactly the rows, values and order the old ones did (compared on the three test databases), and the models read with them are identical.
- SF-056: .NET 11 is a generation target ("New projects use: .NET 11" on the Sync page; `net11.0` projects). .NET 11 is released on 10 November 2026; until then it needs the RC2 SDK, which has a go-live licence.
- Docs: the README and the Installation page list `update` and `info`; Model Generation says new projects can target .NET 10 or .NET 11.
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 37/37 (new: an update drops an object the database does not have and keeps the model's place; replacing a model keeps its place; broken model files say why; null settings lists; a read-only file stops Sync before anything is written; .NET 11 projects target net11.0 and the choice is saved) and SCF.App.Tests 37/37 (opening an incomplete model file shows the reason).
- `sharpfactory update --check` on the test database's model and on the Coverage model: "no changes", before and after the new queries.
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the reference output.
- The style check finds 39, as before.
- Not verified here: building a generated .NET 11 project (no .NET 11 SDK on this machine).

Commit details

```
CLI update and info, safer Sync, faster schema reads, .NET 11

The command line can refresh a model from its database (update,
with --check for builds) and describe a model file (info). Broken or
incomplete model files are refused with the reason instead of
crashing, and Sync checks that every file it would change can be
written before it writes anything. Keys and foreign keys are read
with one catalog query per database instead of one per schema: an
update of the 185-table test model went from about 30 s to 10 s,
with identical results. .NET 11 is a generation target. The
generated code does not change. (SF-116, SF-117, SF-118, SF-056)

Bump version to 1.1.2.0.
```

---

## 1.1.1.9 — 2026-11-06

Changes (sprint 8)
- SF-059: the generator's C# 7 and C# 10 switches are gone. Every supported target is .NET 10 or later, so they were always on; the old-syntax branches (block namespaces, `Action<SqlDataReader>` lambdas closed with a semicolon) and the `CodeGeneratorSettings` class that set the switches are removed. Generated code does not change (GenCheck below).
- SF-115 (the optional polish from the SF-109 review rounds):
  - While searching, a group row says how many of its objects are left: "Tables 2 of 14".
  - Choosing a code file whose tab is past the edge of the row scrolls the tab into view.
  - The generated-code preview no longer wraps lines (as in an IDE): the file banner stays on one line and long lines scroll sideways.
  - The search highlight was checked on a selected row: it stays readable.
  - The Model Generation docs screenshots are retaken from this version (the "Connected to" line, the log with times and Copy, the unwrapped code); the six wizard images are cropped to the dialog as before.
- SF-111 (part), the docs:
  - Retrieve has a summary table of the retrieval methods (parameters, what each returns, when it is there), and says that every method has an async version. Its text named the unique-key methods `ByPK{number}`; they are `ByUK`, or `ByUK1`, `ByUK2` and so on when an entity has several.
  - Screenshots shown smaller than their real size open full size on a click (Esc or a click closes them).
- SF-045 and SF-001: `FileTransferTools` (the old SFTP tool), `LinuxScripts` and `PublishBats` (they published the removed APIs and named the old server) are out of the repository and the solution.
- Backlog: SF-043, SF-044 and SF-047 are closed; the website redesign already settled them.
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 32/32 and SCF.App.Tests 37/37 (the polish tests now also check "2 of 14" and the last tab coming into view).
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the reference output, after removing the switches.
- The style check finds 39, as in 1.1.1.8.
- In the browser: the Retrieve table fits the column with no sideways overflow, and on Model Generation all six screenshots open full size and close again.

Commit details

```
Sprint 8: generator cleanup, app and docs polish (SF-059, SF-115)

The generator's always-on C# 7/10 switches and the old-syntax
branches behind them are removed; the generated code is identical.
The app's tree search says "2 of 14" on group rows, the selected code
tab scrolls into view, and the code preview no longer wraps. The docs
get a Retrieve method table (and the right ByUK names), click-to-zoom
screenshots and retaken Model Generation images. The old deploy
folders (FileTransferTools, LinuxScripts, PublishBats) are out of the
repository (SF-045, SF-001).

Bump version to 1.1.1.9.
```

---

## 1.1.1.8 — 2026-11-06

Changes
- SF-109 (user: "SF-109 with judges"): the app polish the reviewers left for after a first release. The three reviewers graded it in two rounds and signed off (mean 8.1 of 10; every item 7 or more).
  - **Home, first run**: until a model file has been opened, a Getting started card shows the three steps (create a model file, shape the code on Layers, preview then Sync) and a "Read the guide" button that opens the Documentation page. The page fits 1280 × 820 without scrolling.
  - **Code file tabs**: when the row of files is wider than the panel, the edge with more tabs fades and shows an arrow, and the mouse wheel scrolls the row sideways.
  - **Add wizard**: after Load, a green line says where it connected: "Connected to localhost (Windows) · 5 databases", or "as sa" for a SQL login. It clears when the server or the sign-in changes.
  - **Tree search**: "7 matches" under the search box, and the matching part of each title is highlighted in amber. Parents shown only because a child matches are not highlighted.
  - **Split panes**: the Models tree and the Sync file list keep the width they were dragged to (saved in the app settings as `PaneWidths`, restored within the columns' minimum widths).
  - **Sync**: while it looks for solutions, the list says so ("Looking for solutions", with a line on what it looks for) instead of showing "Solutions found" over an empty list.
  - **Logs** (Sync and the wizards): every line starts with its time (HH:mm:ss), and a bar above the log shows the line count and a Copy button that copies every line with its time ("Copied" for two seconds).
- The unused moon and sun icons (from the old theme toggle) are removed.
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 32/32 and SCF.App.Tests 37/37 (7 new: one per item, with screenshots; the docs walkthrough now also checks the connected line against the docs database).
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the reference output.
- The style check finds 39: the 38 known, plus the new test class, which xUnit needs public like the other test classes.

Commit details

```
Polish the app: first-run steps, search matches, logs (SF-109)

Three reviewers graded the seven items in two rounds and signed off
(8.1 of 10). Home shows the three getting-started steps until a file
is opened. The tree search counts its matches and highlights them.
The code file tabs show when there are more past the edge, and the
wheel scrolls them. The Add wizard says where Load connected. The
split panes keep their width. Sync says it is looking while it scans
for solutions. Logs show each line's time and have a Copy button.

Bump version to 1.1.1.8.
```

---

## 1.1.1.7 — 2026-11-06

Changes
- SF-114 (user: "do a code review and cleanup"): a review of the 1.1.1.5 and 1.1.1.6 changes (app and website). Nine findings, all fixed:
  - Bug: the Add and Update wizards trimmed the SQL password, so a password that starts or ends with a space could not sign in. The password is now used as typed.
  - Bug: the search highlight broke its own markup when a later word matched inside an earlier highlight or an entity (typing "query ma" or "amp"). Every word is now highlighted in one pass over the plain text.
  - Bug: Copy on a code block dropped the blank lines (the browser's `innerText` collapses empty lines). It now copies one line per code line.
  - The code tag coloured keywords with about 25 regex patterns per line, more than the regex cache holds, so each was rebuilt on every docs request. One compiled pattern does it now; the rendered pages are byte-for-byte the same.
  - The heading id rule was in two places (the heading tag and the search index); the search index now uses the tag's.
  - The Sync page built the file list, and the first file's diff, twice after each run.
  - Small cleanups: one `Plural` helper instead of two, the diff gutter measures its text once per repaint instead of once per line, and a comment that still mentioned the light theme.
- SF-111 (part): the old website files are removed: the Bootstrap and jQuery bundles, the old CSS and scripts, the icon font, the gulp build (`gulpfile.js`, `gulpfile.config.js`, `package.json`), 59 unused images (clip art, PayPal, the trust seal, the old backgrounds), the `_Features` and `_Testimonials` partials and an old notes file with PayPal plan codes. Nothing referenced them; 88 files in all.
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 32/32 and SCF.App.Tests 30/30.
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the reference output.
- The style check finds 38, as before.
- All 23 docs pages, the home page and the search index were saved before the change and compared after it: identical apart from the cache-busting hashes.
- In the browser: the search highlight with "query ma" and "amp lt" has no broken markup, and Copy on a sample with blank lines copies all 13 lines, blank ones included.

Commit details

```
Review and clean up the app and website changes (SF-114, SF-111)

A code review of 1.1.1.5 and 1.1.1.6 found nine issues, all fixed.
The wizards no longer trim the SQL password. The docs search
highlight no longer breaks its own markup, and Copy keeps the blank
lines of a code sample. The code tag colours keywords with one
compiled regex instead of rebuilding 25 per line (the pages render
byte-for-byte the same), the heading id rule lives in one place, and
the Sync page builds its file list once per run.

The old website files nothing referenced are removed: the Bootstrap
and jQuery bundles, the old CSS and scripts, the gulp build, unused
images and partials (88 files).

Bump version to 1.1.1.7.
```

---

## 1.1.1.6 — 2026-11-05

**The website facelift, judged by three reviewers**

Changes
- SF-110 (user): the website redesigned dark-first with the whole team. Three independent reviewers graded 31 items (look, usability, comfort, appeal) over four rounds, and a designer wrote the direction and sketches. The mean went from 4.9 to 8.1 out of 10, and all three sign off on it for a first release.
  - **Stack**: Bootstrap 5, jQuery and the old plugins (owl carousel, parallax, cookie consent) are replaced by `site.v2.css`, `docs.v2.css` and `site.v2.js`, referenced directly with `asp-append-version`. No Node or gulp step. The icons are one SVG sprite (Fluent UI System Icons, and the GitHub mark from Octicons; `THIRD-PARTY-NOTICES.md`).
  - **Theme**: dark only (user: "we dont need light theme at all"). No toggle and nothing stored in the browser.
  - **Header and footer**: a 64 px sticky header with Docs, Quick start, Contact, search (Ctrl K or /), GitHub and Download. On phones it becomes a drawer. The footer has Product, Documentation and Project columns.
  - **Home** leads with the product:
    - a headline, a copyable `sharpfactory sync … --preview` line, Get started and GitHub, and the dark Sync diff from the app
    - an "open source, no account, no telemetry" strip
    - three how-it-works cards with app screenshots
    - eight features
    - a code sample in the real query syntax
    - instead of a gradient (user: it "looks AI"), the hero has a drive train: three meshed outline copies of the logo gear (18, 10 and 6 teeth cut at one tooth size, so they really mesh) at the right, behind the Sync screenshot, white at 20%, 15% and 11%; the headline side stays clean. The reviewers proposed it and the designer drew the gear and its geometry (`gear6`, `gear8`, `gear10`, `gear18` in the icon sprite)
  - **The logo gear at work** (user: "do it, skip the extras"), from the same proposals; a fourth review round graded these 7.5 to 9 and signed off:
    - the step numbers on How it works and Quick start sit in the logo gear, joined by a thin blue shaft (across the cards on desktop, between the stacked cards on phones); each gear turns one tooth once when its row scrolls into view
    - Copy buttons: the copy icon becomes a cog that turns a quarter, then lands as the green check ("Copied" for 2 s); code-block buttons keep one width
    - search shows a small turning cog with "Searching…" when the index takes more than 150 ms
    - the 404 page shows the logo gear with a tooth missing and the tooth lying on the floor beside it
    - with reduced motion nothing turns (Copy goes straight to the check)
  - **Docs**:
    - a sticky sidebar with the current page marked
    - breadcrumbs, a left-aligned title and a one-line lead per page (no owl)
    - an "On this page" rail that follows the scroll, built on the server from the headings (sections are now h2 and h3 with "#" anchors, keeping their ids)
    - code blocks with line numbers, Copy, and C# colours
    - prev/next across the sidebar order
    - "Edit this page on GitHub" once the repository is configured
    - search over a server-built index (`/docs-search.json`, 149 sections)
    - the screenshots retaken from the dark app (the wizard ones cropped to the dialog)
  - **Quick start**: five written steps, then a grid of video thumbnails. A click opens one player from youtube-nocookie.com.
  - **Contact**: GitHub Issues, Discussions and one email (user).
  - **404 and error pages**: search and real buttons.
  - **Privacy**: rewritten to match what the site does now.
  - **Removed** (user): Google Tag Manager, the cookie banner, the trust seal and the 2019 testimonial.
  - **Settings**: a new `Project:GitHubUrl` (and `Branch`) setting. Until it is set, GitHub links go to github.com and the edit link is hidden (SF-112).
- Bugs fixed:
  - Every YouTube video showed "Error 153": the site sent `Referrer-Policy: no-referrer`, and it is now `strict-origin-when-cross-origin`. The CSP drops PayPal and allows the video thumbnails and player.
  - Phones: the pages overflowed sideways and the menu never opened (Bootstrap 4 attributes on Bootstrap 5).
  - The docs sidebar painted over the footer.
  - On Windows, code blocks rendered as one comment line (they were split on `Environment.NewLine`), and C# keywords were coloured inside other words ("int" in "Print").
  - Typos in the Quick start videos.
  - "Privacy Policy" appeared twice in the footer.
- `VersionManager` (the old cache buster) is internal: nothing uses it.
- SF-113 (user): the app is dark only too ("the app only needs the dark theme as well"). The theme menu, the light palette and resources, the `Theme` setting and its two tests are gone; both code editors use the dark C# colours. Settings files that still have a `Theme` value load as before (the value is ignored).
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 32/32 and SCF.App.Tests 30/30 (the two theme tests went with the light theme).
- GenCheck: 1216 of 1216 identical to the saved baseline, and 1508 of 1508 identical to the reference output.
- The style check finds 38: the 37 known, plus `SearchController`, which must be public like every controller.
- Each page was checked at 1280 px and at 390 px in a true phone-width frame: no sideways overflow. Search and the docs drawer were checked in the browser.

Commit details

```
Redesign the website and make the app dark only (SF-110, SF-113)

Three reviewers graded the site over four rounds (4.9 -> 8.1 of 10)
and signed off. Bootstrap, jQuery and the plugins are replaced by
plain CSS and one script. The site is dark only, with a product-first
home page (a drive train of logo gears instead of a gradient; the logo
gear also marks the steps, turns in the Copy buttons and search, and
is missing a tooth on the 404 page) and docs with a sticky sidebar,
breadcrumbs, an On this page rail, prev/next, line-numbered code and
search. Tag Manager, the cookie banner and the trust seal are removed.
Fixes the YouTube error 153 (no-referrer policy), the phone layout and
menu, and code blocks rendered as one line on Windows.

The app is dark only as well: no theme menu, no light theme and no
Theme setting.

Bump version to 1.1.1.6.
```

---

## 1.1.1.5 — 2026-11-05

**The app's UI facelift, judged by three reviewers**

Changes
- SF-108 (user): the Avalonia app redesigned dark-first, over three rounds. Three independent reviewers graded 45 items (look, usability, comfort, appeal) and a designer sketched the larger screens. The mean went from 6.3 to 7.8 out of 10, and all three reviewers sign off on it for a first release.
  - **Theme**: Dark is the default for new installs (users who chose a theme keep it). The top bar's theme button opens Dark / Light / Match the system, which replaces the theme combo on the Settings page.
  - **Look**:
    - Secondary buttons are outlined, and primary buttons a deeper blue with white text.
    - Destructive actions are red: an outline on the Models page, filled in the delete dialog. The dialog has a warning icon, Cancel first, and a sentence saying what happens.
    - One outline icon set, from Fluent UI System Icons (MIT, `THIRD-PARTY-NOTICES.md`), with an icon per kind of object.
  - **Top and status bars**:
    - The model switcher sits next to the file name.
    - A dot marks unsaved changes.
    - The status bar shows short messages, with the full path in a tooltip.
  - **Models**:
    - Denser tree rows with kind icons, and no schema level when a category has one schema.
    - A splitter between the tree and the details.
    - A clear button in the search box.
    - A columns grid in Details (name, SQL type, .NET type, nullable, notes, PK).
    - Generated code in a wrapped editor with one row of file tabs.
  - **Sync**:
    - The target is a "Sync into" strip, and the chooser opens under it only when needed.
    - The review uses the full width: a summary bar with coloured counts, a filter with counts, a file search, and A/M/D chips.
    - The diff is shown in the C# editor, with old and new line numbers, a +/− gutter and tinted changed lines, and it opens at the first change.
    - Empty states say what to do.
  - **Wizards**:
    - Errors show under each field with a red border, and the validation messages are reworded (also in the command line).
    - The dialog sizes to its step and stays pinned near the top.
    - Both wizards use the steps Connect / Choose objects / Build.
    - The stored-procedure warning is an amber callout.
    - The progress step has a bar, a dense log and a green result line.
    - The Update object list marks new and removed objects and says what the update does.
  - **Pages**:
    - Home: recent files show the folder, the time and a remove button.
    - Settings: one column, with a table of the names it gives.
    - Layers: tables instead of cards.
    - About: one card with "Open folder" links.
    - Documentation: link rows.
- Tests: the headless tests follow the new dialogs, field errors and theme menu. They also screenshot the target chooser, a changed-file diff, the Sync log, and an Update that removes an object.
- Generated code does not change.

Verification
- SCF.sln builds with 0 errors. SCF.Tests 32/32 and SCF.App.Tests 32/32.
- GenCheck: 1216 of 1216 files identical to the saved baseline, and 1508 of 1508 identical to the reference output. SchemaCheck: 0 differences.
- The style check finds the same 37 known exceptions.

Commit details

```
Redesign the app UI, dark first (SF-108)

Three reviewers graded the app over three rounds (6.3 -> 7.8 of 10)
and signed off. Dark is the default theme; the theme button offers
Dark, Light or the system. Outlined secondary and red destructive
buttons, one Fluent outline icon set (MIT, THIRD-PARTY-NOTICES.md),
field-level wizard errors with reworded messages, a denser model tree
with kind icons, a columns grid in Details, and a Sync page with a
target strip, coloured counts and an editor diff with line numbers.
Settings, Layers, About, Documentation and Home were tidied to match.
Generated code is unchanged.

Bump version to 1.1.1.5.
```

---

## 1.1.1.4 — 2026-11-05

**No paths outside the repository**

Changes
- SF-107 (user): what gets pushed no longer names folders on the developer's machine, such as the test, database, sample, baseline and publish folders.
  - `RELEASE_NOTES.md`: 15 places, including the commit message of 1.0.9.9. They now say "a folder outside the repository", "a local database" or "the saved baseline", or give just the file name.
  - `BACKLOG.md` (7), `PLAN.md` (1), `TESTING.md` (4): the same. `TESTING.md` shows the Sync example as `<folder>/App.slnx`.
  - `.claude/launch.json` runs the website with `--project Websites/SCF.Web.CoreWebsite` instead of an absolute folder.
  - Left on purpose:
    - made-up Windows paths in two unit tests, which check path parsing;
    - the website's log folder settings, which are configuration and say where the site writes;
    - the old deployment tools (`FileTransferTools`, `LinuxScripts`), which still name the old server's user and folders and are open as SF-001 and SF-045.
- The facts recorded stay the same.

Verification
- A search of the repository's text files for drive-letter paths, `/home/` and `/mnt/` finds only the items left on purpose.
- The website starts from the updated `launch.json` and serves the docs (200).
- Documentation and a tool config only: no code changed.

Commit details

```
Remove paths outside the repository from notes and docs (SF-107)

Commit messages, release notes, the backlog, the plan and the
testing guide no longer name folders on the developer's machine;
they say a folder outside the repository or a local database
instead. launch.json runs the website with a repository-relative
project path. Made-up paths in unit tests, the website's log folder
settings and the old deployment tools (SF-001, SF-045) are left.

Bump version to 1.1.1.4.
```

---

## 1.1.1.3 — 2026-11-05

**The private reference solution is no longer named**

Changes
- SF-106 (user): the private reference solution that GenCheck compares against was named, with its folder path, in commit messages and notes. Both are gone from the repository:
  - `RELEASE_NOTES.md`: 18 places, including the commit messages of 1.0.9.2, 1.1.0.0 and 1.1.0.1. It is now "the reference solution", its model "the reference model", and its generated folder "the reference solution's `DataAccess` folder".
  - `BACKLOG.md`: the guiding principle and SF-027, SF-060 and SF-061.
  - `PLAN.md`: the GenCheck line.
- The facts stay the same: which checks ran and their results (1,508 of 1,508 files byte-identical, and so on).

Verification
- A case-insensitive search of the repository for the name and the path finds nothing.
- Documentation only: no code changed.

Commit details

```
Stop naming the private reference solution (SF-106)

The reference solution GenCheck compares against was named, with
its folder path, in commit messages and notes. RELEASE_NOTES.md,
BACKLOG.md and PLAN.md now call it the reference solution (and its
model the reference model); the checks and results they record are
unchanged.

Bump version to 1.1.1.3.
```

---

## 1.1.1.2 — 2026-11-05 (Sprint 4, change set 7, end of the sprint)

**A docs page for updating a model**

Changes
- SF-079: new page Getting Started → **Updating The Model** (`/Documentation/GettingStarted/UpdatingTheModel`). Until now only Quick Start video 11 covered it. The page covers:
  - when to update: the database changed, or objects should be added to or taken out of the model;
  - the Update wizard: Connection (the model's own connection, shown read-only), Choose objects (what the model has is selected; new objects to check, objects to uncheck, dropped objects disappear), Rebuild;
  - what is read again and what is kept (the connection and the layers);
  - then Save, Preview, Sync (only changed files written, files of removed objects deleted, your own files untouched) and building the solution, where the compiler shows the code that uses what changed.
- Two screenshots (`update_connection`, `update_objects`), rendered by `DocsScreenshotTests` against the Coverage-only database like the Model Generation images.
- The page is in the docs menu and the sitemap, linked from Model Generation's Model Details section, and listed on the app's Documentation page (22 links).

Verification
- Website run locally: the new page returns 200 with its 2 images (200), its title is "Documentation - Getting Started - Updating The Model", it is in the docs menu and the sitemap, and Model Generation links to it.
- Sprint 4 regression:
  - `SCF.sln` builds with 0 errors.
  - GenCheck: the reference solution 1,508 of 1,508 and the test baseline 1,216 of 1,216 byte-identical.
  - SchemaCheck on SharpFactoryTest: 0 differences.
  - `Tests/SCF.Tests` 32 of 32 (with the quoted-names database); `Tests/SCF.App.Tests` 32 of 32 (with the test model and the docs database).
  - Code rule checker: 37 findings, all justified (public test classes that xUnit needs, website controllers and tag helpers, and the public engine and model types the other projects use); the 2 new ones are the new test classes.

Commit details

```
Add the Updating The Model docs page (SF-079)

New Getting Started page: when to update a model, the Update
wizard's Connection, Choose objects and Rebuild steps, what is kept
and what is read again, then Save, Preview and Sync. Two screenshots
rendered by DocsScreenshotTests from the Coverage-only database. The
page is in the docs menu and sitemap, linked from Model Generation,
and listed on the app's Documentation page.

Closes Sprint 4: GenCheck byte-identical on both references,
SchemaCheck 0 differences, all tests pass.

Bump version to 1.1.1.2.
```

---

## 1.1.1.1 — 2026-11-05 (Sprint 4, change set 6)

**The Model Generation page shows the new app**

Changes
- SF-103: new screenshots of the Avalonia app on the website's Model Generation page: Common settings, Connect, Choose objects, Build, and two new ones of the Models page (a table's details, and its generated code).
  - They are rendered headless (no window on screen) by a new test, `DocsScreenshotTests`. It walks the Add wizard against a database holding only the public Coverage schema, connected as `localhost`, so no internal database, server or machine name appears. It runs when `SHARPFACTORY_DOCS_DB` is set, and saves the images when `SCF_SCREENSHOTS` names a folder (`TESTING.md`).
  - The Coverage schema's script is now in the repository (`Tests/SCF.Tests/Fixtures/Coverage.sql`, the source of `Coverage.edmj`). It was run in a new local database, `SharpFactoryDocs`.
  - The page text follows the new screens: field names and their order (Model name, Server, Enable integrated security, User and Password, Database with Load), where connection errors show, Create model, Select all / None and search, the Build step, the Models page.
  - Deleted the 7 old WPF images the page no longer uses.
  - Quick Start video 12, "Visual Studio Integration", was taken off the Quick Start page: that feature was removed in 1.1.0.3.
  - Not included: the Sync page. It lists the solutions open in the machine's IDEs and its recent solutions, which would put local paths in a public image.
- SF-073: the text about the .NET version of new projects now says the generated code targets .NET 10 and that existing projects keep their target framework (the old text offered a choice of versions). No screenshot shows the old app version any more.
- The test harness can now show the app's real version (it always showed "test"); the images show v1.1.1.1.

Verification
- `DocsScreenshotTests` passes against `localhost;SharpFactoryDocs` (28 objects read, model built, the Shipment table's details and 6 code files shown). Each image was checked: `localhost`, the Coverage schema and the app version only.
- Website run locally: the Model Generation page returns 200 and its 6 images return 200. The Quick Start page lists videos 1 to 11.
- `SCF.sln` builds with 0 errors.

Commit details

```
New app screenshots on the Model Generation page (SF-103, SF-073)

The page shows the Avalonia app: Common settings, Connect, Choose
objects, Build, and the Models page with a table's details and its
generated code. The images are rendered headless by
DocsScreenshotTests from a database holding only the public Coverage
schema (its script, Coverage.sql, is now in the repository), on
localhost, so no internal name appears. The text follows the new
screens and says the generated code targets .NET 10. The unused WPF
images and Quick Start video 12 (Visual Studio integration) are gone.

Bump version to 1.1.1.1.
```

---

## 1.1.1.0 — 2026-11-05 (Sprint 4, change set 5)

**No internal database names in the repository**

Changes
- SF-003: scanned the repository's code, docs, website, tests and notes for every table, schema, procedure, database and server name of the reference solution's model and of the test database (about 1,700 names; a script compared them with every word in the repository's text files).
  - Website code examples: they use Microsoft's public WideWorldImporters sample (`AppDb.Sales.Customer`, `GetCustomersByCity`), nothing internal. Tests use the synthetic Coverage schema and `TestDb`.
  - Scrubbed: the real table, column and procedure names in three earlier release notes (1.0.8.6, 1.0.8.7, 1.0.9.5) and in `BACKLOG.md` (SF-084), now described without naming them, and the name of one of the reference models.
  - Not changed here: the old app screenshots on the Model Generation page show an internal server name; they are replaced next (SF-073). The old deployment tools name the old server's host and Linux user; that is noted in SF-045 next to SF-001, both still open.

Verification
- The same scan after the change finds none of the domain names; the remaining matches are ordinary words (`Customer`, `Status`, `Version`, …) and the generic `AppDb`.
- Documentation and notes only: no code changed.

Commit details

```
Scrub internal database names from the notes (SF-003)

Scanned the repository for the reference model's and the test
database's table, schema, procedure, database and server names.
The website's examples use the public WideWorldImporters sample and
the tests the synthetic Coverage schema. Real table, column and
procedure names in three earlier release notes and one backlog item
are now described without naming them. The old screenshots (SF-073)
and the deployment tools (SF-001, SF-045) are handled separately.

Bump version to 1.1.1.0.
```

---

## 1.1.0.9 — 2026-11-05 (Sprint 4, change set 4)

**The schema reader passes names as parameters**

Changes
- SF-033: the reader's queries pasted the database, schema, table, routine and column names into the SQL text inside quotes. A name with an apostrophe (`O'Brien`, `Sales's`) made the query invalid, and characters outside the server's code page could be lost.
  - Every name is now a parameter (`@catalog`, `@schema`, `@column`, `@object`).
  - Lists of tables or routines are numbered parameters (`@n0`, `@n1`, …), split into batches of 2,000 only when a schema has more names than SQL Server's limit of 2,100 parameters per query.
  - The one place a routine is named in SQL text (describing its result with `SET FMTONLY`, then running it) now doubles `]` in the name, as SQL Server's `QUOTENAME` does. For names without `]` the text is the same as before.
  - The queries are otherwise unchanged (same columns, filters and order), and the default-value lookup passes the same `schema.table` text as before, so the results for existing databases do not change.
- New test database (optional, documented in `TESTING.md`): `Tests/SCF.Tests/Fixtures/QuotedNames.sql` creates a schema, table, column, default, procedure and function with apostrophes, plus a procedure with `]` in its name. It was run in a new local database `SharpFactoryQuote's`.

Verification
- `SCF.sln` builds with 0 errors.
- New test `QuotedNamesTests` (runs when `SHARPFACTORY_QUOTED_NAMES_DB` is set): `O'Brien` with its 3 columns, identity, primary key, navigation property and the default `O''Neil`; `Region`'s unique key; the foreign key; `Get]Orders` with its parameter and 2 result columns; `Count's` with its parameter and scalar return. Passed.
- SchemaCheck on SharpFactoryTest: 0 differences.
- Database parity tests: building and updating the test model through the app still gives a byte-identical `.edmj`.
- `Tests/SCF.Tests` 32 of 32; `Tests/SCF.App.Tests` 31 of 31.

Commit details

```
Pass names to the schema reader's queries as parameters (SF-033)

Database, schema, table, routine and column names travel as SQL
parameters instead of being pasted into quoted SQL text, so names
with apostrophes or characters outside the server's code page work.
Name lists are numbered parameters, batched past SQL Server's limit.
Routines named in SQL text get ']' doubled. The queries are
otherwise unchanged: SchemaCheck finds 0 differences and the model
built from the test database is byte-identical.

New fixture QuotedNames.sql and a test that reads such names.

Bump version to 1.1.0.9.
```

---

## 1.1.0.8 — 2026-11-05 (Sprint 4, change set 3)

**No more silently swallowed errors in the schema reader**

Changes
- SF-034: the two empty `catch` blocks in `SqlConnector` now record what they caught.
  - **A failed connection says why.** The reason (wrong password, unknown server, no answer within 2 seconds) is logged, and the new overload `ValidateConnectionInfo(conInfo, out reason)` returns it. The Add and Update wizards show it under "Failed to connect!", and SchemaCheck prints it.
  - **Running a procedure to find its columns is logged.** When SQL Server cannot describe a routine's result without running it (a temporary table, dynamic SQL), a warning names the routine before the fallback runs it with NULL parameters. That fallback is the safety concern in SF-023.
  - `SCF.Db` now logs through `SCF.Common`'s `LogManager`, which writes only when an app has enabled it. That brought the `SCF.Common` namespace into `SCF.Utils`, so `CodeGenUtils` now names the generator's `Common` class as `CodeGeneration.Common` (no change in behavior).
- Left as they are: the engine's best-effort IDE scans (commented, and they only mean fewer suggestions), the logger's own catch, and the SQL engine template `SCF.CodeGenerator/Database.cs`, which is copied into generated code.

Verification
- `SCF.sln` builds with 0 errors.
- `Tests/SCF.Tests`: 31 of 31, including two new tests: a connection to a port nothing listens on returns a reason; and against SharpFactoryTest, describing `Coverage.UsesTempTable` (it uses a temporary table) leaves a warning in the log naming it.
- GenCheck: the reference solution 1,508 of 1,508 and the test baseline 1,216 of 1,216 byte-identical. SchemaCheck: 0 differences.

Commit details

```
Log the errors the schema reader swallowed (SF-034)

A failed connection keeps SQL Server's reason: it is logged and
returned by a new ValidateConnectionInfo overload, so the wizards
and SchemaCheck can show why. Running a routine to discover its
result columns (when SQL Server cannot describe it) is logged as a
warning. SCF.Db now logs through SCF.Common.

Bump version to 1.1.0.8.
```

---

## 1.1.0.7 — 2026-11-05 (Sprint 4, change set 2)

**Small review leftovers**

Changes
- SF-089, the smaller findings of the 1.0.9.6 review:
  - Website: `SecurityHeadersMiddleware` returned a 500 for a request with an empty path (such as `OPTIONS *`), because it called `StartsWith` on a null path. It now treats a null path as "not merchant services".
  - Website: `Program` read `hosting.json` through a static `_currentDir` field set in `Main`; `ConfigureKestrel` now gets the content root from the Kestrel context.
  - SchemaCheck: lists that differed only in how many times an item repeats were reported as "same items, different order". The comparison moved to the engine (`ListComparison`) and counts each copy, so a column listed twice now shows as a difference.
  - `SqlConnector.ConnectionProbe`: removed null checks on fields that are always set.
  - The other two parts went away with the WPF and Visual Studio code in 1.1.0.3: the console code duplicated between two WPF popups, and the comment in `RegistryUtils`.

Verification
- `SCF.sln` builds with 0 errors.
- `Tests/SCF.Tests`: 29 of 29, including a new `ListComparison` test (equal lists, a different order, a repeated item, differences on both sides).
- SchemaCheck on SharpFactoryTest: 0 differences.
- Website over HTTP: it starts on the `hosting.json` port; the home and documentation pages return 200, unknown pages the custom 404, and the security headers are present; `OPTIONS *` returns 405 with no exception in the log.

Commit details

```
Fix the small review leftovers (SF-089)

The website no longer fails on a request with an empty path and reads
hosting.json from the content root instead of a static field.
SchemaCheck counts repeated items through the new
SCF.Engine.ListComparison, so a duplicated column is reported as a
difference instead of "a different order". ConnectionProbe loses
dead null checks.

Bump version to 1.1.0.7.
```

---

## 1.1.0.6 — 2026-11-05 (Sprint 4, change set 1)

**`Model.Clone()` works**

Changes
- SF-104: `Model.Clone()` threw "FKey with Id … already exists", because `FKey.Clone()` did not copy the foreign key's id: every clone had -1, and the model's foreign-key collection refuses duplicates. `FKey.Clone()` now copies the id, which navigation properties use to find their foreign key.
- Backlog: Sprint 4 started (SF-104, SF-089, SF-034, SF-033, SF-003, SF-103 + SF-073, SF-079).

Verification
- New test: the Coverage model and its clone save to identical `.edmj` files.
- `Tests/SCF.Tests`: 28 of 28 pass.
- Nothing in generation or the app calls `FKey.Clone()`, so generated code cannot change. The full regression runs at the end of the sprint.

Commit details

```
Fix Model.Clone: copy the foreign key id

FKey.Clone did not copy the id, so cloning a model with more than one
foreign key threw "FKey with Id -1 already exists". A model and its
clone now save to identical .edmj files.

Bump version to 1.1.0.6.
```

---

## 1.1.0.5 — 2026-11-05

**Fixes for the 1.1.0.3 code review (15 findings)**

SF-105. A full review of the 1.1.0.x code (engine, app, command line, tests, workflows) found 15 issues. All are fixed, each with a test.

Changes
- Sync engine:
  - A project file already on disk (for example from a Sync into another solution next to this one) is kept as it is, NuGet packages included, and only added to the solution. Before, it was replaced by the template. The report lists these as "existing projects added".
  - The dependencies folder may have nested folders (`src/Generated`): projects anywhere inside it are in place. Before, every Sync "moved" them and rewrote the solution.
  - When a project moves, references to it are fixed in every project of the solution, the user's own included ("references updated").
  - Project references inside XML comments are ignored, and new ones are never added next to a commented-out one.
  - The generator's static file writer is restored when a run ends, so a finished preview (with all its file contents) is not kept in memory.
- Open solutions: when an IDE's window title gives only a solution's name (Visual Studio opened from its start window, Rider), it is looked up in Visual Studio's and Rider's own recent-solution lists (`RecentSolutionsReader`). On macOS and Linux the process scan uses `ps -ww` (no cut command lines), and only IDE processes count (a `dotnet build App.sln` no longer looks open).
- App:
  - While a model build or Sync runs, the navigation, the model picker and the Sync target column are locked, and the window refuses to close with a message.
  - Picking another model keeps the Sync target, report and the solution name you typed.
  - On macOS, a double-clicked `.edmj` opens through the system's activation event, after the unsaved-changes question.
  - The "solution already exists" warning stays visible.
  - The Sync page no longer marks an older model file as changed.
  - The theme combo follows the toolbar toggle.
  - Opening a file refreshes the pages once instead of twice.
- Backlog: SF-105 done; new SF-104 (`Model.Clone()` throws on foreign keys; nothing calls it).

Verification
- `SCF.sln` builds with 0 errors.
- Tests: `SCF.Tests` 27 of 27 (7 new); `SCF.App.Tests` 31 of 31 with the database parity tests (7 new in `ReviewFixTests`).
- GenCheck: the reference solution 1,508 of 1,508 and the test baseline 1,216 of 1,216 byte-identical. SchemaCheck: 0 differences.
- Linux (WSL, self-contained CLI): `generate` gives 1,216 of 1,216 identical files; `sync` into a new `.slnx` adds 9 references on the first run and changes nothing on the second.
- Rule checker: no new findings.
- Not run: the macOS file-open event on a real Mac.

Commit details

```
Fix the 15 findings of the 1.1.0.3 code review

Sync keeps project files already on disk, handles nested
dependencies folders, fixes every reference to a moved project,
ignores commented-out references and restores the generator's file
writer. Open solutions are also found through Visual Studio's and
Rider's recent lists, and the Unix scan only counts IDEs.

The app locks navigation and closing during a run, keeps the Sync
target when another model is picked, opens .edmj files handed over
by macOS, keeps the "already exists" warning, no longer marks old
model files as changed, keeps the theme combo current and refreshes
the pages once per open.

Bump version to 1.1.0.5.
```

---

## 1.1.0.4 — 2026-11-05

**The website's documentation describes the cross-platform app and file-based Sync**

SF-101: the documentation still told users to run the WPF app, open Visual Studio and pick an instance to Sync into. Only the app and Sync descriptions changed; code samples and everything about the generated code are unchanged.

Changes
- `GettingStarted/Installation`:
  - download the GitHub release asset for your OS (Windows zip, macOS zip with `TheSharpFactory.app`, Linux tar.gz); the app is self-contained
  - run `TheSharpFactory.exe` (it was `SharpFactory.exe`); on macOS the app is unsigned, so the first time you right-click it and choose **Open**
  - a new section on the `sharpfactory` command line (`sync`, `generate`, `--preview`)
  - pinning the app on Windows, macOS and Linux
- `GettingStarted/ModelGeneration`:
  - the Add wizard starts from **New model file** on Home, or **Add model** on the Models page; it notes **Load** for the database list, the Kerberos note for integrated security on macOS/Linux, and **Done** after the progress log
  - the Models page with its **Generated code** tab replaces "Model Details"; Save is in the top bar
  - "Sync To Visual Studio Solution" is now "Sync To Your Solution": no IDE needs to be running; the target list (open in Visual Studio, Rider or VS Code, synced before, near the `.edmj`), **Browse…**, a new solution (`.slnx` by default), the .NET version of new projects, **Preview** with diffs, Sync with the log and report, and the command line
  - the six WPF screenshots of the old main screen (Add, Model Details, Save, Sync), the Visual Studio picker and the .NET version step are off the page (the image files are still in `wwwroot`, see SF-103)
- `GettingStarted/Introduction`: "solution" instead of "Visual Studio solution", and a new **Any OS, Any IDE** section.
- `DataAccess/Repositories`, `QueryComposition/NavigationProperties`: IntelliSense in Visual Studio, Rider or VS Code; "your solution".
- `_Features`: the **VS Integration** card is now **Any OS, Any IDE**.
- `_Layout`: the meta description no longer says "using the Visual Studio platform".
- Backlog: SF-101 is done. New: SF-103 (new screenshots of the Avalonia app for the wizard steps, delete the unused WPF images, and Quick Start video 12 "Visual Studio Integration").

Verification
- The website builds with 0 errors. Run locally (`website` on port 60080): Installation, Introduction, Model Generation, Repositories, Navigation Properties and the home page return 200 and show the new text; Installation, Model Generation and the home-page feature card were checked in screenshots.
- `grep -rn "Visual Studio"` in the website: only the new text (Visual Studio named next to Rider and VS Code) and the Quick Start video 12 title (SF-103).
- Button and label names were taken from the app's XAML (`WelcomePage`, `ModelsPage`, `AddModelWizard`, `SyncPage`); the app itself was not run.
- The console shows only the known cookieconsent 404 (SF-047).

Commit details

```
Update the website's documentation for the cross-platform app

Installation, Model Generation, Introduction, Repositories,
Navigation Properties, the features list and the meta description
described the WPF app and choosing a Visual Studio instance to Sync
into. They now describe the app for Windows, macOS and Linux, the
sharpfactory command line, file-based Sync (open, synced-before,
nearby, any or a new .sln/.slnx) and Preview with diffs. Outdated
WPF screenshots are off the Model Generation page.

Bump version to 1.1.0.4.
```

---

## 1.1.0.3 — 2026-11-05 (Sprint 3, change set 4)

**No more Visual Studio automation or WPF; CI and releases for every OS**

Last step of `PLAN.md`.

Changes
- SF-097: the WPF app (`WPF/SharpFactory`) is deleted and out of `SCF.sln`. The Avalonia app covers every row of the parity checklist (1.1.0.2).
- SF-010: the Visual Studio automation is gone: `SCF.Utils/VS` (`SolutionManager`, `VSShellUtils`, `VSVersionUtils`, `MessageFilter`), `VSInstallation`/`VSInstance`, and the `Microsoft.VisualStudio.Interop` package. `CodeFile` moved from `SCF.Models/VS` to `SCF.Models` (same namespace). `CodeGenUtils.FileWriter` defaults to a plain write-if-changed (CRLF); the engine's hosts set it for each run.
- SF-086: `RegistryUtils.cs` (unused Visual Studio registry code) removed with the rest.
- SF-098: no Windows-only code left in the app or the libraries; `SCF.sln` builds with no CA1416 warnings. Logs and settings are in each OS's app-data folder.
- SF-099: `.github/workflows/ci.yml` builds `SCF.sln` and runs both test projects on Windows, Linux and macOS. It also generates the public test model on each OS and fails if any generated file differs between them.
- SF-041: `.github/workflows/release.yml`: on a `v*` tag, the app and the command line are published self-contained, single-file and compressed for win-x64, win-arm64, linux-x64, osx-x64 and osx-arm64, then attached to the GitHub release:
  - Windows: zip
  - Linux: tar.gz
  - macOS: an unsigned `.app` from `App/SCF.App/Packaging/macOS/Info.plist` (it opens `.edmj` files)
- `README.md` for the repository; `TESTING.md` covers the tests, the command line and CI; `PLAN.md` marks every step done.
- Backlog:
  - SF-010, SF-086, SF-097 and SF-098 are done.
  - SF-041 and SF-099 are ready (their first run is on GitHub).
  - SF-056 is updated for the new code.
  - New: SF-101 (the website's documentation still describes the WPF app and the Visual Studio picker) and SF-102 (macOS signing: needs a paid Apple account).
  - The guiding principle on the paced progress console now says how the new app keeps the logs readable: they stay on screen until Done or the next run.
  - Sprint 3 is closed.

Verification
- `SCF.sln` builds with 0 errors and no Windows-only (CA1416) warnings.
- GenCheck: the reference solution 1,508 of 1,508 and the test baseline 1,216 of 1,216 byte-identical. SchemaCheck: 0 differences.
- Tests: `SCF.Tests` 20 of 20; `SCF.App.Tests` 24 of 24 with the database parity tests (22 + 2 skipped without the test database).
- Publishing: the app and the command line publish self-contained for all five targets; compression takes the Windows app from 122 MB to 54 MB. In WSL, the macOS bundle step (version in `Info.plist`, executable bit) and the Linux tarball were checked with real builds.
- Not run: the workflows themselves (they need the GitHub repository, SF-007), and the app on a real macOS or Linux desktop.

Commit details

```
Remove the WPF app and the Visual Studio automation; add CI and releases

The Avalonia app replaces the WPF app, and Sync no longer needs
Visual Studio: SCF.Utils/VS, the VS installation models, the interop
package and RegistryUtils are gone, and no Windows-only code is left.

CI builds and tests on Windows, Linux and macOS and checks that all
three generate the same bytes. The release workflow publishes the
app and the command line for win-x64, win-arm64, linux-x64, osx-x64
and osx-arm64 on each version tag. New README.md.

Bump version to 1.1.0.3.
```

---

## 1.1.0.2 — 2026-11-05 (Sprint 3, change set 3)

**The Avalonia app**

SF-095: `App/SCF.App`, a new Avalonia 12 app (`TheSharpFactory`) for Windows, macOS and Linux. It uses the Fluent theme, CommunityToolkit.Mvvm and AvaloniaEdit. It covers every row of the parity checklist in `PLAN.md`.

Changes
- **Window:** a navigation rail (Home, Models, Layers, Settings, Sync, Documentation, About), a top bar with the model picker and New/Open/Save/Save as, a theme toggle, and a status bar with the version. Wizards and messages open over the page. Closing or opening another file with unsaved changes asks Save / Don't save / Cancel, and the title shows a marker while changes are unsaved.
- **Home:** new model file, open, recent files.
- **Models:** the model's counts and connection; a tree of tables, views, procedures, functions and sequences down to columns and parameters, with search; the read-only details the old screen showed, field for field; and a new **Generated code** tab that shows the selected object's generated files with C# highlighting (built in memory, nothing written). Add, Update and Delete (with confirmation).
- **Add wizard:** common settings (only for a new file) → connect (model name, server, integrated security or user/password, Load databases; a note about Kerberos on macOS/Linux) → choose objects (tri-state check boxes, search, select all/none, counts) → build with a progress log. The old rules and messages are kept. The progress log stays until you click Done.
- **Update wizard:** the connection summary → the objects with the model's current selection → rebuild.
- **Layers:** project name and namespace of the 6 model layers and 5 common layers, with the full names they produce.
- **Settings:** root namespace, use it in project names, dependencies folder, indent size (validated), a live preview of the resulting names, and the theme (System/Light/Dark).
- **Sync:**
  - targets: solutions open in an IDE, synced before, or near the model; Browse; or a new `.slnx`/`.sln` (folder, name, format)
  - the .NET version
  - Preview, with the list of added/changed/deleted files and a diff of each
  - Sync, with the log and a report
  - a model marked as synced keeps Sync turned off, as before
- **Documentation:** the 21 links; **About:** product, edition, version, plus where the logs and settings are.
- Errors are logged to the per-user log folder and shown in a message; the app keeps running.
- SF-096: `Tests/SCF.App.Tests` (24 headless tests: Avalonia.Headless with Skia, no window on screen, nothing driven on the desktop). Each test covers rows of the checklist. `DatabaseParityTests` builds the test model through the new Add wizard against SharpFactoryTest, and updates it through the Update wizard, then compares the saved `.edmj` with the one the WPF app built; they run when `SHARPFACTORY_TEST_EDMJ` names that file.
- Fixed while testing: refreshing the Sync target list during the automatic scan returned an empty list; the Add wizard saved an empty user and password with integrated security (the WPF-built file has none).
- `SolutionProjectHost` is internal (only the engine uses it).

Verification
- `SCF.sln` builds with 0 errors and no warnings in the new projects.
- `Tests/SCF.App.Tests`: 24 of 24 pass, including both database parity tests: the `.edmj` built and updated through the new UI is identical to the one the WPF app built from the same database.
- `Tests/SCF.Tests`: 20 of 20 pass.
- Rule checker: no new findings except the test classes, which xUnit and Avalonia need to be public (like the website's controllers), and the engine's result types, which its public methods return.
- Screenshots of every page and wizard step, rendered headless, were reviewed for layout in light and dark themes.
- Not done yet: the WPF app and the Visual Studio code are still in the repo (next change set). Not run: the app on a real macOS or Linux desktop.

Commit details

```
Add the Avalonia app

App/SCF.App replaces the WPF screens on Windows, macOS and Linux:
Home, Models (tree, details, generated-code preview), Layers,
Settings, Sync (target list, preview with diffs), Documentation and
About, with Add and Update wizards. It covers every row of the
parity checklist in PLAN.md.

Tests/SCF.App.Tests runs the app headless (24 tests). The database
parity tests build and update the test model through the new UI and
match the .edmj the WPF app built.

Bump version to 1.1.0.2.
```

---

## 1.1.0.1 — 2026-11-05 (Sprint 3, change set 2)

**Sync without Visual Studio, the target list, and the command line**

Changes
- SF-093: `SCF.Engine.Sync.SolutionSync` syncs a model container into a `.sln` or `.slnx`:
  - Missing projects are created in the dependencies solution folder with the `.csproj` template (`EnforceCodeStyleInBuild` false), as Sync did before.
  - When the dependencies folder changes, projects move (folder and solution entry) and references to them are fixed. Sync refuses to move a project into a folder that already has files, where the Visual Studio Sync deleted that folder.
  - Project references follow the same rules and order as before (`ProjectReferenceRules`); `ProjectFileEditor` adds or fixes them and leaves the rest of the project file as it is.
  - Only files whose content changed are written, always with CRLF.
  - Stale files: generated files the model no longer produces are deleted, only when their first line is the Sharp Factory marker, never in a project's root, `bin`, `obj`, `Properties` or `node_modules`.
  - `.sln` and `.slnx` through `Microsoft.VisualStudio.SolutionPersistence`; an existing `.sln` keeps its BOM, CRLF and every other line; new solutions get Debug/Release on Any CPU.
  - Preview: the same run without writing anything (no folders either), with the old and new content of each changed file for diffs.
- SF-094: `TargetSolutionFinder` and `RunningIdeScanner` build Sync's target list: solutions open in Visual Studio, Rider or VS Code (from their command lines, and window titles on Windows), the ones this `.edmj` was synced to, and the ones near the `.edmj`.
- `AppSettingsStore`: per-user settings (recent files, Sync targets per `.edmj`, theme) in each OS's application-data folder, outside the `.edmj`. `SHARPFACTORY_HOME` replaces the folder, for tests.
- SF-013: the command line, `App/SCF.Cli` (`sharpfactory`): `sync <model.edmj> <solution> [--preview]` and `generate <model.edmj> <folder>`.
- SF-100: on Linux, six Query folders came out as one folder with a backslash in its name (`Query\Builders`). `IOUtils.CreateSubFolder` now uses the OS separator (no change on Windows).
- New `Tests/SCF.Tests` (xUnit v3, 20 tests): Sync into new and existing `.sln`/`.slnx`, re-sync, preview, stale files, moving projects, the reference editor, validation, the target list and settings. They run in temporary folders outside the repo, on `Tests/SCF.Tests/Fixtures/Coverage.edmj`: the Coverage schema of the test database (synthetic names only), without the table named like its schema.
- The Release build of `SolutionManager.cs` failed on a misindented `#else` line; fixed.

Verification
- `SCF.sln` builds with 0 errors; 20 of 20 tests pass.
- GenCheck: the reference solution 1,508 of 1,508 and the test baseline 1,216 of 1,216 byte-identical.
- Sync of the test model into a new `SyncTest.slnx` outside the repository with the CLI: 5 projects in the `DataAccess` solution folder, 9 references, 1,216 files, all byte-identical to the baseline. With the NuGet packages a user adds by hand (`Microsoft.Data.SqlClient`, `Microsoft.SqlServer.Types`), the solution builds except the known SF-083 table. A second Sync changed nothing: 1,216 unchanged, the project files (with the hand-added packages) and the `.slnx` byte-identical.
- **Linux** (Ubuntu 24.04 in WSL, self-contained `linux-x64` build of the CLI, nothing installed in WSL):
  - `generate`: the test model gives 1,216 of 1,216 files and the reference solution 1,508 of 1,508, byte-identical to the Windows references.
  - `sync` into a new `.slnx` and a new `.sln`: the same files, references on the first run, nothing to do on the second; `.slnx` uses `/`, `.sln` the usual `\`.
  - The first Linux run found SF-100 and a Sync bug (project names read from `\` paths), both fixed and re-checked.

Commit details

```
File-based Sync, the Sync target list and the command line

SCF.Engine.Sync syncs a model container into a .sln or .slnx without
Visual Studio: creates and moves projects, adds project references
by the same rules, writes only changed files (CRLF), removes stale
Sharp Factory files, and offers a preview that writes nothing. The
target list suggests solutions open in an IDE, synced before, or
near the .edmj. Per-user settings live in the OS app-data folder.

New sharpfactory command line (sync, generate) and Tests/SCF.Tests
(20 tests). Folders named with a backslash now work on Linux.

Verified: GenCheck byte-identical (reference 1,508, test 1,216); the
CLI's Sync into a new .slnx matches the baseline and a second Sync
changes nothing; on Linux (WSL) generate and sync give the same
bytes.

Bump version to 1.1.0.1.
```

---

## 1.1.0.0 — 2026-11-05 (Sprint 3, change set 1)

**The engine: one place for the logic the screens held**

First step of `PLAN.md` (Avalonia UI, file-based Sync, Windows/macOS/Linux).

Changes
- `PLAN.md`: the plan, the architecture and the parity checklist of every field and action of the WPF app.
- SF-091: new `PackagedDependencies/SCF.Engine` library:
  - `ModelBuilder`: creating a model container, reading a database's objects, building and updating a model (moved from the WPF model screen, same progress messages).
  - `ModelValidation`: the model name, connection and common-settings rules (moved from the WPF popups).
  - `GenerationRunner`: the one generation order, used by both Sync and GenCheck through an `IProjectHost` (where projects and files go). Before, GenCheck carried its own copy of Sync's order.
- GenCheck generates through `GenerationRunner` with a `FolderProjectHost`.
- The WPF app uses the engine: Add/Update build models with `ModelBuilder`, and its Visual Studio Sync runs `GenerationRunner` with a temporary `VisualStudioProjectHost` (removed with the WPF app later in this plan).
- SF-092: generated files always use CRLF (`IOUtils.ToCrlf` where files are written), so macOS and Linux produce the same bytes as Windows.
- `CodeGenUtils.Reset` no longer reads the C# 7/10 switches from Visual Studio: they are always on for .NET 10 (SF-059, partly).
- `IOUtils`: the schema reader's error logs go to `IOUtils.LogFolder` (set by the app; defaults to the app folder through `AppContext.BaseDirectory`, which also works for single-file apps), and `IOUtils.CreateFolders` lets a Sync preview compute paths without creating folders.
- Backlog: Sprint 3 started; new items SF-093 to SF-099.

Verification
- Neither baseline has a lone LF or CR (1,216 and 1,508 files), so enforcing CRLF changes nothing on Windows.
- GenCheck through the new runner: the reference solution 1,508 of 1,508 and the test baseline 1,216 of 1,216 byte-identical.
- SchemaCheck on SharpFactoryTest: 0 differences.
- `SCF.sln` builds with 0 errors.
- Not run: the WPF app's UI (not launched without asking). Its Add/Update and Sync now call the engine with the same steps.

Commit details

```
Add SCF.Engine: model building, validation and one generation order

The logic the WPF screens held moves into a new SCF.Engine library:
ModelBuilder (create a container, read a database, build and update
a model), ModelValidation (model name, connection and settings
rules) and GenerationRunner, the one generation order that Sync and
GenCheck now share through an IProjectHost.

Generated files always use CRLF, so macOS and Linux produce the same
bytes as Windows. CodeGenUtils.Reset no longer asks Visual Studio
for the C# 7/10 switches. GenCheck output is byte-identical (reference
1,508 files, test baseline 1,216 files); SchemaCheck finds 0
differences.

Add PLAN.md with the Avalonia plan and the parity checklist.

Bump version to 1.1.0.0.
```

---

## 1.0.9.9 — 2026-11-05

**No generated code in the repository**

The repository goes to GitHub, so it doesn't carry code generated by test runs, and generated code is only built and tested outside it (user, 2026-11-05).

Changes
- SF-048: the old generated sample `DataAccess/` (5 projects) and its model `DbModels/ModelContainer.edmj` moved to a folder outside the repository. The copy was checked file by file before the repo copies were deleted: 253 files, identical, build output left out. Nothing in the code referenced them.
- `SCF.sln`: the 5 `DataAccess` projects and the `DbModels` solution folder are gone. The file keeps its BOM and CRLF.
- `TESTING.md` and the guiding principles in `BACKLOG.md`: generated code is never built or tested inside the repo, and the repo holds none. SF-003 and SF-060 updated for the new location.

Verification
- `SCF.sln` as a whole builds with **0 errors**. Before, it failed on CA1822 in the generated sample, an error that only this repo's `.editorconfig` produced.
- The moved sample builds in its new folder with 0 errors (one known warning, SYSLIB0051, SF-052).
- The repo scan found no other test output: no models, SQL, logs, database files or archives. Build output (`bin/`, `obj/`, `.vs/`) is already in `.gitignore`.
- A backup of both folders and the old `SCF.sln` is in the session scratchpad (`SF_backup_before_1.0.9.9.zip`).

Commit details

```
Move the generated DataAccess sample out of the repository

The repository goes to GitHub without code generated by test runs,
and generated code is built and tested only outside it. The old
DataAccess sample and DbModels/ModelContainer.edmj now live outside
the repository; their 5 projects and the DbModels folder
are removed from SCF.sln, which now builds with 0 errors.

Bump version to 1.0.9.9.
```

---

## 1.0.9.8 — 2026-11-05

**Generated projects don't fail the build on code-style rules**

Changes
- SF-090: the project files Sync creates now set `<EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>`, so a solution's own code-style rules can't break the build of the generated code (user, 2026-11-05). This changes only the `.csproj` Sync writes for a new project. The generated `.cs` files are unchanged, and Sync doesn't rewrite the project files of projects that already exist.
- The `DataAccess/` sample projects, also Sync output, set it too: three had it `true`, two didn't set it.
- `BACKLOG.md`: the guiding principle "generator output does not change" lists this as a user-approved exception. `TESTING.md`: the compile instructions include the setting.

Verification
- The app and SCF.GenCheck build with 0 errors. GenCheck doesn't write project files, so its output is unaffected.
- `SCF.sln` still has one error: CA1822 in the generated `DataAccess/SCF.Query` sample. That is a code-analysis rule, not a code-style rule, and this repo's `.editorconfig` raises it to an error (`dotnet_diagnostic.CA1822.severity = error`), so the new setting doesn't cover it (SF-036, dropped). Confirmed: a copy of `SCF.Entity` and `SCF.Query` built outside the repository has 0 errors and one warning (SYSLIB0051, SF-052). The error only exists inside this repo.
- Not run: a Sync into a scratch solution to see the new project file. That is part of the Sync test the user is doing.

Commit details

```
Generated projects: EnforceCodeStyleInBuild false

The project file Sync creates for a new generated project sets
EnforceCodeStyleInBuild to false, so code-style rules in the user's
solution can't fail the build of generated code. The DataAccess
sample projects match. Generated .cs files are unchanged.

Bump version to 1.0.9.8.
```

---

## 1.0.9.7 — 2026-11-05

**Fixes from the code review of 1.0.9.6**

A code review of the 1.0.9.6 change set found the refactor itself faithful, and found four bugs in older code inside the touched functions (SF-088). The smaller findings are in SF-089.

Changes
- Website, not-found page: when an action returned `NotFound()` (for example the download page with no release link configured), the response was an empty 404. The re-execute kept the endpoint already chosen for the original path, so routing skipped matching and ran the same action again. `CustomNotFoundPageMiddleware` now clears the endpoint and route values before re-executing, so these requests get the custom page, still with status 404.
- App, Sync screen: listing the open Visual Studio instances crashed (`KeyNotFoundException`, and the app closed) when a running `devenv` was not in vswhere's list, such as a Preview edition, or when its path could not be read. Those instances are now skipped, and the path lookup ignores case. vswhere now runs with `-prerelease -utf8` (UTF-8 output keeps non-ASCII paths intact), and the process is waited for and disposed.
- App, Add and Update Model: an error while reading the database (or a model no longer loaded) left the progress screen open forever with no message. The error is now logged, shown in a message box, and the progress screen closes. On a failed Update the model is not changed.
- App, error handling: the UI error handler logged the exception but left it unhandled, so WPF closed the app and unsaved model changes were lost. It now marks the error handled and shows a message pointing to the error log.
- Backlog: SF-088 done; SF-089 added for the smaller findings.

Verification
- Builds: the app, the website, SCF.GenCheck and SCF.SchemaCheck build with 0 errors and no warnings. (`SCF.sln` as a whole still fails on the generated `DataAccess/SCF.Query` sample, CA1822, SF-036 dropped. It's not part of the checked builds.)
- Website, over HTTP:
  - With the test release link set: all 32 responses (per-request GUIDs masked) are identical to the 1.0.9.6 snapshot.
  - Without it: only the download page changes. It now returns the custom not-found page (404, 8,405 bytes) instead of an empty 404.
- vswhere with the new arguments returns valid JSON with both installed versions (Visual Studio 2026 and 2022).
- Not run: the app UI (not launched on the user's laptop without asking). The changed paths are the error paths of Add/Update and the Sync instance list. Generator, schema reader and `.edmj` code are untouched, so GenCheck, SchemaCheck and the round trip were not re-run.

Commit details

```
Fix four bugs found in the 1.0.9.6 code review

- Website: the 404 re-execute clears the endpoint and route values,
  so actions returning NotFound() get the custom page instead of an
  empty 404.
- Sync: skip Visual Studio instances vswhere does not list instead of
  crashing; case-insensitive path lookup; vswhere -prerelease -utf8.
- Add/Update Model: catch, log and show errors, then close the
  progress screen.
- App: the UI error handler marks errors handled and shows a message
  instead of letting the app close.

Bump version to 1.0.9.7.
```

---

## 1.0.9.6 — 2026-11-05

**C# rules applied to all of The Sharp Factory's own source**

The user's C# rules now apply everywhere in the generator, the app, the website and the tools. Generated code is not affected: its output does not change. The changes touch 168 `.cs` files and 17 `.xaml` files, and add 2 new files.

Changes
- Classes: every class is `sealed`, `abstract` or `static`. Classes used only inside their own project are `internal`: the whole app, the website's helpers and view models, the tools, and the library classes no other project uses. WPF windows and controls that became internal got `x:ClassModifier="internal"` in their XAML. Methods that were `internal` or `protected` (not overrides) in internal classes are now `public`.
- No lambdas, anonymous delegates, local functions or LINQ query syntax remain:
  - Schema reader (`StoreUtils`, `DbModelUtils`): the GroupBy/ToDictionary chains are plain loops that build the same dictionaries in the same order. The sorts keep `OrderBy`/`ThenBy` (stable, default comparer) with named key methods, so ordering is unchanged.
  - `SqlConnector.ValidateConnectionInfo`: the thread body moved into a small `ConnectionProbe` class.
  - `JsonUtils`: the deserialization error handler is a named method (`JsonErrorCollector`), and the writable-property filter is a loop.
  - App: background work and dispatcher calls are named methods, using `Task.Factory.StartNew(method, state)` and `Dispatcher.BeginInvoke(new Action<...>(method), args)`. `StartSTATask` uses a small runner class.
  - Website: the two inline middlewares are now `SecurityHeadersMiddleware` and `CustomNotFoundPageMiddleware`, added with `UseMiddleware`. The Kestrel, cookie policy, Razor and JSON setup in `Program` are named methods.
  - Tools: SchemaCheck rewritten without lambdas or its switch expression. The output is unchanged.
- `var` everywhere. `foreach` over the non-generic `DataRowCollection` and Visual Studio COM collections uses `.Cast<T>()`. Declarations that start as null use `var x = (T)null;`, as the code already did elsewhere.
- Naming: `VSInstallation`'s properties are PascalCase. vswhere's camelCase JSON still maps onto them, because Newtonsoft matches property names case-insensitively, so their naming suppressions were removed. `RegistryUtils` constants are `_camelCase` and its P/Invoke parameters camelCase.
- Exceptions the compiler or framework requires, which stay public:
  - 8 library classes exposed through public members that other projects use (`ModelCollection`, `SchemaCollection`, `TypeMappingInfo`, `VSProperties`, `VSCatalog`, and the 3 declaration base classes).
  - The website's controllers and tag helpers, because ASP.NET Core only discovers public ones. Their base-class members that hand out internal view models are `private protected`.
- 13 files that earlier shell edits had turned into LF are back to CRLF.
- `.claude/launch.json` added: preview configurations to run the website locally.
- Backlog: SF-085 done. New items SF-086 (`RegistryUtils` is unused: remove it?) and SF-087 (add the rule checker to `Tools/`?). The rules are listed in the guiding principles.

Verification
- A Roslyn rule checker (in the scratchpad) found 571 findings before and 23 after. The 23 are exactly the exceptions above.
- Builds: the app, the website, SCF.GenCheck, SCF.SchemaCheck and WinSCPClient build with 0 errors and no new warnings.
- Generated code: the reference solution 1,508 of 1,508 files byte-identical; test database baseline 1,216 of 1,216 files byte-identical.
- Schema reader: SchemaCheck on SharpFactoryTest finds 0 differences.
- `.edmj` save: loading and saving the reference model and the test model gives byte-identical files.
- Website: all 32 responses (status, selected headers, body; per-request GUIDs masked) are identical to the previous snapshot, including the 404 page, the security headers and the download redirect.
- App, driven with UI Automation once the user had freed the laptop:
  - It starts and shows v1.0.9.6. The Model Details, Documentation and About screens open.
  - **Add Model** against SharpFactoryTest with every object selected: Common Settings, connection, database list, object tree (16 schemas, 185 tables, 2 views, 22 procedures, 4 functions, 2 sequences), then the progress console. It finished in 40 s, and the model shows 185 / 2 / 22 / 4 objects.
  - **Update Model**: the connection summary, object tree and progress console work. It finished in 22 s with the same counts.
  - **Sync**: the Visual Studio instance list shows the open instance by name and version, read from vswhere through the renamed `VSInstallation` properties.
  - No error logs were written.
- Not verified: a full Visual Studio Sync. The only open instance was the user's own solution, so the test stopped at the instance list.

Commit details

```
Apply the C# rules to The Sharp Factory's own source

Every class is sealed, abstract or static; classes used only in their
project are internal, with public or private methods. No lambdas,
anonymous delegates, local functions, LINQ query syntax or switch
expressions remain: loops, named methods and small helper classes
replace them. var everywhere, PascalCase properties, _camelCase
private fields.

Generated output, the schema reader, .edmj saving and the website's
responses are unchanged (GenCheck, SchemaCheck, round-trip and
snapshot checks).

Bump version to 1.0.9.6.
```

---

## 1.0.9.5 — 2026-11-05

**Fix: generated code for stored procedures without parameters that return rows**

Changes
- SF-084: `StoredProcsGenerator` generated code that did not compile for a stored procedure with no parameters that returns rows. It passed `materializer` (the local function is `materialize`), wrote `new StoredProcResult<X>;` before the initializer, ended `Result = ...` with `;`, and closed with a bare `}`, which also threw off the indentation of the rest of the file. That branch now writes the same code as the with-parameters branch, except it passes `null` for the parameters. The SQL is unchanged.
- SF-083 (table named the same as its schema) dropped at the user's call: naming a table like its schema is bad practice, so it won't be fixed.
- The guiding principles in `BACKLOG.md` note the one approved exception to "generator output does not change": output that does not compile today. `TESTING.md` updated.

Verification
- Reference solution: 1,508 of 1,508 files byte-identical (exit code 0). Its model has no procedures of this kind, so nothing changed.
- Test database: 1,214 of 1,216 files identical to the baseline. The 2 that differ are the stored procedure repositories of the two schemas with parameterless procedures that return rows (`Coverage` and one schema of the copied database). The saved baseline was updated with those 2 files.
- The new output compiles: all 5 generated projects build with 0 errors on .NET 10. The only change was one line patched in a scratch copy to get past the known SF-083 table.
- The app builds with 0 errors.

Backlog: SF-084 done, SF-083 dropped.

Commit details

```
Fix generated code for parameterless stored procedures with results

The no-parameters branch of StoredProcsGenerator passed the wrong
local function name (materializer), wrote "new StoredProcResult<X>;"
before its initializer and closed it with a bare brace, so the
generated repository did not compile. It now writes the same code as
the with-parameters branch. Output changes only for those procedures.

Drop SF-083 (table named like its schema): won't fix.

Bump version to 1.0.9.5.
```

---

## 1.0.9.4 — 2026-11-05 (Sprint 2, change set 2)

**Permanent test database, schema regression tool, TESTING.md**

Changes
- SF-081: new permanent test database `SharpFactoryTest` on the local SQL Server (set up outside the repo):
  - structure-only copy of AppDbTest (170 tables, 17 procedures, keys and foreign keys), made with DacFx (structure export `SharpFactoryTestSchema.dacpac`);
  - plus a Coverage schema (`SharpFactoryTest.Coverage.sql`, in the repository as `Tests/SCF.Tests/Fixtures/Coverage.sql` since 1.1.1.1): every SQL Server column type, identity / computed / rowversion columns and defaults (one `varbinary` default over 4,000 characters), composite primary and unique keys, multi-column / self-referencing / one-to-one foreign keys, two foreign keys to one table, names that need cleaning, a table without a primary key, a trigger, 2 views, 4 functions (scalar, inline table, multi-statement table), 2 sequences, 5 stored procedures (with and without parameters and result sets, `OUTPUT` parameter, temp table);
  - test model `SharpFactoryTest.edmj` built with the app's own reader (185 tables, 2 views, 22 procedures, 4 functions, 2 sequences, 177 foreign keys) and generated baseline code (1,216 files), with compile-check projects.
- New tool `Tools/SCF.SchemaCheck` (in `SCF.sln`): `SCF.SchemaCheck <model.edmj> [model name ...]` re-reads each model's selected objects from its database the way the app's Update does and compares them with the stored model (columns, keys, navigation properties, foreign keys, procedure parameters and results, functions, sequences). Exit code 0 only when everything matches.
- New `TESTING.md`: the regression routine (build, GenCheck against a reference solution and against the test database baseline, SchemaCheck against the test database), what a good test database contains, and how to compile generated code. No private project, database or table names.

Verification
- SchemaCheck on the test model: "0 differences", exit code 0. On the reference AppDb model against AppDbTest: the same 5 known database differences, exit code 1. On a model whose database does not exist: "cannot connect", exit code 1.
- Compiling the generated baseline found two generator edge cases that produce code which does not compile: a table named the same as its schema (SF-083) and stored procedures without parameters that return rows (SF-084). With those objects left out (in a scratch copy) the rest compiles with 0 errors on .NET 10.
- The app, the website, SCF.GenCheck and SCF.SchemaCheck build with 0 errors. The reference solution still regenerates 1,508 of 1,508 byte-identical (checked in 1.0.9.3; nothing in the generator changed since).
- No private names in `TESTING.md` or the tools.

Backlog: SF-081 done. Sprint 2 closed (1.0.9.3 – 1.0.9.4).

Commit details

```
Add SCF.SchemaCheck and TESTING.md

New Tools/SCF.SchemaCheck console app: re-reads each model's selected
objects from its database the way the app's Update does and compares
them with the stored .edmj (columns, keys, navigation properties,
foreign keys, procedures, functions, sequences); exit code 0 only
when everything matches.

TESTING.md documents the regression routine: build, SCF.GenCheck
against a reference solution and a test database baseline, and
SCF.SchemaCheck against the test database.

Bump version to 1.0.9.4.
```

---

## 1.0.9.3 — 2026-11-05 (Sprint 2, change set 1)

**Schema reader: no crash on tables with cleaned names and identity/computed columns**

Found by the new test database (`SharpFactoryTest`, Coverage schema): creating a model crashed with `KeyNotFoundException: 'ORDER LINE'`.

Changes
- SF-082: `SCF.Utils/ModelUtils.ApplyIdentityComputedValues` looked the entity up by the singularized *raw* table name, but entities are named from the *cleaned* name (spaces and symbols replaced), so any table like `[Order Line]` with an identity or computed column crashed model creation. It also looked the column up by store name through the property-name index. It now uses `Utils.GenerateEntityName(tableName)` (the same function that names the entity) and `GetByStoreName` for the column. Every lookup that worked before resolves to the same entity and column.

Verification
- The full SharpFactoryTest model (185 tables, 2 views, 22 procedures, 4 functions, 2 sequences, 177 foreign keys) now reads without errors.
- Reference solution regenerated with `SCF.GenCheck`: 1,508 of 1,508 byte-identical.
- Re-reading AppDb from AppDbTest: same result as the baseline, and the saved model file is identical to the one from before the fix.
- `dotnet build` of the WPF app: 0 errors.

Backlog: SF-082 done. Added SF-083 and SF-084 (generator output that does not compile for two edge cases; waiting on the user's decision).

Commit details

```
Fix model creation crash for cleaned table names with identity columns

ApplyIdentityComputedValues looked entities up by the raw table name
while entities are named from the cleaned name, so a table such as
[Order Line] with an identity or computed column crashed model
creation. Look the entity up with GenerateEntityName and the column by
store name. Existing models and generated output are unchanged.

Bump version to 1.0.9.3.
```

---

## 1.0.9.2 — 2026-11-05 (Sprint 1, change set 5)

**Generator regression check in the repo: `Tools/SCF.GenCheck`**

Changes
- SF-058: new console tool `Tools/SCF.GenCheck` (added to `SCF.sln` under a "Tools" solution folder):
  `SCF.GenCheck <model.edmj> <output folder> [reference folder]`
  It generates every file for the model without Visual Studio, in the same order as Sync, laid out like the projects Sync creates. With a reference folder (e.g. the `DataAccess` folder of a previously generated solution) it compares every generated file byte for byte and reports identical / different / differing only in line endings or BOM / not in the reference. Exit code 0 only when everything is byte-identical (1 otherwise, 2 for usage errors).
- `SCF.Utils/CodeGenUtils.cs`: the 54 file writes go through a new `FileWriter` property, which defaults to the existing Visual Studio behavior (`VSShellUtils.ProcessFile`). The tool swaps in a plain folder writer; Sync is unchanged. The tool uses the same `CodeGenUtils` code paths as the app.

Verification
- Builds with code-style rules on: the tool and the WPF app, 0 errors.
- Reference model copy vs the reference solution's `DataAccess` folder: "1508 identical, 0 different" and exit code 0.
- Repo sample model vs repo `DataAccess`: "1 different (SQLEngine/Database.cs), 227 differ only in line endings" and exit code 1, matching the earlier analysis (SF-060).
- No arguments: usage message, exit code 2.

Backlog: SF-058 done. Sprint 1 closed (1.0.8.8 – 1.0.9.2, 15 items).

Commit details

```
Add SCF.GenCheck generator regression tool

New Tools/SCF.GenCheck console app: generates all files for an .edmj
without Visual Studio (same order as Sync) and optionally compares
them byte for byte with a reference folder; exit code 0 only when
identical. CodeGenUtils now writes through a replaceable FileWriter
that defaults to the existing Visual Studio behavior.

Verified: the reference model regenerates 1,508 of 1,508 files
byte-identical.

Bump version to 1.0.9.2.
```

---

## 1.0.9.1 — 2026-11-05 (Sprint 1, change set 4)

**Three small schema-reader bug fixes**

Changes
- SF-030: `SCF.Db/Sql/SqlTrigger.cs` read trigger names from the `TABLE_NAME` column instead of `TRIGGER_NAME`. Only the "has triggers" flag is used, so the model was never wrong; the trigger list itself is now correct.
- SF-031: `Lookup.GetTypeId` checked the lower-cased type name but looked up the original, so an upper- or mixed-case type name (e.g. `INT`) threw instead of resolving.
- SF-035: `CommonSettings.Clone()` now also copies `UseRootNSInProjectName` and `TargetFramework` (nothing calls it today).

Verification
- Direct tests: `GetTypeId("INT")` = `GetTypeId("int")` = 13 (`NVarChar` = 18, unknown = -1); `Clone()` keeps every setting; on a throwaway LocalDB database with one trigger (created and dropped by the test) the trigger list returns `TR_Item_Audit` instead of the table name. AppDbTest has no triggers.
- Generator: reference solution regenerated, 1,508 of 1,508 files byte-identical.
- Schema reader: re-reading AppDb from AppDbTest gives the same result as the baseline, and the saved re-read model file is identical to the one from before these fixes.
- `dotnet build` of the WPF app: 0 errors.

Backlog: SF-030, SF-031, SF-035 done. Added SF-081 (permanent test database, offered by the user).

Commit details

```
Fix trigger name column, type lookup casing and CommonSettings.Clone

- SqlTrigger: read TRIGGER_NAME, not TABLE_NAME
- Lookup.GetTypeId: look up the lower-cased name it checked
- CommonSettings.Clone: copy UseRootNSInProjectName and TargetFramework

Generated output and the model read from the database are unchanged.

Bump version to 1.0.9.1.
```

---

## 1.0.9.0 — 2026-11-05 (Sprint 1, change set 3)

**Website startup: one modern Program.cs instead of Program.cs + Startup.cs**

Requested by the user during the sprint: the website had both `Program.cs` and `Startup.cs`, and `WebHostBuilder` raised .NET 10 deprecation warnings.

Changes
- SF-050: `Websites/SCF.Web.CoreWebsite/Program.cs` now builds the site with `WebApplication.CreateBuilder` and contains everything `Startup.cs` did; `Startup.cs` is deleted. Same behavior:
  - content root: the current directory, as before
  - configuration: `appsettings.json`, `appsettings.{Environment}.json` (optional, reload on change) and environment variables
  - logging: only Console, Debug and EventSource, configured from the `Logging` section (the builder's default providers are cleared to match)
  - Kestrel endpoints from `hosting.json`
  - same middleware in the same order (forwarded headers, security headers, custom 404, developer exception page or exception handler + HSTS, static files, routing, cookie policy) and the same three routes, now registered with `app.MapControllerRoute`
  - same `ConfigManager` setup, logging folder and sitemap

Verification
- Website build: 0 errors; the ASPDEPR004/ASPDEPR008 deprecation warnings are gone.
- Behavior comparison: captured every response from the old build (all crawled pages, 404, sitemap, a stylesheet, a removed URL) and from the new build. After normalizing the per-request nonces and the random code-block IDs, all 32 responses are identical (status, response headers and content). Both listen on `http://localhost:60080` from `hosting.json`.
- Not checked: running in the Production environment.

Backlog: SF-050 done.

Commit details

```
Replace WebHostBuilder and Startup.cs with WebApplication

Merge Startup.cs into Program.cs using WebApplication.CreateBuilder,
keeping the same configuration sources, logging providers, Kestrel
endpoints (hosting.json), middleware order and routes. Removes the
.NET 10 WebHostBuilder deprecation warnings.

Every page, header and the sitemap were compared before and after:
identical apart from per-request nonces.

Bump version to 1.0.9.0.
```

---

## 1.0.8.9 — 2026-11-05 (Sprint 1, change set 2)

**Small app cleanups**

Changes
- SF-078: the main menu says "Exception" instead of "Exeption" (`WPF/SharpFactory/MainWindow.xaml`).
- SF-019: removed the leftover subscription limit on how many database objects could be imported. `ConnectionInfo` lost its read-only `ArtifactCount` and the constructor that took it (it now has a normal parameterless constructor); `Model`, the two connection popups and `MainWindow` stopped passing `-1`; `DbModelUtils.PopulateDbCategory` no longer counts objects against a cap. `ArtifactCount` was never written to the `.edmj` (read-only properties are skipped), so the file format is unchanged.
- SF-026: deleted the `BuildTools` folder (only `ILMerge.doc` and a leftover restore `obj` folder from the removed Zipper project were left; nothing referenced it).
- SF-051: the website's security-header code uses `Headers.Append` instead of `Headers.Add` (9 calls; ASP0019 warning gone; same headers sent).

Verification
- `dotnet build` of the WPF app and the website with code-style rules on: 0 errors; ASP0019 no longer reported.
- `.edmj` format: the reference model copy saved with the new code and with the pre-sprint code differs only in `TargetFramework` 9 → 11, the intended 1.0.8.4 behavior.
- Generator: reference solution regenerated, 1,508 of 1,508 files byte-identical.
- Schema reader: re-reading AppDb from AppDbTest gives exactly the same result as before the change.
- App launched with the reference model; its menu (read through UI Automation) shows "Exception".
- Website: home page 200 with all security headers present.

Backlog: SF-078, SF-019, SF-026, SF-051 done.

Commit details

```
Small cleanups: menu typo, license limit, BuildTools, header warning

- Fix the "Exeption" menu label
- Remove the leftover ArtifactCount subscription limit from
  ConnectionInfo and the schema reader (not part of the .edmj format)
- Delete the leftover BuildTools folder
- Use Headers.Append for the website security headers (ASP0019)

Generated output and the .edmj format are unchanged.

Bump version to 1.0.8.9.
```

---

## 1.0.8.8 — 2026-11-05 (Sprint 1, change set 1)

**Website and documentation text fixes; docs layout fixed on desktop**

Changes
- SF-070: Documentation → Introduction no longer offers a "30 day money back guarantee"; it now invites readers to try the free, open-source tool on their own database.
- SF-071: Contact no longer has the sales / pricing / volume licensing address.
- SF-074: Dependency Injection and Query Logging samples moved from `Startup.cs` ("in .NET Core") to a .NET 10 `Program.cs` using `WebApplication.CreateBuilder` (same calls: `RegisterModelConnectionString`, `Logger`, `LogEnabled`, `AddSingleton<IRepositoryContainer, RepositoryContainer>`). The DI page mentions `Host.CreateApplicationBuilder` for console and worker apps.
- SF-075: Project Setup now has a "Required NuGet Package" section (`Microsoft.Data.SqlClient` in the generated Repository project) and a note that servers without a certificate need `Encrypt=False` in the application's connection string.
- SF-076: the `.edmj` is described as "its own lightweight JSON based format" instead of "proprietary".
- SF-077: the seven Microsoft links now point at their current learn.microsoft.com addresses (each checked: 200), including the Visual Studio IntelliSense page without the VS 2019 version pin.
- SF-080 (found during verification): documentation pages were unreadable on desktop because the sidebar covered the content. The stylesheet positions it absolutely inside its column, which relied on Bootstrap 4 making columns `position:relative`; the site uses Bootstrap 5. Added that one rule for the docs sidebar column in `wwwroot/css/sticky_navbar.css`.

Verification
- Website builds; full crawl: all 28 pages 200, Download redirects; only broken asset is the known cookie-consent script (SF-047).
- Wording scan: no money-back, sales/licensing, `Startup.cs` / .NET Core or VS 2019 text left (the remaining "trial" is the testimonial, SF-043).
- Doc samples still check out against the generated code (same 7 demo-database names as before, everything else found).
- Visual check in the browser: Project Setup and Dependency Injection render correctly at desktop size (sidebar left, content right) and on a phone-size screen.

Backlog: SF-070, SF-071, SF-074, SF-075, SF-076, SF-077, SF-080 done.

Commit details

```
Fix outdated website and docs text; fix docs layout on desktop

- Remove the money-back guarantee and the sales/licensing contact
- Update DI and query logging samples to .NET 10 Program.cs
- Document the Microsoft.Data.SqlClient package and Encrypt=False
- Reword the "proprietary" .edmj description
- Point Microsoft links at learn.microsoft.com
- Make the docs sidebar column position:relative so the sidebar no
  longer covers the content (Bootstrap 5 dropped it from columns)

Bump version to 1.0.8.8.
```

---

## 1.0.8.7 — 2026-11-05

**Connect to SQL Server without encryption; schema reader verified against the user's database**

Decision (user, 2026-11-05): the server uses no certificates; no certificate option.

Changes
- `SCF.Db/SqlConnector.cs`: the connection string the app builds now sets `Encrypt=False` (encryption optional). Microsoft.Data.SqlClient 4.0 and later require encryption by default, which needs a server certificate; older versions didn't. Example of the resulting string (Windows authentication):
  `Data Source=localhost;Initial Catalog=AppDbTest;Integrated Security=True;Encrypt=False;Application Name="The Sharp Factory"`

Verification
- That exact connection string (taken from `SCF.Db` itself) connects to the local SQL Server, database AppDbTest; before the change it was rejected with "The certificate chain was issued by an authority that is not trusted".
- Schema re-read: read the AppDb model's tables and procedures from AppDbTest the way the app updates an existing model (`PopulateDbObjectModel` → `OverlayCurrentModel` → read tables, keys, foreign keys, identity/computed columns, procedures) and compared with the AppDb model in the reference `.edmj` (a scratch copy; the reference solution untouched):
  - 172 tables, 171 foreign keys, 9 stored procedures compared.
  - 5 differences, all in the database rather than the reader: 2 tables not in AppDbTest, an extra column in one table, a different column order in another (confirmed in `INFORMATION_SCHEMA.COLUMNS`), and a stored procedure that no longer returns one of its result columns.
  - Everything else matches: column types, sizes, nullability, identity, computed, defaults, primary and unique keys, navigation properties, foreign keys and procedure parameters.
  - Reading took about 35 seconds (baseline for SF-021).
- `dotnet build` of the WPF app: 0 errors.

Backlog: SF-061 and SF-062 done. SF-021 has a measured baseline.

Commit details

```
Connect to SQL Server without requiring encryption

Microsoft.Data.SqlClient 4.0+ requires an encrypted connection by
default, which fails against servers without a certificate. Build the
schema-reader connection string with Encrypt=False, as older SqlClient
versions behaved.

Verified by re-reading a 172-table model from a local SQL Server and
comparing it with the stored model.

Bump version to 1.0.8.7.
```

---

## 1.0.8.6 — 2026-11-05

**Generator output now byte-identical to the reference solution**

The user provided a reference solution generated by the real generator (read-only; never modified), with its model `DataModels/ModelContainer.edmj` (two models: one with 70 tables, and AppDb with 172 tables and 9 stored procedures).

Changes
- `SCF.CSharpGenerator/CodeBuilder/XmlComment.cs`: `MethodXmlComment` returned early for methods with no parameters, so they never got their `<returns>` doc comment. It now writes `<returns>` for every method, as the reference does (274 files were missing it).
- `SCF.CodeGenerator/Database*.cs` (the eight embedded SQL engine templates): converted from LF to CRLF, matching the reference output (SF-027).
- `SCF.CodeGenerator/DatabaseStoredProc.cs` and `DatabaseUpdate.cs`: added the final newline the reference output has.

Verification
- Copied the reference `.edmj` to a scratch folder and switched only the copy's connections to Windows authentication on `localhost` (AppDb → AppDbTest). The reference file is unchanged; nothing in the reference solution was modified (checked afterwards).
- Generated code from the copy with the no-Visual-Studio harness (1,508 files in about 3.5 seconds) and compared with the reference solution's `DataAccess` folder:
  - Before the fixes: 1,232 identical; 274 differed only by missing `<returns>` comments; 2 by a trailing blank line; 8 SQL engine files by line endings.
  - After the fixes: **all 1,508 generated files byte-identical** to the reference. The 130 files that exist only in the reference are hand-written (no generated-file marker).
- The repo's own `DataAccess` sample now matches on 227 of 228 files; the remaining one comes from an older template (SF-060).
- `dotnet build` of the WPF app: 0 errors.
- Found while preparing the database check: the app can't connect to the local SQL Server (default self-signed certificate rejected, because `SCF.Db` doesn't set `TrustServerCertificate`). Logged as SF-062; the schema re-read against AppDbTest waits on it.

Backlog: SF-027 and SF-063 done; SF-061 in progress (code comparison done). Added SF-062, SF-063.

Commit details

```
Match reference generator output exactly

Compared against a reference solution produced by the generator
(1,508 generated files) and fixed every difference:

- Write the <returns> doc comment for methods without parameters too
  (MethodXmlComment returned early)
- Convert the embedded SQL engine templates to CRLF
- Add the final newline to the DatabaseStoredProc and DatabaseUpdate
  templates

Generated output is now byte-identical to the reference.

Bump version to 1.0.8.6.
```

---

## 1.0.8.5 — 2026-11-05

**Microsoft.Data.SqlClient upgraded to the latest (7.1.1)**

Decision (user, 2026-11-05): use the latest Microsoft.Data.SqlClient.

Changes
- `PackagedDependencies/SCF.Db/SCF.Db.csproj` and `DataAccess/SCF.Repository/SCF.Repository.csproj`: `Microsoft.Data.SqlClient` 5.1.1 → 7.1.1 (latest stable).
- Same two projects: added a direct reference to `System.Security.Cryptography.Pkcs` 10.0.14. SqlClient 7.1.1 brings in 9.0.18, which has a known high-severity advisory (GHSA-846f-f764-76mx).
- No source code changes.

Verification
- `dotnet build` of the WPF app (with all generator projects): 0 errors. All NU1902/NU1903 vulnerability warnings are gone (previously from SqlClient 5.1.1, Azure.Identity 1.7.0, Microsoft.IdentityModel 6.24.0, then Pkcs 9.0.18); only the Windows-only COM warnings (CA1416) remain.
- Schema reader end-to-end on SqlClient 7.1.1 against a throwaway LocalDB database (created and dropped by the test): connection check, database list, tables with identity/computed/rowversion/default/XML columns, primary/unique/foreign keys and navigation properties, a view, a stored procedure with parameters and result columns, table and scalar functions, and a sequence were all read correctly.
- Not done: the same test on the full SQL Server instance; the user will provide their own database and code base to compare against (SF-061).

Backlog: SF-025 done. Added SF-061.

Commit details

```
Upgrade Microsoft.Data.SqlClient to 7.1.1

Move SCF.Db and the DataAccess sample from Microsoft.Data.SqlClient
5.1.1 to 7.1.1, and reference System.Security.Cryptography.Pkcs
10.0.14 directly to replace the vulnerable 9.0.18 it pulls in. Clears
all NuGet vulnerability warnings. No source changes.

Bump version to 1.0.8.5.
```

---

## 1.0.8.4 — 2026-11-05

**Generated code targets .NET 10 only; template-loading bug fixed**

Decision (user, 2026-11-05): the new version works with .NET 10 onwards; every other version is gone. .NET 11 will follow soon, so adding it should stay easy.

Changes
- `SCF.Models/EntityDataModel/TargetFrameworks.cs`: the enum now has only `NET10 = 11`. Removed .NET Framework 4.6.1–4.8.1, .NET Standard, .NET Core, .NET 6 and .NET 7. Numbering continues from the old values, so .NET 11 will be `NET11 = 12`.
- `CommonSettings.TargetFramework`: a model with no target framework, or one saved with a removed framework, loads as .NET 10 (verified: the sample `.edmj`, saved as .NET 6, loads as `NET10`).
- `SCF.Utils/VS/SolutionManager.cs`: the four hard-coded project templates (netcoreapp3.1, net6.0, net7.0, netstandard2.1) are replaced by a small template table with one entry, `net10.0`; same project file shape as the old .NET 7 template. Removed the classic .NET Framework path (Visual Studio class-library template, `Class1.cs` cleanup).
- `SCF.Utils/CodeGenUtils.cs`: the C# 7 / C# 10 syntax switches no longer depend on the target framework (every supported target allows them); they depend only on the Visual Studio instance, as before.
- `SCF.CodeGenerator/Query/LookupGenerator.cs`: removed the "older than .NET Framework 4.7.2" branches; generated `HashSet`s always get their capacity, as they already did for every target from 4.7.2 on.
- `SCF.CodeGenerator/Common.cs`: generated code always uses `Microsoft.Data.SqlClient`. Previously only .NET 6 did; .NET 7 got `System.Data.SqlClient` (SF-032).
- `SCF.Utils/VS/VSShellUtils.cs` and the WPF sync dialog: removed the .NET Core / .NET Standard special case that skipped refreshing project items; .NET 10 projects refresh like .NET 7 projects did. The sync dialog's ".NET Version" list now shows only .NET 10.
- **Bug fix:** `SCF.CodeGenerator.csproj` now pins the embedded SQL engine templates' resource names (`LogicalName="SCF.CodeGeneration.Database*.cs"`). The DLL named them `SCF.CodeGenerator.Database*.cs` while the code looks up `SCF.CodeGeneration.Database*.cs`, so Sync crashed when it reached the Repository project. This was already the case in the .NET 7 build (checked); found by the regression harness below.
- C# files touched were normalized to CRLF per `.editorconfig`.

Verification
- `dotnet build` of the WPF app (including all generator projects): 0 errors.
- Regression check without Visual Studio: a scratch harness loads `DbModels/ModelContainer.edmj` and runs the generators in the same order as Sync, writing the files to a folder.
  - Ran it with the generator from before this change (from the pre-1.0.8.2 backup; only the template-name fix applied so it could run) and with the new generator: **all 228 generated files byte-identical**.
  - Compared with the checked-in `DataAccess/`: 178 identical. The rest differ only in ways unrelated to this change (doc `<returns>` comments, an older `Database.cs` template, trailing blank lines), so `DataAccess/` predates the current generator (SF-060).
- Not verified: the new `net10.0` project template inside Visual Studio (Sync needs a running Visual Studio).

Backlog: SF-012, SF-032, SF-053, SF-055 done. Added SF-056 (.NET 11), SF-057, SF-058 (keep the regression harness), SF-059, SF-060. Guiding principles: generated code targets .NET 10 and later only.

Commit details

```
Generate .NET 10 projects only; fix SQL engine template loading

Drop every target framework except .NET 10 from the generator:
.NET Framework 4.x, .NET Standard, .NET Core, .NET 6 and .NET 7.

- TargetFrameworks enum: only NET10 (= 11; NET11 will be 12)
- Models saved with a removed or missing framework load as .NET 10
- SolutionManager: one net10.0 project template in a small table;
  remove the classic .NET Framework project path
- Generated code always uses Microsoft.Data.SqlClient
- Remove framework-specific branches in LookupGenerator, CodeGenUtils,
  VSShellUtils and the sync dialog
- Fix: pin LogicalName on the embedded Database*.cs templates; they
  were looked up under a different name and Sync crashed

Generated output for the sample model is byte-identical to before.

Bump version to 1.0.8.4.
```

---

## 1.0.8.3 — 2026-11-05

**Migrate to .NET 10, code unchanged**

Decision (user, 2026-11-05): move everything to .NET 10 and keep it the same as much as possible; no new C# features.

Changes
- All 14 projects retargeted: `net7.0` → `net10.0`, and the WPF app `net7.0-windows` → `net10.0-windows`. No other project properties changed.
- Every project now pins `<LangVersion>11.0</LangVersion>`, the C# version `net7.0` used by default. Without it .NET 10 would compile as C# 14, and the enforced style analyzers would start asking for newer syntax.
- `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 7.0.5 → 10.0.14 in the website: it ships with ASP.NET Core and has to match the framework version.
- No source code changes. Other packages unchanged: Newtonsoft.Json 13.0.3, Microsoft.Data.SqlClient 5.1.1 (upgrade tracked as SF-025), Microsoft.VisualStudio.Interop 17.14.40260, WinSCP 5.21.8.

Verification
- `dotnet build` with code-style rules on: the WPF app, the website and all generator projects build with 0 errors.
- Whole-solution build: the only error is the existing one in the generated `DataAccess/SCF.Query` sample (CA1822, SF-036). It stops MSBuild before some other projects in a solution build, so the WPF app and the website were also built individually.
- New warnings on .NET 10, all non-blocking and left as-is (logged in the backlog): ASPDEPR004/ASPDEPR008 (`WebHostBuilder` obsolete, SF-050), ASP0019 (`Headers.Add` in `HeaderManager`, SF-051), SYSLIB0051 (generated `DataExchangeException`, SF-052), NU1902/NU1903 (SqlClient 5.1.1 and its transitive packages, SF-025).
- WPF app launched natively on .NET 10 (`Microsoft.NETCore.App` / `Microsoft.WindowsDesktop.App` 10.0.14, no roll-forward): main window opened, responsive, no error logs.
- Website ran natively on .NET 10: `/`, `/QuickStart`, `/Contact`, `/PrivacyPolicy`, `/sitemap.xml` and the Documentation pages checked return 200. Download redirects (302) when the releases URL is set; removed pages return 404; security headers still sent.
- The project files were backed up before the change.

Backlog: SF-011 done. Added SF-049 (System.Text.Json, optional), SF-050, SF-051, SF-052, SF-053 (.NET 10 as a target for generated code). Added ".NET 10, same code" to the guiding principles.

Commit details

```
Migrate to .NET 10

Retarget all projects from net7.0 to net10.0 (WPF app:
net10.0-windows). Pin LangVersion to 11.0 in every project so the code
stays on the C# version it was written for and no new language features
creep in. Update Microsoft.AspNetCore.Mvc.NewtonsoftJson to 10.0.14 to
match ASP.NET Core 10. No source changes.

Bump version to 1.0.8.3.
```

---

## 1.0.8.2 — 2026-11-05

**Remove the company and licensing leftovers; free forever**

Background (user, 2026-11-05): Sun Code Factory LLC was the user's own company and is defunct. The user owns the code and the thesharpfactory.com domain. The Sharp Factory will be free forever.

Changes
- Removed the "Copyright (c) Sun Code Factory LLC. All rights reserved." header block (copyright, company and author lines) from 274 files: 255 C# files with the `//` header, 17 XAML files with the header in a `<!-- -->` comment, and 2 variants. BOMs and line endings preserved. The attribution comment in `SCF.Models/Inflector.cs` ("based on https://github.com/abelepereira/Inflector-Portable") was kept.
- WPF About box: edition now "Free & Open Source"; the copyright line is replaced with "Free forever." and the website address.
- Website footer: removed the company street address and "© year The Sharp Factory. All Rights Reserved"; now reads "The Sharp Factory. Free & open source, forever." (the Email Us link stays).
- Website Privacy Policy: removed the company name and mailing address, and the parts that no longer apply: sign-up for subscriptions/licenses, registered users, logged-in users, e-commerce, and references to the removed Terms and License Agreement pages. The general visitor, security, links, statistics, cookies, changes and contact sections are unchanged.
- References to thesharpfactory.com (docs links, info@ and privacy@ email addresses, cookie domain) are kept: the user owns the domain.
- Generated code is not affected; the generator never wrote the company header into its output.
- `BACKLOG.md`: added "Guiding principles" (free forever; user owns the code and domain).

Verification
- No file outside the notes mentions Sun Code Factory anymore.
- `dotnet build` of the WPF app and the website with code-style rules on: 0 errors each. Warnings unchanged in kind (CA1416, NETSDK1138, NU1903).
- `DataAccess` still shows only the existing generated-code error (SF-036); nothing new.
- A zip backup of the whole folder was taken before the header removal.

Backlog: SF-006 and SF-042 done.

Commit details

```
Remove company and licensing leftovers

Sun Code Factory LLC no longer exists and The Sharp Factory is going
to be free forever.

- Strip the "Copyright (c) Sun Code Factory LLC. All rights reserved."
  header from 274 source, XAML and Razor files (third-party
  attribution in Inflector.cs kept)
- About box: "Free & Open Source" / "Free forever." instead of the
  company copyright
- Website footer: drop the company address and "All Rights Reserved"
- Privacy Policy: drop the company name and address and the sections
  about subscriptions, registered users and e-commerce

Bump version to 1.0.8.2.
```

---

## 1.0.8.1 — 2026-11-05

**Website becomes a free download site; backend, subscriptions, accounts and encryption removed**

Decisions (user, 2026-11-05): the website stays and offers the download; no subscription or licensing features; no encrypted components; no user accounts; Download goes to GitHub Releases; no updater.

Changes
- Deleted 20 projects and removed them from `SCF.sln`:
  - WebAPI: `SCF.Web.CoreApi`, `SCF.Web.CoreApiLibrary` (the app's encrypted session/DLL download API), `SCF.Web.CoreWebsiteApi`, `SCF.Web.CoreWebsiteApiLibrary` (the website's backend), `SCF.Web.Routing`, `SCF.Web.Common`, `SCF.Web.Swagger`
  - Dependencies: `SCF.BusinessLayer`, `SCF.BusinessLayer.Interfaces`, `SCF.Communications.Backend`, `SCF.Configuration`, `SCF.Constants`, `SCF.Facades.Base.SecureServer`
  - Common: `SCF.Communications` (encryption), `SCF.Compression`, `SCF.Facades.AppSecureClient`, `SCF.Facades.Base.SecureClient`, `SCF.Facades.Common`
  - Websites: `SCF.CoreWebsiteApi.JsonClient`, `SCF.Web.ClientDetection`
- Website (`Websites/SCF.Web.CoreWebsite`):
  - Removed the Account (subscriptions, billing, support tickets), Membership (register, login, password reset, email confirmation), Store (cart, PayPal checkout) and Merchant (PayPal transactions) areas, the Subscribe page, the commercial License Terms and Subscription Agreement pages, email templates and the email sender (MailKit), Identity/cookie sign-in, and data-protection keys stored through the API.
  - Home page: pricing plans, discount banners, payment logos and checkout scripts removed. Header and call to action now say "Free & Open Source" with a Download button.
  - Layout: removed Buy, Sign Up, Login and the account menu; footer "Get Sharp Factory" now links to Download; dropped the Subscription Agreement / License Terms / Terms of Use links.
  - Download (`/Downloads/Installers/SharpFactory`): now redirects to `Downloads:SharpFactoryReleasesUrl` (the latest GitHub release). It returns 404 until that URL is set (SF-040). Download logging, the subscriber lookup and browser/OS detection are gone.
  - Documentation → Installation page rewritten: download from GitHub, unzip, run `SharpFactory.exe`; no login, no subscription token, no internet needed.
  - Controllers no longer take a sign-in manager; `ViewModelContainer`/`LayoutViewModel` lost the subscriber, login/register and password-policy members.
  - New website-local `Helpers/Configuration/ConfigManager.cs` (site title, host domain, environment, logs folder, releases URL) and `Helpers/Routing/AreaNames.cs` replace the deleted shared projects.
  - `appsettings.json` / `appsettings.Development.json` now hold only AppSettings (SiteTitle, HostDomain), Downloads, AppLogs, Logging and AllowedHosts. The API bearer token, SMTP credentials, PayPal settings and encryption password are gone from these files (rotation still needed, SF-002).
  - Response-compression registration dropped: it was registered but the middleware was never added, so behavior is unchanged.
  - Deleted unused assets: account/checkout JS, billing/subscription/support CSS and JS bundles, payment logos.
  - Website C# files normalized to CRLF per `.editorconfig`.
- `Common/SCF.Common/AppName.cs`: removed the `SFUpdater` and `SharpFactoryDeps` constants.
- The website's original SMTP settings (production and Development) were extracted from the pre-change backup and handed to the user in a file outside the repo.

Verification
- `dotnet build` of the WPF app and the website with code-style rules on: 0 errors each.
- Whole-solution build: the only error is in the untouched generated sample `DataAccess/SCF.Query` (CA1822 under the .NET 10 SDK analyzers; generator bug SF-036).
- Ran the website locally (Development, rolled forward to a newer ASP.NET runtime):
  - 200: `/`, `/QuickStart`, `/Contact`, `/PrivacyPolicy`, `/sitemap.xml`, and the Documentation pages checked (Introduction, Installation, Syntax, Repositories).
  - Custom 404: `/Terms/LicenseTerms`, `/Membership/Auth/Login`, `/Store/Purchase/Checkout`, `/Account/Subscriptions`.
  - `/Downloads/Installers/SharpFactory`: 404 with no URL set; 302 to the configured URL when one is set.
  - Home page shows no Buy/Login/Sign Up/pricing. Only console error: `wwwroot/lib/cookieconsent` 404, because `wwwroot/lib` is empty in the repo (predates this change, SF-047).
- No encryption APIs remain in the kept code (`Common`, `PackagedDependencies`, `WPF`, `Websites`).
- A zip backup of the whole folder was taken before the removals.

Backlog: SF-004, SF-008, SF-009 done; SF-002 files done (rotation open); SF-018 dropped. Added SF-036, SF-040 to SF-048.

Commit details

```
Turn the website into a free download site; remove backend and encryption

The Sharp Factory is going open source. Remove everything that existed
to sell and enforce subscriptions:

- Delete the WebAPI projects (app session/DLL download API and the
  website backend), the business layer, configuration/constants
  projects, and the encryption, compression and secure-client/server
  facade projects (20 projects)
- Website: remove accounts (register/login/password reset), support
  tickets, subscriptions, billing, PayPal store, email sending, the
  commercial terms pages and the pricing section of the home page
- Download now redirects to the latest GitHub release
  (Downloads:SharpFactoryReleasesUrl)
- Rewrite the Installation docs page for the free download
- Replace shared config/routing with small website-local classes
- Strip secrets from the website appsettings files

Bump version to 1.0.8.1.
```

---

## 1.0.8.0 — 2026-11-05

**WPF app references the generator projects directly; no more download or in-memory loading**

Changes
- `WPF/SharpFactory/SharpFactory.csproj`: replaced the `SharpFactoryDeps.dll` reference with project references to `SCF.CodeGenerator`, `SCF.Db`, `SCF.Models` and `SCF.Utils`. Removed the references to `SCF.Compression`, `SCF.Facades.AppSecureClient`, `SCF.Facades.Base.SecureClient` and `SCF.Facades.Common`. `SCF.Common` (logging, versioning) is still referenced.
- Deleted the `SharpFactoryDeps` intermediary: `DependencyContainers/SharpFactoryDeps` (including its ILMerge step), `PackagedBinaries/`, and their entries in `SCF.sln`.
- Removed the light-client startup:
  - Deleted `SCFEntryPoint.cs` (embedded/compressed assembly loading and `AssemblyResolve`), `LoaderWindow` (version check, updater, dependency download), and `LoginPopupControl` (subscription login).
  - `Program.cs` now enables logging, creates `App`, and opens `MainWindow` directly. Also dropped the 5-second Debug sleep that waited for the server.
  - `MainWindow.Initialize` takes only the version. Edition and artifact limit came from the subscription and are gone; the artifact limit stays at -1 (unlimited).
- Removed the Help → Support section (it sent tickets to the server): `SupportSectionManagerControl`, `Models/SupportCategory.cs`, and the nav button.
- `Display/DisplayManager.cs`: replaced the `ScreenInfo` DTO (from `SCF.Facades.Common`) with a plain scale factor.
- XAML: `ModelManagerControl.xaml` and `AddOrUpdatePopupControl.xaml` now import `SCF.Models` from `assembly=SCF.Models` instead of `assembly=SharpFactoryDeps`.
- Deleted `Microsoft.VisualStudio.Settings.15.0.dll` and `Microsoft.VisualStudio.Shell.15.0.dll` from the WPF folder (only embedded for in-memory loading; nothing references them).
- `PackagedDependencies/SCF.Utils/SCF.Utils.csproj`: replaced the hard-coded `..\..\..\Program Files\...` DLL paths for EnvDTE/VSLangProj with the `Microsoft.VisualStudio.Interop` 17.14.40260 NuGet package, so it builds on any machine.
- Build fixes for the .NET 10 SDK's stricter code-style checks (`EnforceCodeStyleInBuild` stays on):
  - Converted 202 LF-only `.cs` files in `PackagedDependencies`, `WPF` and `Common/SCF.Common` to CRLF, as `.editorconfig` requires (`end_of_line = crlf`). Whitespace only, no code changes. The eight embedded engine templates (`SCF.CodeGenerator/Database*.cs`) were left as-is so generated output is unchanged (SF-027).
  - `SCF.Utils/JsonUtils.cs`: dropped explicit lambda parameter types (IDE0350).

Verification
- `dotnet build WPF/SharpFactory/SharpFactory.csproj` with code-style enforcement on: **0 errors**. Warnings: NETSDK1138 (net7.0 out of support, SF-011), NU1903 (SqlClient vulnerability, SF-025), CA1416 (Windows-only COM calls).
- Output folder contains `SCF.CodeGenerator.dll`, `SCF.Db.dll`, `SCF.Models.dll`, `SCF.Utils.dll`, `SCF.CSharpGenerator.dll` directly; no `SharpFactoryDeps`.
- Smoke test: launched `SharpFactory.exe` (rolled forward to the .NET 8 desktop runtime because .NET 7 isn't installed here). The main window opened directly, stayed responsive, and wrote no error logs. Not yet tested: loading an `.edmj`, connecting to SQL Server, generating code.

Backlog: SF-005, SF-016, SF-017 done. Added SF-019, SF-025, SF-026, SF-027. Updated SF-004, SF-008, SF-009, SF-010, SF-011.

Commit details

```
Reference generator projects directly from the WPF app

The WPF client used to log in, download SharpFactoryDeps (ILMerged,
LZMA-compressed) from the server and load it in memory. Replace that
with direct project references to SCF.CodeGenerator, SCF.Db,
SCF.Models and SCF.Utils, and delete the SharpFactoryDeps intermediary
and PackagedBinaries.

- Remove SCFEntryPoint, LoaderWindow, LoginPopupControl and the
  Support section; Program opens MainWindow directly
- Drop the facade/compression references from SharpFactory.csproj
- Point XAML namespaces at the SCF.Models assembly
- Use the Microsoft.VisualStudio.Interop NuGet package in SCF.Utils
  instead of machine-specific Program Files paths
- Normalize C# files to CRLF per .editorconfig and fix IDE0350 so
  the build passes the .NET 10 SDK code-style checks

Bump version to 1.0.8.0.
```

---

## 1.0.7.9 — 2026-11-05

**Remove the VersionGenerator and Zipper build tools**

Changes
- Deleted `BuildTools/VersionGenerator` and `BuildTools/Zipper`, and removed both from `SCF.sln`. `dotnet sln` also dropped the now-empty `BuildTools` and `Test` solution folders and saved the file with CRLF line endings.
- `WPF/SharpFactory/SharpFactory.csproj`: removed the targets that only served the LZMA embed pipeline:
  - `ZipDependencies` (ran Zipper on the client DLLs)
  - `EmbedDependencies` (embedded the `.lzma` outputs and `SCF.Compression.dll`)
  - `Cleanup` (deleted the reference DLLs from the output because they were embedded, then copied to a local publish folder)
  - the `GetFirstItem` inline task they used
  - the Release pre-build event that ran VersionGenerator on `Versionable.cs`
- `DependencyContainers/SharpFactoryDeps/SharpFactoryDeps.csproj`: removed the Zipper call and the copy of the `.lzma` file. The ILMerge step is still there (SF-016).
- `WPF/SharpFactory/Versionable.cs` now reads 1.0.7.9, kept in sync with `VERSION`.
- Added `RELEASE_NOTES.md`; moved the version history out of `BACKLOG.md`.

Verification
- `SCF.sln`, `SharpFactory.csproj` and `SharpFactoryDeps.csproj` load cleanly in MSBuild.
- `dotnet build` of the WPF app still fails, for reasons that predate this change:
  - `SCF.Compression` fails the .NET 10 SDK code-style checks (SF-009).
  - With code-style checks off, the XAML can't find `Category` / `DbObjectCategory`, because those come from the downloaded `SharpFactoryDeps.dll`, which isn't in the repo (SF-005).

Backlog: SF-015 done. Added SF-008, SF-009, SF-016.

Commit details

```
Remove VersionGenerator and Zipper build tools

Drop BuildTools/VersionGenerator and BuildTools/Zipper and remove them
from SCF.sln. Strip the build steps that depended on them: the LZMA
zip/embed/cleanup targets and the VersionGenerator pre-build event in
SharpFactory.csproj, and the Zipper/.lzma copy steps in
SharpFactoryDeps.csproj.

Bump version to 1.0.7.9 and start RELEASE_NOTES.md.
```

---

## 1.0.7.8 — 2026-11-05

**Baseline**

- Starting version, taken from the existing `WPF/SharpFactory/Versionable.cs`.
- Added `BACKLOG.md` and `VERSION`. No code changes.
