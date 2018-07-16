using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour {

    public Behaviour[] disableWhenNotLocal;

	// Use this for initialization
	void Start () {
		if (!isLocalPlayer)
        {
            foreach (var b in disableWhenNotLocal)
            {
                b.enabled = false;
            }
        }
        else
        {
            GameManager.inst.playerController = GetComponent<PlayerController>();
            GameManager.inst.cameraController.CameraStart();
        }
        GameManager.inst.players.Add(GetComponent<NetworkIdentity>().netId, GetComponent<Player>());
        name = "Player" + GetComponent<NetworkIdentity>().netId.ToString();
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
