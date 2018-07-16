using UnityEngine;

public static class WorldToViewport
{

    public static void WorldToUIPosition(RectTransform canvas, RectTransform self, Transform target, float offset)
    {
        Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(target.position);
        Vector2 WorldObject_ScreenPosition = new Vector2(
        ((ViewportPosition.x * canvas.sizeDelta.x) - (canvas.sizeDelta.x * 0.5f)),
        ((ViewportPosition.y * canvas.sizeDelta.y) - (canvas.sizeDelta.y * 0.5f)));

        WorldObject_ScreenPosition.y += offset;

        self.anchoredPosition = WorldObject_ScreenPosition;
    }

    public static Vector3 WorldToScreenPosition(RectTransform canvas, RectTransform self, Transform target, float offset)
    {
        Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(target.position);
        Vector2 WorldObject_ScreenPosition = new Vector2(
        ((ViewportPosition.x * canvas.sizeDelta.x) - (canvas.sizeDelta.x * 0.5f)),
        ((ViewportPosition.y * canvas.sizeDelta.y) - (canvas.sizeDelta.y * 0.5f)));

        WorldObject_ScreenPosition.y += offset;

        return WorldObject_ScreenPosition;
    }

}
