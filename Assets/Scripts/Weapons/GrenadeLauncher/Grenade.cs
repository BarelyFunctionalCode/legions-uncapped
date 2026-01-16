using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : Projectile
{
    [SerializeField] private float impactVelocityThreshold = 50;
    [SerializeField, Range(1, 5)] private int bouncesBeforeExplode = 2;
    private int bounceCount;

    // Grenades allegedly bounce at slow speeds instead of exploding on impact
    protected override bool DoImpactCheck(Collision collision)
    {
        bounceCount++;
        return bounceCount >= bouncesBeforeExplode || GetComponent<Rigidbody>().linearVelocity.magnitude >= impactVelocityThreshold;
    }
}
