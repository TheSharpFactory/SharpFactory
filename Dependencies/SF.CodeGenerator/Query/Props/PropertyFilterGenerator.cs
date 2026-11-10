#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GeneratePropertyFilter(ModelContainer modelContainer)
    {
        return PropertyFilterClass.Generate(modelContainer);
    }

    private static class PropertyFilterClass
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(new List<UsingNamespace>(2)
            {
                new UsingNamespace { Name = "System" },
                new UsingNamespace { Name = "System.Collections.Generic" }
            });
            CSharpFile.OpenNamespace(typeNamespace);

            var type = new ClassDeclaration
            {
                Name = Common.Names.PropertyFilter,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Internal,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Interfaces = new List<string> { Common.Names.IQueryFilter },
                Summary = new List<string>
                {
                    "The filter rules, flags and messages of a Prop: the property id, name, filter type, nullability and binary flag come from the container."
                }
            };

            CSharpFile.OpenClass(type);

            GenerateStaticFields();
            CSharpFile.EmptyLine();
            GenerateInstanceFields();
            CSharpFile.EmptyLine();
            GenerateConstructor();
            CSharpFile.EmptyLine();
            GenerateProperties();
            CSharpFile.EmptyLine();
            GenerateSupportsCondition();

            CSharpFile.CloseType();
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        private static void GenerateStaticFields()
        {
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _stringConditions = {_hashSetForString}");
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _valueTypeConditions = {_hashSetForNumbersAndDateTime}");
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _dateConditions = {_hashSetForDates}");
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _yearConditions = {_hashSetForYear}");
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _monthConditions = {_hashSetForMonth}");
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _dayConditions = {_hashSetForDay}");
            CSharpFile.Line($"private static readonly HashSet<{_conditionEnumType}> _containConditions = {_hashSetForContains}");
            CSharpFile.Line("private static readonly HashSet<string> _valueTypes = new HashSet<string> { \"Byte\", \"Int16\", \"Int32\", \"Int64\", \"Single\", \"Double\", \"Decimal\", \"DateTime\" }");
        }

        private static void GenerateInstanceFields()
        {
            CSharpFile.Line("private readonly int _propertyId");
            CSharpFile.Line("private readonly string _propName");
            CSharpFile.Line($"private readonly {_conditionEnumType} _condition");
            CSharpFile.Line("private readonly string _typeName");
            CSharpFile.Line("private readonly Type _type");
            CSharpFile.Line("private readonly TypeCode _typeCode");
            CSharpFile.Line("private readonly bool _isArray");
            CSharpFile.Line("private readonly bool _isDateFilter");
            CSharpFile.Line("private readonly bool _isYearFilter");
            CSharpFile.Line("private readonly bool _isMonthFilter");
            CSharpFile.Line("private readonly bool _isDayFilter");
            CSharpFile.Line("private readonly bool _isNullable");
            CSharpFile.Line("private readonly bool _isBoolean");
            CSharpFile.Line("private readonly bool _hasValue = true");
            CSharpFile.Line("private readonly object _value");
            CSharpFile.Line($"private {_operatorEnumType} _operator");
            CSharpFile.Line("private int _startExpressions");
            CSharpFile.Line("private int _endExpressions");
        }

        private static void GenerateConstructor()
        {
            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.PropertyFilter,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>(8)
                {
                    new MethodParameter { Name = "propertyId", ParamType = "int" },
                    new MethodParameter { Name = "propertyName", ParamType = "string" },
                    new MethodParameter { Name = "filterType", ParamType = "Type" },
                    new MethodParameter { Name = "isNullable", ParamType = "bool" },
                    new MethodParameter { Name = "isBinary", ParamType = "bool" },
                    new MethodParameter { Name = "condition", ParamType = _conditionEnumType },
                    new MethodParameter { Name = "op", ParamType = _operatorEnumType },
                    new MethodParameter { Name = "value", ParamType = "object" }
                }
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_propertyId = propertyId");
            CSharpFile.Line("_propName = propertyName");
            CSharpFile.Line("_type = filterType");
            CSharpFile.Line("_typeCode = Type.GetTypeCode(_type)");
            CSharpFile.Line("_typeName = _type.Name");
            CSharpFile.Line("_condition = condition");
            CSharpFile.Line("_isNullable = isNullable");
            CSharpFile.Line("_isDateFilter = _dateConditions.Contains(_condition)");
            CSharpFile.Line("_isYearFilter = _yearConditions.Contains(_condition)");
            CSharpFile.Line("_isMonthFilter = _monthConditions.Contains(_condition)");
            CSharpFile.Line("_isDayFilter = _dayConditions.Contains(_condition)");
            CSharpFile.Line("var isContains = _containConditions.Contains(_condition)");
            CSharpFile.Line("var isDatePart = _isDateFilter || _isYearFilter || _isMonthFilter || _isDayFilter");
            CSharpFile.Line("var requiresInt = _isYearFilter || _isMonthFilter || _isDayFilter");
            CSharpFile.Line("_isBoolean = _type == typeof(bool)");
            CSharpFile.Line("_operator = op");
            CSharpFile.Line("_value = value");
            CSharpFile.EmptyLine();

            CSharpFile.If("_value == null");
            CSharpFile.OpenBlock();
            CSharpFile.Line("_hasValue = false");
            CSharpFile.If("!_isNullable");
            CSharpFile.LineIndented("throw new ArgumentException(\"Property: \" + _propName + \" is not nullable.\")");
            CSharpFile.If("isContains");
            CSharpFile.LineIndented("throw new ArgumentException(\"Any Contains or DoesNotContain condition requires a non null value.\")");
            CSharpFile.If("isDatePart");
            CSharpFile.LineIndented("throw new ArgumentException(\"Condition: \" + _condition.ToString() + \" cannot be applied to null values.\")");
            CSharpFile.If($"_condition != {_conditionEnumType}.Equals && _condition != {_conditionEnumType}.NotEquals");
            CSharpFile.LineIndented("throw new ArgumentException(\"When Value is null the property: \" + _propName + \" supports only Equals, NotEquals.\")");
            CSharpFile.Line("_isArray = false");
            CSharpFile.CloseBlock();
            CSharpFile.Else();
            CSharpFile.OpenBlock();
            CSharpFile.Line("var valType = _value.GetType()");
            CSharpFile.Line("var valTypeName = string.Empty");
            CSharpFile.If("valType.IsArray");
            CSharpFile.OpenBlock();
            CSharpFile.Line("valTypeName = valType.GetElementType().Name");
            CSharpFile.Line("_isArray = true");
            CSharpFile.Line("_hasValue = ((Array)_value).Length > 0");
            CSharpFile.CloseBlock();
            CSharpFile.Else();
            CSharpFile.OpenBlock();
            CSharpFile.Line("valTypeName = valType.Name");
            CSharpFile.Line("_isArray = false");
            CSharpFile.CloseBlock();
            CSharpFile.EmptyLine();

            CSharpFile.If("requiresInt");
            CSharpFile.OpenBlock();
            CSharpFile.If("string.CompareOrdinal(valTypeName, \"Int32\") != 0");
            CSharpFile.LineIndented("throw new ArgumentException(\"Invalid type. Condition: \" + _condition.ToString() + \" expects int or int[]).\")");
            CSharpFile.CloseBlock();
            CSharpFile.Else();
            CSharpFile.OpenBlock();
            CSharpFile.If("string.CompareOrdinal(_typeName, \"Int64\") == 0");
            CSharpFile.OpenBlock();
            CSharpFile.If("string.CompareOrdinal(valTypeName, \"Int64\") != 0 && string.CompareOrdinal(valTypeName, \"Int32\") != 0");
            CSharpFile.LineIndented("throw new ArgumentException(\"Invalid type. Property: \" + _propName + \" expects \" + _typeName + \" or \" + _typeName + \"[]).\")");
            CSharpFile.CloseBlock();
            CSharpFile.Else();
            CSharpFile.OpenBlock();
            CSharpFile.If("string.CompareOrdinal(valTypeName, _typeName) != 0");
            CSharpFile.LineIndented("throw new ArgumentException(\"Invalid type. Property: \" + _propName + \" expects \" + _typeName + \" or \" + _typeName + \"[]).\")");
            CSharpFile.CloseBlock();
            CSharpFile.CloseBlock();
            CSharpFile.CloseBlock();
            CSharpFile.EmptyLine();

            CSharpFile.If("_isArray");
            CSharpFile.OpenBlock();
            CSharpFile.If("isBinary");
            CSharpFile.OpenBlock();
            CSharpFile.If($"_condition != {_conditionEnumType}.Equals && _condition != {_conditionEnumType}.NotEquals");
            CSharpFile.LineIndented("throw new ArgumentException(\"When Value is byte[] and the property is binary... \" + _propName + \" supports only Equals, NotEquals.\")");
            CSharpFile.CloseBlock();
            CSharpFile.Else();
            CSharpFile.OpenBlock();
            CSharpFile.If("!isContains");
            CSharpFile.LineIndented("throw new ArgumentException(\"When Value is array \" + _typeName + \"[] the property: \" + _propName + \" supports only one of the Contains or DoesNotContain condition.\")");
            CSharpFile.If("!_hasValue");
            CSharpFile.LineIndented("throw new ArgumentException(\"Condition \" + _condition.ToString() + \" is not supported when Value is an empty array.\")");
            CSharpFile.CloseBlock();
            CSharpFile.CloseBlock();
            CSharpFile.Else();
            CSharpFile.OpenBlock();
            CSharpFile.If("!SupportsCondition(_typeName, _condition)");
            CSharpFile.LineIndented("throw new ArgumentException(\"Condition \" + _condition.ToString() + \" is not supported for type \" + _typeName)");
            CSharpFile.CloseBlock();
            CSharpFile.CloseBlock();
        }

        private static void GenerateProperties()
        {
            PropertyExtensions.FullNoSetter(TypeInfo.Int, "PropertyId", backingField: "_propertyId");
            PropertyExtensions.FullNoSetter(TypeInfo.String, "PropertyName", backingField: "_propName");
            PropertyExtensions.FullNoSetter(new TypeInfo(_conditionEnumType), "Condition", backingField: "_condition");
            PropertyExtensions.Full(new TypeInfo(_operatorEnumType), "Operator", AccessModifiers.Public, AccessModifiers.None, backingField: "_operator");
            PropertyExtensions.FullNoSetter(TypeInfo.String, "TypeName", backingField: "_typeName");
            PropertyExtensions.FullNoSetter(new TypeInfo("Type"), "Type", backingField: "_type");
            PropertyExtensions.FullNoSetter(new TypeInfo("TypeCode"), "TypeCode", backingField: "_typeCode");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsArray", backingField: "_isArray");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsDateFilter", backingField: "_isDateFilter");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsYearFilter", backingField: "_isYearFilter");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsMonthFilter", backingField: "_isMonthFilter");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsDayFilter", backingField: "_isDayFilter");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsNullable", backingField: "_isNullable");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsBoolean", backingField: "_isBoolean");
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "HasValue", backingField: "_hasValue");
            PropertyExtensions.FullNoSetter(new TypeInfo("object"), "Value", backingField: "_value");
            PropertyExtensions.Full(TypeInfo.Int, "StartExpressions", AccessModifiers.Public, AccessModifiers.None, backingField: "_startExpressions");
            PropertyExtensions.Full(TypeInfo.Int, "EndExpressions", AccessModifiers.Public, AccessModifiers.None, backingField: "_endExpressions");
        }

        private static void GenerateSupportsCondition()
        {
            var method = new MethodDeclaration
            {
                Name = "SupportsCondition",
                AccessModifier = AccessModifiers.Private,
                IsStatic = true,
                ReturnType = TypeInfo.Bool,
                Parameters = new List<MethodParameter>(2)
                {
                    new MethodParameter { Name = "typeName", ParamType = "string" },
                    new MethodParameter { Name = "condition", ParamType = _conditionEnumType }
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"condition == {_conditionEnumType}.Equals || condition == {_conditionEnumType}.NotEquals");
            CSharpFile.ReturnIndented("true");
            CSharpFile.If("string.CompareOrdinal(typeName, \"String\") == 0");
            CSharpFile.ReturnIndented("_stringConditions.Contains(condition)");
            CSharpFile.If("string.CompareOrdinal(typeName, \"DateTime\") == 0");
            CSharpFile.OpenBlock();
            CSharpFile.Line("var dateOrPart = _dateConditions.Contains(condition) || _yearConditions.Contains(condition) || _monthConditions.Contains(condition) || _dayConditions.Contains(condition)");
            CSharpFile.If("dateOrPart");
            CSharpFile.LineIndented("return true");
            CSharpFile.CloseBlock();
            CSharpFile.If("_valueTypes.Contains(typeName)");
            CSharpFile.ReturnIndented("_valueTypeConditions.Contains(condition)");
            CSharpFile.Return("false");
            CSharpFile.CloseBlock();
        }
    }
}
