#region Usings
using SF.Engine;
using SF.Engine.Sync;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// Async methods that only hand a call on return the inner Task (no async state machine per layer); async methods that
/// do work after the await keep async/await.
/// </summary>
public sealed class GeneratedAsyncTests
{
    private static readonly Regex _asyncMethod = new Regex(@"^[ \t]*(?:(?:public|protected|private|internal|static|virtual|override) )+async Task(?:<.+?>)? (?<name>\w+)(?:<[^>(]*>)?\(.*\)\r?$", RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void AsyncMethodsThatOnlyHandACallOnReturnTheTask()
    {
        using(var folder = new TestFolder())
        {
            var text = Generate(folder);

            //hand the call on: no async, no await
            Assert.Contains("public Task<List<TEntity>> ToListAsync(Query<TProps> query)", text);
            Assert.Contains("return SelectQueryAsync(query.Prepare(), query.Prepare().Pagination, 0);", text);
            Assert.Contains("protected override Task<List<TEntity>> SelectQueryAsync(", text);
            Assert.Contains("internal static Task<bool> SelectAsync(EntityQuery entityQuery, Action<SqlDataReader> materializer)", text);
            Assert.Matches(new Regex(@"public Task<\w+> ByPKAsync\("), text);
            Assert.DoesNotMatch(new Regex(@"async Task<\w+> ByPKAsync\("), text);

            //work after the await: keep async/await
            Assert.Matches(new Regex(@"public async Task<bool> DeleteByPKAsync\("), text);
            Assert.Contains("protected async Task<TEntity> SelectSingleAsync(List<IQueryFilter> where, List<ISortFilter> orderby)", text);
            Assert.Contains("internal static async Task<bool> ReaderAsync(", text);
        }
    }

    [Fact]
    public void NoGeneratedAsyncMethodOnlyReturnsAnAwaitedCall()
    {
        using(var folder = new TestFolder())
        {
            var offenders = new List<string>();
            var asyncMethods = 0;
            foreach(var file in GenerateFiles(folder))
            {
                var text = File.ReadAllText(file);
                foreach(Match m in _asyncMethod.Matches(text))
                {
                    asyncMethods++;
                    var body = Body(text, m.Index + m.Length);
                    if(OnlyReturnsAwaitedCalls(body))
                        offenders.Add(Path.GetFileName(file) + ": " + m.Groups["name"].Value);
                }
            }

            Assert.True(asyncMethods > 20, "async methods found: " + asyncMethods);
            Assert.Empty(offenders);
        }
    }

    private static List<string> GenerateFiles(TestFolder folder)
    {
        var output = folder.Combine("generate");
        GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
        return [.. Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories)];
    }

    private static string Generate(TestFolder folder)
    {
        var all = new System.Text.StringBuilder();
        foreach(var file in GenerateFiles(folder))
            all.Append(File.ReadAllText(file)).Append('\n');
        return all.ToString();
    }

    private static string Body(string text, int start)
    {
        var open = text.IndexOf('{', start);
        var depth = 0;
        for(var i = open; i < text.Length; i++)
        {
            if(text[i] == '{')
                depth++;
            else if(text[i] == '}' && --depth == 0)
                return text.Substring(open + 1, i - open - 1);
        }

        return text.Substring(open + 1);
    }

    //every await is 'return await <one call>;' and nothing (using, try, lock, a loop) needs the method to stay async
    private static bool OnlyReturnsAwaitedCalls(string body)
    {
        if(Regex.IsMatch(body, @"\b(using|try|lock|foreach|for|while)\b\s*[\(\{]"))
            return false;

        var awaits = 0;
        foreach(var raw in body.Split('\n'))
        {
            var line = raw.Trim();
            if(!line.Contains("await "))
                continue;

            awaits++;
            if(!line.StartsWith("return await ", StringComparison.Ordinal) || !line.EndsWith(");", StringComparison.Ordinal) || !IsOneCall(line.Substring(13, line.Length - 14)))
                return false;
        }

        return awaits > 0;
    }

    private static bool IsOneCall(string expression)
    {
        var open = expression.IndexOf('(');
        if(open < 1)
            return false;

        var depth = 0;
        var inString = false;
        for(var i = open; i < expression.Length; i++)
        {
            var c = expression[i];
            if(c == '"')
                inString = !inString;
            if(inString)
                continue;
            if(c == '(')
                depth++;
            else if(c == ')' && --depth == 0)
                return i == expression.Length - 1;
        }

        return false;
    }
}
