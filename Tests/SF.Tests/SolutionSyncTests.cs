#region Usings
using SF.Engine;
using SF.Engine.Sync;
using SF.Models;
using SF.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
#endregion

namespace SF.Tests;

public sealed class SolutionSyncTests
{
    private const string _marker = "/*SFF*/";

    [Fact]
    public void NewSlnxGetsEveryProjectReferenceAndFile()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            var report = Sync(Fixtures.LoadCoverage(), solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.True(report.SolutionCreated);
            Assert.Equal(5, report.ProjectsCreated.Count);
            Assert.True(report.ReferencesAdded.Count > 0);
            Assert.True(File.Exists(solution));
            Assert.Contains("<Folder Name=\"/DataAccess/\">", File.ReadAllText(solution));

            var files = Directory.GetFiles(folder.Combine("DataAccess"), "*.cs", SearchOption.AllDirectories);
            Assert.Equal(report.CountFiles(SyncFileChangeKind.Added), files.Length);
            foreach(var file in files)
                AssertCrlfOnly(file);

            foreach(var project in report.ProjectsCreated)
            {
                var projectFile = folder.Combine("DataAccess", project, project + ".csproj");
                Assert.True(File.Exists(projectFile), projectFile);
                Assert.Contains("<EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>", File.ReadAllText(projectFile));
            }
        }
    }

    [Fact]
    public void OnlyTheCommonRepositoryProjectGetsTheSqlClientPackage()
    {
        using(var folder = new TestFolder())
        {
            var report = Sync(Fixtures.LoadCoverage(), folder.Combine("App.slnx"), false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Equal(5, report.ProjectsCreated.Count);
            foreach(var project in report.ProjectsCreated)
            {
                var text = File.ReadAllText(folder.Combine("DataAccess", project, project + ".csproj"));
                if(project == "SFTest.Repository")
                    Assert.Contains("<PackageReference Include=\"Microsoft.Data.SqlClient\" Version=\"7.1.1\" />", text);
                else
                    Assert.DoesNotContain("PackageReference", text);
            }
        }
    }

    [Fact]
    public void DotNet11ProjectsTargetNet11AndTheChoiceIsSaved()
    {
        using(var folder = new TestFolder())
        {
            var container = Fixtures.LoadCoverage();
            container.CommonSettings.TargetFramework = TargetFrameworks.NET11;
            ModelUtils.Save(container, folder.Combine("net11.edmj"));
            var reloaded = ModelUtils.Load(folder.Combine("net11.edmj"));
            Assert.Equal(TargetFrameworks.NET11, reloaded.CommonSettings.TargetFramework);

            var report = Sync(reloaded, folder.Combine("App.slnx"), false);
            Assert.True(report.Success, report.ErrorMessage);
            foreach(var project in report.ProjectsCreated)
                Assert.Contains("<TargetFramework>net11.0</TargetFramework>", File.ReadAllText(folder.Combine("DataAccess", project, project + ".csproj")));

            Assert.Equal(".NET 11", ProjectFileTemplate.GetDisplayName(TargetFrameworks.NET11));
        }
    }

    [Fact]
    public void AReadOnlyFileStopsSyncBeforeAnythingIsWritten()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Assert.True(Sync(Fixtures.LoadCoverage(), solution, false).Success);

            //a setting that changes every file, and one of them read-only
            var files = Directory.GetFiles(folder.Combine("DataAccess"), "*.cs", SearchOption.AllDirectories);
            var locked = files[files.Length - 1];
            var other = files[0];
            var otherBefore = File.ReadAllText(other);
            File.SetAttributes(locked, File.GetAttributes(locked) | FileAttributes.ReadOnly);
            try
            {
                var container = Fixtures.LoadCoverage();
                container.CommonSettings.IndentSize = 2;
                var report = Sync(container, solution, false);

                Assert.False(report.Success);
                Assert.StartsWith("Nothing was written. This file cannot be written: ", report.ErrorMessage);
                Assert.Contains(Path.GetFileName(locked) + " (read-only)", report.ErrorMessage);
                Assert.Equal(otherBefore, File.ReadAllText(other));
            }
            finally
            {
                File.SetAttributes(locked, FileAttributes.Normal);
            }
        }
    }

    [Fact]
    public void AHandWrittenFileAtAGeneratedPathIsNotOverwritten()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Assert.True(Sync(Fixtures.LoadCoverage(), solution, false).Success);

            //a hand-written file where the generator writes one
            var file = folder.Combine("DataAccess", "SFTest.Entity", "TestDb", "Coverage", "AllType.cs");
            var handWritten = "public class AllType { }\r\n";
            File.WriteAllText(file, handWritten);

            //a setting that would change every generated file
            var other = folder.Combine("DataAccess", "SFTest.Entity", "TestDb", "Coverage", "Region.cs");
            var otherBefore = File.ReadAllText(other);
            var container = Fixtures.LoadCoverage();
            container.CommonSettings.IndentSize = 2;

            //the preview says so; the file is not among the changes
            var preview = Sync(container, solution, true);
            Assert.True(preview.Success, preview.ErrorMessage);
            Assert.Single(preview.UnmarkedFiles);
            Assert.False(preview.IsUpToDate);

            var report = Sync(container, solution, false);

            Assert.False(report.Success);
            Assert.StartsWith("Nothing was written. This file cannot be written: ", report.ErrorMessage);
            Assert.Contains("(not a Sharp Factory file)", report.ErrorMessage);
            Assert.EndsWith("Move or rename the files that are not Sharp Factory files, close the others or clear read-only, then Sync again.", report.ErrorMessage);
            Assert.Equal(handWritten, File.ReadAllText(file));
            Assert.Equal(otherBefore, File.ReadAllText(other));
        }
    }

    [Fact]
    public void PathNamesThatEscapeTheSolutionStopSync()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");

            var container = Fixtures.LoadCoverage();
            container.Models[0].Name = "..\\x";
            var report = Sync(container, solution, false);
            Assert.False(report.Success);
            Assert.StartsWith("Nothing was written. These names cannot be used in file paths: ", report.ErrorMessage);
            Assert.False(File.Exists(solution));

            container = Fixtures.LoadCoverage();
            container.CommonSettings.DependenciesFolder = "C:\\x";
            report = Sync(container, solution, false);
            Assert.False(report.Success);
            Assert.StartsWith("Nothing was written. These names cannot be used in file paths: ", report.ErrorMessage);
            Assert.False(File.Exists(solution));

            container.CommonSettings.DependenciesFolder = "/x";
            Assert.Single(ModelValidation.ValidatePathNames(container));

            //no dependencies folder (the solution root) and a trailing separator are fine
            container.CommonSettings.DependenciesFolder = "";
            Assert.Empty(ModelValidation.ValidatePathNames(container));
            container.CommonSettings.DependenciesFolder = "src/Generated/";
            Assert.Empty(ModelValidation.ValidatePathNames(container));
        }
    }

    [Fact]
    public void SecondSyncChangesNothing()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            var first = Sync(Fixtures.LoadCoverage(), solution, false);
            var solutionText = File.ReadAllText(solution);
            var repositoryProject = folder.Combine("DataAccess", "SFTest.Repository", "SFTest.Repository.csproj");
            var projectText = File.ReadAllText(repositoryProject);

            var second = Sync(Fixtures.LoadCoverage(), solution, false);

            Assert.True(second.Success, second.ErrorMessage);
            Assert.True(second.IsUpToDate);
            Assert.Equal(first.FileChanges.Count, second.UnchangedFileCount);
            Assert.Equal(solutionText, File.ReadAllText(solution));
            Assert.Equal(projectText, File.ReadAllText(repositoryProject));
        }
    }

    [Fact]
    public void NewSlnHasConfigurationsAndReopens()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.sln");
            var report = Sync(Fixtures.LoadCoverage(), solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            var text = File.ReadAllText(solution);
            Assert.Contains("Debug|Any CPU", text);
            Assert.Contains("Release|Any CPU", text);

            var reopened = SolutionFile.OpenOrCreate(solution);
            Assert.NotNull(reopened.FindProject("SFTest.Entity"));
            Assert.Equal("DataAccess", reopened.FindProject("SFTest.Entity").TopSolutionFolder);
        }
    }

    [Fact]
    public void ExistingSlnKeepsItsProjects()
    {
        using(var folder = new TestFolder())
        {
            //a solution with one hand-made project at the root
            var appProject = folder.Combine("MyApp", "MyApp.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(appProject));
            File.WriteAllText(appProject, ProjectFileTemplate.Compose(TargetFrameworks.NET10, false));
            var solution = folder.Combine("App.sln");
            var start = SolutionFile.OpenOrCreate(solution);
            start.AddProject(appProject, null);
            start.Save();
            var before = File.ReadAllText(solution);

            var report = Sync(Fixtures.LoadCoverage(), solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            var after = SolutionFile.OpenOrCreate(solution);
            Assert.NotNull(after.FindProject("MyApp"));
            Assert.Null(after.FindProject("MyApp").TopSolutionFolder);
            Assert.NotNull(after.FindProject("SFTest.Repository"));
            Assert.Contains("\"MyApp\", \"MyApp\\MyApp.csproj\"", File.ReadAllText(solution));
            Assert.NotEqual(before, File.ReadAllText(solution));
        }
    }

    [Fact]
    public void PreviewWritesNothing()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            var report = Sync(Fixtures.LoadCoverage(), solution, true);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.True(report.IsPreview);
            Assert.Equal(5, report.ProjectsCreated.Count);
            Assert.True(report.CountFiles(SyncFileChangeKind.Added) > 0);
            Assert.False(File.Exists(solution));
            Assert.False(Directory.Exists(folder.Combine("DataAccess")));
            foreach(var change in report.FileChanges)
                Assert.NotNull(change.NewContent);
        }
    }

    [Fact]
    public void PreviewShowsChangedFilesWithBothContents()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            var container = Fixtures.LoadCoverage();
            container.Models[0].Layers.Entity.Namespace = "Renamed";
            var report = Sync(container, solution, true);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.True(report.CountFiles(SyncFileChangeKind.Changed) > 0);
            foreach(var change in report.FileChanges)
            {
                if(change.Kind != SyncFileChangeKind.Changed)
                    continue;

                Assert.NotNull(change.OldContent);
                Assert.NotNull(change.NewContent);
                Assert.NotEqual(change.OldContent, change.NewContent);
                Assert.Equal(change.OldContent, File.ReadAllText(change.FullPath));
            }
        }
    }

    [Fact]
    public void StaleSharpFactoryFilesAreDeletedAndUserFilesKept()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            var entityFolder = folder.Combine("DataAccess", "SFTest.Entity", "TestDb", "Coverage");
            var stale = Path.Combine(entityFolder, "Gone.cs");
            var userFile = Path.Combine(entityFolder, "MyPartial.cs");
            var rootFile = folder.Combine("DataAccess", "SFTest.Entity", "RootFile.cs");
            var binFile = folder.Combine("DataAccess", "SFTest.Entity", "bin", "Old.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(binFile));
            File.WriteAllText(stale, _marker + "\r\nclass Gone {}\r\n");
            File.WriteAllText(userFile, "public partial class Mine {}\r\n");
            File.WriteAllText(rootFile, _marker + "\r\nclass Root {}\r\n");
            File.WriteAllText(binFile, _marker + "\r\nclass Old {}\r\n");

            var report = Sync(Fixtures.LoadCoverage(), solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Equal(1, report.CountFiles(SyncFileChangeKind.Deleted));
            Assert.False(File.Exists(stale));
            Assert.True(File.Exists(userFile));
            Assert.True(File.Exists(rootFile));
            Assert.True(File.Exists(binFile));
        }
    }

    [Fact]
    public void RemovedTableFilesAreDeleted()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            var container = Fixtures.LoadCoverage();
            var schema = container.Models[0].Categories.GetByName(CategoryNames.Entities).Schemas.GetByName("Coverage");
            schema.Entities.RemoveAt(0);
            var report = Sync(container, solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.True(report.CountFiles(SyncFileChangeKind.Deleted) >= 4);
        }
    }

    [Fact]
    public void ChangingTheDependenciesFolderMovesTheProjects()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            var container = Fixtures.LoadCoverage();
            container.CommonSettings.DependenciesFolder = "Generated";
            var report = Sync(container, solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Equal(5, report.ProjectsMoved.Count);
            Assert.False(Directory.Exists(folder.Combine("DataAccess", "SFTest.Entity")));
            Assert.True(File.Exists(folder.Combine("Generated", "SFTest.Entity", "SFTest.Entity.csproj")));
            Assert.Equal("Generated", SolutionFile.OpenOrCreate(solution).FindProject("SFTest.Entity").TopSolutionFolder);

            //the references between moved projects still point at the right place
            var repository = File.ReadAllText(folder.Combine("Generated", "SFTest.Repository", "SFTest.Repository.csproj"));
            Assert.Contains("..\\SFTest.Entity\\SFTest.Entity.csproj", repository);

            var third = Sync(container, solution, false);
            Assert.True(third.IsUpToDate);
        }
    }

    /// <summary>
    /// What happened in production (1.2.0.9): a solution that uses another solution's generated projects (the data
    /// access of a shared repository, in a solution folder of its own) was synced. Sync moved those projects into
    /// its own folder, rewired every reference to them and so deleted them from the other solution. Now they are
    /// used where they are: written there, never moved, their project files and the solution's own ones untouched.
    /// </summary>
    [Fact]
    public void AnotherSolutionsGeneratedProjectsAreUsedWhereTheyAre()
    {
        using(var folder = new TestFolder())
        {
            //the shared repository: its own solution, synced
            var owner = folder.Combine("Shared", "Shared.slnx");
            Assert.True(Sync(Fixtures.LoadCoverage(), owner, false).Success);
            var sharedEntity = folder.Combine("Shared", "DataAccess", "SFTest.Entity");
            var sharedProjectFile = Path.Combine(sharedEntity, "SFTest.Entity.csproj");
            var sharedProjectBytes = File.ReadAllBytes(sharedProjectFile);
            //a generated file the model no longer has: in another solution's project it is not deleted
            var oldFile = Path.Combine(sharedEntity, "TestDb", "Old.cs");
            File.WriteAllText(oldFile, _marker + "\r\nclass Old {}\r\n");

            //the other solution: its own project (with a BOM) references the shared repository project; the shared
            //projects are in a solution folder named after the repository, not the model's "DataAccess"
            var app = folder.Combine("App", "App.slnx");
            var appProject = folder.Combine("App", "Web", "Web.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(appProject));
            var appProjectText = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"..\\..\\Shared\\DataAccess\\SFTest.Repository\\SFTest.Repository.csproj\" />\r\n  </ItemGroup>\r\n</Project>\r\n";
            File.WriteAllText(appProject, appProjectText, new UTF8Encoding(true));
            var appProjectBytes = File.ReadAllBytes(appProject);
            var solution = SolutionFile.OpenOrCreate(app);
            solution.AddProject(appProject, null);
            foreach(var project in SolutionFile.OpenOrCreate(owner).GetProjects())
                solution.AddProject(project.ProjectFile, "Shared");

            solution.Save();

            var preview = Sync(Fixtures.LoadCoverage(), app, true);
            Assert.True(preview.Success, preview.ErrorMessage);
            Assert.Empty(preview.ProjectsMoved);
            Assert.Equal(5, preview.ProjectsOutsideSolution.Count);

            var report = Sync(Fixtures.LoadCoverage(), app, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Empty(report.ProjectsMoved);
            Assert.Empty(report.ProjectsCreated);
            Assert.Empty(report.ReferencesUpdated);
            Assert.Empty(report.ProjectFilesChanged);
            Assert.Equal(0, report.CountFiles(SyncFileChangeKind.Deleted));
            Assert.Equal(5, report.ProjectsOutsideSolution.Count);

            //nothing moved into the app's folder; the shared projects are where they were, unchanged
            Assert.False(Directory.Exists(folder.Combine("App", "DataAccess")));
            Assert.Equal(sharedProjectBytes, File.ReadAllBytes(sharedProjectFile));
            Assert.True(File.Exists(oldFile));
            Assert.Equal("Shared", SolutionFile.OpenOrCreate(app).FindProject("SFTest.Entity").TopSolutionFolder);
            //the app's own project file: byte for byte, BOM included
            Assert.Equal(appProjectBytes, File.ReadAllBytes(appProject));

            //the shared solution syncs as before: in its own projects the stale file is deleted, nothing else changes
            var ownerReport = Sync(Fixtures.LoadCoverage(), owner, false);
            Assert.True(ownerReport.Success, ownerReport.ErrorMessage);
            Assert.Equal(oldFile, Assert.Single(ownerReport.FileChanges).FullPath);
            Assert.False(File.Exists(oldFile));
            Assert.Empty(ownerReport.ProjectsOutsideSolution);
        }
    }

    [Fact]
    public void AProjectFileWithABomKeepsIt()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            //a reference removed by hand, in a project file saved with a BOM (Visual Studio does that)
            var projectFile = folder.Combine("DataAccess", "SFTest.Repository", "SFTest.Repository.csproj");
            var text = File.ReadAllText(projectFile);
            var start = text.IndexOf("    <ProjectReference Include=\"..\\SFTest.Entity\\SFTest.Entity.csproj\" />\r\n", System.StringComparison.Ordinal);
            Assert.True(start >= 0, text);
            text = text.Remove(start, "    <ProjectReference Include=\"..\\SFTest.Entity\\SFTest.Entity.csproj\" />\r\n".Length);
            File.WriteAllText(projectFile, text, new UTF8Encoding(true));

            var report = Sync(Fixtures.LoadCoverage(), solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Contains("SFTest.Repository -> SFTest.Entity", report.ReferencesAdded);
            var bytes = File.ReadAllBytes(projectFile);
            Assert.True(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "the BOM was dropped");
            Assert.Contains("..\\SFTest.Entity\\SFTest.Entity.csproj", File.ReadAllText(projectFile));
        }
    }

    [Fact]
    public void MovingIntoAFolderThatIsNotEmptyFails()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);
            var blocker = folder.Combine("Generated", "SFTest.Query", "keep.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(blocker));
            File.WriteAllText(blocker, "user data");

            var container = Fixtures.LoadCoverage();
            container.CommonSettings.DependenciesFolder = "Generated";
            var report = Sync(container, solution, false);

            Assert.False(report.Success);
            Assert.Contains("not empty", report.ErrorMessage);
            Assert.True(File.Exists(blocker));
        }
    }

    [Fact]
    public void SyncWritesTheSameFilesAsGenerate()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            var output = folder.Combine("generate");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);

            var generated = Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories);
            Assert.True(generated.Length > 100);
            foreach(var file in generated)
            {
                var relative = Path.GetRelativePath(output, file);
                var synced = folder.Combine("DataAccess", relative);
                Assert.True(File.Exists(synced), relative);
                Assert.Equal(File.ReadAllBytes(file), File.ReadAllBytes(synced));
            }
        }
    }

    [Fact]
    public void AProjectFileOnDiskIsKeptWhenSyncingIntoAnotherSolution()
    {
        using(var folder = new TestFolder())
        {
            Sync(Fixtures.LoadCoverage(), folder.Combine("App.sln"), false);
            var repositoryProject = folder.Combine("DataAccess", "SFTest.Repository", "SFTest.Repository.csproj");
            var edited = File.ReadAllText(repositoryProject).Replace("</Project>", "  <PropertyGroup>\r\n    <Description>Edited by hand</Description>\r\n  </PropertyGroup>\r\n\r\n</Project>");
            File.WriteAllText(repositoryProject, edited);

            //a second solution next to the first one: the projects exist on disk but not in it
            var report = Sync(Fixtures.LoadCoverage(), folder.Combine("App.slnx"), false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Empty(report.ProjectsCreated);
            Assert.Equal(5, report.ExistingProjectsAdded.Count);
            Assert.Equal(edited, File.ReadAllText(repositoryProject));
            Assert.NotNull(SolutionFile.OpenOrCreate(folder.Combine("App.slnx")).FindProject("SFTest.Repository"));
            Assert.True(Sync(Fixtures.LoadCoverage(), folder.Combine("App.slnx"), false).IsUpToDate);
        }
    }

    [Fact]
    public void ANestedDependenciesFolderIsUpToDateOnTheSecondSync()
    {
        using(var folder = new TestFolder())
        {
            var container = Fixtures.LoadCoverage();
            container.CommonSettings.DependenciesFolder = "src/Generated";
            var solution = folder.Combine("App.slnx");

            var first = Sync(container, solution, false);
            var second = Sync(container, solution, false);

            Assert.True(first.Success, first.ErrorMessage);
            Assert.True(File.Exists(folder.Combine("src", "Generated", "SFTest.Entity", "SFTest.Entity.csproj")));
            Assert.Contains("<Folder Name=\"/src/Generated/\">", File.ReadAllText(solution));
            Assert.True(second.IsUpToDate);
            Assert.Empty(second.ProjectsMoved);
        }
    }

    [Fact]
    public void MovingAProjectFixesTheUsersOwnReferencesToIt()
    {
        using(var folder = new TestFolder())
        {
            var solution = folder.Combine("App.slnx");
            Sync(Fixtures.LoadCoverage(), solution, false);

            //the user's own project, referencing a generated one
            var appProject = folder.Combine("MyApp", "MyApp.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(appProject));
            File.WriteAllText(appProject, "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"..\\DataAccess\\SFTest.Repository\\SFTest.Repository.csproj\" />\r\n  </ItemGroup>\r\n</Project>\r\n");
            var start = SolutionFile.OpenOrCreate(solution);
            start.AddProject(appProject, null);
            start.Save();

            var container = Fixtures.LoadCoverage();
            container.CommonSettings.DependenciesFolder = "Generated";
            var report = Sync(container, solution, false);

            Assert.True(report.Success, report.ErrorMessage);
            Assert.Contains("MyApp -> SFTest.Repository", report.ReferencesUpdated);
            var app = File.ReadAllText(appProject);
            Assert.Contains("..\\Generated\\SFTest.Repository\\SFTest.Repository.csproj", app);
            Assert.DoesNotContain("DataAccess", app);
        }
    }

    [Fact]
    public void GenerationPutsBackTheGeneratorsFileWriter()
    {
        using(var folder = new TestFolder())
        {
            var before = CodeGenUtils.FileWriter;
            Sync(Fixtures.LoadCoverage(), folder.Combine("App.slnx"), true);
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(folder.Combine("out")), NullProgressLog.Instance);

            Assert.Equal(before, CodeGenUtils.FileWriter);
        }
    }

    private static SyncReport Sync(ModelContainer container, string solution, bool preview)
    {
        return SolutionSync.Run(new SyncRequest { ModelContainer = container, SolutionPath = solution, Preview = preview }, NullProgressLog.Instance);
    }

    private static void AssertCrlfOnly(string file)
    {
        var bytes = File.ReadAllBytes(file);
        for(var i = 0; i < bytes.Length; i++)
        {
            if(bytes[i] == (byte)'\n')
                Assert.True(i > 0 && bytes[i - 1] == (byte)'\r', $"lone LF in {file}");
            else if(bytes[i] == (byte)'\r')
                Assert.True(i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n', $"lone CR in {file}");
        }
    }
}
