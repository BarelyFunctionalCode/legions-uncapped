using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : MonoBehaviour
{
    [SerializeField] protected SphereCollider throwableCollider;
    [SerializeField] protected CapsuleCollider effectRadiusTrigger;

    [SerializeField] private float throwMaxForce = 2000f;
    [SerializeField] private float throwMinForce = 500f;
    [SerializeField] protected float effectRadius = 1f;
    [SerializeField] private float selfDestructTimer = 1.5f;

    protected Transform ownerTransform;
    private Vector3 previousPosition;
    private List<Collider> affectedEntities = new List<Collider>();

    public bool isThrown = false;
    protected bool isDetonating = false;
    protected bool isFinished = false;


    protected virtual void Awake()
    {
        previousPosition = transform.position;
        if (effectRadiusTrigger != null) effectRadiusTrigger.radius = effectRadius * 2;
    }

    protected virtual void FixedUpdate()
    {
        if (!isThrown) return;
        selfDestructTimer -= Time.fixedDeltaTime;
        if (selfDestructTimer <= 0)
        {
            Detonate();
        }

        if (isDetonating && isFinished)
        {
            Destroy(gameObject);
        }

        transform.LookAt(transform.position + GetComponent<Rigidbody>().velocity.normalized);

        float currentDisplacement = (transform.position - previousPosition).magnitude;

        if (effectRadiusTrigger != null) effectRadiusTrigger.height = effectRadius * 2;
        // if (effectRadiusTrigger != null) effectRadiusTrigger.height = currentDisplacement + effectRadius * 2;
        if (effectRadiusTrigger != null) effectRadiusTrigger.radius = effectRadius;
        // if (effectRadiusTrigger != null) effectRadiusTrigger.center = new Vector3(0, effectRadiusTrigger.height / 2, 0);

        previousPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == ownerTransform.gameObject) return;
        if (!ThrowableManager.interactionTags.Contains(collision.gameObject.tag)) return;

        selfDestructTimer /= 2.0f;

        Collider impactCollider = null;
        if (collision.gameObject.CompareTag("Projectile")) Detonate(impactCollider);
        if (collision.gameObject.CompareTag("Player")) impactCollider = collision.collider;
        if (impactCollider != null) Detonate(impactCollider);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isThrown || !isDetonating) return;
        if (!other.gameObject.CompareTag("Player")) return;
        if (!affectedEntities.Contains(other))
        {
            float distance = Vector3.Distance(transform.position, other.ClosestPoint(transform.position));

            // TODO: This is not at all right
            float effectFactor = Mathf.Clamp01(1 - distance / effectRadius);

            DoThrowableEffect(other.transform, effectFactor);
            affectedEntities.Add(other);
        }
    }

    protected virtual void DoThrowableEffect(Transform receiverTransform, float effectFactor) {}

    public void Throw(Transform ownerTransform, float throwForceFactor)
    {
        this.ownerTransform = ownerTransform;

        transform.parent = null;
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<Rigidbody>().AddForce(transform.forward * Mathf.Lerp(throwMinForce, throwMaxForce, throwForceFactor), ForceMode.Impulse);

        throwableCollider.enabled = true;
        isThrown = true;
    }

    private void Detonate(Collider impactCollider = null)
    {
        if (isDetonating) return;
        if (impactCollider != null) 
        {
            DoThrowableEffect(impactCollider.transform, 1);
            affectedEntities.Add(impactCollider);
        }
        isDetonating = true;
        OnDetonate();
    }

    protected virtual void OnDetonate() {}
}
