#region Usings
using SF.Engine;
using SF.Engine.Sync;
using SF.Utils;
using System;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// Fixes from the code review of 1.1.9.7: schema-reader logs roll over instead of overwriting each other, and string IN lists
/// on nvarchar/nchar columns are Unicode literals. (The Sync page's solution list: SyncTargetListTests in SF.App.Tests.)
/// </summary>
public sealed class ReviewFixes1198Tests
{
    [Fact]
    public void SchemaReaderLogsAppendAndRollOver()
    {
        using(var folder = new TestFolder())
        {
            var previous = IOUtils.LogFolder;
            IOUtils.LogFolder = folder.Path;
            try
            {
                //two entries: both kept (each write used to replace the file)
                IOUtils.WriteLog("Shop", "ErrorsStoredProcs", "first entry");
                IOUtils.WriteLog("Shop", "ErrorsStoredProcs", "second entry");
                var logs = folder.Combine("ShopLogs", "ErrorsStoredProcs");
                var log = File.ReadAllText(Path.Combine(logs, "log.txt"));
                Assert.Contains("first entry", log);
                Assert.Contains("second entry", log);

                //past the size limit the file rolls over, and only LogFilesKept files are kept
                var big = new string('x', (int)(IOUtils.LogFileMaxBytes / 3) + 1);
                for(var i = 0; i < 3 * IOUtils.LogFilesKept + 2; i++)
                    IOUtils.WriteLog("Shop", "ErrorsStoredProcs", $"entry {i} {big}");

                IOUtils.WriteLog("Shop", "ErrorsStoredProcs", "last entry");
                Assert.Contains("last entry", File.ReadAllText(Path.Combine(logs, "log.txt")));
                Assert.True(File.Exists(Path.Combine(logs, "log.1.txt")));
                Assert.True(File.Exists(Path.Combine(logs, $"log.{IOUtils.LogFilesKept - 1}.txt")));
                Assert.False(File.Exists(Path.Combine(logs, $"log.{IOUtils.LogFilesKept}.txt")));
                Assert.Equal(IOUtils.LogFilesKept, Directory.GetFiles(logs).Length);
                foreach(var file in Directory.GetFiles(logs))
                    Assert.True(new FileInfo(file).Length < IOUtils.LogFileMaxBytes + big.Length + 200, file);
            }
            finally
            {
                IOUtils.LogFolder = previous;
            }
        }
    }

    [Fact]
    public void StringInListsOnUnicodeColumnsAreUnicodeLiterals()
    {
        using(var folder = new TestFolder())
        {
            var output = folder.Combine("generate");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);

            var select = File.ReadAllText(Directory.GetFiles(output, "DatabaseSelect.cs", SearchOption.AllDirectories)[0]);
            Assert.Contains("var unicode = prop.SqlType == SqlDbType.NVarChar || prop.SqlType == SqlDbType.NChar;", select);
            Assert.Contains("w.AppendQuoted(strings[i], unicode);", select);

            var database = File.ReadAllText(Directory.GetFiles(output, "Database.cs", SearchOption.AllDirectories)[0]);
            var appendQuoted = database.Substring(database.IndexOf("public void AppendQuoted(string value, bool unicode)", StringComparison.Ordinal));
            Assert.Contains("_length += value.Length + quotes + (unicode ? 3 : 2);", appendQuoted);
            Assert.True(appendQuoted.IndexOf("Append('N');", StringComparison.Ordinal) < appendQuoted.IndexOf("Append('\\'');", StringComparison.Ordinal), "N comes before the opening quote");
        }
    }
}
