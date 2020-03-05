using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class locator : MonoBehaviour
{

   // public Transform player;
    public Transform target;

    public float range = 5f; //在范围内就不显示，否则显示
    private float offect;

    private float GetDistance()
    {
        var viewCenter = CameraView.mCorners[4];
        return Vector3.Distance(viewCenter, target.position);
    }

    private Vector2 GetDiection()
    {
        var viewCenter = CameraView.mCorners[4];
        Vector2 ret = new Vector2(target.transform.position.x - viewCenter.x, target.transform.position.z - viewCenter.z );
        Debug.DrawLine(viewCenter,target.position );
        return ret;
    }

    void Start()
    {
        offect = (this.GetComponent<RectTransform>().sizeDelta.x + this.GetComponent<RectTransform>().sizeDelta.y) / 4;
    }
    public GameObject line;

    public bool IsInView()
    {
        Transform camTransform = Camera.main.transform;
        Vector2 viewPos = Camera.main.WorldToViewportPoint(target.position);
        Vector3 dir = (target.position - camTransform.position).normalized;
        float dot = Vector3.Dot(camTransform.forward, dir);     //判断物体是否在相机前面


        if (dot > 0 && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
            return true;
        else
            return false;
    }

    void Update()
    {
        bool isIN = IsInView();



        if (isIN)
        {
            transform.localScale = Vector3.zero; //隐藏
            return;
        }
        else
        {
            transform.localScale = Vector3.one; //显示
        }
        Vector3 viewCenter = CameraView.mCorners[4];

        Vector3 dir = (target.position - viewCenter).normalized;
        dir.y = 0;
        float dot = Vector3.Dot(dir, Vector3.forward); //以Vector3.forward为基准
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        Vector3 cross = Vector3.Cross(dir, Vector3.forward); //判断左右

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle * (cross.y > 0 ? 1 : -1)));

        //test
        line.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle * (cross.y > 0 ? 1 : -1)));




            var localPoint = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(this.transform.parent.GetComponent<RectTransform>(),  new Vector2(Screen.width/2, Screen.height/2),null , out localPoint);
            RaycastHit2D hit = Physics2D.Raycast(  new Vector2(960, 540), GetDiection(), 1024f, (1 << LayerMask.NameToLayer("UI")));
            Debug.DrawRay(localPoint, Vector2.up);
            if (hit.collider != null)
            {

                transform.position = new Vector3(hit.point.x, hit.point.y, transform.position.z);
                Vector2 normal = -1 * GetDiection().normalized;
                transform.localPosition = new Vector3( transform.localPosition.x +(normal.x * offect),transform.localPosition.y +(normal.y * offect), transform.localPosition.z);
            }



    }
}