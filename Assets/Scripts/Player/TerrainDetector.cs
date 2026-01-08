using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDetector : MonoBehaviour
{
    List<ContactPoint> terrainContactPoints = new List<ContactPoint>();

    private CapsuleCollider playerCollider;
    void Awake()
    {
        playerCollider = transform.parent.GetComponent<CapsuleCollider>();
    }

    public List<ContactPoint> GetTerrainContactPoints()
    {
        List<ContactPoint> tempTerrainContactPoints = new List<ContactPoint>(terrainContactPoints);
        terrainContactPoints.Clear();
        return tempTerrainContactPoints;
    }

    void OnCollisionEnter(Collision col)
    {
        // Debug.Log("Collision");
        terrainContactPoints.AddRange(col.contacts);
    }

    void OnCollisionStay(Collision col)
    {
        // Debug.Log("Collision");
        terrainContactPoints.AddRange(col.contacts);
    }

    void OnCollisionExit(Collision col)
    {
        // Debug.Log("Collision");
        terrainContactPoints.AddRange(col.contacts);
    }
}
