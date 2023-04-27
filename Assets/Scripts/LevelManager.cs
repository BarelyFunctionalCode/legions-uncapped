using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private float gravity = 40f;
    void Awake()
    {
        Physics.gravity = Vector3.up * -gravity;
    }
}
