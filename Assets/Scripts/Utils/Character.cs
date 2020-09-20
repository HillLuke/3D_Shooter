using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("General")]
    public Health Health;
    public Animator Animator;

    [Header("Weapon")]
    public Transform Muzzle;
    public Vector3 Target;
    public LayerMask HitLayer;
    public float BulletSpreadOverride = -1;
    public float DamageMultiplierOverride = -1;

    void Start()
    {
        Health = GetComponent<Health>();
        Animator = GetComponent<Animator>();
    }

    void Update()
    {
        
    }
}
