
using XLua;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class XluaExportConfig
{
    [LuaCallCSharp]
    public static List<Type> lua_call_cs_list = new List<Type>()
    {
        typeof(UnityEngine.Time),
        typeof(Debug),
        typeof(TestXlua),
    };
}
