#region Usings
using System;
using System.Collections.Generic;
using System.Text;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static class EntityGenerator
{
    #region Entity
    public static string Generate(CommonSettings commonSettings, Model model, Entity entity, Schema schema)
    {
        CSharpFile.Start(commonSettings.IndentSize);

        var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Entity, model, schema);
        var usingNamespaces = new List<UsingNamespace>(3)
  {
    new UsingNamespace { Name = "System" },
    new UsingNamespace { Name = "System.Collections.Generic" },
  };

        if(entity.HXml)
        {
            usingNamespaces.Add(new UsingNamespace { Name = "System.Xml.Linq" });
        }

        var type = new ClassDeclaration
        {
            Name = Common.GenerateTypeName(entity),
            Namespace = typeNamespace,
            IsPartial = true,
            Summary = new List<string>(1) { "This class is a DTO. Can be transferred between the participants of the Data Exchange" }
        };

        //Generate file header with information about the interface.
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenClassWithNamespace(type);
        GenerateBody(commonSettings, model, entity.Ps, entity.NPs);
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }
    private static void GenerateBody(CommonSettings commonSettings, Model model, PropertyCollection props, NavPropertyCollection navProps)
    {
        if(props?.Count > 0)
        {
            CSharpFile.OpenRegion("Simple Properties");

            foreach(var p in props)
            {
                var prop = new PropertyDeclaration
                {
                    IsStatic = false,
                    IsInterfaceMember = false,
                    Name = p.N,
                    Summary = null,
                    Kind = PropertyKind.Auto,
                    ReturnType = GeneratePropType(p),
                    DefaultValue = GeneratePropDefaultValue(p)
                };
                CSharpFile.Property(prop);
            }

            CSharpFile.CloseRegion();
        }

        if(navProps?.Count > 0)
        {
            CSharpFile.EmptyLine();
            CSharpFile.OpenRegion("Navigation Properties");

            foreach(var np in navProps)
            {
                var fk = model.FKeys.GetById(np.FKId);
                if(!(fk.FKParts?.Count > 0))
                    continue;
                GenerateNavProp(commonSettings, model, np, fk);
            }

            CSharpFile.CloseRegion();
        }
    }
    #endregion

    #region StoredProcs

    public static string GenerateStoredProc(CommonSettings commonSettings, Model model, StoredProc proc, Schema schema)
    {//if it got here it means that either has parameters or return type
        CSharpFile.Start(commonSettings.IndentSize);

        var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Entity, model, schema);

        var usingNamespaces = new List<UsingNamespace>(2)
  {
    new UsingNamespace { Name = "System" },
    new UsingNamespace { Name = "System.Collections.Generic" },
  };

        var requestType = (ClassDeclaration)null;
        var responseType = (ClassDeclaration)null;

        if(proc.HPar)
        {//Generate file header with information about the class.
            requestType = new ClassDeclaration
            {
                Name = Common.GenerateRequestTypeName(proc.N),
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string>(1) { "This class represents the request parameters of this StoredProcedure" }
            };

            CSharpFile.Header(requestType, Common.SharpFactoryFileMarker);
        }

        if(proc.REn)
        {
            responseType = new ClassDeclaration
            {
                Name = proc.En.N,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string>(1) { "This class represents the result of this StoredProcedure" }
            };
            //Generate file header with information about the class.       
            CSharpFile.Header(responseType, Common.SharpFactoryFileMarker);
        }

        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenNamespace(typeNamespace);

        if(proc.HPar)
        {
            CSharpFile.OpenClass(requestType);
            GenerateRoutineBody(proc.Par, "Parameters", true);
            CSharpFile.CloseType();
        }

        if(proc.REn)
        {
            CSharpFile.OpenClass(responseType);
            GenerateRoutineBody(proc.En.Ps, "Simple Properties", false);
            CSharpFile.CloseType();
        }

        CSharpFile.CloseNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }
    #endregion

    #region Functions
    public static string GenerateFunction(CommonSettings commonSettings, Model model, Function func, Schema schema)
    {//if it got here it means that either has parameters or return type
        CSharpFile.Start(commonSettings.IndentSize);

        var hasParameters = func.Par?.Count > 0;
        var hasReturn = func.En?.Ps?.Count > 0;
        var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Entity, model, schema);

        var usingNamespaces = new List<UsingNamespace>(2)
  {
    new UsingNamespace { Name = "System" },
    new UsingNamespace { Name = "System.Collections.Generic" },
  };

        var requestType = (ClassDeclaration)null;
        var responseType = (ClassDeclaration)null;

        if(hasParameters)
        {//Generate file header with information about the class.
            requestType = new ClassDeclaration
            {
                Name = Common.GenerateRequestTypeName(func.N),
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string>(1) { "This class represents the request parameters of this Function" }
            };
            CSharpFile.Header(requestType, Common.SharpFactoryFileMarker);
        }

        if(hasReturn)
        {
            //Generate file header with information about the class.
            responseType = new ClassDeclaration
            {
                Name = func.En.N,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string>(1) { "This class represents the result of this Function" }
            };
            CSharpFile.Header(responseType, Common.SharpFactoryFileMarker);
        }

        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenNamespace(typeNamespace);
        if(hasParameters)
        {
            CSharpFile.OpenClass(requestType);
            GenerateRoutineBody(func.Par, "Parameters", true);
            CSharpFile.CloseType();
        }

        if(hasReturn)
        {
            CSharpFile.OpenClass(responseType);
            GenerateRoutineBody(func.En.Ps, "Simple Properties", false);
            CSharpFile.CloseType();
        }

        CSharpFile.CloseNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }
    #endregion

    private static void GenerateRoutineBody(PropertyCollection props, string regionName, bool isParamContainer)
    {
        CSharpFile.OpenRegion(regionName);
        foreach(var p in props)
        {
            var summary = new List<string>(1);

            if(isParamContainer)
            {
                summary.Add("Direction " + p.M.Value.ToString());
            }
            else
            {
                summary = null;
            }

            var prop = new PropertyDeclaration
            {
                IsStatic = false,
                IsInterfaceMember = false,
                Name = p.N,
                Summary = summary,
                Kind = PropertyKind.Auto,
                ReturnType = GeneratePropType(p),
                DefaultValue = GeneratePropDefaultValue(p)
            };
            CSharpFile.Property(prop);
        }

        CSharpFile.CloseRegion();
    }

    #region Generate Properties  

    private static TypeInfo GeneratePropType(Property prop)
    {
        return PropCannotBeNullable(prop) ? new TypeInfo(prop.T) : new TypeInfo(prop.T, isNullable: true);
    }
    private static bool PropCannotBeNullable(Property prop)
    {
        return
          prop.Nul == YesOrNo.No
          || prop.Str
          || prop.T.LastIndexOf(']') > -1
          || prop.Geog
          || prop.Geom
          || prop.Xml
          || string.CompareOrdinal(prop.T, "object") == 0;
    }
    private static string GeneratePropDefaultValue(Property prop)
    {
        if(string.IsNullOrWhiteSpace(prop.Def) || prop.RO == YesOrNo.Yes)
            return null;

        var defUpper = prop.Def.ToUpper();

        //this is new feature in sql for using sequences
        var sequenceIndex = defUpper.IndexOf("NEXT VALUE FOR", StringComparison.OrdinalIgnoreCase);
        if(sequenceIndex > -1)
            return null;

        //this is legacy SQL Server.  These create default values under the Programmabality\Defaults folder.     
        var createDefaultIndex = defUpper.IndexOf("CREATE DEFAULT", StringComparison.OrdinalIgnoreCase);
        if(createDefaultIndex > -1)
        {
            //CREATE DEFAULT dbo.MIP_MONEY AS 0.00M;
            prop.Def = prop.Def.Replace(Environment.NewLine, " ").Replace("\n", " ");
            var asIndex = defUpper.IndexOf(" AS ", StringComparison.Ordinal);
            if(asIndex > -1)
                prop.Def = prop.Def.Substring(asIndex + 4).Replace("GO", "").Replace("go", "").Trim();
        }

        var val = prop.Def.Replace("(", "").Replace(")", "").Replace('\'', '"');

        if(string.CompareOrdinal(val.ToLower(), "null") == 0)
            return "null";

        if(val.StartsWith("N\"", StringComparison.Ordinal) || val.StartsWith("n\"", StringComparison.Ordinal))
        {
            val = val.Substring(1);

            return val;
        }

        if(string.CompareOrdinal(prop.T, "string") == 0)
        {
            if(val[0] != '"')
            {
                val = val.Replace("\"", "\\\"");//escape double quotes on default values for strings. this used for like json or xml types.         
            }
            else
            {//"12345"
                val = val.Substring(1, val.Length - 2).Replace("\"", "\\\"");//remove the first and last quotes before escaping.       
            }

            return "\"" + val + "\"";
        }

        var isEmpty = string.IsNullOrWhiteSpace(val) || string.CompareOrdinal(val, "\"\"") == 0;
        var isNullable = prop.Nul == YesOrNo.Yes;

        //at this point is not a string and it is not explicitly null.
        if(isNullable && isEmpty)
            return "null";

        var specifiType = HandleSpecificTypesDefaultValue(prop, val);

        if(!string.IsNullOrWhiteSpace(specifiType))
            return specifiType;

        return val;
    }

    private static string HandleSpecificTypesDefaultValue(Property prop, string val)
    {
        if(string.CompareOrdinal(prop.T, "long") == 0)
            return val + "L";

        if(string.CompareOrdinal(prop.T, "decimal") == 0)
            return val + "M";

        if(string.CompareOrdinal(prop.T, "bool") == 0)
        {
            if(val[0] == '0')
                val = "false";
            else
                val = "true";

            return val;
        }

        if(string.CompareOrdinal(prop.T, "DateTime") == 0)
        {
            if(val[0] == '"')
                return "DateTime.Parse(" + val + ")";

            if(val.IndexOf('-') > -1)
                return "DateTime.Parse(\"" + val + "\")";
        }

        if(string.CompareOrdinal(prop.T, "double") == 0)
            return val + "D";

        if(string.CompareOrdinal(prop.T, "float") == 0)
            return val + "F";

        if(string.CompareOrdinal(prop.T, "Guid") == 0 && val[0] == '"')
            return "Guid.Parse(" + val + ")";

        if(string.CompareOrdinal(prop.T, "byte[]") == 0 && (val.StartsWith("0x", StringComparison.Ordinal) || val.StartsWith("0X", StringComparison.Ordinal)))
        {
            val = val.Substring(2);

            if(val.Length % 2 != 0)
            {
                throw new ArgumentException("The binary value cannot have an odd number of digits: " + prop.N);
            }

            var p = new StringBuilder(val.Length * 3);
            p.Append("{ ");
            for(var i = 0; i < val.Length; i += 2)
            {
                var currentHex = val.Substring(i, 2);
                p.Append(_hexToByteTable[currentHex].ToString());
                p.Append(',');
            }

            p.Length--;
            p.Append(" }");
            val = p.ToString();

            return val;
        }

        if(string.CompareOrdinal(prop.T, "XDocument") == 0)
        {
            if(val[0] != '"')
            {
                val = val.Replace("\"", "\\\"");//escape double quotes on default values for strings. this used for like json or xml types.         
            }
            else
            {//"12345"
                val = val.Substring(1, val.Length - 2).Replace("\"", "\\\"");//remove the first and last quotes before escaping.       
            }

            return "XDocument.Parse(\"" + val + "\")";
        }

        return null;
    }

    //private static readonly string[] _hexStringTable = new string[] {
    //    "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F",
    //    "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F",
    //    "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F",
    //    "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F",
    //    "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F",
    //    "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F",
    //    "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F",
    //    "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F",
    //    "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F",
    //    "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F",
    //    "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF",
    //    "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF",
    //    "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF",
    //    "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF",
    //    "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF",
    //    "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF",
    //};

    private static readonly Dictionary<string, byte> _hexToByteTable = new Dictionary<string, byte>(256)
    {
        {"00",0},{"01",1},{"02",2},{"03",3},{"04",4},{"05",5},{"06",6},{"07",7},
        {"08",8},{"09",9},{"0A",10},{"0B",11},{"0C",12},{"0D",13},{"0E",14},{"0F",15},
        {"10",16},{"11",17},{"12",18},{"13",19},{"14",20},{"15",21},{"16",22},{"17",23},
        {"18",24},{"19",25},{"1A",26},{"1B",27},{"1C",28},{"1D",29},{"1E",30},{"1F",31},
        {"20",32},{"21",33},{"22",34},{"23",35},{"24",36},{"25",37},{"26",38},{"27",39},
        {"28",40},{"29",41},{"2A",42},{"2B",43},{"2C",44},{"2D",45},{"2E",46},{"2F",47},
        {"30",48},{"31",49},{"32",50},{"33",51},{"34",52},{"35",53},{"36",54},{"37",55},
        {"38",56},{"39",57},{"3A",58},{"3B",59},{"3C",60},{"3D",61},{"3E",62},{"3F",63},
        {"40",64},{"41",65},{"42",66},{"43",67},{"44",68},{"45",69},{"46",70},{"47",71},
        {"48",72},{"49",73},{"4A",74},{"4B",75},{"4C",76},{"4D",77},{"4E",78},{"4F",79},
        {"50",80},{"51",81},{"52",82},{"53",83},{"54",84},{"55",85},{"56",86},{"57",87},
        {"58",88},{"59",89},{"5A",90},{"5B",91},{"5C",92},{"5D",93},{"5E",94},{"5F",95},
        {"60",96},{"61",97},{"62",98},{"63",99},{"64",100},{"65",101},{"66",102},{"67",103},
        {"68",104},{"69",105},{"6A",106},{"6B",107},{"6C",108},{"6D",109},{"6E",110},{"6F",111},
        {"70",112},{"71",113},{"72",114},{"73",115},{"74",116},{"75",117},{"76",118},{"77",119},
        {"78",120},{"79",121},{"7A",122},{"7B",123},{"7C",124},{"7D",125},{"7E",126},{"7F",127},
        {"80",128},{"81",129},{"82",130},{"83",131},{"84",132},{"85",133},{"86",134},{"87",135},
        {"88",136},{"89",137},{"8A",138},{"8B",139},{"8C",140},{"8D",141},{"8E",142},{"8F",143},
        {"90",144},{"91",145},{"92",146},{"93",147},{"94",148},{"95",149},{"96",150},{"97",151},
        {"98",152},{"99",153},{"9A",154},{"9B",155},{"9C",156},{"9D",157},{"9E",158},{"9F",159},
        {"A0",160},{"A1",161},{"A2",162},{"A3",163},{"A4",164},{"A5",165},{"A6",166},{"A7",167},
        {"A8",168},{"A9",169},{"AA",170},{"AB",171},{"AC",172},{"AD",173},{"AE",174},{"AF",175},
        {"B0",176},{"B1",177},{"B2",178},{"B3",179},{"B4",180},{"B5",181},{"B6",182},{"B7",183},
        {"B8",184},{"B9",185},{"BA",186},{"BB",187},{"BC",188},{"BD",189},{"BE",190},{"BF",191},
        {"C0",192},{"C1",193},{"C2",194},{"C3",195},{"C4",196},{"C5",197},{"C6",198},{"C7",199},
        {"C8",200},{"C9",201},{"CA",202},{"CB",203},{"CC",204},{"CD",205},{"CE",206},{"CF",207},
        {"D0",208},{"D1",209},{"D2",210},{"D3",211},{"D4",212},{"D5",213},{"D6",214},{"D7",215},
        {"D8",216},{"D9",217},{"DA",218},{"DB",219},{"DC",220},{"DD",221},{"DE",222},{"DF",223},
        {"E0",224},{"E1",225},{"E2",226},{"E3",227},{"E4",228},{"E5",229},{"E6",230},{"E7",231},
        {"E8",232},{"E9",233},{"EA",234},{"EB",235},{"EC",236},{"ED",237},{"EE",238},{"EF",239},
        {"F0",240},{"F1",241},{"F2",242},{"F3",243},{"F4",244},{"F5",245},{"F6",246},{"F7",247},
        {"F8",248},{"F9",249},{"FA",250},{"FB",251},{"FC",252},{"FD",253},{"FE",254},{"FF",255},
    };
    #endregion

    #region Generate Navigation Properties
    private static void GenerateNavProp(CommonSettings commonSettings, Model model, NavProperty prop, FKey fk)
    {
        var propDeclaration = new PropertyDeclaration
        {
            Name = prop.N,
            Kind = PropertyKind.Auto,
            AccessModifier = AccessModifiers.Public,
            SetterModifier = AccessModifiers.None,
            Summary = EnumExtensions.NavPropXmlComment(model, prop, fk),
            ReturnType = new TypeInfo(GenerateNavPropType(commonSettings, model, prop)),
        };

        CSharpFile.Property(propDeclaration);
    }

    private static string GenerateNavPropType(CommonSettings commonSettings, Model model, NavProperty prop)
    {
        var fk = model.FKeys.GetById(prop.FKId);

        var schemaName = (string)null;
        var entityName = (string)null;
        switch(prop.End)
        {
            case NavPropertyEnd.Child:
                schemaName = fk.CSc;
                entityName = fk.CEn;
                break;
            case NavPropertyEnd.Parent:
                schemaName = fk.PSc;
                entityName = fk.PEn;
                break;
            default:
                throw new ArgumentException($"Nav property type {prop.End} not recognized");
        }

        var schema = model.Categories.GetByName(CategoryNames.Entities).Schemas.GetByName(schemaName);
        var entityfullName = Common.GenerateTypeFullName(commonSettings, model.Layers.Entity, model, schema, entityName);

        return prop.Mul[0] == '1' ? entityfullName : $"List<{entityfullName}>";
    }

    #endregion

}
