using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityExtensions {

    public static T RemoveAndGetItem<T>(this List<T> list, int index)
    {
        if (index >= 0 && index < list.Count)
        {
            var item = list[index];
            list.RemoveAt(index);
            return item;
        }
        else
        {
            Debug.LogError(string.Format("Error: Cannot remove item of index {0} from list of length {1}", index, list.Count));
            return default(T);
        }
    }

    public static Vector2 GetRandomUnitVector()
    {
        float angleRadians = UnityEngine.Random.Range(0, Mathf.PI * 2);
        return new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
    }
}
