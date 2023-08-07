using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private SphereCollider projectileCollider;
    [SerializeField] protected CapsuleCollider damageRadiusTrigger;
    [SerializeField] protected GameObject firedParticleObj;
    [SerializeField] private GameObject impactParticleObj;
    [SerializeField] private Material firedMaterial;
    [SerializeField] protected float launchForce = 400;
    [SerializeField] private ForceMode launchForceMode = ForceMode.VelocityChange;
    [SerializeField] protected float maxImpactForce = 2000;
    [SerializeField] private float damageRadius = 1f;
    [SerializeField] private float selfDestructTimer = 10;
    [SerializeField] protected float armingTimer = 0f;
    [SerializeField] public bool hasHoldModifier = false;

    protected Transform ownerTransform;
    private Vector3 previousPosition;
    private List<Collider> damagedReceivers = new List<Collider>();
    protected float maxDamage;

    public bool isFired = false;

    protected virtual void Awake()
    {
        previousPosition = GetComponent<Rigidbody>().position;
        if (damageRadiusTrigger != null) damageRadiusTrigger.radius = damageRadius * 2;
    }


    protected virtual void FixedUpdate()
    {
        if (!isFired) return;
        selfDestructTimer -= Time.fixedDeltaTime;
        armingTimer -= Time.fixedDeltaTime;
        if (selfDestructTimer <= 0)
        {
            Impact();
        }

        transform.LookAt(GetComponent<Rigidbody>().position + GetComponent<Rigidbody>().velocity.normalized);

        float currentDisplacement = (GetComponent<Rigidbody>().position - previousPosition).magnitude;

        if (damageRadiusTrigger != null) damageRadiusTrigger.height = currentDisplacement * 2 + damageRadius * 2;
        if (damageRadiusTrigger != null) damageRadiusTrigger.center = new Vector3(0, damageRadiusTrigger.height / 2, 0);

        previousPosition = GetComponent<Rigidbody>().position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == ownerTransform.gameObject) return;
        if (!Weapon.interactionTags.Contains(collision.gameObject.tag)) return;

        if (!DoImpactCheck(collision)) return;

        selfDestructTimer /= 2.0f;

        Collider impactCollider = null;
        if (collision.gameObject.CompareTag("Player")) impactCollider = collision.collider;
        if (impactCollider != null || armingTimer <= 0) Impact(impactCollider);
    }

    protected virtual bool DoImpactCheck(Collision collision) { return true; }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFired) return;
        AddDamageReceiver(other);
    }

    private void AddDamageReceiver(Collider receiverCollider)
    {
        if (!receiverCollider.gameObject.CompareTag("Player")) return;
        if (!damagedReceivers.Contains(receiverCollider)) damagedReceivers.Add(receiverCollider);
    }

    public void Fire(Transform ownerTransform, float maxDamage)
    {
        if (isFired) return;

        this.ownerTransform = ownerTransform;
        this.maxDamage = maxDamage;

        Vector3 intialVelocity = Vector3.Dot(transform.parent.GetComponentInParent<Rigidbody>().velocity, transform.forward) * transform.forward;

        transform.parent = null;
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<Rigidbody>().AddForce(intialVelocity, ForceMode.VelocityChange);
        GetComponent<Rigidbody>().AddForce(transform.forward * launchForce, launchForceMode);

        if (firedMaterial != null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null) child.GetComponent<MeshRenderer>().material = firedMaterial;
            }
        }

        if (firedParticleObj != null)firedParticleObj.SetActive(true);
        projectileCollider.enabled = true;

        isFired = true;
    }

    private void Impact(Collider directImpactCollider = null) {
        if (impactParticleObj != null)
        {
            impactParticleObj.transform.parent = null;
            impactParticleObj.SetActive(true);
        }
        if (firedParticleObj != null)
        {
            Vector3 firedParticlesScale = firedParticleObj.transform.localScale;
            firedParticleObj.transform.parent = null;
            firedParticleObj.transform.localScale = firedParticlesScale;
            firedParticleObj.GetComponent<ParticleSystem>().Stop();
        }

        OnImpact();

        if (directImpactCollider != null)
        {
            ApplyDamage(directImpactCollider.gameObject, maxDamage);
            if (directImpactCollider.transform.GetComponent<Rigidbody>() != null) directImpactCollider.transform.GetComponent<Rigidbody>().AddForce(
                transform.forward * maxImpactForce,
                ForceMode.Impulse
            );
            if (damagedReceivers.Contains(directImpactCollider)) damagedReceivers.Remove(directImpactCollider);
        }

        foreach (Collider receiver in damagedReceivers)
        {
            if (receiver == null) continue;
            
            float distance = Vector3.Distance(damageRadiusTrigger.transform.position, receiver.ClosestPoint(damageRadiusTrigger.transform.position));
            ApplyDamage(receiver.gameObject, maxDamage * Mathf.Max(1 - distance / damageRadius, 0));
            if (receiver.GetComponent<Rigidbody>() != null) receiver.GetComponent<Rigidbody>().AddExplosionForce(
                maxImpactForce,
                GetComponent<Rigidbody>().position,
                damageRadius,
                1,
                ForceMode.Impulse
            );
        }

        Destroy(gameObject);
    }

    protected virtual void OnImpact() {}

    protected void ApplyDamage(GameObject target, float damage)
    {
        print("Applying " + damage + " damage to " + target.name);
        if (target.GetComponent<Dummy>() != null) target.GetComponent<Dummy>().TakeDamage(damage);
    }
}
