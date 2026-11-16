#region Usings
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// The using directives at the top of a file. Each test starts a new file and checks the exact text.
/// </summary>
public sealed class UsingDirectiveTests
{
    [Fact]
    public void UsingNamespace()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingNamespace("System.Collections.Generic");

        Assert.Equal(Code.Lines("using System.Collections.Generic;"), CSharpFile.ToString());
    }

    [Fact]
    public void UsingStatic()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingStatic("System.Math");

        Assert.Equal(Code.Lines("using static System.Math;"), CSharpFile.ToString());
    }

    [Fact]
    public void UsingAlias()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingAlias("Json", "System.Text.Json.JsonSerializer");

        Assert.Equal(Code.Lines("using Json = System.Text.Json.JsonSerializer;"), CSharpFile.ToString());
    }

    [Fact]
    public void GlobalUsing()
    {
        CSharpFile.Start(4);
        CSharpFile.GlobalUsing("System.Linq");

        Assert.Equal(Code.Lines("global using System.Linq;"), CSharpFile.ToString());
    }

    [Fact]
    public void UsingsComeOneAfterAnother()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingNamespace("System");
        CSharpFile.UsingStatic("System.Math");
        CSharpFile.UsingAlias("Text", "System.String");

        Assert.Equal(Code.Lines("using System;", "using static System.Math;", "using Text = System.String;"), CSharpFile.ToString());
    }
}
