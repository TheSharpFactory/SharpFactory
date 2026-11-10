namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    #region Constants

    internal static readonly GenericParameter _genericProp = new GenericParameter { Name = Common.Names.GenericProp, Constraint = Common.Names.PropertyContainer };
    internal static readonly GenericParameter _genericNavProp = new GenericParameter { Name = Common.Names.GenericProp, Constraint = Common.Names.NavPropertyContainer };
    internal static readonly GenericParameter _genericQuery = new GenericParameter { Name = $"{Common.Names.Query}<{Common.Names.GenericProp}>" };
    internal static readonly GenericParameter _genericQueryBuilder = new GenericParameter { Name = $"{Common.Names.QueryBuilder}<{Common.Names.GenericProp}>" };

    internal static readonly GenericParameterList _genericParams = new GenericParameterList
    {
      _genericProp
    };

    internal static readonly GenericParameterList _genericParamsWithNavProps = new GenericParameterList
    {
      _genericNavProp
    };

    internal static readonly string _conditionEnumType = Common.Names.FilterConditionsEnum;
    internal static readonly string _operatorEnumType = Common.Names.FilterOperatorsEnum;
    internal static readonly string _sortDirectionEnumType = Common.Names.SortDirectionEnum;
    internal static readonly string _equals = _conditionEnumType + ".Equals";
    internal static readonly string _notEquals = _conditionEnumType + ".NotEquals";
    internal static readonly string _contains = _conditionEnumType + ".Contains";
    internal static readonly string _notContain = _conditionEnumType + ".DoesNotContain";
    internal static readonly string _startsWith = _conditionEnumType + ".StartsWith";
    internal static readonly string _notStartWith = _conditionEnumType + ".DoesNotStartWith";
    internal static readonly string _endsWith = _conditionEnumType + ".EndsWith";
    internal static readonly string _notEndWith = _conditionEnumType + ".DoesNotEndWith";
    internal static readonly string _greater = _conditionEnumType + ".GreaterThan";
    internal static readonly string _lesser = _conditionEnumType + ".LessThan";
    internal static readonly string _greaterEqual = _conditionEnumType + ".GreaterThanOrEquals";
    internal static readonly string _lessEqual = _conditionEnumType + ".LessThanOrEquals";

    internal static readonly string _dateEquals = _conditionEnumType + ".DateEquals";
    internal static readonly string _dateNotEquals = _conditionEnumType + ".DateNotEquals";
    internal static readonly string _dateGreater = _conditionEnumType + ".DateGreaterThan";
    internal static readonly string _dateLess = _conditionEnumType + ".DateLessThan";
    internal static readonly string _dateGreaterEqual = _conditionEnumType + ".DateGreaterThanOrEquals";
    internal static readonly string _dateLessEqual = _conditionEnumType + ".DateLessThanOrEquals";
    internal static readonly string _dateContains = _conditionEnumType + ".DateContains";
    internal static readonly string _dateNotContains = _conditionEnumType + ".DateDoesNotContain";

    internal static readonly string _yearEquals = _conditionEnumType + ".YearEquals";
    internal static readonly string _yearNotEquals = _conditionEnumType + ".YearNotEquals";
    internal static readonly string _yearGreater = _conditionEnumType + ".YearGreaterThan";
    internal static readonly string _yearLess = _conditionEnumType + ".YearLessThan";
    internal static readonly string _yearGreaterEqual = _conditionEnumType + ".YearGreaterThanOrEquals";
    internal static readonly string _yearLessEqual = _conditionEnumType + ".YearLessThanOrEquals";
    internal static readonly string _yearContains = _conditionEnumType + ".YearContains";
    internal static readonly string _yearNotContains = _conditionEnumType + ".YearDoesNotContain";

    internal static readonly string _monthEquals = _conditionEnumType + ".MonthEquals";
    internal static readonly string _monthNotEquals = _conditionEnumType + ".MonthNotEquals";
    internal static readonly string _monthGreater = _conditionEnumType + ".MonthGreaterThan";
    internal static readonly string _monthLess = _conditionEnumType + ".MonthLessThan";
    internal static readonly string _monthGreaterEqual = _conditionEnumType + ".MonthGreaterThanOrEquals";
    internal static readonly string _monthLessEqual = _conditionEnumType + ".MonthLessThanOrEquals";
    internal static readonly string _monthContains = _conditionEnumType + ".MonthContains";
    internal static readonly string _monthNotContains = _conditionEnumType + ".MonthDoesNotContain";

    internal static readonly string _dayEquals = _conditionEnumType + ".DayEquals";
    internal static readonly string _dayNotEquals = _conditionEnumType + ".DayNotEquals";
    internal static readonly string _dayGreater = _conditionEnumType + ".DayGreaterThan";
    internal static readonly string _dayLess = _conditionEnumType + ".DayLessThan";
    internal static readonly string _dayGreaterEqual = _conditionEnumType + ".DayGreaterThanOrEquals";
    internal static readonly string _dayLessEqual = _conditionEnumType + ".DayLessThanOrEquals";
    internal static readonly string _dayContains = _conditionEnumType + ".DayContains";
    internal static readonly string _dayNotContains = _conditionEnumType + ".DayDoesNotContain";

    internal static readonly string _hashSetType = "new HashSet<" + _conditionEnumType + ">";
    internal static readonly string _hashSetForString = _hashSetType + "{" + _contains + ", " + _notContain + ", " + _startsWith + ", " + _notStartWith + ", " + _endsWith + ", " + _notEndWith + "}";
    internal static readonly string _hashSetForNumbersAndDateTime = _hashSetType + "{" + _greater + ", " + _lesser + ", " + _greaterEqual + ", " + _lessEqual + "}";
    internal static readonly string _hashSetForDates = _hashSetType + "{" + _dateEquals + ", " + _dateNotEquals + ", " + _dateGreater + ", " + _dateLess + ", " + _dateGreaterEqual + ", " + _dateLessEqual + ", " + _dateContains + "," + _dateNotContains + "}";
    internal static readonly string _hashSetForYear = _hashSetType + "{" + _yearEquals + ", " + _yearNotEquals + ", " + _yearGreater + ", " + _yearLess + ", " + _yearGreaterEqual + ", " + _yearLessEqual + "}";
    internal static readonly string _hashSetForMonth = _hashSetType + "{" + _monthEquals + ", " + _monthNotEquals + ", " + _monthGreater + ", " + _monthLess + ", " + _monthGreaterEqual + ", " + _monthLessEqual + ", " + _monthContains + ", " + _monthNotContains + "}";
    internal static readonly string _hashSetForDay = _hashSetType + "{" + _dayEquals + ", " + _dayNotEquals + ", " + _dayGreater + ", " + _dayLess + ", " + _dayGreaterEqual + ", " + _dayLessEqual + ", " + _dayContains + ", " + _dayNotContains + "}";
    internal static readonly string _hashSetForContains = _hashSetType + "{" + _contains + ", " + _notContain + ", " + _dayContains + ", " + _dayNotContains + ", " + _monthContains + ", " + _monthNotContains + ", " + _yearContains + ", " + _yearNotContains + ", " + _dateContains + "," + _dateNotContains + "}";

    #endregion
}
