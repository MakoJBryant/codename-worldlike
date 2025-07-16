using UnityEngine;
using System.Collections.Generic;

public class GravityManager : MonoBehaviour
{
    [Tooltip("Gravitational force applied to nearby rigidbodies")]
    public float gravityStrength = 20f;

    [Tooltip("Effective gravity range")]
    public float gravityRadius = 150f;

    private static List<GravityManager> allGravitySources = new List<GravityManager>();

    void OnEnable()
    {
        if (!allGravitySources.Contains(this))
            allGravitySources.Add(this);
    }

    void OnDisable()
    {
        if (allGravitySources.Contains(this))
            allGravitySources.Remove(this);
    }

    public static Vector3 GetGravityAtPoint(Vector3 position, out GravityManager source)
    {
        GravityManager closest = null;
        float closestDistance = float.MaxValue;

        foreach (var g in allGravitySources)
        {
            float dist = Vector3.Distance(position, g.transform.position);
            if (dist < g.gravityRadius && dist < closestDistance)
            {
                closest = g;
                closestDistance = dist;
            }
        }

        if (closest != null)
        {
            source = closest;
            return (closest.transform.position - position).normalized * closest.gravityStrength;
        }

        source = null;
        return Vector3.zero;
    }
}
