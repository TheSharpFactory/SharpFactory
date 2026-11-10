#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GeneratePaginationSettings(ModelContainer modelContainer)
    {
        return PaginationSettings.Generate(modelContainer);
    }
    public static string GeneratePaginationSettingsInterface(ModelContainer modelContainer)
    {
        return PaginationSettings.GenerateInterface(modelContainer);
    }
    private static class PaginationSettings
    {
        public static string GenerateInterface(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
            };

            //Generate file header with information about the interface.
            var type = new InterfaceDeclaration
            {
                Name = Common.Names.IPaginationSettings,
                Namespace = typeNamespace,
                IsPartial = false,
                Summary = new List<string>(1) { "Represents the Pagination Settings to apply to a query when calling the Repository." },
            };

            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenInterfaceWithNamespace(type);

            PropertyExtensions.InterfaceAuto(TypeInfo.Bool, "GetTotalCount", "If true gets the total count for a paged query.  This can have a performance impact. Set true with caution.");
            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Int, "PageCount", "Gets or the total number of pages.  Only works when GetTotalCount is set to true.");
            PropertyExtensions.InterfaceAuto(TypeInfo.Int, "PageNumber", "Gets or sets the desired page number to return.  Defaults to 1, meaning the first page.");
            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Int, "PageNumberForRep", "Gets the value of the PageNumber used for the repository which is zero based.");
            PropertyExtensions.InterfaceAuto(TypeInfo.Int, "PageSize", "Gets or sets the number of items to return.");
            PropertyExtensions.InterfaceAuto(TypeInfo.Double, "TotalCount", "Gets or sets the total count of paged query.  This is the count that would be returned without pagination.");

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System" },
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.PaginationSettings,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                IsPartial = true,
                Summary = new List<string>(1) { "Represents the Pagination Settings to apply to a query when calling the Repository." },
                Interfaces = new List<string> { Common.Names.IPaginationSettings }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.Line("private int _pageSize = 0", "_pageSize zero means pagination DISABLED. Cannot be se to less than zero as per logic in the property setter.");
            CSharpFile.Line("private int _pageNumber = 1", "by the default the first page. Cannot be set to less than one as per logic in the property setter.");
            CSharpFile.Line("private int _pageNumberForRep = 0", "this is because the Repository uses zero based page numbering. So it is one less as per logic in the setter of PageNumber.");
            CSharpFile.Line("private bool _getTotalCount = false", "used to decide if to get the total count of a paged query. can have performance impact to do so.");
            CSharpFile.Line("private double _totalCount = 0D", "used to store the total count of a paged query.");

            var prop = new PropertyDeclaration
            {
                Name = "PageSize",
                AccessModifier = AccessModifiers.Public,
                ReturnType = TypeInfo.Int,
                Summary = new List<string>(1) { "Gets or sets the number of items to return." }
            };
            CSharpFile.OpenProperty(prop);
            CSharpFile.OpenGetter();
            CSharpFile.Return("_pageSize");
            CSharpFile.CloseGetter();
            CSharpFile.OpenSetter();
            CSharpFile.If("value > -1");
            CSharpFile.LineIndented("_pageSize = value");
            CSharpFile.Else();
            CSharpFile.LineIndented("_pageSize = 0");
            CSharpFile.CloseSetter();
            CSharpFile.CloseProperty();

            prop.Name = "PageNumber";
            prop.Summary = new List<string>(1) { "Gets or sets the desired page number to return." };

            CSharpFile.OpenProperty(prop);
            CSharpFile.OpenGetter();
            CSharpFile.Return("_pageNumber");
            CSharpFile.CloseGetter();
            CSharpFile.OpenSetter();
            CSharpFile.If("value > 0");
            CSharpFile.LineIndented("_pageNumber = value");
            CSharpFile.Else();
            CSharpFile.LineIndented("_pageNumber = 1");
            CSharpFile.Line("_pageNumberForRep = _pageNumber - 1");
            CSharpFile.CloseSetter();
            CSharpFile.CloseProperty();

            prop.Name = "PageNumberForRep";
            prop.Summary = new List<string>(1) { "Gets the value of the PageNumber used for the repository which is zero based." };
            prop.Kind = PropertyKind.FullNoSetter;
            prop.BackingField = "_pageNumberForRep";

            CSharpFile.Property(prop);

            prop.Name = "GetTotalCount";
            prop.Summary = new List<string>(2)
            {
              "If true gets the total count for a paged query.",
              "This can have a performance impact. Set true with caution."
            };
            prop.ReturnType = TypeInfo.Bool;
            prop.Kind = PropertyKind.Full;
            prop.BackingField = "_getTotalCount";

            CSharpFile.Property(prop);

            prop.Name = "TotalCount";
            prop.Summary = new List<string>(2)
            {
              "Gets or sets the total count of paged query.",
              "This is the count that would be returned without pagination."
            };
            prop.ReturnType = TypeInfo.Double;
            prop.BackingField = "_totalCount";

            CSharpFile.Property(prop);

            prop.Name = "PageCount";
            prop.Summary = new List<string>(2)
            {
              "Gets or the total number of pages.",
              "Only works when GetTotalCount is set to true."
            };
            prop.ReturnType = TypeInfo.Int;

            CSharpFile.OpenProperty(prop);
            CSharpFile.OpenGetter();
            CSharpFile.If("_pageSize < 1");
            CSharpFile.ReturnIndented("1");
            CSharpFile.Return("(int)Math.Ceiling(_totalCount / (double)_pageSize)");
            CSharpFile.CloseSetter();
            CSharpFile.CloseProperty();

            CSharpFile.ConstructorEmpty(new ConstructorDeclaration
            {
                Name = Common.Names.PaginationSettings,
                AccessModifier = AccessModifiers.Public,
                Summary = new List<string> { "Uses default values which means pagination DISABLED." }
            });

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.PaginationSettings,
                AccessModifier = AccessModifiers.Public,
                Summary = new List<string> { "Initializes a new instance of the <see cref=\"" + Common.Names.PaginationSettings + "\"/> class." },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter{Name =  "pagesize", ParamType = "int", Description = "The page size for the query."},
                  new MethodParameter{Name =  "pagenumber", ParamType = "int", Description = "The page number to get."},
                  new MethodParameter{Name =  "gettotalcount", ParamType = "bool", Description = "If true get the total count of the query. Use with caution performance hit is possible."}
                }
            });
            CSharpFile.OpenBlock();
            CSharpFile.Line("PageSize = pagesize", "use property setter. there is custom logic");
            CSharpFile.Line("PageNumber = pagenumber", "use property setter. there is custom logic");
            CSharpFile.Line("_getTotalCount = gettotalcount");
            CSharpFile.CloseBlock();

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}
