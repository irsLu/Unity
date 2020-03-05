using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class btn : MonoBehaviour
{
    public Canvas canvas;
    private RectTransform rt;

    private Button button;
    // Start is called before the first frame update
    void Start()
    {
        rt = this.gameObject.GetComponent<RectTransform>();
        button = gameObject.GetComponent<Button>();

        Debug.Log(rt.anchoredPosition);
        Debug.Log(rt.position);
        Debug.Log(rt.localPosition);
    }

    // Update is called once per frame
    void Update()
    {
        C();
    }

    void C(){
        Vector2 pos;
        Camera camera = canvas.GetComponent<Camera>();
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, transform.position, camera, out pos)){
            Debug.Log(pos);
        }

    }

}
