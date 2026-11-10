#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using SF.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The read-only details of the selected tree node: label/value rows, lists (keys, navigation properties),
/// and for procedures and functions a column picker.
/// </summary>
internal sealed partial class DetailsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Property _selectedColumn;

    public string Title { get; set; }
    public string Kind { get; set; }
    public List<DetailRow> Rows { get; set; } = new List<DetailRow>();
    public List<DetailList> Lists { get; set; } = new List<DetailList>();

    /// <summary>
    /// A table's or view's columns, for the columns grid.
    /// </summary>
    public List<ColumnRow> EntityColumns { get; set; } = new List<ColumnRow>();

    public bool HasEntityColumns
    {
        get
        {
            return EntityColumns.Count > 0;
        }
    }

    public string EntityColumnsHeader
    {
        get
        {
            return EntityColumns.Count == 1 ? "1 column" : $"{EntityColumns.Count} columns";
        }
    }

    /// <summary>
    /// The result or return columns to pick from (procedures and table functions).
    /// </summary>
    public List<Property> Columns { get; set; } = new List<Property>();

    public string ColumnsHeader { get; set; }

    public ObservableCollection<DetailRow> ColumnRows { get; } = new ObservableCollection<DetailRow>();

    public bool HasColumns
    {
        get
        {
            return Columns.Count > 0;
        }
    }

    public bool HasColumnRows
    {
        get
        {
            return ColumnRows.Count > 0;
        }
    }

    partial void OnSelectedColumnChanged(Property value)
    {
        ShowColumn(value);
    }

    /// <summary>
    /// Shows one return column (or a scalar return) below the picker.
    /// </summary>
    public void ShowColumn(Property column)
    {
        ColumnRows.Clear();
        if(column != null)
        {
            foreach(var row in DetailsBuilder.ReturnColumnRows(column))
                ColumnRows.Add(row);
        }

        OnPropertyChanged(nameof(HasColumnRows));
    }
}

/// <summary>
/// One column of a table or view in the columns grid.
/// </summary>
internal sealed class ColumnRow
{
    public string Name { get; set; }
    public string StoreType { get; set; }
    public string DotNetType { get; set; }
    public bool IsNullable { get; set; }
    public bool IsKey { get; set; }

    /// <summary>
    /// Identity, computed, default value: whatever applies, in a few words.
    /// </summary>
    public string Notes { get; set; }

    public string NullableText
    {
        get
        {
            return IsNullable ? "null" : "not null";
        }
    }
}

internal sealed class DetailRow
{
    public string Label { get; set; }
    public string Value { get; set; }
}

internal sealed class DetailList
{
    public string Header { get; set; }
    public List<string> Items { get; set; } = new List<string>();

    public bool IsEmpty
    {
        get
        {
            return Items.Count == 0;
        }
    }
}

/// <summary>
/// The details the old model screen showed for each kind of object, field for field.
/// </summary>
internal static class DetailsBuilder
{
    public static DetailsViewModel Build(ModelTreeNode node)
    {
        if(node == null)
            return null;

        switch(node.Kind)
        {
            case ModelNodeKind.Table:
            case ModelNodeKind.View:
                return BuildEntity((Entity)node.Item, node.Kind == ModelNodeKind.View ? "View" : "Table");
            case ModelNodeKind.Column:
            case ModelNodeKind.Parameter:
                return BuildProperty((Property)node.Item, node.Kind == ModelNodeKind.Parameter ? "Parameter" : "Column");
            case ModelNodeKind.Procedure:
                return BuildProcedure((StoredProc)node.Item);
            case ModelNodeKind.Function:
                return BuildFunction((Function)node.Item);
            case ModelNodeKind.Sequence:
                return BuildSequence((Sequence)node.Item);
            default:
                return new DetailsViewModel
                {
                    Title = node.Title,
                    Kind = node.Kind.ToString(),
                    Rows = new List<DetailRow> { Row("Objects", node.Detail) },
                };
        }
    }

    private static DetailsViewModel BuildEntity(Entity entity, string kind)
    {
        var result = new DetailsViewModel { Title = entity.N, Kind = kind };
        result.Rows.Add(Row("Entity Name", entity.N));
        result.Rows.Add(Row("Store Name", entity.SN));

        var primaryKey = new DetailList { Header = "Primary Key" };
        if(entity.PK?.Ps != null)
        {
            foreach(var p in entity.PK.Ps)
                primaryKey.Items.Add(p.N);
        }

        var navProps = new DetailList { Header = "Navigation Properties" };
        if(entity.NPs != null)
        {
            foreach(var np in entity.NPs)
                navProps.Items.Add(np.N);
        }

        var uniqueKeys = new DetailList { Header = "Unique Keys" };
        if(entity.UKs != null)
        {
            foreach(var uk in entity.UKs)
            {
                var columns = new List<string>();
                if(uk.Ps != null)
                {
                    foreach(var p in uk.Ps)
                        columns.Add(p.N);
                }

                uniqueKeys.Items.Add($"{uk.N}: {string.Join(", ", columns)}");
            }
        }

        result.Lists.Add(primaryKey);
        result.Lists.Add(navProps);
        result.Lists.Add(uniqueKeys);

        if(entity.Ps != null)
        {
            foreach(var p in entity.Ps)
                result.EntityColumns.Add(ToColumnRow(p, primaryKey.Items.Contains(p.N)));
        }

        return result;
    }

    private static ColumnRow ToColumnRow(Property p, bool isKey)
    {
        var notes = new List<string>();
        if(p.Ide == YesOrNo.Yes)
            notes.Add("identity");

        if(p.IsComp == YesOrNo.Yes)
            notes.Add("computed");

        if(!string.IsNullOrEmpty(p.Def))
            notes.Add($"default {p.Def}");

        var storeType = p.ST;
        if(p.S.HasValue && p.S.Value > 0)
            storeType = $"{p.ST}({p.S.Value})";

        return new ColumnRow
        {
            Name = p.N,
            StoreType = storeType,
            DotNetType = p.T,
            IsNullable = p.Nul == YesOrNo.Yes,
            IsKey = isKey,
            Notes = string.Join(", ", notes),
        };
    }

    private static DetailsViewModel BuildProperty(Property p, string kind)
    {
        var result = new DetailsViewModel { Title = p.N, Kind = kind };
        result.Rows.Add(Row("Name", p.N));
        result.Rows.Add(Row("Store Name", p.SN));
        result.Rows.Add(Row(".NET Type", p.T));
        result.Rows.Add(Row("Store Type", p.ST));
        result.Rows.Add(Row("Nullable", p.Nul.ToString()));
        result.Rows.Add(Row("Identity", p.Ide.ToString()));
        result.Rows.Add(Row("Computed", p.IsComp.ToString()));
        result.Rows.Add(Row("SqlGenerated", p.IsGen.ToString()));
        result.Rows.Add(Row("ReadOnly", p.RO.ToString()));
        result.Rows.Add(Row("Default", p.Def));
        result.Rows.Add(Row("Max Length", p.S?.ToString()));
        result.Rows.Add(Row("Param Mode", p.M?.ToString()));
        return result;
    }

    private static DetailsViewModel BuildProcedure(StoredProc proc)
    {
        var result = new DetailsViewModel { Title = proc.N, Kind = "Stored procedure", ColumnsHeader = "Result columns" };
        result.Rows.Add(Row("Name", proc.N));
        result.Rows.Add(Row("Store Name", proc.SN));
        result.Rows.Add(Row("Returns", proc.En?.N));

        if(proc.REn && proc.En?.Ps != null)
        {
            foreach(var column in proc.En.Ps)
                result.Columns.Add(column);

            if(result.Columns.Count > 0)
                result.SelectedColumn = result.Columns[0];
        }

        return result;
    }

    private static DetailsViewModel BuildFunction(Function func)
    {
        var result = new DetailsViewModel { Title = func.N, Kind = "Function", ColumnsHeader = "Return columns" };
        result.Rows.Add(Row("Name", func.N));
        result.Rows.Add(Row("Store Name", func.SN));
        result.Rows.Add(Row("Returns", func.REn ? func.En?.N : func.Sc?.T));

        if(func.En == null)
        {
            result.ShowColumn(func.Sc);//a scalar function: show the scalar
            return result;
        }

        if(func.En.Ps != null)
        {
            foreach(var column in func.En.Ps)
                result.Columns.Add(column);

            if(result.Columns.Count > 0)
                result.SelectedColumn = result.Columns[0];
        }

        return result;
    }

    private static DetailsViewModel BuildSequence(Sequence seq)
    {
        var result = new DetailsViewModel { Title = seq.N, Kind = "Sequence" };
        result.Rows.Add(Row("Name", seq.N));
        result.Rows.Add(Row("Store Name", seq.SN));
        result.Rows.Add(Row("Returns .NET Type", seq.Sc?.T));
        result.Rows.Add(Row("Returns Store Type", seq.Sc?.ST));
        return result;
    }

    /// <summary>
    /// The six fields shown for a return column or a scalar return.
    /// </summary>
    public static List<DetailRow> ReturnColumnRows(Property p)
    {
        return new List<DetailRow>
        {
            Row("Name", p.N),
            Row("Store Name", p.SN),
            Row(".NET Type", p.T),
            Row("Store Type", p.ST),
            Row("Nullable", p.Nul.ToString()),
            Row("Max Length", p.S?.ToString()),
        };
    }

    private static DetailRow Row(string label, string value)
    {
        return new DetailRow { Label = label, Value = string.IsNullOrEmpty(value) ? "—" : value };
    }
}
