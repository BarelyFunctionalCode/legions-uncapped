using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterLauncher : Weapon
{
    private int clusterCount = 6;


    protected override void DoHoldModifierStart(Projectile currentProjectile)
    {
        ((ClusterGrenade)currentProjectile).SetToClusterMode(clusterCount);
    }

    protected override void DoHoldModifierEnd(Projectile currentProjectile)
    {
        ((ClusterGrenade)currentProjectile).SetToExplodeCluster();
    }
}
