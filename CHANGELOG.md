# Changelog

What changed in each published release of The Sharp Factory, newest first. Each section is also the text of that
release on GitHub, with its downloads.

## Unreleased

### Changes

- **Two JSON settings for the generated Entities project**, on the Settings page (both off, so nothing changes
  until you turn one on):
  - **Enable System.Text.Json:** each database gets a System.Text.Json source generation context, such as
    `AppDbJsonContext`, listing every class of the Entities project (tables, views, and the request and result
    classes of procedures and functions), each also as a List. Use it as
    `JsonSerializer.Serialize(customer, AppDbJsonContext.Default.dboCustomer)`, or add `AppDbJsonContext.Default` to
    your `JsonSerializerOptions`. xml columns are written as their XML text.
  - **Optimize JSON:** every property gets a short JSON name with `[JsonPropertyName]`: A to Z, then A0 to A9, AA to
    AZ, B0 and so on. The model file keeps each name, so a property keeps its name when columns are added or
    removed; a new column gets the next name, and a removed column's name is not given again. Sync gives the new names
    (the app then has a change to save; the command line saves the model file itself).

---

## 1.2.2.9 — 2026-11-18

### Changes

- **Build and release:** the workflows use the current GitHub Actions, which run on Node.js 24; GitHub is retiring
  Node.js 20. macOS builds and tests in a workflow of its own, so a day when GitHub has no free Mac no longer fails CI
  and its badge; the project page shows a badge for each. Nothing changes in the app, the command line or the generated code.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.8 — 2026-11-18

### Changes

- **.NET 10 or .NET 11 for the generated code, always on the Sync page.** The choice sits next to Change target
  (it was inside the new-solution panel). Switching it moves the generated projects that already exist to that
  version: Sync shows each project file's change in its list and diff before it writes them, and the command line
  prints one line per project. Only the version changes in those files. Projects you created yourself are not
  changed, so a project of yours that references the generated ones needs the same .NET version to build.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.7 — 2026-11-18

### Changes

- **The project page** on GitHub welcomes a visitor: the logo, links to the download, the Quick Start, the
  walkthroughs, the documentation and Discussions, three screenshots of the app, badges for the build, the latest release and the license, and
  where to ask questions and report bugs. Nothing changes in the app, the command line or the generated code.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.6 — 2026-11-18

### Changes

- **The project page** on GitHub shows what The Sharp Factory is, a picture of Sync, how to try it (the downloads,
  the Quick Start and the sample database), the generated code in use and what to know first. Nothing changes in the
  app, the command line or the generated code.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.5 — 2026-11-17

### Fixes

- **A query before the connection string is registered** says what to do. The generated code threw
  "KeyNotFoundException: The given key '1' was not present in the dictionary"; now it throws an
  `InvalidOperationException`: "No connection string is registered for the model with id 1. Call
  Database.RegisterModelConnectionString with that model's RepoLookup.ModelId and its connection string once, when
  the application starts, before the first query." Regenerate with Sync to get it.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.4 — 2026-11-17

### Fixes

- **Navigation properties two or more levels deep.** These now load correctly in the generated code (regenerate with
  Sync to get the fix):
  - With `First`, `FirstOrDefault`, `Single` or `Top`, a query with a second level such as
    `.Append(CustomerNavProperty.Orders).Then(OrderNavProperty.Lines)` failed in SQL Server with "The ORDER BY clause
    is invalid in … subqueries".
  - With paging, the second level skipped rows (an order came back without its lines).

  Queries with one level, or without `Top`, `First`, `Single` or paging, send the same SQL as before.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.3 — 2026-11-16

### Changes

- **Microsoft.Data.SqlClient 7.1.2.** The app, the command line and the Repository project Sync creates use
  `Microsoft.Data.SqlClient` 7.1.2 (was 7.1.1). Projects Sync created before keep the version they have: change it
  in the Repository project's file if you want the new one.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.2 — 2026-11-15

### What's new

- **Sync shows what it will change, by itself.** There is no Preview button any more: as soon as a solution is
  chosen, the Sync page works out every file Sync would add, change or delete (writing nothing) and shows them with
  their differences. It works it out again when you update the model or change a layer or a setting. The button says
  how many files it will write ("Sync 165 files"), the Sync entry in the menu shows the same count, and after a Sync
  the page says when it ran and what it wrote, with a Save button when the model file has unsaved changes.
- **A round of UI polish.** The generated code on the Models page gets most of the width; the object checkboxes in
  the New model wizard are drawn properly, and Create model waits for at least one object; disabled buttons and menu
  entries say why; release notes in the update question are laid out; secondary text is easier to read; skipped
  objects stand out in the logs, with a filter to show only them.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.1 — 2026-11-15

### Fixes

- **Dates after a day.** The "date greater than" filter left out values at exactly midnight of the next day, and on a
  `date` column the whole next day. Sync your solutions to get the fix: only the SQL engine's `DatabaseSelect.cs`
  changes.
- **Slower servers can connect.** The Add and Update wizards gave a server 2 seconds to answer; they now wait as long
  as the connection's timeout (15 seconds), so remote and cloud servers no longer show as unreachable.
- **Logging needs a logger.** Turning on `Database.LogEnabled` in the generated code without setting
  `Database.Logger` first now throws right there, with a message that says so, instead of every query failing later.
  Sync to get it.
- **The master database is refused.** A model whose database is `master` is not saved, and the Add wizard says so:
  master belongs to the server, and no code should be generated from it.
- **Values longer than their column are refused.** A text or binary value longer than its column (or a function's
  parameter) was cut to fit without a word, so only part of it was saved, or a filter compared only part of it. The
  generated code now throws before anything is sent to the database, and names the property. Sync to get it.

### Downloads

| Download | What |
|---|---|
| `TheSharpFactory-win-x64.zip`, `TheSharpFactory-win-arm64.zip` | The app for Windows |
| `TheSharpFactory-osx-arm64.zip`, `TheSharpFactory-osx-x64.zip` | The app for macOS (Apple silicon, Intel) |
| `TheSharpFactory-linux-x64.tar.gz` | The app for Linux |
| `sharpfactory-cli-*` | The command line, `sharpfactory`, for the same systems |

---

## 1.2.2.0 — 2026-11-14

### What's new

- **The code writer is its own library.** The part that writes the C# files is now a separate project,
  `SF.CSharpWriter`, that takes text and knows nothing of your model; the generators write through it. The
  generated code is exactly the same, byte for byte, so there is nothing to Sync.

### Downloads

| Download | What |
|

---

## 1.2.1.9 — 2026-11-14

### Fixes

- **Code writer details.** A static constructor is written with one space after `static`, and a block comment
  closes at its own indent. Nothing generated today uses either, so the generated code does not change.

### Downloads

| Download | What |
|

---

## 1.2.1.8 — 2026-11-14

### What's new

- **Code generation uses less than half the memory.** Generating a large model (1,783 files) allocated 138 MB; it
  now allocates 64 MB. Every file was copied twice on its way to disk to make its line endings CRLF, although they
  already were; the code writer no longer builds a string for each line it writes; and it keeps the file in one
  buffer that it reuses without reallocating. On Linux and macOS the writer now ends lines with CRLF itself, so
  the files no longer need converting. The generated code is exactly the same, byte for byte, so there is nothing
  to Sync.

### Downloads

| Download | What |
|

---

## 1.2.1.7 — 2026-11-14

### What's new

- **Leaner code writer.** The part of the app that writes the C# no longer builds a new string for every level of
  indentation or for the type names in doc comments. The generated code is exactly the same as in 1.2.1.6, byte
  for byte, so there is nothing to Sync.

### Downloads

| Download | What |
|

---

## 1.2.1.6 — 2026-11-14

### Fixes

- **Your own code in generic types.** The header of `BatchEntry<TEntity>` and `StoredProcResult<TResult>` showed
  how to add your own code as `public partial class BatchEntry`, which declares a different class. It now shows
  `public partial class BatchEntry<TEntity>`.
- **Tidier generated code.** Properties document their value with `<value>` (it was `<returns>`, which tooltips
  ignore on properties), fields no longer get a `<returns>` comment, and base types, type parameters and trailing
  comments are spaced (`Base<A, B>, IThing`, `x = 0; // comment`).

Sync your solutions to get it; the SQL does not change.

### Downloads

| Download | What |
|

---

## 1.2.1.5 — 2026-11-14

### Fixes

- **Same code on every machine.** Parameter and variable names were lowered with the machine's language, so a
  computer set to Turkish generated `ByPK(int ıd)` instead of `ByPK(int id)` (and Sync rewrote those files). Names
  are now the same everywhere.
- **Clearer errors from the repositories.** A null argument now names its parameter (`ArgumentNullException`
  with the parameter name), a null `Original` or `Changed` in a batch update throws `ArgumentException` instead
  of `NullReferenceException`, and a list create that fails throws `InvalidOperationException` instead of
  `Exception`. Typos in the generated comments are fixed.

Sync your solutions to get it; the SQL does not change.

### Downloads

| Download | What |
|

---

## 1.2.1.4 — 2026-11-14

### What's new

- **Roomier popups.** Messages and questions are wider (up to 760 pixels, 720 when they show details such as paths
  or release notes), and their text scrolls with a scroll bar that stays visible, so the title and the buttons stay
  on screen however long the text is. The New model file and Update model wizards are wider too, and their steps
  scroll when the window is short.

### Downloads

| Download | What |
|

---

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
