#region Usings
using Avalonia.Headless.XUnit;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The property container (SF-136, SF-141) in the app.
/// </summary>
public sealed class TypedPropsTests
{
    [AvaloniaFact]
    public void TheCodePreviewShowsATablesContainer()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            page.SelectedNode = ParityTests.FindNode(page, "Warehouse");

            var props = (Services.CodeFileViewModel)null;
            foreach(var file in page.CodeFiles)
            {
                if(string.Equals(file.FileName, "WarehouseProperty.cs", System.StringComparison.Ordinal))
                    props = file;
            }

            Assert.NotNull(props);
            Assert.Contains("public sealed class WarehouseProperty : NavPropertyContainer", props.Content);
            Assert.Contains("public static readonly Prop<WarehouseProperty, int> Id = new Prop<WarehouseProperty, int>((int)WarehouseProp.Id, \"Id\", false);", props.Content);
        }
    }
}
