# The Sharp Factory

The Sharp Factory reads a SQL Server database's tables, views, stored procedures, functions and sequences and
generates the C# data access for them: entities, repositories, query composition (filtering, sorting,
pagination, navigation properties) and a repository container. Free and open source, forever.

It runs on **Windows, macOS and Linux**, as a desktop app and from the command line.

## The app

`App/SF.App` (Avalonia). Start a **new model file**, or open an `.edmj`:

- **Models**: add a model from a database (choose the objects), update it, browse it, and preview the code
  generated for any table, view, procedure or function.
- **Layers** and **Settings**: the project names and namespaces of the generated code.
- **Sync**: generate everything into a solution. Pick one that is open in Visual Studio, Rider or VS Code, one
  you synced to before, one next to the model file, any `.sln`/`.slnx`, or a new solution (`.slnx` by default).
  **Preview** shows every file Sync would add, change or delete, with a diff, before anything is written.

Sync creates the projects (in the dependencies solution folder), adds the references between them, writes only
the files whose content changed, and removes generated files the model no longer produces. Files you add to the
generated projects are never touched: only files that start with the Sharp Factory marker are.

## The command line

```
sharpfactory sync <model.edmj> <solution.sln|.slnx> [--preview]
sharpfactory generate <model.edmj> <output folder>
sharpfactory update <model.edmj> [--model <name>] [--check]
sharpfactory info <model.edmj>
sharpfactory --version
```

`update` reads each model's objects again from its database and saves the file (objects the database no longer
has drop out; new ones are not added). With `--check` it saves nothing and exits with 1 when a model would change,
so a build can tell that the model is behind the database. `--version` prints one line, such as
`sharpfactory 1.1.9.2`. Exit codes: 0 success, 1 failure, 2 usage error.

## Using the generated code

The generated projects target .NET 10 or .NET 11 (chosen on the Sync page). Add the `Microsoft.Data.SqlClient` NuGet package to the repository
project, and `Microsoft.SqlServer.Types` if the database has `geography` or `geometry` columns. The
documentation is at https://www.thesharpfactory.com/Documentation.

Point the app at development or test databases: to discover a stored procedure's result columns it may run the
procedure when SQL Server cannot describe it.

## Building

```
dotnet build SF.slnx
dotnet test Tests/SF.Tests/SF.Tests.csproj
dotnet test Tests/SF.App.Tests/SF.App.Tests.csproj
dotnet run --project App/SF.App
```

The database tests need SQL Server: `python .github/scripts/setup_test_databases.py` starts one in Docker with the
test databases (see `TESTING.md`). CI does the same on Linux, so every push runs them.

Requires the .NET 10 SDK. `TESTING.md` describes the regression checks (generated code must stay
byte-identical), `PLAN.md` the app's design, `BACKLOG.md` the open work, `RELEASE_NOTES.md` every change and
`CHANGELOG.md` what each published release brings.

## Repository

| Folder | What |
|---|---|
| `App/SF.App` | The desktop app (Avalonia) |
| `App/SF.Cli` | The command line (`sharpfactory`) |
| `Dependencies/SF.Engine` | Building models, the generation order, Sync, the Sync target list, settings |
| `Dependencies/SF.CodeGenerator`, `SF.CSharpGenerator` | The code generators |
| `Dependencies/SF.Db`, `SF.Utils`, `SF.Models` | The schema reader, the model and its `.edmj` file |
| `Tools/` | Regression tools: `SF.GenCheck`, `SF.SchemaCheck` |
| `Tests/` | Engine tests and headless UI tests |

The website, thesharpfactory.com, is a static site in its own repository, `TheSharpFactory/website` (private).

## Releases

Each release on GitHub has its notes and the downloads for Windows (x64, ARM64), Linux (x64) and macOS (x64, Apple
silicon): the app and the command line, self-contained, with `LICENSE`, `THIRD-PARTY-NOTICES.md` and a `licenses`
folder. The macOS app is not signed yet: the first time, right-click it and choose **Open**.

To publish one: add the version's section to `CHANGELOG.md` (the heading `## <VERSION> — <date>`), commit, and push
the tag `v<VERSION>`. `.github/workflows/release.yml` checks that the tag matches `VERSION`, builds everything and
creates the release with that section as its text.

## License

The Sharp Factory is written by Jonathan Alfaro (JAM) and released under the [MIT License](LICENSE). It includes
third-party code and icons under their own licenses: see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

The code The Sharp Factory generates for you is yours: use it, change it and license it however you like.
