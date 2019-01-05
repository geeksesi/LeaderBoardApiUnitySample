using System;
using UnityEngine;

public static class DateTimeExtensions
{
    public static long ToUnixTimestamp(this DateTime d)
    {
        var epoch = d - new DateTime(1970, 1, 1, 0, 0, 0);

        return (long)epoch.TotalSeconds;
    }
}

public static class TransformExtensions
{
    public static void DestroyAllChilds(this Transform transform)
    {
        foreach (Transform x in transform)
        {
            UnityEngine.Object.Destroy(x.gameObject);
        }
    }
}