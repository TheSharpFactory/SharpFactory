#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

/// <summary>
/// Enable System.Text.Json: one source generation context for each database, in the Entities project's database
/// folder. It lists every class of the database's Entities project (tables, views, and the request and result classes
/// of procedures and functions), each also as a List, so System.Text.Json reads and writes them without reflection.
/// </summary>
public static class JsonContextGenerator
{
    private const string _xDocumentConverter = "XDocumentJsonConverter";

    /// <summary>
    /// The context's class name: AppDb gives AppDbJsonContext.
    /// </summary>
    public static string GenerateContextName(Model model)
    {
        return model.Name + "JsonContext";
    }

    /// <summary>
    /// The converter's class name, for its file.
    /// </summary>
    public static string GenerateXDocumentConverterName()
    {
        return _xDocumentConverter;
    }

    /// <summary>
    /// True when a class of the database has an xml column (an XDocument property): the context then needs the
    /// converter.
    /// </summary>
    public static bool HasXml(Model model)
    {
        var finder = new XmlFinder();
        new ClassWalker(model).Walk(finder);
        return finder.Found;
    }

    public static string Generate(CommonSettings commonSettings, Model model)
    {
        var collector = new ClassCollector(commonSettings, model);
        new ClassWalker(model).Walk(collector);
        if(collector.Attributes.Count == 0)
            return string.Empty;

        CSharpFile.Start(commonSettings.IndentSize);

        var name = GenerateContextName(model);
        var attributes = new List<string>(collector.Attributes.Count + 1);
        if(HasXml(model))
            attributes.Add($"JsonSourceGenerationOptions(Converters = new[] {{ typeof({_xDocumentConverter}) }})");

        attributes.AddRange(collector.Attributes);

        var type = new ClassDeclaration
        {
            Name = name,
            Namespace = GenerateModelNamespace(commonSettings, model),
            IsPartial = true,
            InheritanceModifier = InheritanceModifiers.Sealed,
            BaseClass = "JsonSerializerContext",
            Attributes = attributes,
            Summary = new List<string>(2)
            {
                $"System.Text.Json source generation for the classes of {model.Name}, each also as a List.",
                $"Use it like JsonSerializer.Serialize(value, {name}.Default.{collector.FirstPropertyName}) or add {name}.Default to JsonSerializerOptions.TypeInfoResolverChain.",
            }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(new List<UsingNamespace>(2)
        {
            new UsingNamespace { Name = "System.Collections.Generic" },
            new UsingNamespace { Name = "System.Text.Json.Serialization" },
        });
        CSharpFile.OpenClassWithNamespace(type);
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }

    /// <summary>
    /// The converter the context uses for xml columns: the XML text as a JSON string.
    /// </summary>
    public static string GenerateXDocumentConverter(CommonSettings commonSettings, Model model)
    {
        CSharpFile.Start(commonSettings.IndentSize);

        var type = new ClassDeclaration
        {
            Name = _xDocumentConverter,
            Namespace = GenerateModelNamespace(commonSettings, model),
            InheritanceModifier = InheritanceModifiers.Sealed,
            BaseClass = "JsonConverter<XDocument>",
            Summary = new List<string>(1) { "xml columns in JSON: an XDocument is written as its XML text and read back from it" }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(new List<UsingNamespace>(4)
        {
            new UsingNamespace { Name = "System" },
            new UsingNamespace { Name = "System.Text.Json" },
            new UsingNamespace { Name = "System.Text.Json.Serialization" },
            new UsingNamespace { Name = "System.Xml.Linq" },
        });
        CSharpFile.OpenClassWithNamespace(type);
        CSharpFile.LineNoEnding("public override XDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        CSharpFile.OpenBlock();
        CSharpFile.Line("return XDocument.Parse(reader.GetString())");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.LineNoEnding("public override void Write(Utf8JsonWriter writer, XDocument value, JsonSerializerOptions options)");
        CSharpFile.OpenBlock();
        CSharpFile.Line("writer.WriteStringValue(value.ToString(SaveOptions.DisableFormatting))");
        CSharpFile.CloseBlock();
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }

    private static string GenerateModelNamespace(CommonSettings commonSettings, Model model)
    {
        return $"{model.Layers.Entity.FullNamespace(commonSettings)}.{model.Name}";
    }

    #region Classes of the database
    /// <summary>
    /// What is done with each class of the Entities project.
    /// </summary>
    private abstract class ClassVisitor
    {
        public abstract void Visit(Schema schema, string className, PropertyCollection props);
    }

    /// <summary>
    /// Every class the Entities project has for a database, in generation order: tables, views, then the request and
    /// result classes of procedures and functions (the same conditions as their files).
    /// </summary>
    private sealed class ClassWalker
    {
        private readonly Model _model;

        public ClassWalker(Model model)
        {
            _model = model;
        }

        public void Walk(ClassVisitor visitor)
        {
            WalkEntities(visitor, CategoryNames.Entities, false);
            WalkEntities(visitor, CategoryNames.Views, true);
            WalkStoredProcs(visitor);
            WalkFunctions(visitor);
        }

        private void WalkEntities(ClassVisitor visitor, string categoryName, bool isView)
        {
            var category = FindCategory(categoryName);
            if(category?.Schemas == null)
                return;

            foreach(var schema in category.Schemas)
            {
                var entities = isView ? schema.Views : schema.Entities;
                if(entities == null)
                    continue;

                foreach(var entity in entities)
                    visitor.Visit(schema, Common.GenerateTypeName(entity), entity.Ps);
            }
        }

        private void WalkStoredProcs(ClassVisitor visitor)
        {
            var category = FindCategory(CategoryNames.Procs);
            if(category?.Schemas == null)
                return;

            foreach(var schema in category.Schemas)
            {
                if(schema.StoredProcs == null)
                    continue;

                foreach(var proc in schema.StoredProcs)
                {
                    if(!(proc.Par?.Count > 0) && proc.En == null)
                        continue;

                    if(proc.HPar)
                        visitor.Visit(schema, Common.GenerateRequestTypeName(proc.N), proc.Par);

                    if(proc.REn)
                        visitor.Visit(schema, proc.En.N, proc.En.Ps);
                }
            }
        }

        private void WalkFunctions(ClassVisitor visitor)
        {
            var category = FindCategory(CategoryNames.Funcs);
            if(category?.Schemas == null)
                return;

            foreach(var schema in category.Schemas)
            {
                if(schema.Functions == null)
                    continue;

                foreach(var func in schema.Functions)
                {
                    if(!(func.Par?.Count > 0) && func.En == null)
                        continue;

                    if(func.Par?.Count > 0)
                        visitor.Visit(schema, Common.GenerateRequestTypeName(func.N), func.Par);

                    if(func.En?.Ps?.Count > 0)
                        visitor.Visit(schema, func.En.N, func.En.Ps);
                }
            }
        }

        private Category FindCategory(string categoryName)
        {
            if(_model.Categories == null || !_model.Categories.Contains(categoryName))
                return null;

            return _model.Categories.GetByName(categoryName);
        }
    }

    /// <summary>
    /// The [JsonSerializable] of each class and of its List, named with the schema so two tables with the same name in
    /// different schemas both get one: Sales.Customer gives SalesCustomer and ListSalesCustomer.
    /// </summary>
    private sealed class ClassCollector : ClassVisitor
    {
        private readonly CommonSettings _commonSettings;
        private readonly Model _model;

        public ClassCollector(CommonSettings commonSettings, Model model)
        {
            _commonSettings = commonSettings;
            _model = model;
        }

        public List<string> Attributes { get; } = new List<string>();

        /// <summary>
        /// The context property of the first class, for the example in the context's summary.
        /// </summary>
        public string FirstPropertyName { get; private set; }

        public override void Visit(Schema schema, string className, PropertyCollection props)
        {
            var fullName = "global::" + Common.GenerateTypeFullName(_commonSettings, _model.Layers.Entity, _model, schema, className);
            var propertyName = schema.Name + className;
            if(FirstPropertyName == null)
                FirstPropertyName = propertyName;

            Attributes.Add($"JsonSerializable(typeof({fullName}), TypeInfoPropertyName = \"{propertyName}\")");
            Attributes.Add($"JsonSerializable(typeof(List<{fullName}>), TypeInfoPropertyName = \"List{propertyName}\")");
        }
    }

    private sealed class XmlFinder : ClassVisitor
    {
        public bool Found { get; private set; }

        public override void Visit(Schema schema, string className, PropertyCollection props)
        {
            if(Found || props == null)
                return;

            foreach(var p in props)
            {
                if(p.Xml)
                {
                    Found = true;
                    return;
                }
            }
        }
    }
    #endregion
}
