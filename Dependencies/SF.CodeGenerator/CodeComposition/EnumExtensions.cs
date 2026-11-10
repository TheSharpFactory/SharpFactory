#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

internal static partial class EnumExtensions
{
    public static void PropEnumMember(Property p)
    {
        var member = new EnumMemberDeclaration { Name = p.N, Summary = new List<string>(1) { $"Type: {p.T}." } };
        CSharpFile.EnumMember(member);
    }

    public static void NavPropEnumMember(Model model, NavProperty p, FKey fk)
    {
        var member = new EnumMemberDeclaration { Name = p.N, Summary = NavPropXmlComment(model, p, fk) };
        CSharpFile.EnumMember(member);
    }
    public static List<string> NavPropXmlComment(Model model, NavProperty p, FKey fk)
    {
        var child = model.Categories.GetByName(CategoryNames.Entities).Schemas.GetByName(fk.CSc).Entities.GetByName(fk.CEn);

        var count = fk.FKParts.Count;
        var summaryLines = new List<string>(count);
        if(count == 1)
        {
            var childProp = child.Ps.GetByStoreName(fk.FKParts[0].CCol);

            if(p.End == NavPropertyEnd.Child)
                summaryLines.Add($"Related to property {childProp.N} on Entity {child.N}");
            else
                summaryLines.Add($"Related to property {childProp.N} on this entity");
        }
        else
        {
            summaryLines.Add("Related to properties");

            for(var i = 0; i < count; i++)
            {
                var childProp = child.Ps.GetByStoreName(fk.FKParts[i].CCol);
                summaryLines.Add($"{childProp.N}");
            }

            if(p.End == NavPropertyEnd.Child)
                summaryLines.Add($"On Entity {child.N}");
            else
                summaryLines.Add("On this entity");
        }

        return summaryLines;
    }
}
