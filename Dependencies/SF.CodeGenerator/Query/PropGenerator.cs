#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

/// <summary>
/// The typed query API: Prop&lt;TProps, TValue&gt; (one property of a container, with the .NET type of its column),
/// the typed filter builder whose methods only take values of the column's type, and the containers of the tables
/// and views. Everything goes through the same PropertyFilter, so the SQL and the parameters are the same.
/// </summary>
public static partial class QueryGenerator
{
    public static string GeneratePropClass(ModelContainer modelContainer)
    {
        return PropClass.Generate(modelContainer);
    }

    public static string GenerateFilterBuilderExtensions(ModelContainer modelContainer)
    {
        return FilterBuilderExtensions.Generate(modelContainer);
    }

    /// <summary>
    /// The container of a table or view; null when it has no property a query can use.
    /// </summary>
    public static string GenerateEntityProps(ModelContainer modelContainer, Model model, Schema schema, Entity entity)
    {
        return EntityProps.Generate(modelContainer, model, schema, entity);
    }

    /// <summary>
    /// The container of a stored procedure request; null when it has no parameters.
    /// </summary>
    public static string GenerateStoredProcProps(ModelContainer modelContainer, Model model, Schema schema, StoredProc proc)
    {
        return StoredProcProps.Generate(modelContainer, model, schema, proc);
    }

    private static class PropClass
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(new List<UsingNamespace>(1) { new UsingNamespace { Name = "System" } });
            CSharpFile.OpenNamespace(typeNamespace);

            var type = new ClassDeclaration
            {
                Name = Common.Names.Prop,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = new GenericParameterList { _genericProp, new GenericParameter { Name = Common.Names.GenericValue } },
                Summary = new List<string>
                {
                    "One property of a container, with the .NET type of its column. Only generated code creates them (internal constructor).",
                    "The filter type and the binary flag come from TValue; the nullability is written by the generator."
                }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.Line($"private static readonly Type _filterType = FilterTypes.GetFilterType(typeof({Common.Names.GenericValue}))");
            CSharpFile.Line($"private static readonly bool _isBinary = FilterTypes.IsBinary(typeof({Common.Names.GenericValue}))");
            CSharpFile.EmptyLine();
            CSharpFile.Line("private readonly int _enumValue");
            CSharpFile.Line("private readonly string _name");
            CSharpFile.Line("private readonly bool _isNullable");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.Prop,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>(3)
                {
                    new MethodParameter { Name = "enumValue", ParamType = "int" },
                    new MethodParameter { Name = "name", ParamType = "string" },
                    new MethodParameter { Name = "isNullable", ParamType = "bool" }
                }
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_enumValue = enumValue");
            CSharpFile.Line("_name = name");
            CSharpFile.Line("_isNullable = isNullable");
            CSharpFile.CloseBlock();

            PropertyExtensions.FullNoSetter(TypeInfo.Int, "EnumValue", backingField: "_enumValue");
            PropertyExtensions.FullNoSetter(TypeInfo.String, "Name", backingField: "_name");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsNullable", backingField: "_isNullable");

            var method = new MethodDeclaration
            {
                Name = "CreateFilter",
                AccessModifier = AccessModifiers.Internal,
                ReturnType = new TypeInfo($"{Common.Names.PropertyFilter}<{Common.Names.GenericProp}>"),
                Parameters = new List<MethodParameter>(3)
                {
                    new MethodParameter { Name = "condition", ParamType = _conditionEnumType },
                    new MethodParameter { Name = "op", ParamType = _operatorEnumType },
                    new MethodParameter { Name = "value", ParamType = "object" }
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"new {Common.Names.PropertyFilter}<{Common.Names.GenericProp}>(_enumValue, _name, _filterType, _isNullable, _isBinary, condition, op, value)");
            CSharpFile.CloseBlock();

            method.Name = "CreateSort";
            method.ReturnType = new TypeInfo(Common.Names.ISortFilter);
            method.Parameters = new List<MethodParameter>(1) { new MethodParameter { Name = "sortDirection", ParamType = _sortDirectionEnumType } };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"new {Common.Names.PropertySortFilter}(_enumValue, _name, sortDirection)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();

            GenerateFilterTypes(typeNamespace);
            GeneratePropertySortFilter(typeNamespace);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        private static void GenerateFilterTypes(string typeNamespace)
        {
            var type = new ClassDeclaration
            {
                Name = "FilterTypes",
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Internal,
                IsStatic = true,
                Summary = new List<string>
                {
                    "The filter type of a column from the .NET type of its Prop: the underlying type of a nullable, the element type of an array. Binary columns are the byte[] ones."
                }
            };

            CSharpFile.OpenClass(type);

            var method = new MethodDeclaration
            {
                Name = "GetFilterType",
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                ReturnType = new TypeInfo("Type"),
                Parameters = new List<MethodParameter>(1) { new MethodParameter { Name = "valueType", ParamType = "Type" } }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("valueType.IsArray");
            CSharpFile.Return("valueType.GetElementType()");
            CSharpFile.EmptyLine();
            CSharpFile.Line("var underlying = Nullable.GetUnderlyingType(valueType)");
            CSharpFile.Return("underlying ?? valueType");
            CSharpFile.CloseBlock();

            method.Name = "IsBinary";
            method.IsStatic = true;
            method.ReturnType = TypeInfo.Bool;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("valueType == typeof(byte[])");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }

        private static void GeneratePropertySortFilter(string typeNamespace)
        {
            var type = new ClassDeclaration
            {
                Name = Common.Names.PropertySortFilter,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Internal,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Interfaces = new List<string> { Common.Names.ISortFilter },
                Summary = new List<string> { "The sorting criteria of a Prop: what the SQL engine reads from ISortFilter." }
            };

            CSharpFile.OpenClass(type);

            PropertyExtensions.AutoNoSetter(TypeInfo.Int, "PropertyId");
            PropertyExtensions.AutoNoSetter(TypeInfo.String, "PropertyName");
            PropertyExtensions.AutoNoSetter(new TypeInfo(_sortDirectionEnumType), "SortDirection");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.PropertySortFilter,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>(3)
                {
                    new MethodParameter { Name = "propertyId", ParamType = "int" },
                    new MethodParameter { Name = "propertyName", ParamType = "string" },
                    new MethodParameter { Name = "sortDirection", ParamType = _sortDirectionEnumType }
                }
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("PropertyId = propertyId");
            CSharpFile.Line("PropertyName = propertyName");
            CSharpFile.Line($"{_sortDirectionEnumType} = sortDirection");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }

    private static class FilterBuilderExtensions
    {
        //the value types the filter builder compares, as the enum API does
        private static readonly string[] _comparedTypes = { "int", "long", "short", "byte", "decimal", "double", "DateTime" };

        //the value types IN() takes, as the enum API does (string is added on its own)
        private static readonly string[] _inTypes = { "int", "long", "short", "byte" };

        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(new List<UsingNamespace>(1) { new UsingNamespace { Name = "System" } });
            CSharpFile.OpenNamespace(typeNamespace);

            var type = new ClassDeclaration
            {
                Name = Common.Names.FilterBuilderExtensions,
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                Summary = new List<string>
                {
                    "The filters that apply to some types only: text filters on string properties, comparisons on numbers",
                    "and dates, date parts on dates, IN() on whole numbers and strings, and null checks on nullable properties."
                }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.OpenRegion("Strings");
            GenerateForColumn("StartsWith", "string", "string", "StartsWith", "starts with", "");
            GenerateForColumn("EndsWith", "string", "string", "EndsWith", "ends with", "");
            GenerateForColumn("DoesNotStartWith", "string", "string", "DoesNotStartWith", "does not start with", "");
            GenerateForColumn("DoesNotEndWith", "string", "string", "DoesNotEndWith", "does not end with", "");
            GenerateForColumn("Like", "string", "string", "Contains", "contains", "");
            GenerateForColumn("NotLike", "string", "string", "DoesNotContain", "does not contain", "");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("In");
            GenerateInMethods("In", "Contains", "is IN", "This translates to an IN() clause in SQL.");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Not In");
            GenerateInMethods("NotIn", "DoesNotContain", "is not IN", "This translates to a NOT IN() clause in SQL.");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Greater Than");
            GenerateComparisonMethods("GreaterThan", "is greater than");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Less Than");
            GenerateComparisonMethods("LessThan", "is less than");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Greater Than Or Equals");
            GenerateComparisonMethods("GreaterThanOrEquals", "is greater than or equals");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Less Than Or Equals");
            GenerateComparisonMethods("LessThanOrEquals", "is less than or equals");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Date Only");
            GenerateDatePartMethods("Date", "DateTime", "Takes into account only the date and not the hours, minutes, seconds, milliseconds.");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Year");
            GenerateDatePartMethods("Year", "int", "Takes into account only the Year.");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Month");
            GenerateDatePartMethods("Month", "int", "Takes into account only the Month.");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Day");
            GenerateDatePartMethods("Day", "int", "Takes into account only the Day.");
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Is Null");
            GenerateNullMethod("IsNull", "Equals", "is null");
            GenerateNullMethod("IsNotNull", "NotEquals", "is not null");
            CSharpFile.CloseRegion();

            CSharpFile.CloseType();
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        private static void GenerateInMethods(string name, string condition, string descCondition, string extraInfo)
        {
            foreach(var t in _inTypes)
            {
                GenerateForColumn(name, t, t + "[]", condition, descCondition, extraInfo);
                GenerateForColumn(name, t + "?", t + "[]", condition, descCondition, extraInfo);
            }

            GenerateForColumn(name, "string", "string[]", condition, descCondition, extraInfo);
        }

        private static void GenerateComparisonMethods(string name, string descCondition)
        {
            foreach(var t in _comparedTypes)
            {
                GenerateForColumn(name, t, t, name, descCondition, "");
                GenerateForColumn(name, t + "?", t, name, descCondition, "");
            }
        }

        /// <summary>
        /// EqualsDate, NotEqualsDate ... LessThanOrEqualsDate (and Year, Month, Day) on DateTime properties.
        /// </summary>
        private static void GenerateDatePartMethods(string part, string paramType, string extraInfo)
        {
            var operations = new[]
            {
                new[] { "Equals", "is equals" },
                new[] { "NotEquals", "is not equals" },
                new[] { "GreaterThan", "is greater than" },
                new[] { "LessThan", "is less than" },
                new[] { "GreaterThanOrEquals", "is greater than or equals" },
                new[] { "LessThanOrEquals", "is less than or equals" },
            };

            foreach(var operation in operations)
            {
                GenerateForColumn(operation[0] + part, "DateTime", paramType, part + operation[0], operation[1], extraInfo);
                GenerateForColumn(operation[0] + part, "DateTime?", paramType, part + operation[0], operation[1], extraInfo);
            }
        }

        /// <summary>
        /// One filter method for the properties of one type: an extension of the FilterBuilder of that type.
        /// </summary>
        private static void GenerateForColumn(string name, string columnType, string paramType, string condition, string descCondition, string extraInfo)
        {
            var summary = new List<string>(3)
            {
                "Add a filter to the predicate(where clause).",
                $"Where the Property {descCondition} the passed in value."
            };

            if(!string.IsNullOrWhiteSpace(extraInfo))
                summary.Add(extraInfo);

            var method = new MethodDeclaration
            {
                Name = name,
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                ReturnType = new TypeInfo($"{Common.Names.PredicateBuilder}<{_genericParams}>"),
                ReturnTypeForComments = "PredicateBuilder",
                GenericParams = new GenericParameterList(_genericParams),
                Summary = summary,
                Parameters = new List<MethodParameter>(2)
                {
                    new MethodParameter { Name = "filter", ParamType = "this " + ComposeBuilderType(columnType), Description = "The filter builder of a property of this type." },
                    new MethodParameter { Name = "value", ParamType = paramType, Description = "The value to filter by." }
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"filter.AddFilter({_conditionEnumType}.{condition}, value)");
            CSharpFile.CloseBlock();
        }

        /// <summary>
        /// IsNull and IsNotNull: on nullable value types and on strings only.
        /// </summary>
        private static void GenerateNullMethod(string name, string condition, string descCondition)
        {
            var nullableValue = new MethodDeclaration
            {
                Name = name,
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                ReturnType = new TypeInfo($"{Common.Names.PredicateBuilder}<{_genericParams}>"),
                ReturnTypeForComments = "PredicateBuilder",
                GenericParams = new GenericParameterList
                {
                    _genericProp,
                    new GenericParameter { Name = Common.Names.GenericValue, Constraint = "struct" }
                },
                Summary = new List<string> { "Add a filter to the predicate(where clause).", $"Where the Property {descCondition}." },
                Parameters = new List<MethodParameter>(1)
                {
                    new MethodParameter { Name = "filter", ParamType = "this " + ComposeBuilderType(Common.Names.GenericValue + "?"), Description = "The filter builder of a nullable property." }
                }
            };

            CSharpFile.DeclareMethod(nullableValue);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"filter.AddFilter({_conditionEnumType}.{condition}, null)");
            CSharpFile.CloseBlock();

            var text = new MethodDeclaration
            {
                Name = name,
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                ReturnType = new TypeInfo($"{Common.Names.PredicateBuilder}<{_genericParams}>"),
                ReturnTypeForComments = "PredicateBuilder",
                GenericParams = new GenericParameterList(_genericParams),
                Summary = new List<string> { "Add a filter to the predicate(where clause).", $"Where the Property {descCondition}." },
                Parameters = new List<MethodParameter>(1)
                {
                    new MethodParameter { Name = "filter", ParamType = "this " + ComposeBuilderType("string"), Description = "The filter builder of a string property." }
                }
            };

            CSharpFile.DeclareMethod(text);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"filter.AddFilter({_conditionEnumType}.{condition}, null)");
            CSharpFile.CloseBlock();
        }

        private static string ComposeBuilderType(string columnType)
        {
            return $"{Common.Names.FilterBuilder}<{Common.Names.GenericProp}, {columnType}>";
        }
    }

    private static class EntityProps
    {
        public static string Generate(ModelContainer modelContainer, Model model, Schema schema, Entity entity)
        {
            if(!(entity.Ps?.Count > 0))
                return null;

            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var settings = modelContainer.CommonSettings;

            var hasXml = false;
            foreach(var p in entity.Ps)
            {
                if(p.Xml)
                    hasXml = true;
            }

            var usingNamespaces = new List<UsingNamespace>(4) { new UsingNamespace { Name = "System" } };
            if(hasXml)
                usingNamespaces.Add(new UsingNamespace { Name = "System.Xml.Linq" });

            usingNamespaces.Add(new UsingNamespace { Name = modelContainer.CommonLayers.Query.FullNamespace(settings) });

            if(entity.NPs?.Count > 0)
            {
                var propEnumNamespace = Common.GenerateTypeNamespace(settings, model.Layers.PropEnum, model, schema);
                var navPropEnumNamespace = Common.GenerateTypeNamespace(settings, model.Layers.NavPropEnum, model, schema);
                if(string.CompareOrdinal(propEnumNamespace, navPropEnumNamespace) != 0)
                    usingNamespaces.Add(new UsingNamespace { Name = navPropEnumNamespace });
            }

            var propEnum = Common.GeneratePropEnumName(entity);
            var containerName = Common.GeneratePropClassName(entity);
            var type = new ClassDeclaration
            {
                Name = containerName,
                Namespace = Common.GenerateTypeNamespace(settings, model.Layers.PropEnum, model, schema),
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                BaseClass = entity.NPs?.Count > 0 ? Common.Names.NavPropertyContainer : Common.Names.PropertyContainer,
                Summary = new List<string>
                {
                    $"The properties of {entity.N} with the .NET type of each column.",
                    "Pass them to Where, Or, OrderBy and Set: the compiler then checks the values the query uses with them."
                }
            };

            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            foreach(var p in entity.Ps)
            {
                var valueType = ComposeValueType(p);
                var propType = $"{Common.Names.Prop}<{containerName}, {valueType}>";
                CSharpFile.Field(new FieldDeclaration
                {
                    Name = p.N,
                    AccessModifier = AccessModifiers.Public,
                    IsStatic = true,
                    IsReadonly = true,
                    ReturnType = new TypeInfo(propType),
                    DefaultValue = $"new {propType}((int){propEnum}.{p.N}, \"{p.N}\", {ComposeBool(p.Nul != YesOrNo.No)})",
                    Summary = new List<string> { $"Type: {valueType}." }
                });
            }

            GenerateCreateChange(entity, propEnum);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = containerName,
                AccessModifier = AccessModifiers.Private
            });
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();

            if(entity.NPs?.Count > 0)
                GenerateNavContainer(modelContainer, model, entity, schema);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        /// <summary>
        /// CreateChange(propertyId, value): the change of the property with that id (its XProp value), for code that
        /// picks the property at run time; UpdateValuesBuilder.Set(change) adds it. A switch on the property enum, one
        /// case per property, each the typed Prop's filter.
        /// </summary>
        private static void GenerateCreateChange(Entity entity, string propEnum)
        {
            CSharpFile.DeclareMethod(new MethodDeclaration
            {
                Name = "CreateChange",
                IsStatic = true,
                ReturnType = new TypeInfo($"{Common.Names.IQueryFilter}<{Common.GeneratePropClassName(entity)}>"),
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "propertyId", ParamType = "int", Description = $"The property's id: its {propEnum} value." },
                  new MethodParameter { Name = "value", ParamType = "object", Description = "The new value, of the property's type." },
                },
                Summary = new List<string> { $"The change that sets the property of {entity.N} with id propertyId to value: what Set(prop, value) adds to an update. Pass it to Set(change). Throws ArgumentException for an id that is not a property, InvalidCastException for a value of another type." }
            });
            CSharpFile.OpenBlock();
            CSharpFile.Switch($"({propEnum})propertyId");
            CSharpFile.OpenBlock();
            foreach(var p in entity.Ps)
            {
                CSharpFile.Case($"{propEnum}.{p.N}");
                CSharpFile.Indent();
                CSharpFile.Return($"{Common.Names.QueryFilter}.New({p.N}, {Common.Names.FilterConditionsEnum}.Equals, ({ComposeValueType(p)})value)");
                CSharpFile.Outdent();
            }

            CSharpFile.CaseDefault();
            CSharpFile.Indent();
            CSharpFile.Line($"throw new ArgumentException($\"{{propertyId}} is not a property of {entity.N}.\", nameof(propertyId))");
            CSharpFile.Outdent();
            CSharpFile.CloseBlock();
            CSharpFile.CloseBlock();
        }

        private static void GenerateNavContainer(ModelContainer modelContainer, Model model, Entity entity, Schema schema)
        {
            var settings = modelContainer.CommonSettings;
            var containerName = Common.GeneratePropClassName(entity);
            var navEnum = Common.GenerateNavPropEnumName(entity);
            var navClassName = Common.GenerateNavPropClassName(entity);

            var type = new ClassDeclaration
            {
                Name = navClassName,
                Namespace = Common.GenerateTypeNamespace(settings, model.Layers.PropEnum, model, schema),
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string>
                {
                    $"The navigation properties of {entity.N}, each leading to the container of the entity on the other end of the foreign key."
                }
            };

            CSharpFile.OpenClass(type);

            foreach(var nav in entity.NPs)
            {
                var fk = model.FKeys.GetById(nav.FKId);
                if(!(fk.FKParts?.Count > 0))
                    continue;

                var isParent = nav.End == NavPropertyEnd.Parent;
                var targetSchema = isParent ? fk.PSc : fk.CSc;
                var targetEntity = isParent ? fk.PEn : fk.CEn;
                var targetType = $"{Common.GenerateTypeNamespace(settings, model.Layers.PropEnum, model, targetSchema)}.{Common.GeneratePropClassName(targetEntity)}";
                var navType = $"{Common.Names.NavProp}<{containerName}, {targetType}>";
                CSharpFile.Field(new FieldDeclaration
                {
                    Name = nav.N,
                    AccessModifier = AccessModifiers.Public,
                    IsStatic = true,
                    IsReadonly = true,
                    ReturnType = new TypeInfo(navType),
                    DefaultValue = $"new {navType}((int){navEnum}.{nav.N}, \"{nav.N}\")",
                    Summary = new List<string> { $"Type: {targetType}." }
                });
            }

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = navClassName,
                AccessModifier = AccessModifiers.Private
            });
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }

    private static class StoredProcProps
    {
        public static string Generate(ModelContainer modelContainer, Model model, Schema schema, StoredProc proc)
        {
            if(!proc.HPar || !(proc.Par?.Count > 0))
                return null;

            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var settings = modelContainer.CommonSettings;
            var containerName = Common.GeneratePropClassName(proc);
            var propEnum = Common.GeneratePropEnumName(proc);

            var hasXml = false;
            foreach(var p in proc.Par)
            {
                if(p.Xml)
                    hasXml = true;
            }

            var usingNamespaces = new List<UsingNamespace>(3) { new UsingNamespace { Name = "System" } };
            if(hasXml)
                usingNamespaces.Add(new UsingNamespace { Name = "System.Xml.Linq" });

            usingNamespaces.Add(new UsingNamespace { Name = modelContainer.CommonLayers.Query.FullNamespace(settings) });

            var type = new ClassDeclaration
            {
                Name = containerName,
                Namespace = Common.GenerateTypeNamespace(settings, model.Layers.PropEnum, model, schema),
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                BaseClass = Common.Names.PropertyContainer,
                Summary = new List<string>
                {
                    $"The parameters of stored procedure {proc.N} with the .NET type of each one."
                }
            };

            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            foreach(var p in proc.Par)
            {
                var valueType = ComposeValueType(p);
                var propType = $"{Common.Names.Prop}<{containerName}, {valueType}>";
                CSharpFile.Field(new FieldDeclaration
                {
                    Name = p.N,
                    AccessModifier = AccessModifiers.Public,
                    IsStatic = true,
                    IsReadonly = true,
                    ReturnType = new TypeInfo(propType),
                    DefaultValue = $"new {propType}((int){propEnum}.{p.N}, \"{p.N}\", {ComposeBool(p.Nul != YesOrNo.No)})",
                    Summary = new List<string> { $"Type: {valueType}." }
                });
            }

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = containerName,
                AccessModifier = AccessModifiers.Private
            });
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }

    /// <summary>
    /// The property's type as the entity declares it: a nullable column of a value type gets a question mark.
    /// </summary>
    private static string ComposeValueType(Property p)
    {
        var isNullableValue = p.Nul == YesOrNo.Yes && !p.Str && p.T.LastIndexOf(']') < 0 && !p.Geog && !p.Geom && !p.Xml;
        return isNullableValue ? p.T + "?" : p.T;
    }

    private static string ComposeBool(bool value)
    {
        return value ? "true" : "false";
    }
}
