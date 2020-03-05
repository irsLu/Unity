
using System;
using UnityEngine;
using XLua;

public static class XLuaMgr
{
    public static LuaEnv Env { get; private set; }

    private static Action _luaRenderingUpdateFunc;

    private static Action _luaFixedUpdateFunc;

    private static Action _luaLateUpdateFunc;

    // 驱动入口函数名
    const string FixedUpdateFuncName = "FixedUpdate";
    const string RenderingUpdateFuncName = "RenderingUpdate";
    const string LateUpdateFuncName = "LateUpdate";

    private static byte[] XluaLoader(ref string file)
    {
        if (string.IsNullOrEmpty(file))
        {
            return null;
        }

        if (file.EndsWith(".lua"))
        {
            return loadFile(file);
        }

        return loadFile(file + ".lua");
    }

    internal static byte[] loadFile(string file)
    {
        try
        {
           // TextAsset ret = (TextAsset)Resources.Load("LuaCode/"+ file, typeof(TextAsset));
           var ret = System.IO.File.ReadAllBytes(Application.dataPath + "/XLua/LuaCode/" + file);

            return ret;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static void OnXluaInitCompleted(string luaRootFile)
    {
        Env.DoString("require '" + luaRootFile + "'");

        //获取驱动接口
        _luaFixedUpdateFunc = Env.Global.Get<Action>(FixedUpdateFuncName);
        _luaRenderingUpdateFunc = Env.Global.Get<Action>(RenderingUpdateFuncName);
        _luaLateUpdateFunc = Env.Global.Get<Action>(LateUpdateFuncName);

        var main = Env.Global.Get<Action>("main");
        main.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Init()
    {

        Env = new LuaEnv();
        Env.AddLoader(XluaLoader);
        OnXluaInitCompleted("main");
    }

    /// <summary>
    /// 逻辑帧更新.
    /// </summary>
    public static void FixedUpdate()
    {
        if (Env == null) return;
        // 驱动Slua的 LuaTimer
        Env.Tick();

        // 驱动Slua逻辑帧更新
        if (_luaFixedUpdateFunc != null)
            _luaFixedUpdateFunc.Invoke();
    }

    /// <summary>
    /// 渲染驱动.
    /// </summary>
    public static void Update()
    {
        if (Env == null) return;
        // 驱动Slua逻辑帧更新
        if (_luaRenderingUpdateFunc != null)
            _luaRenderingUpdateFunc.Invoke();
    }

    /// <summary>
    /// Late Update
    /// </summary>
    public static void LateUpdate()
    {
        if (Env == null)
        {
            return;
        }
        _luaLateUpdateFunc?.Invoke();
    }

}
