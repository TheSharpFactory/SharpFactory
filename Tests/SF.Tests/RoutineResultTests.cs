#region Usings
using SF.Models;
using SF.Utils;
using System;
using System.Data;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// A routine's result columns from SQL Server's description of its result. A column marked hidden (a key column
/// KeyInfo adds) is skipped; a column computed from an expression (SELECT CAST(0 AS BIGINT) AS [PlatformBetId]) has an
/// empty flag with Microsoft.Data.SqlClient (System.Data.SqlClient said false) and is kept, as it was for years.
/// </summary>
public sealed class RoutineResultTests
{
    [Fact]
    public void OnlyColumnsMarkedHiddenAreSkipped()
    {
        var raw = new DataTable();
        raw.Columns.Add("ColumnName", typeof(string));
        raw.Columns.Add("DataTypeName", typeof(string));
        raw.Columns.Add("AllowDBNull", typeof(object));
        raw.Columns.Add("ColumnSize", typeof(object));
        raw.Columns.Add("IsHidden", typeof(object));
        //as SQL Server describes SELECT CAST(0 AS BIGINT) AS [PlatformBetId] (checked against a real server)
        raw.Rows.Add("PlatformBetId", "bigint", true, 8, DBNull.Value);
        raw.Rows.Add("Name", "nvarchar", false, 50, false);
        //a key column SQL Server adds for CommandBehavior.KeyInfo
        raw.Rows.Add("Hidden", "int", false, 4, true);
        //no AllowDBNull either: nullable
        raw.Rows.Add("Note", "nvarchar", DBNull.Value, 10, DBNull.Value);

        var result = StoreUtils.ComposeRoutineResult("GetClientBets", raw);

        Assert.Equal("GetClientBets_Result", result.SN);
        Assert.Equal(3, result.Ps.Count);
        Assert.Equal("PlatformBetId", result.Ps[0].SN);
        Assert.Equal(YesOrNo.Yes, result.Ps[0].Nul);
        Assert.Equal(8, result.Ps[0].S);
        Assert.Equal("Name", result.Ps[1].SN);
        Assert.Equal(YesOrNo.No, result.Ps[1].Nul);
        Assert.Equal("Note", result.Ps[2].SN);
        Assert.Equal(YesOrNo.Yes, result.Ps[2].Nul);
        Assert.Null(StoreUtils.ComposeRoutineResult("Empty", new DataTable()));
    }
}
