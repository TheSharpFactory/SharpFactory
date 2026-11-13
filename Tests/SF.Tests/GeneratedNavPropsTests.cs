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
/// NavProps&lt;T&gt; is a collection, so navigation properties can be given with a collection initializer:
/// repo.ByPK(id, new NavProps&lt;CustomerProperty&gt; { CustomerNavProperty.Orders }). GeneratedPagingTests compiles
/// and runs such a call against the test database.
/// </summary>
public sealed class GeneratedNavPropsTests
{
    [Fact]
    public void NavPropsOfAContainerIsACollection()
    {
        using(var folder = new TestFolder())
        {
            var output = folder.Combine("generate");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
            var files = Directory.GetFiles(output, "NavProps.cs", SearchOption.AllDirectories);
            var text = File.ReadAllText(Assert.Single(files)).Replace("\r\n", "\n", StringComparison.Ordinal);

            Assert.Contains("public sealed class NavProps<TProps> : IEnumerable<NavProp> where TProps : NavPropertyContainer", text, StringComparison.Ordinal);
            Assert.Contains("    public void Add<TTarget>(NavProp<TProps, TTarget> item) where TTarget : PropertyContainer", text, StringComparison.Ordinal);
            Assert.Contains("    public IEnumerator<NavProp> GetEnumerator()\n    {\n        return _items.GetEnumerator();\n    }", text, StringComparison.Ordinal);
            //an explicit interface implementation: no access modifier, and no stray space where it would be
            Assert.Contains("\n    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()\n", text, StringComparison.Ordinal);
        }
    }
}
