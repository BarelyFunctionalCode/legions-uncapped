using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterGrenade : Projectile
{
    private GameObject grenadePrefabObj;
    private int clusterCount = 1;
    private float clusterModeDebounceTimer = 0.5f;
    public bool isClusterMode = false;
    private bool doExplodeCluster = false;

    protected override void Awake()
    {
        base.Awake();

        grenadePrefabObj = Resources.Load<GameObject>("Prefabs/Weapons/ClusterLauncher/ClusterGrenade");
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!isFired) return;
        clusterModeDebounceTimer -= Time.fixedDeltaTime;
        if (doExplodeCluster && clusterModeDebounceTimer <= 0)
        {
            print("Explode cluster " + clusterCount);
            if (clusterCount > 1) ExplodeCluster();
        }
    }

    protected override bool DoImpactCheck(Collision collision)
    {
        if (!isClusterMode && clusterModeDebounceTimer <= 0) return true;
        if (doExplodeCluster) return true;
        return false;
    }

    public void SetToClusterMode(int clusterCount)
    {
        this.clusterCount = clusterCount;
        if (clusterModeDebounceTimer <= 0 && !isClusterMode) isClusterMode = true;
    }

    public void SetToExplodeCluster()
    {
        if (isClusterMode && !doExplodeCluster) doExplodeCluster = true;
    }

    public void SetSubCluster(int clusterCount, float maxImpactForce)
    {
        this.clusterCount = clusterCount;
        this.maxImpactForce = maxImpactForce;
        launchForce = 0;
        doExplodeCluster = true;
    }

    
    private void ExplodeCluster()
    {
        Vector3 firedParticlesScale = firedParticleObj.transform.localScale;
        firedParticleObj.transform.parent = null;
        firedParticleObj.transform.localScale = firedParticlesScale;
        firedParticleObj.GetComponent<ParticleSystem>().Stop();

        RaycastHit hit;
        bool didHit = Physics.Raycast(
            new Ray(
                transform.position,
                Vector3.down
            ),
            out hit
        );

        bool isGrounded = didHit && hit.distance < 1;

        for (int i = 0; i < clusterCount; i++)
        {
            // Create ring of grenades
            float angle = 360.0f + Random.Range(-30f, 30f) / clusterCount * i;
            Vector3 spawnPos = transform.position + Quaternion.Euler(0, angle, 0) * transform.forward * 0.1f;
            if (isGrounded) spawnPos += Vector3.up * 0.5f;

            GameObject newGrenadeObj = Instantiate(grenadePrefabObj, spawnPos, transform.rotation, transform);
            int newClusterCount = Mathf.Max(clusterCount - 2, 1);
            newGrenadeObj.GetComponent<ClusterGrenade>().SetSubCluster(newClusterCount, maxImpactForce / newClusterCount);
            newGrenadeObj.GetComponent<ClusterGrenade>().Fire(
                ownerTransform,
                maxDamage / (clusterCount-1)
            );

            newGrenadeObj.GetComponent<Rigidbody>().AddExplosionForce(80, transform.position + Random.insideUnitSphere * 0.1f, 10, 0, ForceMode.Impulse);
        }

        Destroy(gameObject);
    }
}
