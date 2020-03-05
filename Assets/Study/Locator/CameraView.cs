using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraView : MonoBehaviour
{
    private Camera mCamera;
    private Transform mCameraTransform;

    private Vector3 mOriPos;
    private Vector3 mOriAng;
    public static Vector3[] mCorners;

    void Start()
    {
        mCamera = GetComponent<Camera>();
        mCameraTransform = GetComponent<Transform>();
        mOriPos = mCameraTransform.position;
        mOriAng = mCameraTransform.eulerAngles;

        CameraTools.GetPlaneCorners(Vector3.up, Vector3.zero, mCamera, ref mCorners);
    }

    void Update()
    {
        if (mOriPos != mCameraTransform.position || mOriAng != mCameraTransform.eulerAngles)
        {
            CameraTools.GetPlaneCorners(Vector3.up, Vector3.zero, mCamera, ref mCorners);
        }

#if UNITY_EDITOR
        DrawCameraViewRect();
#endif
    }

    private void DrawCameraViewRect()
    {
#if UNITY_EDITOR
            // mCorners 4个角的顺序：左下、右下、右上、左上、中心
            if (mCorners != null && mCorners.Length > 0)
            {
                Debug.DrawLine(mCorners[0], mCorners[1], Color.green); // bottom
                Debug.DrawLine(mCorners[1], mCorners[2], Color.green); // right
                Debug.DrawLine(mCorners[2], mCorners[3], Color.green); // top
                Debug.DrawLine(mCorners[3], mCorners[0], Color.green); // left

                Vector3 lineOrigin = mCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));
                float range = (lineOrigin - mCorners[4]).magnitude;
                Debug.DrawRay(lineOrigin, mCamera.transform.forward * range, Color.green);
            }
#endif
    }
}