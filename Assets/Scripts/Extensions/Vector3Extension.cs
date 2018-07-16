using UnityEngine;
using System.Collections;

public static class Vector3Extension
{

    /// <summary>
    /// Vector3를 pivot을 기준으로 angles만큼 회전합니다.
    /// </summary>
    /// <param name="pivot">원점 좌표</param>
    /// <param name="angles">Euler 각도</param>
    public static Vector3 RotatePointAroundPivot(this Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    public static Vector3 ExceptY(this Vector3 point)
    {
        return new Vector3(point.x, 0, point.y);
    }

}