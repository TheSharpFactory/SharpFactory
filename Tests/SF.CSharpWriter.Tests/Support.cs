#region Usings
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
#endregion

//CSharpFile writes one file at a time into one shared buffer: the tests run one after another
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SF.CSharpWriter.Tests;

/// <summary>
/// The expected text of a file: each line ends in CRLF, as CSharpFile writes it.
/// </summary>
internal static class Code
{
    public static string Lines(params string[] lines)
    {
        var text = new StringBuilder();
        foreach(var line in lines)
            text.Append(line).Append("\r\n");

        return text.ToString();
    }
}

/// <summary>
/// Compiles a written file with Roslyn against the .NET libraries the tests run on: proof that it is valid C#.
/// </summary>
internal static class Compiler
{
    private static List<MetadataReference> _references;

    /// <summary>
    /// The compiler errors of the file (warnings are left out); empty when it compiles.
    /// </summary>
    public static List<string> Errors(string code)
    {
        var errors = new List<string>();
        foreach(var diagnostic in Diagnostics(code, DocumentationMode.Parse))
        {
            if(diagnostic.Severity == DiagnosticSeverity.Error)
                errors.Add(diagnostic.ToString());
        }

        return errors;
    }

    /// <summary>
    /// The warnings of the file with its doc comments checked (malformed XML, a param that does not exist...),
    /// except CS1591, a public member without a doc comment.
    /// </summary>
    public static List<string> Warnings(string code)
    {
        var warnings = new List<string>();
        foreach(var diagnostic in Diagnostics(code, DocumentationMode.Diagnose))
        {
            if(diagnostic.Severity == DiagnosticSeverity.Warning && diagnostic.Id != "CS1591")
                warnings.Add(diagnostic.ToString());
        }

        return warnings;
    }

    private static IEnumerable<Diagnostic> Diagnostics(string code, DocumentationMode documentation)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest, documentation);
        var tree = CSharpSyntaxTree.ParseText(code, parse);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true);
        var compilation = CSharpCompilation.Create("Written", new SyntaxTree[] { tree }, References(), options);
        return compilation.GetDiagnostics();
    }

    private static List<MetadataReference> References()
    {
        if(_references != null)
            return _references;

        _references = new List<MetadataReference>();
        var paths = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        foreach(var path in paths.Split(Path.PathSeparator))
        {
            if(path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                _references.Add(MetadataReference.CreateFromFile(path));
        }

        return _references;
    }
}
