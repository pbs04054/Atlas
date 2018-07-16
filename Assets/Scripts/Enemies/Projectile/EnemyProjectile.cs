using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : NetworkBehaviour
{

    bool isTriggered;
    float damage;
    float originSpeed;
    public float speed;
    Rigidbody rigidBody;

    public float Damage { get { return damage; } }

    void Awake()
    {
        transform.eulerAngles = transform.eulerAngles.y * Vector3.up;
        rigidBody = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        if (!isServer)
            return;
        rigidBody.MovePosition(transform.position + transform.forward * speed * Time.deltaTime);
    }

    public void Init(float damage, float speed)
    {
        if (!isServer)
            return;
        this.damage = damage;
        this.speed = speed;
        originSpeed = speed;
        StartCoroutine("SyncUpdator");
    }

    public void ReturnSpeed()
    {
        speed = originSpeed;
    }
    
    protected virtual void OnTriggerEnter(Collider col)
    {
        if (!isServer)
            return;
        Player player = col.GetComponent<Player>();
        if (player != null && isTriggered == false)
        {
            isTriggered = true;
            player.GetDamaged(damage);
            Destroy(gameObject);
            return;
        }
        if(col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            isTriggered = true;
            Destroy(gameObject);
        }
    }
    
    
    #region Network

    [SyncVar] Vector3 syncPos = default(Vector3);
    private float lerpRate = 5f;
    private int networkSendRate = 9;

    void FixedUpdate()
    {
        if (isServer)
            return;
        LerpPosition();
    }

    void LerpPosition()
    {
        if (syncPos == default(Vector3)) return;
        transform.position = Vector3.Lerp(transform.position, syncPos, Time.deltaTime * lerpRate);
    }
    
    IEnumerator SyncUpdator()
    {
        while (true)
        {
            RpcSendTransform(transform.position);
            yield return new WaitForSeconds(1 / (float)networkSendRate);
        }
    }

    [ClientRpc]
    void RpcSendTransform(Vector3 position)
    {
        if (isServer) return;
        syncPos = position;
    }

    #endregion

}