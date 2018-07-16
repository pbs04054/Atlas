public static class ArrayExtension
{
    public static T Random<T>(this T[] t)
    {
        return t[UnityEngine.Random.Range(0, t.Length)];
    }
}