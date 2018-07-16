using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

[RequireComponent(typeof(NavMeshAgent))]
public class SyncPlayer : NetworkBehaviour
{

    [SyncVar] Vector3 syncPos;
    [SyncVar] Quaternion syncRot;
    [SyncVar] Vector3 syncVelocity;

    [SerializeField] bool pos, rot;
    [SerializeField] float lerpRate;
    [SerializeField] [Range(1, 30)] int networkSendRate;
    [SerializeField] float timeRate;

    NavMeshAgent agent;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    
    
    IEnumerator Start()
    {
        while (true)
        {
            GetSyncToServer();
            yield return new WaitForSeconds(1 / (float)networkSendRate);
        }
   }

    void FixedUpdate()
    {
        LerpPosition();
    }

    void LerpPosition()
    {
        if (isLocalPlayer)
            return;
        byte error;
        //if (pos) transform.position = Vector3.Lerp(transform.position, syncPos + syncVelocity * NetworkTransport.GetCurrentRTT(NetworkManager.singleton.client.connection.hostId, NetworkManager.singleton.client.connection.connectionId, out error) * timeRate, Time.deltaTime * lerpRate);
        if (pos) transform.position = Vector3.Lerp(transform.position, syncPos, Time.deltaTime * lerpRate);
        if (rot) transform.rotation = Quaternion.Lerp(transform.rotation, syncRot, Time.deltaTime * lerpRate);
    }

    [Command]
    void CmdSyncToServer(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        syncPos = position;
        syncRot = rotation;
        syncVelocity = velocity;       
    }

    [ClientCallback]
    void GetSyncToServer()
    {
        if (!isLocalPlayer)
            return;
        CmdSyncToServer(transform.position, transform.rotation, agent.velocity);
    }

    void Reset()
    {
        pos = true;
        rot = true;
        lerpRate = 10f;
        networkSendRate = 9;
    }
    
}