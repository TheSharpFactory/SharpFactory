#region Usings
using SF.Engine;
using SF.Models;
using SF.Utils;
using System;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// The schema reader against names that need quoting (SF-033): a schema with an apostrophe, and objects with
/// apostrophes, ']' and other characters. Those names are not supported by the app (the schema reader embeds them in
/// a name), so every object is left out and listed. Runs when SHARPFACTORY_QUOTED_NAMES_DB is "server;database" of a
/// test database where Fixtures/QuotedNames.sql was run.
/// </summary>
public sealed class QuotedNamesTests
{
    [Fact]
    public void TheReaderLeavesOutNamesThatAreNotSupported()
    {
        var setting = Environment.GetEnvironmentVariable("SHARPFACTORY_QUOTED_NAMES_DB");
        if(string.IsNullOrWhiteSpace(setting) || setting.IndexOf(';') < 0)
            Assert.Skip("SHARPFACTORY_QUOTED_NAMES_DB is not set to \"server;database\".");

        //"server;database" signs in with Windows; "server;database;user;password" with a SQL Server login
        var parts = setting.Split(';');
        var conInfo = new ConnectionInfo { Server = parts[0], Database = parts[1], DisableIntegratedSecurity = parts.Length >= 4 };
        if(parts.Length >= 4)
        {
            conInfo.User = parts[2];
            conInfo.Password = parts[3];
        }

        Assert.True(DbModelUtils.ValidateConnectionInfo(conInfo, out var reason), reason);
        var categories = ModelBuilder.ReadDatabaseObjects(conInfo);
        foreach(var category in categories)
        {
            foreach(var schema in category.Schemas)
            {
                foreach(var dbObject in schema.Objects)
                    dbObject.IsSelected = true;
            }
        }

        var model = ModelBuilder.Populate("Quotes", conInfo, Defaults.ModelLayers(), categories, NullProgressLog.Instance);

        //the schema name is not supported, so its two tables, procedure and function are left out
        var found = 0;
        foreach(var record in categories.NotSupported)
        {
            Assert.EndsWith("(the schema name has characters that are not supported)", record);
            found++;
        }

        Assert.Equal(4, found);
        Assert.Equal(0, model.Categories.GetByName(CategoryNames.Entities).Count);
        Assert.Equal(0, model.Categories.GetByName(CategoryNames.Procs).Count);
        Assert.Equal(0, model.Categories.GetByName(CategoryNames.Funcs).Count);
    }
}
