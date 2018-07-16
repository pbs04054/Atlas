using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roller : MonoBehaviour, IDamageable
{

    public Transform Transform
    {
        get { return transform; }
    }
    public float MaxHealth { get; private set; }
    public float CurHealth { get; private set; }
    
    Rigidbody rigidBody;
    [SerializeField] float velocity;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        CurHealth = MaxHealth = 500;
        StartCoroutine("RollerUpdator");
    }

    IEnumerator RollerUpdator()
    {
        while (true)
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, 1);
            foreach (Collider col in cols)
            {
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable == null || damageable == GetComponent<IDamageable>() || damageable.Transform.GetComponent<Jiralhanae>() != null)
                    continue;
                damageable.GetDamaged(50);
            }
            rigidBody.MovePosition(transform.position + transform.forward * velocity * Time.deltaTime);
            yield return null;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Vector3 reflection = Vector3.Reflect(transform.forward, other.contacts[0].normal);
            transform.rotation = Quaternion.LookRotation(reflection);
        }
    }
    
    public void GetDamaged(float amount)
    {
        //Todo : Bullet이 데미지를 줄 수 있게 변경해야함.
        CurHealth -= amount;
        if(CurHealth <= 0)
            Destroy(gameObject);
    }
}