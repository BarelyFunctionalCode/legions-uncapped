using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dummy : MonoBehaviour
{
    private float health = 100;
    private float maxHealth = 100;

    private Material material;

    [SerializeField] private GameObject explodeParticleObj;

    void Awake()
    {
        material = GetComponent<MeshRenderer>().materials[0];
    }

    // Update is called once per frame
    void Update()
    {
        material.color = Color.Lerp(Color.green, Color.red, 1.0f - health / maxHealth);

    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0) Die();
    }

    private void Die()
    {
        explodeParticleObj.transform.parent = null;
        explodeParticleObj.SetActive(true);
        Destroy(gameObject);
    }
}
