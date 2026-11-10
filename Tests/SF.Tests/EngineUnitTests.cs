#region Usings
using SF.Common.Logging;
using SF.Db;
using SF.Engine;
using SF.Engine.Settings;
using SF.Engine.Sync;
using SF.Engine.Targets;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

public sealed class EngineUnitTests
{
    [Fact]
    public void ToCrlfNormalizesEveryLineEnding()
    {
        Assert.Equal("a\r\nb\r\nc\r\nd", IOUtils.ToCrlf("a\nb\r\nc\rd"));
        Assert.Equal("", IOUtils.ToCrlf(""));
        Assert.Null(IOUtils.ToCrlf(null));
    }

    [Fact]
    public void ProjectReferencesAreAddedOnceAndFixed()
    {
        var content = ProjectFileTemplate.Compose(TargetFrameworks.NET10, false);

        content = ProjectFileEditor.SetProjectReference(content, "B", "..\\B\\B.csproj", out var added);
        Assert.True(added);
        content = ProjectFileEditor.SetProjectReference(content, "C", "..\\C\\C.csproj", out added);
        Assert.True(added);
        content = ProjectFileEditor.SetProjectReference(content, "b", "..\\B\\B.csproj", out var again);
        Assert.False(again);

        var references = ProjectFileEditor.GetProjectReferences(content);
        Assert.Equal(2, references.Count);
        Assert.Equal(1, CountOccurrences(content, "<ItemGroup>"));
        Assert.Contains("    <ProjectReference Include=\"..\\C\\C.csproj\" />\r\n  </ItemGroup>", content);

        content = ProjectFileEditor.SetProjectReference(content, "B", "..\\..\\Moved\\B\\B.csproj", out var fixedPath);
        Assert.True(fixedPath);
        Assert.Contains("..\\..\\Moved\\B\\B.csproj", content);
        Assert.Equal(2, ProjectFileEditor.GetProjectReferences(content).Count);
    }

    [Fact]
    public void ReferencesInsideCommentsDoNotCount()
    {
        var content = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <!-- <ProjectReference Include=\"..\\B\\B.csproj\" /> -->\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"..\\C\\C.csproj\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

        Assert.Single(ProjectFileEditor.GetProjectReferences(content));
        var updated = ProjectFileEditor.SetProjectReference(content, "B", "..\\B\\B.csproj", out var changed);

        Assert.True(changed);
        Assert.Equal(2, ProjectFileEditor.GetProjectReferences(updated).Count);
        Assert.Contains("    <ProjectReference Include=\"..\\C\\C.csproj\" />\r\n    <ProjectReference Include=\"..\\B\\B.csproj\" />\r\n  </ItemGroup>", updated);
    }

    [Fact]
    public void RecentSolutionListsAreRead()
    {
        var vs = "<content><collection name=\"CodeContainers.Offline\"><value name=\"value\">[{&quot;Key&quot;:&quot;D:\\\\Code\\\\App.sln&quot;,&quot;Value&quot;:{&quot;LocalProperties&quot;:{&quot;FullPath&quot;:&quot;D:\\\\Code\\\\App.sln&quot;}}},{&quot;Key&quot;:&quot;D:\\\\Code\\\\Folder&quot;,&quot;Value&quot;:{&quot;LocalProperties&quot;:{&quot;FullPath&quot;:&quot;D:\\\\Code\\\\Folder&quot;}}}]</value></collection></content>";
        Assert.Equal(new List<string> { "D:\\Code\\App.sln" }, RecentSolutionsReader.ParseVisualStudioSettings(vs));

        var rider = "<application><component name=\"RiderRecentProjectsManager\"><option name=\"additionalInfo\"><map><entry key=\"$USER_HOME$/src/Shop/Shop.slnx\"><value /></entry></map></option></component></application>";
        var fromRider = RecentSolutionsReader.ParseRiderSettings(rider, "home");
        Assert.Single(fromRider);
        Assert.Equal(Path.Combine("home", "src", "Shop", "Shop.slnx"), fromRider[0]);
    }

    [Fact]
    public void AnIdeShowingOnlyANameIsFoundThroughItsRecentSolutions()
    {
        using(var folder = new TestFolder())
        {
            var modelFile = folder.Combine("Models", "Model.edmj");
            Directory.CreateDirectory(Path.GetDirectoryName(modelFile));
            File.WriteAllText(modelFile, "{}");
            var elsewhere = folder.Combine("Elsewhere", "Shop.sln");
            Directory.CreateDirectory(Path.GetDirectoryName(elsewhere));
            File.WriteAllText(elsewhere, "");

            //Visual Studio opened from its start window: only the title names the solution
            var hints = new List<OpenSolutionHint> { new OpenSolutionHint { IdeName = "Visual Studio", SolutionName = "Shop" } };
            var candidates = TargetSolutionFinder.Find(modelFile, new AppSettings(), hints, new List<string> { elsewhere });

            Assert.Single(candidates);
            Assert.Equal(elsewhere, candidates[0].SolutionPath);
            Assert.Equal("Visual Studio", candidates[0].OpenIn);
        }
    }

    [Fact]
    public void AFailedConnectionSaysWhy()
    {
        //nothing listens on port 1: SQL Server's client fails at once with its reason
        var conInfo = new ConnectionInfo { Server = "127.0.0.1,1", Database = "master", DisableIntegratedSecurity = true, User = "nobody", Password = "nothing" };

        Assert.False(DbModelUtils.ValidateConnectionInfo(conInfo, out var reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void RunningAProcedureToFindItsColumnsIsLogged()
    {
        var reference = Environment.GetEnvironmentVariable("SHARPFACTORY_TEST_EDMJ");
        if(string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
            Assert.Skip("SHARPFACTORY_TEST_EDMJ is not set to the test database's model file.");

        using(var folder = new TestFolder())
        {
            LogManager.Enable(folder.Path);
            var model = ModelUtils.Load(reference).Models[0];

            //Coverage.UsesTempTable fills a temporary table, which SQL Server cannot describe without running it
            var columns = SqlConnector.GetRoutineReturnTypeColumns(model.ConInfo, "Coverage", "UsesTempTable", 0, false);

            Assert.NotNull(columns);
            var warnings = Path.Combine(LogManager.LogFolder, "Warning");
            Assert.True(Directory.Exists(warnings));
            var logged = false;
            foreach(var file in Directory.GetFiles(warnings, "*.txt", SearchOption.AllDirectories))
            {
                if(File.ReadAllText(file).Contains("[Coverage].[UsesTempTable]", StringComparison.Ordinal))
                    logged = true;
            }

            Assert.True(logged);
        }
    }

    [Fact]
    public void AnUpdateDropsWhatTheDatabaseNoLongerHasAndKeepsTheModelInPlace()
    {
        var reference = Environment.GetEnvironmentVariable("SHARPFACTORY_TEST_EDMJ");
        if(string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
            Assert.Skip("SHARPFACTORY_TEST_EDMJ is not set to the test database's model file.");

        //the Coverage model reads the same test database, through the reference file's connection (the build
        //machine's server is not localhost); one table is renamed to a name the database does not have
        var container = Fixtures.LoadCoverage();
        var model = container.Models[0];
        model.ConInfo = ModelUtils.Load(reference).Models[0].ConInfo;
        var tables = model.Categories.GetByName(CategoryNames.Entities);
        var renamed = tables.Schemas[0].Entities[0];
        var schemaName = tables.Schemas[0].Name;
        renamed.SN = "NoSuchTable";
        var tableCount = tables.Count;

        //a second model after it: the updated one must stay first
        var second = model.Clone();
        second.Name = "Second";
        container.Models.Add(second);

        var result = ModelUpdater.Update(container, model, new NullProgressLog());

        Assert.True(result.IsChanged);
        Assert.Equal(new List<string> { $"Tables: {schemaName}.NoSuchTable" }, result.Removed);
        Assert.Same(result.Model, container.Models[0]);
        Assert.Same(second, container.Models[1]);
        Assert.Equal(tableCount - 1, result.Model.Categories.GetByName(CategoryNames.Entities).Count);
        Assert.DoesNotContain($"Tables: {schemaName}.NoSuchTable", ModelUpdater.ObjectNames(result.Model));
    }

    [Fact]
    public void ReplacingAModelKeepsItsPlace()
    {
        var container = Fixtures.LoadCoverage();
        var first = container.Models[0];
        var second = first.Clone();
        second.Name = "Second";
        container.Models.Add(second);

        var updated = first.Clone();
        ModelBuilder.ReplaceModel(container, first, updated);

        Assert.Equal(2, container.Models.Count);
        Assert.Same(updated, container.Models[0]);
        Assert.Same(second, container.Models[1]);
    }

    [Fact]
    public void ListComparisonCountsRepeatedItems()
    {
        Assert.Empty(ListComparison.Describe("cols", new[] { "a", "b" }, new[] { "a", "b" }));
        Assert.Empty(ListComparison.Describe("cols", null, new string[0]));
        Assert.Equal(new List<string> { "cols: same items, different order" }, ListComparison.Describe("cols", new[] { "a", "b" }, new[] { "b", "a" }));

        //a repeated item is a real difference, not a different order
        var repeated = ListComparison.Describe("cols", new[] { "a", "a", "b" }, new[] { "a", "b" });
        Assert.Equal(new List<string> { "cols: only in the model    -> a" }, repeated);

        var both = ListComparison.Describe("cols", new[] { "a", "x" }, new[] { "a", "y", "y" });
        Assert.Equal(new List<string> { "cols: only in the model    -> x", "cols: only in the database -> y", "cols: only in the database -> y" }, both);
    }

    [Fact]
    public void ACloneOfAModelSavesExactlyLikeTheModel()
    {
        using(var folder = new TestFolder())
        {
            var container = Fixtures.LoadCoverage();
            var model = container.Models[0];
            ModelUtils.Save(container, folder.Combine("original.edmj"));

            var clone = model.Clone();
            container.Models.Remove(model);
            container.Models.Add(clone);
            ModelUtils.Save(container, folder.Combine("clone.edmj"));

            Assert.NotSame(model, clone);
            Assert.Equal(model.FKeys.Count, clone.FKeys.Count);
            Assert.Equal(File.ReadAllText(folder.Combine("original.edmj")), File.ReadAllText(folder.Combine("clone.edmj")));
        }
    }

    [Fact]
    public void ProjectNamesComeFromEitherSeparator()
    {
        Assert.Equal("A.B", ProjectFileEditor.GetProjectName("..\\A.B\\A.B.csproj"));
        Assert.Equal("A.B", ProjectFileEditor.GetProjectName("../A.B/A.B.csproj"));
    }

    [Fact]
    public void ReferenceRulesNeverReferenceAProjectItself()
    {
        var container = Fixtures.LoadCoverage();
        var pairs = ProjectReferenceRules.GetReferences(container, container.Models[0]);

        Assert.NotEmpty(pairs);
        foreach(var pair in pairs)
            Assert.False(string.Equals(pair.ProjectName, pair.ReferencedProjectName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidationRulesMatchTheOldApp()
    {
        var container = Fixtures.LoadCoverage();
        var conInfo = new ConnectionInfo { Server = "", Database = "", DisableIntegratedSecurity = true };

        var errors = ModelValidation.ValidateDbInfo(container, container.Models[0].Name, conInfo, true);
        Assert.Contains(ModelValidation.ModelNameExists, errors);
        Assert.Contains(ModelValidation.ServerRequired, errors);
        Assert.Contains(ModelValidation.DatabaseRequired, errors);
        Assert.Contains("User is required.", errors);
        Assert.Contains("Password is required.", errors);

        Assert.NotNull(ModelValidation.ValidateModelNameAgainstRootNamespace(container, container.CommonSettings.RootNamespace));
        Assert.Null(ModelValidation.ValidateModelNameAgainstRootNamespace(container, "Other"));
        Assert.Equal(ModelValidation.IndentSizeInvalid, ModelValidation.ValidateIndentSize("0", out _));
        Assert.Null(ModelValidation.ValidateIndentSize("4", out var indent));
        Assert.Equal(4, indent);
        Assert.Equal("My_Root", ModelValidation.CleanName("  My Root "));
    }

    [Fact]
    public void NewContainerHasTheDefaults()
    {
        var container = ModelBuilder.CreateContainer(" Acme App ", " Dependencies ", 4, true);

        Assert.Equal("Acme_App", container.CommonSettings.RootNamespace);
        Assert.Equal("Dependencies", container.CommonSettings.DependenciesFolder);
        Assert.Equal(YesOrNo.Yes, container.CommonSettings.UseRootNSInProjectName);
        Assert.NotNull(container.CommonLayers.Query);
        Assert.NotNull(container.CommonLayers.RepoContainer);
    }

    [Fact]
    public void NotUsingTheRootNamespaceInProjectNamesIsSaved()
    {
        using(var folder = new TestFolder())
        {
            var container = Fixtures.LoadCoverage();
            container.CommonSettings.UseRootNSInProjectName = YesOrNo.No;
            ModelUtils.Save(container, folder.Combine("no.edmj"));
            Assert.Equal(YesOrNo.No, ModelUtils.Load(folder.Combine("no.edmj")).CommonSettings.UseRootNSInProjectName);

            //a file without the setting reads as Yes
            File.WriteAllText(folder.Combine("missing.edmj"), File.ReadAllText(Fixtures.CoverageModelFile).Replace("\"UseRootNSInProjectName\": 1,", ""));
            Assert.Equal(YesOrNo.Yes, ModelUtils.Load(folder.Combine("missing.edmj")).CommonSettings.UseRootNSInProjectName);
        }
    }

    [Fact]
    public void SavingAModelTwiceLeavesNoTemporaryFile()
    {
        using(var folder = new TestFolder())
        {
            var file = folder.Combine("twice.edmj");
            var container = Fixtures.LoadCoverage();
            ModelUtils.Save(container, file);
            ModelUtils.Save(container, file);

            Assert.Single(Directory.GetFiles(folder.Path));
            Assert.NotNull(ModelUtils.Load(file));
        }
    }

    [Fact]
    public void SavingAFileAnotherProgramHoldsOpenStillWorks()
    {
        using(var folder = new TestFolder())
        {
            var file = folder.Combine("open.txt");
            File.WriteAllText(file, "old");

            //open for reading and writing by others, but not for deleting: Windows cannot replace it
            using(var other = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IOUtils.SaveFile(file, "new");
            }

            Assert.Equal("new", File.ReadAllText(file));
            Assert.Single(Directory.GetFiles(folder.Path));
        }
    }

    [Fact]
    public void CommandLinesSplitOnQuotes()
    {
        var parts = RunningIdeScanner.SplitCommandLine("\"C:\\Program Files\\VS\\devenv.exe\" \"D:\\My Code\\App.sln\" /nosplash");

        Assert.Equal(3, parts.Count);
        Assert.Equal("D:\\My Code\\App.sln", parts[1]);
        Assert.True(RunningIdeScanner.IsSolutionFile(parts[1]));
        Assert.False(RunningIdeScanner.IsSolutionFile("App.csproj"));
    }

    [Fact]
    public void TargetListPutsOpenThenSyncedThenNearby()
    {
        using(var folder = new TestFolder())
        {
            var modelFile = folder.Combine("Models", "Model.edmj");
            Directory.CreateDirectory(Path.GetDirectoryName(modelFile));
            File.WriteAllText(modelFile, "{}");
            var nearby = folder.Combine("Nearby.slnx");
            var synced = folder.Combine("Elsewhere", "Synced.sln");
            var open = folder.Combine("Elsewhere", "Open.slnx");
            File.WriteAllText(nearby, "<Solution />");
            Directory.CreateDirectory(Path.GetDirectoryName(synced));
            File.WriteAllText(synced, "");
            File.WriteAllText(open, "<Solution />");

            var settings = new AppSettings();
            AppSettingsStore.RecordSyncTarget(settings, modelFile, synced);
            AppSettingsStore.RecordSyncTarget(settings, modelFile, open);
            var hints = new List<OpenSolutionHint> { new OpenSolutionHint { IdeName = "Visual Studio", SolutionName = "Open" } };

            var candidates = TargetSolutionFinder.Find(modelFile, settings, hints, new List<string>());

            Assert.Equal(3, candidates.Count);
            Assert.Equal("Open", candidates[0].Name);
            Assert.Equal("Visual Studio", candidates[0].OpenIn);
            Assert.Equal("Synced", candidates[1].Name);
            Assert.Equal("Nearby", candidates[2].Name);
            Assert.True(candidates[2].NearModel);
            Assert.Contains("Open in Visual Studio", candidates[0].Description);
        }
    }

    [Fact]
    public void SettingsRoundTripInTheirOwnFolder()
    {
        using(var folder = new TestFolder())
        {
            var previous = Environment.GetEnvironmentVariable("SHARPFACTORY_HOME");
            Environment.SetEnvironmentVariable("SHARPFACTORY_HOME", folder.Path);
            try
            {
                var settings = AppSettingsStore.Load();
                Assert.Empty(settings.RecentFiles);

                AppSettingsStore.AddRecentFile(settings, folder.Combine("a.edmj"));
                AppSettingsStore.AddRecentFile(settings, folder.Combine("b.edmj"));
                AppSettingsStore.AddRecentFile(settings, folder.Combine("a.edmj"));
                AppSettingsStore.Save(settings);

                var loaded = AppSettingsStore.Load();
                Assert.Equal(2, loaded.RecentFiles.Count);
                Assert.Equal(folder.Combine("a.edmj"), loaded.RecentFiles[0]);
                Assert.True(File.Exists(Path.Combine(folder.Path, "Config", "settings.json")));

                //lists written as null read as empty
                File.WriteAllText(Path.Combine(folder.Path, "Config", "settings.json"), "{\"RecentFiles\":null,\"SyncTargets\":null,\"PaneWidths\":null}");
                var nulls = AppSettingsStore.Load();
                Assert.Empty(nulls.RecentFiles);
                Assert.Empty(nulls.SyncTargets);
                Assert.Empty(nulls.PaneWidths);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SHARPFACTORY_HOME", previous);
            }
        }
    }

    [Fact]
    public void ABrokenModelFileSaysWhy()
    {
        using(var folder = new TestFolder())
        {
            File.WriteAllText(folder.Combine("garbage.edmj"), "not json");
            File.WriteAllText(folder.Combine("empty.edmj"), "");
            File.WriteAllText(folder.Combine("partial.edmj"), "{\"Models\":[{\"Name\":\"X\"}]}");

            Assert.Null(ModelFileCheck.Load(folder.Combine("garbage.edmj"), out var garbage));
            Assert.StartsWith("It is not a model file: ", garbage);
            Assert.Null(ModelFileCheck.Load(folder.Combine("empty.edmj"), out var empty));
            Assert.Equal("The file is empty or is not a model file.", empty);
            Assert.Null(ModelFileCheck.Load(folder.Combine("partial.edmj"), out var partial));
            Assert.Equal("It has no common settings.", partial);

            //a model without its sequences category gets it back, empty
            var container = Fixtures.LoadCoverage();
            var categories = container.Models[0].Categories;
            categories.Remove(categories.GetByName(CategoryNames.Seqs));
            Assert.False(categories.Contains(CategoryNames.Seqs));
            Assert.Null(ModelFileCheck.Check(container));
            Assert.Equal(0, categories.GetByName(CategoryNames.Seqs).Count);

            //and one without a connection cannot be used
            container.Models[0].ConInfo = null;
            Assert.Equal($"Model {container.Models[0].Name} has no server and database.", ModelFileCheck.Check(container));

            //the real model files pass
            Assert.Null(ModelFileCheck.Check(Fixtures.LoadCoverage()));
        }
    }

    [Fact]
    public void ASyncedModelTurnsSyncOff()
    {
        var container = Fixtures.LoadCoverage();
        container.Models[0].Synced = YesOrNo.Yes;
        Assert.Equal($"Model {container.Models[0].Name} is marked as synced, so Sync is turned off for this file.", ModelFileCheck.SyncBlockText(container));

        container.Models[0].Synced = YesOrNo.No;
        Assert.Null(ModelFileCheck.SyncBlockText(container));
    }

    [Fact]
    public void NameRulesKnowWhatTheAppSupports()
    {
        Assert.True(NameRules.IsSupportedObjectName("Order Line"));
        Assert.False(NameRules.IsSupportedObjectName("A.B"));
        Assert.False(NameRules.IsSupportedObjectName("O'Brien"));
        Assert.False(NameRules.IsSupportedObjectName("Get]Orders"));
        Assert.False(NameRules.IsSupportedObjectName("Item#"));
        Assert.False(NameRules.IsSupportedObjectName("Unit-Price"));
        Assert.False(NameRules.IsSupportedObjectName("Café"));
        Assert.True(NameRules.IsSupportedColumnName("Unit_Price"));
        Assert.True(NameRules.IsSupportedColumnName("2ndValue"));
        Assert.False(NameRules.IsSupportedColumnName("Weight.Kg"));
        Assert.False(NameRules.IsSupportedColumnName("Größe"));

        Assert.False(NameRules.IsSupportedSchemaName("Sales Data"));
        Assert.False(NameRules.IsSupportedSchemaName("2024"));
        Assert.True(NameRules.IsSupportedSchemaName("dbo"));
        Assert.True(NameRules.IsSupportedSchemaName("_x"));
        Assert.True(NameRules.IsSupportedSchemaName("Sales2"));
    }

    [Fact]
    public void NamesThatCannotBecomeIdentifiersGiveNull()
    {
        Assert.Null(SF.Models.Utils.GeneratePropertyName("_"));
        Assert.Null(SF.Models.Utils.GeneratePropertyName("@"));
        Assert.Null(SF.Models.Utils.GeneratePropertyName("_1"));

        Assert.Equal("Underscore", SF.Models.Utils.GeneratePropertyName("_Underscore"));
        Assert.Equal("TwondValue", SF.Models.Utils.GeneratePropertyName("2ndValue"));
        Assert.Equal("ItemNumber", SF.Models.Utils.GeneratePropertyName("Item#"));
        Assert.Equal("WeightDotKg", SF.Models.Utils.GeneratePropertyName("Weight.Kg"));

        //a one-letter table name has nothing left once singularized
        Assert.Equal("S", SF.Models.Utils.GenerateEntityName("s"));
        Assert.Equal("S", SF.Models.Utils.GenerateEntityName("_s"));
        Assert.Equal("Order", SF.Models.Utils.GenerateEntityName("Orders"));

        //a property with such a name does not throw when it is put in a collection
        var entity = new Entity { SN = "Good" };
        var props = new PropertyCollection(entity);
        props.Add(new Property { SN = "_" });
        Assert.Null(props[0].N);
    }

    [Fact]
    public void ModelNameCheckFindsNamesTheAppDoesNotSupport()
    {
        Assert.Empty(ModelNameCheck.Find(Fixtures.LoadCoverage()));

        var container = Fixtures.LoadCoverage();
        container.Models[0].Categories.GetByName(CategoryNames.Entities).Schemas[0].Name = "Bad.Schema";
        var found = ModelNameCheck.Find(container);
        Assert.Single(found);
        Assert.Equal($"Model {container.Models[0].Name}: Tables: the schema name Bad.Schema has characters that are not supported", found[0]);

        //a property whose name cannot become a C# identifier
        var nullNameContainer = Fixtures.LoadCoverage();
        var entity = nullNameContainer.Models[0].Categories.GetByName(CategoryNames.Entities).Schemas[0].Entities[0];
        entity.Ps[0].SN = "_";
        var nullNames = ModelNameCheck.Find(nullNameContainer);
        Assert.Single(nullNames);
        Assert.Contains("column '_' cannot become a C# name", nullNames[0]);

        //a column name with characters that are not supported
        var columnContainer = Fixtures.LoadCoverage();
        columnContainer.Models[0].Categories.GetByName(CategoryNames.Entities).Schemas[0].Entities[0].Ps[0].SN = "Item#";
        var columns = ModelNameCheck.Find(columnContainer);
        Assert.Single(columns);
        Assert.Contains("column 'Item#' has characters that are not supported", columns[0]);
    }

    [Fact]
    public void AModelWithATableNameThatCannotBecomeAnIdentifierStillOpens()
    {
        using(var folder = new TestFolder())
        {
            File.WriteAllText(folder.Combine("table1.edmj"), File.ReadAllText(Fixtures.CoverageModelFile).Replace("\"SN\":\"AllTypes\"", "\"SN\":\"_1\""));
            var container = ModelUtils.Load(folder.Combine("table1.edmj"));
            var found = ModelNameCheck.Find(container);
            Assert.Single(found);
            Assert.Contains("Coverage._1 (the name cannot become a C# name)", found[0]);
        }
    }

    [Fact]
    public void SqlVariantColumnsAreSkippedFromTheGeneratedModel()
    {
        var source = Fixtures.LoadCoverage();
        var filtered = SqlVariantFilter.Apply(source, NullProgressLog.Instance);
        Assert.NotSame(source, filtered);

        var sourceAllTypes = FindEntity(source, "AllTypes");
        var allTypes = FindEntity(filtered, "AllTypes");
        Assert.NotNull(FindProperty(sourceAllTypes, "SqlVariantCol"));
        Assert.Null(FindProperty(allTypes, "SqlVariantCol"));

        var expected = new List<string>();
        foreach(var p in sourceAllTypes.Ps)
        {
            if(string.CompareOrdinal(p.SN, "SqlVariantCol") != 0)
                expected.Add(p.SN);
        }

        var actual = new List<string>();
        foreach(var p in allTypes.Ps)
            actual.Add(p.SN);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SqlVariantKeyColumnsTakeTheirKeysAndNavPropsWithThem()
    {
        var source = Fixtures.LoadCoverage();
        FindProperty(FindEntity(source, "Region"), "RegionCode").TId = 24;

        var filtered = SqlVariantFilter.Apply(source, NullProgressLog.Instance);
        var region = FindEntity(filtered, "Region");
        var store = FindEntity(filtered, "Store");

        Assert.Null(region.PK);
        Assert.Single(region.UKs);
        Assert.Equal("UK_Region_Name", region.UKs[0].N);
        Assert.Null(region.NPs);
        Assert.Null(store.NPs);
        Assert.Null(FindFKey(filtered, "FK_Store_Region"));
        Assert.Null(FindProperty(region, "RegionCode"));
        Assert.NotNull(FindProperty(region, "CountryCode"));
    }

    [Fact]
    public void SqlVariantParametersRemoveTheRoutine()
    {
        var source = Fixtures.LoadCoverage();
        var proc = FindProc(source, "GetEmployees");
        proc.Par = new PropertyCollection(proc);
        proc.Par.Add(new Property { SN = "VariantParam", TId = 24 });

        var func = FindFunction(source, "FullName");
        if(func.Par == null)
            func.Par = new PropertyCollection(func);

        func.Par.Add(new Property { SN = "VariantParam", TId = 24 });

        var filtered = SqlVariantFilter.Apply(source, NullProgressLog.Instance);
        Assert.Null(FindProc(filtered, "GetEmployees"));
        Assert.Null(FindFunction(filtered, "FullName"));
    }

    [Fact]
    public void SqlVariantResultColumnsRemoveTheRoutine()
    {
        var source = Fixtures.LoadCoverage();

        //the first result column: the later ones would be read from the wrong position if only the column went
        FindProc(source, "GetEmployees").En.Ps[0].TId = 24;
        FindFunction(source, "ShipmentsFrom").En.Ps[0].TId = 24;

        var filtered = SqlVariantFilter.Apply(source, NullProgressLog.Instance);
        Assert.Null(FindProc(filtered, "GetEmployees"));
        Assert.Null(FindFunction(filtered, "ShipmentsFrom"));
        Assert.NotNull(FindProc(filtered, "GetShipments"));
        Assert.NotNull(FindFunction(filtered, "EmployeesUnder"));
    }

    [Fact]
    public void ATableWithOnlySqlVariantColumnsIsRemoved()
    {
        var source = Fixtures.LoadCoverage();
        foreach(var p in FindEntity(source, "AuditLog").Ps)
            p.TId = 24;

        var filtered = SqlVariantFilter.Apply(source, NullProgressLog.Instance);
        Assert.Null(FindEntity(filtered, "AuditLog"));
        Assert.NotNull(FindEntity(source, "AuditLog"));
        Assert.NotNull(FindEntity(filtered, "Status"));
    }

    [Fact]
    public void AModelWithoutSqlVariantIsNotCopied()
    {
        var source = Fixtures.LoadCoverage();
        var allTypes = FindEntity(source, "AllTypes");
        for(var i = allTypes.Ps.Count - 1; i >= 0; i--)
        {
            if(string.CompareOrdinal(allTypes.Ps[i].SN, "SqlVariantCol") == 0)
                allTypes.Ps.RemoveAt(i);
        }

        Assert.Same(source, SqlVariantFilter.Apply(source, NullProgressLog.Instance));
    }

    private static Entity FindEntity(ModelContainer container, string tableName)
    {
        foreach(var schema in container.Models[0].Categories.GetByName(CategoryNames.Entities).Schemas)
        {
            foreach(var entity in schema.Entities)
            {
                if(string.Equals(entity.SN, tableName, StringComparison.Ordinal))
                    return entity;
            }
        }

        return null;
    }

    private static Property FindProperty(Entity entity, string storeName)
    {
        foreach(var property in entity.Ps)
        {
            if(string.Equals(property.SN, storeName, StringComparison.Ordinal))
                return property;
        }

        return null;
    }

    private static StoredProc FindProc(ModelContainer container, string storeName)
    {
        foreach(var schema in container.Models[0].Categories.GetByName(CategoryNames.Procs).Schemas)
        {
            foreach(var proc in schema.StoredProcs)
            {
                if(string.Equals(proc.SN, storeName, StringComparison.Ordinal))
                    return proc;
            }
        }

        return null;
    }

    private static Function FindFunction(ModelContainer container, string storeName)
    {
        foreach(var schema in container.Models[0].Categories.GetByName(CategoryNames.Funcs).Schemas)
        {
            foreach(var func in schema.Functions)
            {
                if(string.Equals(func.SN, storeName, StringComparison.Ordinal))
                    return func;
            }
        }

        return null;
    }

    private static FKey FindFKey(ModelContainer container, string name)
    {
        foreach(var fk in container.Models[0].FKeys)
        {
            if(string.Equals(fk.N, name, StringComparison.Ordinal))
                return fk;
        }

        return null;
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = text.IndexOf(value, StringComparison.Ordinal);
        while(index > -1)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
