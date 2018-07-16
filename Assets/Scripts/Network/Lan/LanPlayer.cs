using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class LanPlayer : NetworkLobbyPlayer
{
    
    [SyncVar] PlayerClass playerClass;
    public PlayerClass PlayerClass
    {
        get { return playerClass;}
        set { playerClass = value; }
    }

    Lan lan;

    void Awake()
    {
        lan = FindObjectOfType<Lan>();
    }

    public override void OnClientEnterLobby()
    {
        StartCoroutine("WaitForFrame");
    }

    IEnumerator WaitForFrame()
    {
        yield return null;
        base.OnClientEnterLobby();
        lan.UpdateLobbySlot();
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
                lan.UpdateLobbySlot();
            });
        }
        
        Button button = GameObject.Find("ReadyButton").GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            SendReadyToBeginMessage();
            button.gameObject.SetActive(false);
            DontDestroyOnLoad(lan.gameObject);
        });
    }

    public override void OnClientReady(bool readyState)
    {
        base.OnClientReady(readyState);
        lan.UpdateLobbySlot();
    }
    
    [Command]
    void CmdUpdatePlayerClass(byte slot, PlayerClass playerclass)
    {
        lan.lobbySlots[slot].GetComponent<LanPlayer>().playerClass = playerclass;
        RpcUpdateLobby(slot, playerclass);
    }

    [ClientRpc]
    void RpcUpdateLobby(byte slot, PlayerClass playerclass)
    {
        //Syncvar가 Rpc보다 느린가? 아무튼 그래서 로컬에서도 바꿔줌.
        lan.lobbySlots[slot].GetComponent<LanPlayer>().playerClass = playerclass;
        lan.UpdateLobbySlot();
    }
    
    
}
