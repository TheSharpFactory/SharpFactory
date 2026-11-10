#region Usings
using System.Collections.Generic;
#endregion

namespace SF.Utils;

internal static class UtilsLookup
{
    public static class Invalid
    {
        public static readonly HashSet<string> Tables = new HashSet<string> { "sysdiagrams", "dtproperties" };
        public static readonly HashSet<string> Views = new HashSet<string> { };
        public static readonly HashSet<string> Procs = new HashSet<string>
        {
          "aba_lockinfo",
          "dt_addtosourcecontrol",
          "dt_addtosourcecontrol_u",
          "dt_adduserobject",
          "dt_adduserobject_vcs",
          "dt_checkinobject",
          "dt_checkinobject_u",
          "dt_checkoutobject",
          "dt_checkoutobject_u",
          "dt_displayoaerror",
          "dt_displayoaerror_u",
          "dt_droppropertiesbyid",
          "dt_dropuserobjectbyid",
          "dt_generateansiname",
          "dt_getobjwithprop",
          "dt_getobjwithprop_u",
          "dt_getpropertiesbyid",
          "dt_getpropertiesbyid_u",
          "dt_getpropertiesbyid_vcs",
          "dt_getpropertiesbyid_vcs_u",
          "dt_isundersourcecontrol",
          "dt_isundersourcecontrol_u",
          "dt_removefromsourcecontrol",
          "dt_setpropertybyid",
          "dt_setpropertybyid_u",
          "dt_validateloginparams",
          "dt_validateloginparams_u",
          "dt_vcsenabled",
          "dt_verstamp006",
          "dt_verstamp007",
          "dt_whocheckedout",
          "dt_whocheckedout_u",
          "sp_alterdiagram",
          "sp_creatediagram",
          "sp_dropdiagram",
          "sp_helpdiagramdefinition",
          "sp_helpdiagrams",
          "sp_renamediagram",
          "sp_upgraddiagrams",
        };
        public static readonly HashSet<string> Funcs = new HashSet<string> { "fn_diagramobjects" };
        public static readonly HashSet<string> Seqs = new HashSet<string> { };
    }
}
