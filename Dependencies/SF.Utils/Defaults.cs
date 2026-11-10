#region Usings
using SF.Models;
#endregion

namespace SF.Utils;

public sealed class Defaults
{
    public static CategoryCollection Categories()
    {
        return new CategoryCollection
        {
           new Category { Name = CategoryNames.Entities },
           new Category { Name = CategoryNames.Views },
           new Category { Name = CategoryNames.Procs},
           new Category { Name = CategoryNames.Funcs },
           new Category { Name = CategoryNames.Seqs },
        };
    }
    public static DBObjectCategoryCollection DbCategories()
    {
        return new DBObjectCategoryCollection
        {
           new DbObjectCategory { Name = DbCategoryNames.Entities, CategoryType = DbObjectType.Table, Schemas = new DbSchemaCollection() },
           new DbObjectCategory { Name = DbCategoryNames.Views, CategoryType = DbObjectType.View, Schemas = new DbSchemaCollection() },
           new DbObjectCategory { Name = DbCategoryNames.Procs, CategoryType = DbObjectType.Proc, Schemas = new DbSchemaCollection() },
           new DbObjectCategory { Name = DbCategoryNames.Funcs, CategoryType = DbObjectType.Func, Schemas = new DbSchemaCollection() },
           new DbObjectCategory { Name = DbCategoryNames.Seqs, CategoryType = DbObjectType.Seq, Schemas = new DbSchemaCollection() },
        };
    }
    public static CommonLayers CommonLayers()
    {
        var cl = new CommonLayers();
        cl.Exceptions.Namespace = "Exceptions";
        cl.Exceptions.Project = "Entity";

        cl.RepoContainer.Namespace = "Repository.Container";
        cl.RepoContainer.Project = "Repository";

        cl.Query.Namespace = "Query";
        cl.Query.Project = "Query";

        cl.IRepoContainer.Namespace = "Repository.Container.Interfaces";
        cl.IRepoContainer.Project = "Repository.Interfaces";

        cl.Repository.Namespace = "Repository.Common";
        cl.Repository.Project = "Repository";
        return cl;
    }
    public static ModelLayers ModelLayers()
    {
        var l = new ModelLayers();
        l.Entity.Namespace = "Entity";
        l.Entity.Project = "Entity";

        l.PropEnum.Namespace = "Entity";
        l.PropEnum.Project = "Entity";

        l.NavPropEnum.Namespace = "Entity";
        l.NavPropEnum.Project = "Entity";

        l.EntityUtil.Namespace = "Entity.Utils";
        l.EntityUtil.Project = "Entity.Utils";

        l.Repository.Namespace = "Repository";
        l.Repository.Project = "Repository";

        l.IRepository.Namespace = "Repository.Interfaces";
        l.IRepository.Project = "Repository.Interfaces";

        return l;
    }
}
