using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public static class Helper
{
    /// <summary>
    /// Easier way of creating a TransformState as TransformState cannot have a constructor 
    /// due to it being serialized for network usage.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="hasStartedMoving"></param>
    /// <returns></returns>
    public static TransformState TransformState(int tick, Vector3 position, Quaternion rotation, bool hasStartedMoving)
    {
        return new TransformState()
        {
            Tick = tick,
            Position = position,
            Rotation = rotation,
            HasStartedMoving = hasStartedMoving
        };
    }

    /// <summary>
    /// Gets all flags in given enum.
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static IEnumerable<Enum> GetFlags(this Enum enumValue)
    {
        return Enum.GetValues(enumValue.GetType()).Cast<Enum>().Where(enumValue.HasFlag);
    }

    /// <summary>
    /// Checks if given enum contains any flags that the other enum also has.
    /// </summary>
    /// <param name="enumValue"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool ContainsAnyFlags(this Enum enumValue, Enum other)
    {
        return
            enumValue.GetFlags().ToList().Where(value => !value.ToString().Equals("None")).Any(
                value => enumValue.HasFlag(value) && other.HasFlag(value));
    }

    /// <summary>
    /// Sets layermask of parent and all it's children.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="layerMask"></param>
    /// <param name="mask"></param>
    public static void SetLayerMask(Transform parent, LayerMask layerMask, int mask = -2)
    {
        if (mask == -2)
            mask = Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));

        foreach (Transform child in parent)
        {
            child.gameObject.layer = mask;
            SetLayerMask(child, layerMask, mask);
        }
    }


}
