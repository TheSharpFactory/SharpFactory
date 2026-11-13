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
/// Paged reads in the generated code. The generated readers are sequential (CommandBehavior.SequentialAccess):
/// columns must be read left to right, and a paged read returns the total row count in the last column. Reading
/// that column before the row's own columns made every paged read throw on SQL Server (found in 1.2.0.2), while
/// every test that does not run the generated code against a database passed.
/// </summary>
public sealed class GeneratedPagingTests
{
    private const string _databaseVariable = "SHARPFACTORY_TEST_EDMJ";

    [Fact]
    public void APagedReadReadsTheRowBeforeTheTotalCount()
    {
        using(var folder = new TestFolder())
        {
            var output = folder.Combine("gen");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
            var repositoryBase = File.ReadAllText(Path.Combine(output, "SFTest.Repository", "BaseClasses", "RepositoryBase.cs"));
            var database = File.ReadAllText(Path.Combine(output, "SFTest.Repository", "SQLEngine", "Database.cs"));

            //why the order matters
            Assert.Contains("ExecuteReader(CommandBehavior.SequentialAccess)", database, StringComparison.Ordinal);
            Assert.Contains("ExecuteReaderAsync(CommandBehavior.SequentialAccess)", database, StringComparison.Ordinal);

            var body = ExtractMethodBody(repositoryBase, "protected static object MaterializeEntityResult<TEntity>(");
            var firstRow = body.IndexOf("result.Add(materializer(r))", StringComparison.Ordinal);
            var totalCount = body.IndexOf("r.GetInt32(fieldCount)", StringComparison.Ordinal);
            Assert.True(firstRow >= 0 && totalCount >= 0, body);
            Assert.True(firstRow < totalCount, "The total row count (the last column) is read before the first row's columns:" + Environment.NewLine + body);
        }
    }

    /// <summary>
    /// Compiles the code generated for the Coverage model with a small program (Fixtures/PagingRunner) and runs paged
    /// reads through it against the test database: with and without the total, the last page, a to-many and a
    /// to-one navigation property, sync and async. The program adds its own rows (country "ZZ") and deletes them.
    /// </summary>
    [Fact]
    public void PagedReadsWorkOnSqlServer()
    {
        var modelFile = Environment.GetEnvironmentVariable(_databaseVariable);
        if(string.IsNullOrWhiteSpace(modelFile) || !File.Exists(modelFile))
            Assert.Skip($"{_databaseVariable} is not set to a model file of the test database (the Coverage schema).");

        var conInfo = ModelUtils.Load(modelFile).Models[0].ConInfo;
        using(var folder = new TestFolder())
        {
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(folder.Combine("gen")), NullProgressLog.Instance);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "PagingRunner", "Program.cs.txt"), folder.Combine("Program.cs"));
            File.WriteAllText(folder.Combine("PagingRunner.csproj"), ComposeRunnerProject());

            var output = RunDotnet(folder.Path, ComposeConnectionString(conInfo), out var exitCode);

            Assert.True(exitCode == 0, output);
            Assert.Contains("PASS page with the total, the total", output, StringComparison.Ordinal);
            Assert.Contains("PASS page with the total, async", output, StringComparison.Ordinal);
            Assert.Contains("PASS page with a to-many navigation property, its rows", output, StringComparison.Ordinal);
            Assert.Contains("PASS page with a to-many navigation property, the total", output, StringComparison.Ordinal);
            Assert.Contains("PASS page with a to-one navigation property, async", output, StringComparison.Ordinal);
            Assert.Contains("PASS navigation properties from a collection initializer", output, StringComparison.Ordinal);
            Assert.Contains("PASS update from GetChanges", output, StringComparison.Ordinal);
            Assert.Contains("PASS update from CreateChange", output, StringComparison.Ordinal);
            Assert.Contains("ALL PASSED", output, StringComparison.Ordinal);
        }
    }

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
        project.AppendLine("    <RunAnalyzers>false</RunAnalyzers>");
        project.AppendLine("  </PropertyGroup>");
        project.AppendLine("  <ItemGroup>");
        project.AppendLine("    <Compile Include=\"gen\\**\\*.cs\" />");
        project.AppendLine("    <Compile Include=\"Program.cs\" />");
        project.AppendLine("  </ItemGroup>");
        project.AppendLine("  <ItemGroup>");
        project.AppendLine("    <PackageReference Include=\"Microsoft.Data.SqlClient\" Version=\"7.1.1\" />");
        project.AppendLine("    <PackageReference Include=\"Microsoft.SqlServer.Types\" Version=\"170.1000.7\" />");
        //SqlClient's own Pkcs dependency has known vulnerabilities: the fixed version
        project.AppendLine("    <PackageReference Include=\"System.Security.Cryptography.Pkcs\" Version=\"10.0.14\" />");
        project.AppendLine("  </ItemGroup>");
        project.AppendLine("</Project>");
        return project.ToString();
    }

    /// <summary>
    /// The model's connection: Windows sign-in, or the SQL Server login it was built with.
    /// </summary>
    private static string ComposeConnectionString(ConnectionInfo conInfo)
    {
        var text = new StringBuilder();
        text.Append("Server=").Append(conInfo.Server).Append(";Database=").Append(conInfo.Database).Append(';');
        if(conInfo.DisableIntegratedSecurity == true)
            text.Append("User ID=").Append(conInfo.User).Append(";Password=").Append(conInfo.Password).Append(';');
        else
            text.Append("Integrated Security=true;");

        text.Append("Encrypt=Optional;Application Name=SharpFactoryTests");
        return text.ToString();
    }

    /// <summary>
    /// dotnet run in the folder; returns everything it printed.
    /// </summary>
    private static string RunDotnet(string folder, string connectionString, out int exitCode)
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
        startInfo.Environment["SF_PAGING_CONNECTION"] = connectionString;

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

    private static string ExtractMethodBody(string text, string signature)
    {
        var start = text.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, signature + " is not in the generated code.");
        var open = text.IndexOf('{', start);
        var depth = 0;
        for(var i = open; i < text.Length; i++)
        {
            if(text[i] == '{')
                depth++;
            else if(text[i] == '}' && --depth == 0)
                return text.Substring(open, i - open + 1);
        }

        return text.Substring(open);
    }
}
