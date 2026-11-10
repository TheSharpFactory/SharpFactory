# Testing The Sharp Factory

The generator has one hard rule: **its output does not change.** Apps built on the generated code must keep compiling and behaving the same after they regenerate. That includes the SQL the generated code runs. Every change to the generator, the schema reader or the model is checked against that rule with the two tools below.

Test material (models, databases, reference code) contains real table names, so it lives **outside this repository**. Nothing below needs to be committed.

## The tools

Both are in `Tools/` and are part of `SF.slnx`. Build them with the solution or on their own.

### SF.GenCheck: generated code must not change

```
dotnet run --project Tools/SF.GenCheck -- <model.edmj> <empty output folder> [reference folder]
```

- Generates every file for the model through the same generation order as Sync (`SF.Engine.GenerationRunner`), laid out by project (`<output>/<ProjectName>/...`, without the dependencies folder or project files).
- With a reference folder (the generated projects of a solution produced earlier, e.g. its `DataAccess` folder) it compares every generated file byte for byte and reports: identical, different, different only in line endings or BOM, and not in the reference.
- Exit code: `0` only when every file is byte-identical; `1` when anything differs; `2` for usage errors.
- Files that exist only in the reference are not reported: those are usually hand-written partial classes.

### SF.SchemaCheck: reading the database must not change

```
dotnet run --project Tools/SF.SchemaCheck -- <model.edmj> [model name ...]
```

- For each model in the `.edmj` (or only the ones named) it re-reads the selected tables, views, stored procedures, functions and sequences from the model's database, the same way the app's **Update** does, and compares them with the stored model: columns (type, size, nullability, identity, computed, defaults), primary and unique keys, navigation properties, foreign keys, procedure parameters and result columns, function parameters and returns, sequence types.
- Read-only on the database (but note: to discover a stored procedure's result columns the reader may execute the procedure when SQL Server cannot describe it, so point it at test databases only).
- Exit code: `0` when every model matches; `1` when anything differs or a database cannot be reached; `2` for usage errors.

## What to have ready

1. **A reference solution**: a solution generated earlier by a known-good version, with its `.edmj`. Make a copy of the `.edmj` and, if needed, change only its connection (server, database, authentication) in the copy. Never modify the reference solution itself.
2. **A test database** whose model exercises everything the reader supports. A good one is a structure-only copy of a real database plus an extra schema with:
   - every SQL Server column type, identity, computed and rowversion columns, defaults (including a `varbinary` default longer than 4,000 characters);
   - composite primary and unique keys, multi-column, self-referencing and one-to-one foreign keys, two foreign keys to the same table;
   - names that need cleaning (spaces, leading digits, a leading `_`), a table without a primary key, a trigger. Names may have only `A-Z`, `a-z`, `0-9`, spaces and `_` (schemas: no space and no leading digit); others are not supported and are left out;
   - views, scalar / inline-table / multi-statement-table functions, sequences;
   - stored procedures with and without parameters, with `OUTPUT` parameters, with and without result sets.
3. **That test database's model** (`.edmj`), built by the app (Add, select everything, Save), and **its generated code** saved once as the baseline (for example with `sharpfactory generate`).
4. **A database with names that need quoting** (optional): an empty test database (an apostrophe in its own name helps, e.g. `SharpFactoryQuote's`) where `Tests/SF.Tests/Fixtures/QuotedNames.sql` was run: a schema, table, column, default, procedure and function with apostrophes, and a procedure with `]` in its name. Set `SHARPFACTORY_QUOTED_NAMES_DB` to `server;database` and the engine tests read it through the schema reader.
5. **A database with only the Coverage schema** (optional, for the website's screenshots): an empty test database (e.g. `SharpFactoryDocs`) where `Tests/SF.Tests/Fixtures/Coverage.sql` was run with `sqlcmd -I`. With `SHARPFACTORY_DOCS_DB` set to `server;database` (use `localhost`, so no machine name appears) and `SF_SCREENSHOTS` set to a folder, `DocsScreenshotTests` walks the Add wizard and the Models page and saves the screens named like the images of the Model Generation and Updating The Model pages (the website's `SharpFactoryWebsite/images/documentation/gettingstarted`). Nothing in it is internal, so the images can be published as they are. The six wizard images on the website are cropped to the dialog (with a 20 px margin) so they stay readable at the docs column width; `model_details` and `model_code` are full windows.

### The automated tests

```
dotnet test Tests/SF.Tests/SF.Tests.csproj
dotnet test Tests/SF.App.Tests/SF.App.Tests.csproj
```

- `SF.Tests` (engine): Sync into new and existing `.sln`/`.slnx` in temporary folders, re-sync, preview, stale files, moving projects, project references, validation, the Sync target list and settings. It uses `Tests/SF.Tests/Fixtures/Coverage.edmj`, a model of the synthetic Coverage schema only.
- `SF.App.Tests` (app, headless): one test per group of the parity checklist in `PLAN.md`. Avalonia runs headless with Skia, so no window appears and nothing on the desktop is driven. Set `SF_SCREENSHOTS` to a folder to save a PNG of each screen. Set `SHARPFACTORY_TEST_EDMJ` to a model file built by the old app from a reachable test database to also run the database parity tests: they build and update that model through the new UI and require the saved `.edmj` to be identical.
- Both set `SHARPFACTORY_HOME` (or use temporary folders), so they never touch your own settings.

### The command line

```
dotnet run --project App/SF.Cli -- sync <model.edmj> <solution.sln|.slnx> [--preview]
dotnet run --project App/SF.Cli -- generate <model.edmj> <output folder>
```

`generate` writes the same files as GenCheck; `sync` does what the app's Sync does. To check another OS without installing .NET there, publish the command line self-contained (`dotnet publish App/SF.Cli -c Release -r linux-x64 --self-contained -o <folder outside the repo>`) and run it there, for example in WSL; its `generate` output must be byte-identical to the Windows reference.

## The routine after every change

1. Build everything with 0 errors: `dotnet build SF.slnx`.
   Generated code is never built or tested inside the repository (user, 2026-11-05). Inside the repository it picks up this repository's strict `.editorconfig` (for example CA1822 as an error) and fails in ways real solutions don't. Generate, compile and Sync-test it only in a folder outside the repository. The repository holds no generated code: the old generated sample (`DataAccess/`, `DbModels/`) moved out of the repository in 1.0.9.9 (`BACKLOG.md`, SF-048), and `SF.slnx` builds as a whole with 0 errors.
2. Reference solution: `SF.GenCheck <reference copy.edmj> <empty folder> <saved copy of the reference DataAccess folder>` must report every file identical (exit code 0). Since 1.1.2.2 the copy's seven SQL engine files are the new engine's (an agreed change, SF-120); the reference solution itself is never modified.
3. Test database, code: `SF.GenCheck <test.edmj> <empty folder> <saved baseline folder>` must report every file identical (exit code 0).
4. Test database, schema: `SF.SchemaCheck <test.edmj>` must report 0 differences (exit code 0).
5. Tests: `Tests/SF.Tests` and `Tests/SF.App.Tests` pass (with `SHARPFACTORY_TEST_EDMJ` set when the test database is available).
6. If the change touches the website: run it and check the pages and response headers.
7. If the change touches the generated SQL engine (`Dependencies/SF.CodeGenerator/Database*.cs`): run the engine check below.

CI (`.github/workflows/ci.yml`) builds and runs both test projects on Windows, Linux and macOS, generates the public test model on each, and fails when the generated files differ between them.

If a check fails, the change is not done: either fix it or, if the difference is intended, agree on it explicitly before replacing a baseline.

## The engine check (the SQL the generated code sends)

The SQL engine templates (`Dependencies/SF.CodeGenerator/Database*.cs`) build the SQL text the generated repositories send. Their output must not change: the same text means the same SQL Server query plans for the apps that use them. To change how the text is built (as in 1.1.2.2), keep a harness outside the repository that:

1. generates the Coverage model's code with the old and the new generator (`sharpfactory sync` into two folders) and compiles each into a small console app with `Microsoft.Data.SqlClient`;
2. creates a fresh database from `Tests/SF.Tests/Fixtures/Coverage.sql` for each run;
3. runs one fixed script through the public repository API: inserts, reads, updates, upserts and deletes for every kind of key (identity, composite, no identity, primary key that is a foreign key), every filter condition, IN lists, and combinations of filters, ordering, Top, paging and nav props (parent, child, nested, self-referencing);
4. records every command SqlClient sends (subscribe to its `DiagnosticListener`, `WriteCommandBefore`): the text, and each parameter's name, type, size, precision, scale, direction, nullability and value; and every result;
5. compares the two recordings line by line, masking only what the database generates (today's timestamps, `NEWID()` values). Any other difference fails the check.

In 1.1.2.2 the check covered 884 commands, 1,729 parameters and 848 results. The same harness measures the bytes allocated and the time per call (end to end against the local database, and the engine alone).

## Compiling the generated code

The generated projects target .NET 10 or .NET 11. The easiest check is a Sync into a new solution outside the repository (`sharpfactory sync <model.edmj> <folder>/App.slnx`), then add the NuGet packages below and `dotnet build` it. To compile a generated baseline folder instead, create one SDK project per generated project (target `net10.0`, `ImplicitUsings` and `Nullable` disabled, `EnforceCodeStyleInBuild` false as in the project files Sync writes), include the generated `.cs` files, add the references Sync adds between the projects, and add the packages `Microsoft.Data.SqlClient` (Repository) and `Microsoft.SqlServer.Types` (only for `geography`/`geometry` columns). Known case that does not compile: a table named the same as its schema (`BACKLOG.md`, SF-083, won't fix: avoid that naming).
