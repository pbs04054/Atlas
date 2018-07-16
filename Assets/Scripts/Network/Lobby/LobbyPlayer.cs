using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPlayer : NetworkLobbyPlayer
{

    [SyncVar] PlayerClass playerClass;
    public PlayerClass PlayerClass
    {
        get { return playerClass;}
        set { playerClass = value; }
    }
    
    Lobby lobby;
    
    void Awake()
    {
        lobby = FindObjectOfType<Lobby>();
    }
    
    public override void OnClientEnterLobby()
    {
        StartCoroutine("WaitForFrame");
    }

    IEnumerator WaitForFrame()
    {
        yield return null;
        base.OnClientEnterLobby();
        lobby.UpdateLobbySlot();
        if (!isLocalPlayer)
            yield break;

        Button[] classButtons = GameObject.Find("CharacterSlots").transform.GetChild(slot).Find("ClassButtons")
            .GetComponentsInChildren<Button>();

        for (int i = 0; i < classButtons.Length; i++)
        {
            int index = i;
            ColorBlock colorBlock = classButtons[i].colors;
            colorBlock.highlightedColor = new Color(245/255f,245/255f,245/255f);
            classButtons[i].colors = colorBlock;
            classButtons[i].onClick.AddListener(() =>
            {
                CmdUpdatePlayerClass(slot, (PlayerClass)index);
                lobby.UpdateLobbySlot();
            });
        }
        
        Button button = GameObject.Find("ReadyButton").GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            SendReadyToBeginMessage();
            button.gameObject.SetActive(false);
        });
    }

    public override void OnClientReady(bool readyState)
    {
        base.OnClientReady(readyState);
        lobby.UpdateLobbySlot();
    }
    
    [Command]
    void CmdUpdatePlayerClass(byte slot, PlayerClass playerclass)
    {
        lobby.lobbySlots[slot].GetComponent<LobbyPlayer>().playerClass = playerclass;
        RpcUpdateLobby(slot, playerclass);
    }

    [ClientRpc]
    void RpcUpdateLobby(byte slot, PlayerClass playerclass)
    {
        //Syncvar가 Rpc보다 느린가? 아무튼 그래서 로컬에서도 바꿔줌.
        lobby.lobbySlots[slot].GetComponent<LobbyPlayer>().playerClass = playerclass;
        lobby.UpdateLobbySlot();
    }
    
}
