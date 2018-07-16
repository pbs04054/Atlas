using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : SingletonBehaviour<GameManager> {

    public EnemyManager enemyManager;
    public PlayerController playerController;
    //public SkillManager skillManager;
    public ShopManager shopManager;
    public InGameUIManager inGameUIManager;
    public CameraController cameraController;
    public ServerSimulator serverSimulator;

    public Dictionary<NetworkInstanceId, Player> players = new Dictionary<NetworkInstanceId, Player>();

    public AudioClip BGM2;

    // Update is called once per frame

    void Update()
    {
        if (playerController != null)
        {
            playerController.PlayerControllerUpdate();
            playerController.player.PlayerUpdate();
        }
    }

    public void Coroutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public void CheckIsGameOver()
    {
        bool isGameOver = true;
        foreach (var dic in players)
        {
            if (dic.Value.CurHealth > 0)
            {
                isGameOver = false;
                break;
            }
        }
        if (isGameOver)
            FindObjectOfType<ServerSimulator>().RpcGameOver();
    }
}