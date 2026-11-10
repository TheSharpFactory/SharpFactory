# Changelog

What changed in each published release of The Sharp Factory, newest first. Each section is also the text of that
release on GitHub, with its downloads.

## 1.1.8.7 — 2026-11-10

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

Nothing to install: unpack and run. The .NET runtime is included. The macOS app is not signed yet: the first time,
right-click it and choose **Open**.

The generated code needs the `Microsoft.Data.SqlClient` NuGet package in the repository project, and
`Microsoft.SqlServer.Types` when the database has `geography` or `geometry` columns. Documentation:
https://www.thesharpfactory.com/Documentation
