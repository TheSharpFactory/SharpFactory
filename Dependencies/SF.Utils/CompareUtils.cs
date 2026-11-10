#region Usings
using SF.Models;
#endregion

namespace SF.Utils;

public static class Compare
{
    public static class Models
    {
        //public static bool AreDifferent(Model model1, Model model2)
        //{
        //  foreach(var mod1Cat in model1.Categories)
        //  {
        //    var mod2Cat = model2.Categories.GetByName(mod1Cat.Name);

        //    if(mod1Cat.Count != mod2Cat.Count)//if the number of entities is different
        //      return true;

        //    switch(mod1Cat.Name)
        //    {
        //      case CategoryNames.Entities:
        //        if(Generic.AreBothNull(mod1Cat.Schemas, mod2Cat.Schemas))//this means both are null so skip
        //          continue;

        //        if(Generic.AreNullDifferent(mod1Cat.Schemas, mod2Cat.Schemas))//one is null and the other is not
        //          return true;

        //        if(mod1Cat.Schemas.Count != mod2Cat.Schemas.Count)//if the number of schemas is different
        //          return true;

        //        foreach(var schema1 in mod1Cat.Schemas)
        //        {
        //          if(!mod2Cat.Schemas.Contains(schema1.Name))//if the schema is not present. taking into account that by now both have the same number of schemas. 
        //            return true;

        //          var schema2 = mod2Cat.Schemas.GetByName(schema1.Name);//now get the model 2 schema

        //          if(Generic.AreBothNull(schema1.Entities, schema2.Entities))//this means both are null so skip
        //            continue;

        //          if(schema1.Count != schema2.Count)//if the number of entities is different
        //            return true;

        //          //at this point the schema exists on both models.
        //          if(Generic.AreNullDifferent(schema1.Entities, schema2.Entities))//one is null and the other is not
        //            return true;

        //          //at this point both EntityCollections look the same. so lets compare the entities one by one
        //          foreach(var ent1 in schema1.Entities)
        //          {
        //            if(!schema2.Entities.Contains(ent1.Name))//if the entity is not present. taking into account that by now both have the same number of entities. 
        //              return true;

        //            if(Entities.AreDifferent(ent1, schema2.Entities.GetByName(ent1.Name)))//compare both entities
        //              return true;
        //          }
        //        }
        //        break;
        //      case CategoryNames.Views:
        //        break;
        //      case CategoryNames.Procs:
        //        break;
        //      case CategoryNames.Funcs:
        //        break;
        //      default:
        //        break;
        //    }
        //  }
        //  return true;
        //}
        //public static Model GetDiffModel(Model changed, Model orignal)
        //{//this method returns a model with only changed entities
        //  var result = new Model
        //  {
        //    Name = changed.Name,
        //    ConInfo = new ConnectionInfo
        //    {
        //      Database = changed.ConInfo.Database,
        //      Server = changed.ConInfo.Server,
        //      User = changed.ConInfo.User,
        //      Password = changed.ConInfo.Password,
        //      DisableIntegratedSecurity = changed.ConInfo.DisableIntegratedSecurity
        //    },
        //    Categories = Defaults.Categories(),
        //  };

        //  foreach(var changedCat in changed.Categories)
        //  {
        //    var orignalCat = orignal.Categories.GetByName(changedCat.Name);
        //    var resultCat = result.Categories.GetByName(changedCat.Name);
        //    switch(changedCat.Name)
        //    {
        //      case CategoryNames.Entities:
        //        if(changedCat.Schemas == null || changedCat.Schemas.Count < 1)//this means there is no schemas in the changed category.
        //          continue;

        //        if(orignalCat.Schemas == null || orignalCat.Schemas.Count < 1)//this means there is no schemas in the original category so add all of the entities as changed.
        //        {
        //          foreach(var schemaChanged in changedCat.Schemas)
        //            resultCat.Schemas.Add(schemaChanged.Clone());// clone all schemas since the original has none. this means that all schemas are new.
        //          continue;//move to the next category.
        //        }

        //        //at this point both categories have entities
        //        foreach(var schemaChanged in changedCat.Schemas)
        //        {
        //          if(schemaChanged.Entities == null || schemaChanged.Entities.Count < 1)//if the change schema is empty then skip to the next.
        //            continue;

        //          if(!orignalCat.Schemas.Contains(schemaChanged.Name))//if the original category does not contain this schema it means it is new and all entities must be added as changed
        //          {
        //            resultCat.Schemas.Add(schemaChanged.Clone());
        //            continue;//move to the next category.
        //          }
        //          var schemaOriginal = orignalCat.Schemas.GetByName(schemaChanged.Name);
        //          if(schemaOriginal.Entities == null || schemaOriginal.Entities.Count < 1)//if the original category is empty then it means all entities are new and must be added
        //          {
        //            resultCat.Schemas.Add(schemaChanged.Clone());
        //            continue;//move to the next category.
        //          }

        //          //if the schema is present in both changed and original categories then compare each entity. at this point both schemas have entities.
        //          var schemaAddedToResult = false;
        //          Schema resultSchema = new Schema { Name = schemaChanged.Name, Type = ModelObjectType.Entities, Entities = new EntityCollection(), ForeignKeys = new FKeyCollection() };
        //          foreach(var entChanged in schemaChanged.Entities)
        //          {
        //            if(!schemaOriginal.Entities.Contains(entChanged.Name) || Compare.Entities.AreDifferent(entChanged, schemaOriginal.Entities.GetByName(entChanged.Name)))//if the original schema does not contain the entity it means is new and must be added.
        //            {                                                    //at this point both schemas contain the same entity. it is time to compare them.   
        //              if(!schemaAddedToResult)
        //              {
        //                resultCat.Schemas.Add(resultSchema);
        //                schemaAddedToResult = true;
        //              }
        //              resultSchema.Entities.Add(entChanged.Clone());
        //              continue;
        //            }
        //          }
        //        }
        //        break;
        //      case CategoryNames.Views:
        //        break;
        //      case CategoryNames.Procs:
        //        break;
        //      case CategoryNames.Funcs:
        //        break;
        //      default:
        //        break;
        //    }
        //  }
        //  return result;
        //}
        //public static Dictionary<string, List<string>> GetEntitiesToDelete(Model changed, Model orignal)
        //{//look for any entity in the original that is NOT present in the changed
        //  var catOriginal = changed.Categories.GetByName(CategoryNames.Entities);
        //  var catChanged = changed.Categories.GetByName(CategoryNames.Entities);

        //  if(catOriginal.Schemas == null || catOriginal.Schemas.Count < 1)//if there is no schemas in the original model then nothing needs to be returned
        //    return new Dictionary<string, List<string>>(1);

        //  var result = new Dictionary<string, List<string>>(catOriginal.Schemas.Count);
        //  if(catChanged.Schemas == null || catChanged.Schemas.Count < 1)//if the changed model has no schemas then remove all
        //  {
        //    foreach(var schemaOriginal in catOriginal.Schemas)
        //    {
        //      if(schemaOriginal.Entities == null || schemaOriginal.Entities.Count < 1)// if the schema is empty in the original model skip it.
        //        continue;

        //      PopulateDeleteAllEntitiesFromSchema(schemaOriginal, result);
        //    }
        //    return result;
        //  }
        //  //at this point both models have schemas
        //  foreach(var schemaOriginal in catOriginal.Schemas)
        //  {
        //    if(schemaOriginal.Entities == null || schemaOriginal.Entities.Count < 1)// if the schema is empty in the original model skip it.
        //      continue;

        //    if(!catChanged.Schemas.Contains(schemaOriginal.Name))//if the changed model does not contain the original schema then remove all.
        //    {
        //      PopulateDeleteAllEntitiesFromSchema(schemaOriginal, result);
        //      continue;
        //    }

        //    var schemaChanged = catChanged.Schemas.GetByName(schemaOriginal.Name);

        //    if(schemaChanged.Entities == null || schemaChanged.Entities.Count < 1)//if changed model's schema is empty
        //    {
        //      PopulateDeleteAllEntitiesFromSchema(schemaOriginal, result);
        //      continue;
        //    }

        //    //at this point both schemas have entities. it is time to compare them. any entity not present in the changed model's schema needs to be added to the delete list
        //    var entitiesToDelete = new List<string>(schemaOriginal.Entities.Count);
        //    foreach(var entOriginal in schemaOriginal.Entities)
        //    {
        //      if(schemaChanged.Entities.Contains(entOriginal.Name))//if the entity is present in the changed model then no need to delete it
        //        continue;

        //      if(!result.ContainsKey(schemaOriginal.Name))
        //        result.Add(schemaOriginal.Name, entitiesToDelete);

        //      entitiesToDelete.Add(entOriginal.Name);
        //    }
        //    entitiesToDelete.TrimExcess();
        //  }

        //  return result;
        //}
        //private static void PopulateDeleteAllEntitiesFromSchema(Schema s, Dictionary<string, List<string>> dic)
        //{
        //  var entities = new List<string>(s.Entities.Count);
        //  dic.Add(s.Name, entities);

        //  foreach(var ent in s.Entities)
        //    entities.Add(ent.Name);
        //}

    }
    public static class Entities
    {
        public static bool AreDifferent(Entity one, Entity two)
        {
            return JsonUtils.AreDifferent(one, two);
        }
    }
    public static class Views
    {
        public static bool AreDifferent(Entity one, Entity two)
        {
            return JsonUtils.AreDifferent(one, two);
        }
        //public static bool AreDifferent(View one, View two)
        //{
        //  return JsonUtils.AreDifferent(one, two);
        //}
    }
    public static class StoredProcs
    {
        public static bool AreDifferent(StoredProc one, StoredProc two)
        {
            return JsonUtils.AreDifferent(one, two);
        }
    }
    public static class Functions
    {
        public static bool AreDifferent(Function one, Function two)
        {
            return JsonUtils.AreDifferent(one, two);
        }
    }

    public static class Generic
    {
        public static bool AreBothNull<T>(T thing1, T thing2)
        {
            return (thing1 == null && thing2 == null);
        }
        public static bool AreBothInstance<T>(T thing1, T thing2)
        {
            return (thing1 != null && thing2 != null);
        }
        public static bool AreNullDifferent<T>(T thing1, T thing2)
        {
            return (thing1 == null && thing2 != null) || (thing1 != null && thing2 == null);
        }
    }
}
