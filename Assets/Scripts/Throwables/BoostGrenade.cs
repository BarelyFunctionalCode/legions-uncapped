using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostGrenade : Throwable
{
    [SerializeField] private Transform blastWaveObj;
    [SerializeField] private Transform coreObj;
    private Color startColor;
    private Color endColor;
    private float blastWaveRadius = 10f;
    private float blastWaveRadiusIncreaseRate = 5f;
    private float maxBoostForce = 3000f;
    private float minBoostForce = 1000f;
    private float boostForceFactor = 1f;

    private float customGravity = 5f;

    protected override void Awake()
    {
        base.Awake();
        startColor = blastWaveObj.GetComponent<MeshRenderer>().material.color;
        endColor = startColor;
        endColor.a = 0;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Apply custom gravity
        GetComponent<Rigidbody>().AddForce(Vector3.down * customGravity, ForceMode.Acceleration);

        if (!isDetonating) return;

        if (boostForceFactor > 0)
        {
            boostForceFactor -= Time.fixedDeltaTime * blastWaveRadiusIncreaseRate;

            blastWaveObj.localScale = Vector3.Lerp(Vector3.one * blastWaveRadius, Vector3.zero, boostForceFactor);
            effectRadius = 1 + blastWaveRadius * (1 - boostForceFactor);
            blastWaveObj.GetComponent<MeshRenderer>().material.color = Color.Lerp(endColor, startColor, boostForceFactor);
        }
        else
        {
            isFinished = true;
        }
    }

    protected override void DoThrowableEffect(Transform receiverTransform, float effectFactor)
    {
        Vector3 boostDirection = (receiverTransform.position - transform.position).normalized;
        float boostForce = Mathf.Lerp(minBoostForce, maxBoostForce, boostForceFactor) * effectFactor;

        receiverTransform.gameObject.GetComponent<Rigidbody>().AddForce(boostDirection * boostForce, ForceMode.Impulse);
    }

    protected override void OnDetonate()
    {
        coreObj.gameObject.SetActive(false);
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        GetComponent<Rigidbody>().isKinematic = true;
        throwableCollider.enabled = false;
    }
}
