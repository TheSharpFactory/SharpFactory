# Changelog

What changed in each published release of The Sharp Factory, newest first. Each section is also the text of that
release on GitHub, with its downloads.

## 1.2.1.3 — 2026-11-13

### What's new

- **Changes typed by entity.** `XUtils.GetChanges`, `XProperty.CreateChange` and `QueryFilter.New` return
  `IQueryFilter<XProperty>`, and an update's `Set(change)` takes only changes of its own entity: passing another
  entity's change no longer compiles. Code that uses `var` for the changes needs no change. Sync your solutions to
  get it; the SQL does not change.

> The macOS tests did not run for this release: GitHub had no macOS machine free for CI. The Windows and Linux
> tests passed; the macOS downloads are built the same way as always. The macOS tests run again with the next release.

### Downloads

| Download | What |
|

---

## 1.2.1.2 — 2026-11-13

**Important if one solution uses another solution's generated projects:** in earlier versions, a Sync into a
solution that had the generated projects in another folder (another solution's data access, added to it) moved
those projects into its own folder, pointed every reference at the new place and so took them away from the other
solution. Sync no longer moves or changes anything outside the solution's own folder: such projects are written
where they are, their project files are left as they are, and no file is deleted in them.

### What's new

- **Updates from changes.** `Set(change)` on an update takes a change made by `XUtils.GetChanges(original, changed)`,
  and `XProperty.CreateChange(propertyId, value)` makes one for a property picked at run time by its `XProp` value.
  Code written for the earlier query API that set changed columns in a loop (`uv.Set(change.Property, change.Value)`)
  becomes `uv.Set(change)`. Same Set clause and SQL as `Set(prop, value)`.

### Fixes

- **Stored procedures and functions keep result columns computed from an expression.** A column such as
  `SELECT CAST(0 AS BIGINT) AS [PlatformBetId]` was left out of the model on Update, so a procedure whose result is
  only such columns lost its result type. Update your models to get them back.
- **Sync suggests the right solution.** The solution the model was last synced into comes first; when the model was
  never synced, the solution nearest the model file. The app no longer guesses a solution "open in an IDE": that
  guess named the solution an IDE was started with, even after another one was opened in it.
- **A Sync into another solution asks first:** another one than the model was last synced into or, for a model
  never synced, one whose folder does not have the model file. The question names both.
- Project files Sync edits keep their byte order mark.
- The update question shows the release notes in their own box, as paragraphs and list items.

### Downloads

| Download | What |
|

---

## 1.2.0.9 — 2026-11-13

**Code written for the earlier query API:** navigation properties can again be given with a collection initializer,
`new NavProps<CustomerProperty> { CustomerNavProperty.Orders }`. Code that used `new NavProps<CustomerNavProperty> { ... }`
compiles after changing the type argument to the entity's property container. Sync your solutions with this version
to get it; nothing else in the generated code changes, and neither does the SQL.

### Downloads

| Download | What |
|

---

## 1.2.0.8 — 2026-11-13

**Regenerate your code:** in 1.1.8.8 every paged read in the generated code failed on SQL Server ("Invalid attempt to
read from column ordinal '0'. With CommandBehavior.SequentialAccess, ..."). Sync your solutions with this version to
fix it. The fix does not change the SQL the generated code sends.

### What's new

- **Automatic updates.** The app looks for a newer release on GitHub once a day, and when you click
  **Check for updates** on the About page. When there is one it shows what is new and asks; **Update now** downloads
  it, checks it, replaces the app and the command line next to it, and restarts with the model that was open.
  Every release is signed: the app installs only a download whose checksums carry The Sharp Factory's signature.
  Nothing is downloaded before you say so, and **Cancel** stops the download. The check sends only the app's name and
  version; "Check for updates once a day" on the About page turns it off. Where the app cannot replace itself (a
  folder it cannot write to, or on macOS an app opened straight from Downloads), it offers the release page.
- **The command line comes with the app.** Every app download now also has `sharpfactory` (`sharpfactory.exe` on
  Windows, inside `TheSharpFactory.app/Contents/MacOS` on macOS). The separate command-line downloads are still
  there for build servers.
- `sharpfactory --version` prints the version, for build logs. The downloads carry their real version number.

### Fixes

- Paged reads in the generated code work again (see above).
- `IN` lists of strings on `nvarchar` and `nchar` columns are written as Unicode literals (`N'...'`), so values
  outside the database's code page match.
- Sync shows the solution open in Visual Studio again, and its solution list is up to date right after a Sync.
- The schema reader's log files roll over at 150 KB.
- Generated async methods that only pass a call on return its task directly, and result lists start at their first
  row: less work per call, same results and the same SQL.
- The app: Sync is right under Home, zero counts on Sync are grey, warnings show an amber sign, and the About page
  links to the releases and the issue tracker.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows, with the command line |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel), with the command line |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux, with the command line |
| `sharpfactory-cli-*` | The command line alone, for the same systems |
| `SHA256SUMS.txt` | The SHA-256 of every download (the app checks its update against it) |
| `SHA256SUMS.txt.sig` | The signature of `SHA256SUMS.txt` (the app installs an update only when it matches) |

Nothing to install: unpack and run. The .NET runtime is included. The macOS app is not signed yet, so macOS blocks
it the first time: open it once, then go to System Settings > Privacy & Security and choose **Open Anyway**. On
macOS 14 or earlier, right-click the app and choose **Open** instead.

The generated code needs the `Microsoft.Data.SqlClient` NuGet package in the repository project, and
`Microsoft.SqlServer.Types` when the database has `geography` or `geometry` columns. Documentation:
https://www.thesharpfactory.com/Documentation

---

## 1.1.8.8 — 2026-11-10

The first open-source release of The Sharp Factory: a desktop app and a command line that read a SQL Server
database and generate the C# data access for it. Free and open source under the MIT License.

### What it does

- Reads a database's tables, views, stored procedures, functions and sequences, and keeps them in a model file
  (`.edmj`).
- Generates the data access: entities, repositories, a repository container and typed query composition
  (filtering, sorting, pagination, navigation properties). The generated projects target .NET 10 or .NET 11.
- **Sync** writes the generated projects into your solution (`.sln` or `.slnx`): it creates the projects, adds the
  references between them, writes only the files that changed and removes generated files the model no longer
  produces. Files you add yourself are never touched. **Preview** shows every change with a diff first.
- **Update** reads the database again and refreshes the model; from the command line, `update --check` fails a
  build when the model is behind the database.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

Nothing to install: unpack and run. The .NET runtime is included. The macOS app is not signed yet, so macOS blocks
it the first time: open it once, then go to System Settings > Privacy & Security and choose **Open Anyway**. On
macOS 14 or earlier, right-click the app and choose **Open** instead.

The generated code needs the `Microsoft.Data.SqlClient` NuGet package in the repository project, and
`Microsoft.SqlServer.Types` when the database has `geography` or `geometry` columns. Documentation:
https://www.thesharpfactory.com/Documentation
