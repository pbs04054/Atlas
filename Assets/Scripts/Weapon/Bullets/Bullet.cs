using UnityEngine;
using UnityEngine.Networking;

public class Bullet : NetworkBehaviour, IBullet
{
    public Transform Transform { get { return transform; } }
    public float Damage { get; private set; }
    protected Rigidbody rigidBody;
    public NetworkInstanceId ID { get; private set; }

    [SerializeField] AudioClip[] impactOnMetals, impactOnStones, impactOnEnemies;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public void Init(float damage, Vector3 force)
    {
        this.Damage = damage;
        rigidBody.AddForce(force);
    }

    public void Init(float damage, Vector3 force, NetworkInstanceId id)
    {
        Init(damage, force);
        ID = id;
    }

    protected virtual void OnTriggerEnter(Collider col)
    {
        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy != null)
        {
            if(impactOnEnemies != null && impactOnEnemies.Length > 0)
                SoundManager.inst.PlaySFX(enemy.gameObject, impactOnEnemies.Random());
            enemy.GetDamaged(this);
            Destroy(gameObject);
            return;
        }

        if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position - transform.forward * 2f, transform.forward, out hit))
            {
                if (hit.collider.gameObject.CompareTag("Stone"))
                {
                    GameObject decal = Instantiate(Resources.Load<GameObject>("Effects/BulletDecalStone"), hit.point, Quaternion.LookRotation(hit.normal));
                    if (impactOnStones != null && impactOnStones.Length > 0)
                        SoundManager.inst.PlaySFX(decal, impactOnStones.Random());
                }
                else if (hit.collider.gameObject.CompareTag("Metal"))
                {
                    GameObject decal = Instantiate(Resources.Load<GameObject>("Effects/BulletDecalMetal"), hit.point, Quaternion.LookRotation(hit.normal));
                    if (impactOnMetals != null&& impactOnMetals.Length > 0)
                        SoundManager.inst.PlaySFX(decal, impactOnMetals.Random());
                }
            }

            Destroy(gameObject);
        }
    }
}
