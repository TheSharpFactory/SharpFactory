#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateNavPropCollection(ModelContainer modelContainer)
    {
        return NavPropCollection.Generate(modelContainer);
    }
    private static class NavPropCollection
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var navPropCollection = Common.Names.NavPropCollection;
            var navProp = Common.Names.NavProp;

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.NavPropCollection,
                Namespace = typeNamespace,
                IsPartial = false,
                Summary = new List<string>(1) { "This class is a collection of Navigation Properties." },
                Interfaces = new List<string> { $"List<{navProp}>" }
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenNamespace(typeNamespace);

            CSharpFile.OpenClass(type);

            CSharpFile.Line($"private readonly Dictionary<int, {navProp}> _items");
            PropertyExtensions.FullNoSetter(TypeInfo.Int, "TotalCount", backingField: "CalculateCount(this)", description: "Returns the total count of the entire tree. Takes into account all sub navigation properties.");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = navPropCollection,
                AccessModifier = AccessModifiers.Public,
                BaseConstructor = "base()"
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line($"_items = new Dictionary<int, {navProp}>()");
            CSharpFile.CloseBlock();

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = navPropCollection,
                AccessModifier = AccessModifiers.Public,
                BaseConstructor = "base(capacity)",
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "capacity", ParamType = "int" } }
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line($"_items = new Dictionary<int, {navProp}>(capacity)");
            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "Get",
                Parameters = new List<MethodParameter> { new MethodParameter { ParamType = navProp, Name = "item", Description = "Navigation Property" } },
                ReturnType = new TypeInfo(navProp),
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("!_items.ContainsKey(item.IntValue)");
            CSharpFile.Line("throw new ArgumentException($\"Navigation Property {item.ToString()} does not exist in the collection.\")");
            CSharpFile.Return("_items[item.IntValue]");
            CSharpFile.CloseBlock();

            method.Name = "Add";
            method.ReturnType = TypeInfo.Void;
            method.InheritanceModifier = InheritanceModifiers.New;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("_items.ContainsKey(item.IntValue)");
            CSharpFile.Line("throw new ArgumentException($\"Navigation Property {item.ToString()} already exists in the collection.\")");
            CSharpFile.Line("_items.Add(item.IntValue, item)");
            CSharpFile.Line("base.Add(item)");
            CSharpFile.CloseBlock();

            method.Name = "Insert";
            method.ReturnType = TypeInfo.Void;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Insert(0, new MethodParameter { ParamType = "int", Name = "index", Description = "Index to insert at" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("_items.ContainsKey(item.IntValue)");
            CSharpFile.Line("throw new ArgumentException($\"Navigation Property {item.ToString()} already exists in the collection.\")");
            CSharpFile.Line("_items.Add(item.IntValue, item)");
            CSharpFile.Line("base.Insert(index, item)");
            CSharpFile.CloseBlock();

            method.Name = "Remove";
            method.ReturnType = TypeInfo.Bool;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.RemoveAt(0);

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line("_items.Remove(item.IntValue)");
            CSharpFile.Return("base.Remove(item)");
            CSharpFile.CloseBlock();

            method.Name = "RemoveAt";
            method.ReturnType = TypeInfo.Void;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = "int", Name = "index", Description = "Index to insert at" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line("_items.Remove(this[index].IntValue)");
            CSharpFile.Line("base.RemoveAt(index)");
            CSharpFile.CloseBlock();

            method.Name = "Contains";
            method.ReturnType = TypeInfo.Bool;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = navProp, Name = "item", Description = "Navigation Property" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_items.ContainsKey(item.IntValue)");
            CSharpFile.CloseBlock();

            method.Name = "AddRange";
            method.ReturnType = TypeInfo.Void;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = $"IEnumerable<{navProp}>", Name = "collection", Description = "Navigation Property Collection" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.ThrowNotSupportedException();
            CSharpFile.CloseBlock();

            method.Name = "InsertRange";
            method.ReturnType = TypeInfo.Void;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = "int", Name = "index", Description = "Index to insert at" });
            method.Parameters.Add(new MethodParameter { ParamType = $"IEnumerable<{navProp}>", Name = "collection", Description = "Navigation Property Collection" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.ThrowNotSupportedException();
            CSharpFile.CloseBlock();

            method.Name = "RemoveRange";
            method.ReturnType = TypeInfo.Void;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = "int", Name = "index", Description = "Index to remove at" });
            method.Parameters.Add(new MethodParameter { ParamType = "int", Name = "count", Description = "Count of items to remove" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.ThrowNotSupportedException();
            CSharpFile.CloseBlock();

            method.Name = "RemoveAll";
            method.ReturnType = TypeInfo.Int;
            method.InheritanceModifier = InheritanceModifiers.New;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = $"Predicate<{navProp}>", Name = "match", Description = "Predicate to match" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.ThrowNotSupportedException();
            CSharpFile.CloseBlock();

            method.Name = "CalculateCount";
            method.ReturnType = TypeInfo.Int;
            method.AccessModifier = AccessModifiers.Private;
            method.InheritanceModifier = InheritanceModifiers.None;
            method.IsStatic = true;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { ParamType = navPropCollection, Name = "list", Description = "Navigation Property list" });

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("list == null");
            CSharpFile.LineIndented("return 0");
            CSharpFile.EmptyLine();
            CSharpFile.Line("var totalCount = list.Count");
            CSharpFile.ForEach("item", "list");
            CSharpFile.LineIndented("totalCount += CalculateCount(item.NavProps)");
            CSharpFile.EmptyLine();
            CSharpFile.Return("totalCount");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();

            var genericType = new ClassDeclaration
            {
                Name = Common.Names.NavPropCollection,
                Namespace = typeNamespace,
                IsPartial = false,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string>(1) { "This class is a collection of Navigation Properties." },
                GenericParams = _genericParamsWithNavProps,
                //a collection, so it takes a collection initializer: new NavProps<CustomerProperty> { CustomerNavProperty.Orders }
                Interfaces = new List<string> { $"IEnumerable<{navProp}>" }
            };

            CSharpFile.OpenClass(genericType);

            CSharpFile.Line($"private readonly {navPropCollection} _items = new {navPropCollection}()");
            PropertyExtensions.FullNoSetter(new TypeInfo(navPropCollection), "Items", backingField: "_items");

            method = new MethodDeclaration
            {
                Name = "Add",
                ReturnType = TypeInfo.Void,
                GenericParams = new GenericParameterList { new GenericParameter { Name = "TTarget", Constraint = Common.Names.PropertyContainer } },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { ParamType = $"{navProp}<{Common.Names.GenericProp}, TTarget>", Name = "item", Description = "Navigation Property" }
                },
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line("_items.Add(item.CreateItem())");
            CSharpFile.CloseBlock();

            CSharpFile.DeclareMethod(new MethodDeclaration
            {
                Name = "GetEnumerator",
                ReturnType = new TypeInfo($"IEnumerator<{navProp}>"),
                Summary = new List<string>(1) { "The navigation properties added, in order." },
            });
            CSharpFile.OpenBlock();
            CSharpFile.Return("_items.GetEnumerator()");
            CSharpFile.CloseBlock();

            CSharpFile.DeclareMethod(new MethodDeclaration
            {
                Name = "System.Collections.IEnumerable.GetEnumerator",
                AccessModifier = AccessModifiers.None,
                ReturnType = new TypeInfo("System.Collections.IEnumerator"),
            });
            CSharpFile.OpenBlock();
            CSharpFile.Return("_items.GetEnumerator()");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();

            CSharpFile.CloseNamespace();

            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
}
