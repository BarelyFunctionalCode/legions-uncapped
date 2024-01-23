using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dummy : Entity
{
    private Material material;

    [SerializeField] private GameObject explodeParticleObj;

    protected override void Awake()
    {
        base.Awake();

        material = GetComponent<MeshRenderer>().materials[0];
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        material.color = Color.Lerp(Color.green, Color.red, 1.0f - GetHealthPercentage());
    }

    protected override void OnDie()
    {
        explodeParticleObj.transform.parent = null;
        explodeParticleObj.SetActive(true);
    }
}
