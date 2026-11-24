#region Usings
using SF.Engine;
using SF.Engine.Sync;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// The two JSON settings of the Entities project: Enable System.Text.Json (a source generation context for each
/// database) and Optimize JSON (short JSON names, kept in the model so a property's name never changes).
/// </summary>
public sealed class GeneratedJsonTests
{
    [Fact]
    public void TheIdSequenceGoesFromLettersToTwoCharacters()
    {
        var sequence = new IdSequence();
        var ids = new List<string>(1000);
        for(var i = 0; i < 963; i++)
            ids.Add(sequence.NextId());

        Assert.Equal("A", ids[0]);
        Assert.Equal("Z", ids[25]);
        Assert.Equal("A0", ids[26]);
        Assert.Equal("A9", ids[35]);
        Assert.Equal("AA", ids[36]);
        Assert.Equal("AZ", ids[61]);
        Assert.Equal("B0", ids[62]);
        Assert.Equal("ZZ", ids[961]);
        Assert.Equal("A00", ids[962]);
        Assert.Equal(ids.Count, new HashSet<string>(ids, StringComparer.Ordinal).Count);

        //each sequence is its own: a second one starts at A
        Assert.Equal("A", new IdSequence().NextId());

        //seeded with the last id, it goes on after it
        var seeded = new IdSequence();
        seeded.Seed("AZ");
        Assert.Equal("B0", seeded.NextId());
    }

    [Fact]
    public void WithBothSettingsOffNothingChanges()
    {
        var container = Fixtures.LoadCoverage();
        Assert.Equal(YesOrNo.No, container.CommonSettings.EnableSystemTextJson);
        Assert.Equal(YesOrNo.No, container.CommonSettings.OptimizeJson);

        using(var folder = new TestFolder())
        {
            var output = folder.Combine("gen");
            var result = GenerationRunner.Run(container, new FolderProjectHost(output), NullProgressLog.Instance);

            Assert.Equal(0, result.JsonNamesGiven);
            Assert.Null(FindProperty(FindEntity(container, "Region"), "CountryCode").JN);
            Assert.Empty(Directory.GetFiles(output, "*JsonContext.cs", SearchOption.AllDirectories));
            foreach(var file in Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories))
                Assert.DoesNotContain("System.Text.Json", File.ReadAllText(file), StringComparison.Ordinal);
        }

        //and the model file does not get the settings
        Assert.DoesNotContain("Json", ModelUtils.ToJson(container.Models[0]), StringComparison.Ordinal);
    }

    [Fact]
    public void OptimizeJsonNamesTheColumnsThenTheNavigationProperties()
    {
        var container = Fixtures.LoadCoverage();
        container.CommonSettings.OptimizeJson = YesOrNo.Yes;

        Assert.True(JsonNames.Assign(container) > 0);
        var region = FindEntity(container, "Region");
        Assert.Equal("A", FindProperty(region, "CountryCode").JN);
        Assert.Equal("B", FindProperty(region, "RegionCode").JN);
        Assert.Equal("C", FindProperty(region, "Name").JN);
        Assert.Equal("D", region.NPs.GetByName("Stores").JN);
        Assert.Equal("D", region.JL);

        //the request class of a procedure has its own names
        var proc = FindProc(container, "CountShipments");
        Assert.Equal("A", proc.Par[0].JN);

        //every property has one now: nothing more to give
        Assert.Equal(0, JsonNames.Assign(container));
    }

    [Fact]
    public void ANewColumnGetsTheNextNameAndARemovedOnesNameIsNotGivenAgain()
    {
        var container = Fixtures.LoadCoverage();
        container.CommonSettings.OptimizeJson = YesOrNo.Yes;
        JsonNames.Assign(container);

        //the table loses Name (C) and gets Notes: Notes takes E, after the last name the class gave (D)
        var region = FindEntity(container, "Region");
        region.Ps.Remove(FindProperty(region, "Name"));
        region.Ps.Add(new Property { SN = "Notes", TId = Lookup.GetTypeId("nvarchar") });

        Assert.Equal(1, JsonNames.Assign(container));
        Assert.Equal("E", FindProperty(region, "Notes").JN);
        Assert.Equal("E", region.JL);
        Assert.Equal("A", FindProperty(region, "CountryCode").JN);
        Assert.Equal("B", FindProperty(region, "RegionCode").JN);
        Assert.Equal("D", region.NPs.GetByName("Stores").JN);

        //the names are saved in the model file and read back
        var saved = ModelUtils.ToJson(container.Models[0]);
        Assert.Contains("\"JN\":\"E\"", saved, StringComparison.Ordinal);
        Assert.Contains("\"JL\":\"E\"", saved, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateKeepsTheNames()
    {
        var current = Fixtures.LoadCoverage();
        current.CommonSettings.OptimizeJson = YesOrNo.Yes;
        JsonNames.Assign(current);

        //the same model as Update rebuilds it from the database: no names yet
        var rebuilt = Fixtures.LoadCoverage();
        rebuilt.CommonSettings.OptimizeJson = YesOrNo.Yes;
        JsonNames.CarryOver(current.Models[0], rebuilt.Models[0]);

        Assert.Equal(0, JsonNames.Assign(rebuilt));
        Assert.Equal(ModelUtils.ToJson(current.Models[0]), ModelUtils.ToJson(rebuilt.Models[0]));
    }

    [Fact]
    public void GenerationGivesTheNamesInTheModelItself()
    {
        var container = Fixtures.LoadCoverage();
        container.CommonSettings.OptimizeJson = YesOrNo.Yes;

        using(var folder = new TestFolder())
        {
            var output = folder.Combine("gen");
            var result = GenerationRunner.Run(container, new FolderProjectHost(output), NullProgressLog.Instance);

            Assert.True(result.JsonNamesGiven > 0);
            Assert.Equal("A", FindProperty(FindEntity(container, "Region"), "CountryCode").JN);

            var region = File.ReadAllText(Assert.Single(Directory.GetFiles(output, "Region.cs", SearchOption.AllDirectories)));
            Assert.Contains("using System.Text.Json.Serialization;", region, StringComparison.Ordinal);
            Assert.Contains("    [JsonPropertyName(\"A\")]\r\n    public string CountryCode { get; set; }", region, StringComparison.Ordinal);
            Assert.Contains("    [JsonPropertyName(\"D\")]\r\n    public List<SFTest.Entity.TestDb.Coverage.Store> Stores { get; set; }", region, StringComparison.Ordinal);

            //Optimize JSON alone: no context
            Assert.Empty(Directory.GetFiles(output, "*JsonContext.cs", SearchOption.AllDirectories));
        }
    }

    /// <summary>
    /// Compiles the Entities project generated with both settings on, with a small program (Fixtures/JsonRunner),
    /// and runs it: short names, a round trip with a navigation property, a list and an xml column, all through the
    /// generated context.
    /// </summary>
    [Fact]
    public void TheGeneratedContextBuildsAndRoundTrips()
    {
        var container = Fixtures.LoadCoverage();
        container.CommonSettings.EnableSystemTextJson = YesOrNo.Yes;
        container.CommonSettings.OptimizeJson = YesOrNo.Yes;

        using(var folder = new TestFolder())
        {
            GenerationRunner.Run(container, new FolderProjectHost(folder.Combine("gen")), NullProgressLog.Instance);
            var context = File.ReadAllText(folder.Combine("gen", "SFTest.Entity", "TestDb", "TestDbJsonContext.cs"));
            Assert.Contains("public sealed partial class TestDbJsonContext : JsonSerializerContext", context, StringComparison.Ordinal);
            Assert.Contains("[JsonSerializable(typeof(global::SFTest.Entity.TestDb.Coverage.Region), TypeInfoPropertyName = \"CoverageRegion\")]", context, StringComparison.Ordinal);
            Assert.Contains("[JsonSerializable(typeof(List<global::SFTest.Entity.TestDb.Coverage.Region>), TypeInfoPropertyName = \"ListCoverageRegion\")]", context, StringComparison.Ordinal);
            Assert.Contains("[JsonSourceGenerationOptions(Converters = new[] { typeof(XDocumentJsonConverter) })]", context, StringComparison.Ordinal);
            Assert.True(File.Exists(folder.Combine("gen", "SFTest.Entity", "TestDb", "XDocumentJsonConverter.cs")));

            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "JsonRunner", "Program.cs.txt"), folder.Combine("Program.cs"));
            File.WriteAllText(folder.Combine("JsonRunner.csproj"), ComposeRunnerProject());

            var output = RunDotnet(folder.Path, out var exitCode);

            Assert.True(exitCode == 0, output);
            Assert.Contains("PASS short names", output, StringComparison.Ordinal);
            Assert.Contains("PASS round trip", output, StringComparison.Ordinal);
            Assert.Contains("PASS list", output, StringComparison.Ordinal);
            Assert.Contains("PASS xml column", output, StringComparison.Ordinal);
            Assert.Contains("ALL PASSED", output, StringComparison.Ordinal);
            Assert.DoesNotContain("warning SYSLIB", output, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The Entities project only: the program needs nothing else.
    /// </summary>
    private static string ComposeRunnerProject()
    {
        var project = new StringBuilder();
        project.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        project.AppendLine("  <PropertyGroup>");
        project.AppendLine("    <OutputType>Exe</OutputType>");
        project.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        project.AppendLine("    <ImplicitUsings>disable</ImplicitUsings>");
        project.AppendLine("    <Nullable>disable</Nullable>");
        project.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
        project.AppendLine("  </PropertyGroup>");
        project.AppendLine("  <ItemGroup>");
        project.AppendLine("    <Compile Include=\"gen\\SFTest.Entity\\**\\*.cs\" />");
        project.AppendLine("    <Compile Include=\"Program.cs\" />");
        project.AppendLine("  </ItemGroup>");
        project.AppendLine("  <ItemGroup>");
        project.AppendLine("    <PackageReference Include=\"Microsoft.SqlServer.Types\" Version=\"170.1000.7\" />");
        project.AppendLine("  </ItemGroup>");
        project.AppendLine("</Project>");
        return project.ToString();
    }

    /// <summary>
    /// dotnet run in the folder; returns everything it printed.
    /// </summary>
    private static string RunDotnet(string folder, out int exitCode)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");

        //the test host's MSBuild settings would point the inner build at the test run's
        var inherited = new List<string>();
        foreach(var key in startInfo.Environment.Keys)
        {
            if(key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase))
                inherited.Add(key);
        }

        foreach(var key in inherited)
            startInfo.Environment.Remove(key);

        using(var process = Process.Start(startInfo))
        {
            //both streams are read in the background, so the time limit applies while the program runs
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if(!process.WaitForExit(TimeSpan.FromMinutes(5)))
            {
                process.Kill(true);
                process.WaitForExit();
                exitCode = -1;
                return output.Result + errors.Result + Environment.NewLine + "(stopped after 5 minutes)";
            }

            process.WaitForExit();
            exitCode = process.ExitCode;
            return output.Result + errors.Result;
        }
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
}
