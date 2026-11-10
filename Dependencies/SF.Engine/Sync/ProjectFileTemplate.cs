#region Usings
using SF.Models;
using System;
using System.Text;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// The project file Sync writes for a new generated project. To support a new .NET version add it here and to TargetFrameworks.
/// </summary>
public static class ProjectFileTemplate
{
    private const string _newLine = "\r\n";

    /// <summary>
    /// The project file for a new generated project. addSqlClient adds the Microsoft.Data.SqlClient package (the common
    /// Repository project, where the SQL engine is).
    /// </summary>
    public static string Compose(TargetFrameworks targetFramework, bool addSqlClient)
    {
        var b = new StringBuilder();

        b.Append("<Project Sdk=\"Microsoft.NET.Sdk\">").Append(_newLine);
        b.Append(_newLine);
        b.Append("  <PropertyGroup>").Append(_newLine);
        b.Append("    <TargetFramework>").Append(GetMoniker(targetFramework)).Append("</TargetFramework>").Append(_newLine);
        b.Append("    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>").Append(_newLine);//generated code must not fail the build on the user's own code-style rules
        b.Append("  </PropertyGroup>").Append(_newLine);
        b.Append(_newLine);

        if(addSqlClient)
        {
            b.Append("  <ItemGroup>").Append(_newLine);
            b.Append("    <PackageReference Include=\"Microsoft.Data.SqlClient\" Version=\"7.1.1\" />").Append(_newLine);
            b.Append("  </ItemGroup>").Append(_newLine);
            b.Append(_newLine);
        }

        b.Append("</Project>").Append(_newLine);

        return b.ToString();
    }

    public static string GetMoniker(TargetFrameworks targetFramework)
    {
        switch(targetFramework)
        {
            case TargetFrameworks.NET10:
                return "net10.0";
            case TargetFrameworks.NET11:
                return "net11.0";
            default:
                throw new ArgumentException($"Target framework {targetFramework} is not supported.");
        }
    }

    /// <summary>
    /// The display name of a supported target framework.
    /// </summary>
    public static string GetDisplayName(TargetFrameworks targetFramework)
    {
        switch(targetFramework)
        {
            case TargetFrameworks.NET10:
                return ".NET 10";
            case TargetFrameworks.NET11:
                return ".NET 11";
            default:
                return targetFramework.ToString();
        }
    }
}
