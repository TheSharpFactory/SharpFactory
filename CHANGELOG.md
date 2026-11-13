# Changelog

What changed in each published release of The Sharp Factory, newest first. Each section is also the text of that
release on GitHub, with its downloads.

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
