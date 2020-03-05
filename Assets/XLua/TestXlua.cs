
using System;
using UnityEngine;
using UnityEngine.UI;

public class TestXlua
{

    public string name = "f0ur";

    public static void Out(string str)
    {
        Debug.Log("[TestXlua] Out :" + str);
    }

    public static void Add(Action func)
    {
        var go = GameObject.Find("Logo");
        Logo script = go.GetComponent<Logo>();
        script.AddFunc(func);
    }

}
