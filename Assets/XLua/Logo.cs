using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logo : MonoBehaviour
{

    private Action mFunc;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Log Start");
        Action<string> test = XLuaMgr.Env.Global.Get<Action<string>>("TestOneParamAction");
        test.Invoke("from Log");
    }

    // Update is called once per frame
    void Update()
    {
        XLuaMgr.Update();
        mFunc?.Invoke();
    }

    private void LateUpdate()
    {
        XLuaMgr.LateUpdate();
    }

    private void FixedUpdate()
    {
        XLuaMgr.FixedUpdate();
    }

    public void AddFunc(Action func)
    {
        mFunc += func;
    }
}
