using UnityEngine;
using System.Collections;

public static class RectExtension
{

    public static Rect AddY(this Rect rect, float y)
    {
        rect.y += y;
        return rect;
    }

}
