using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class Lan : NetworkLobbyManager
{

    public bool IsServer { get { return NetworkServer.active; } }
    bool flag { get { return client == null || client.connection == null || client.connection.connectionId == -1; } }
    [SerializeField] GameObject mainPanel, lobbyPanel, serverPanel, clientPanel;
    
    #region Buttons

    public void StartServerButton()
    {
        if (IsClientConnected() || IsServer) return;
        StartServer();
        GotoServerPanel();
    }

    public void StartClientButton()
    {
        if (IsClientConnected() || IsServer) return;
        GotoClientPanel();
    }

    public void ConnectToIpButton()
    {
        networkAddress = clientPanel.transform.Find("Panel").Find("IPHolder").Find("InputField").Find("Text").GetComponent<Text>().text;
        StartClient();
    }

    public void StartSingleButton()
    {
        if (IsClientConnected() || IsServer) return;
        StartHost();
    }

    public void StopHostButton()
    {
        if (IsServer || IsClientConnected())
            StopHost();
        GotoMainPanel();
    }

    public void StopClientButton()
    {
        if (flag) return;
        StopClient();
        GotoMainPanel();
    }

    public void GotoMainPanel()
    {
        mainPanel.SetActive(true);
        clientPanel.SetActive(false);
        serverPanel.SetActive(false);
        lobbyPanel.SetActive(false);
    }
    
    #endregion

    void GotoServerPanel()
    {
        mainPanel.SetActive(false);
        serverPanel.SetActive(true);
        Text text = serverPanel.transform.Find("Text").GetComponent<Text>();
        text.text = "server listening on\n" + Network.player.ipAddress + " port " + networkPort;
    }

    void GotoClientPanel()
    {
        mainPanel.SetActive(false);
        clientPanel.SetActive(true);
    }

    void GotoLobbyPanel()
    {
        mainPanel.SetActive(false);
        clientPanel.SetActive(false);
        serverPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        GotoLobbyPanel();
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        GotoLobbyPanel();
    }


    public void UpdateLobbySlot()
    {
        Transform slots = lobbyPanel.transform.Find("CharacterSlots");
        for (var i = 0; i < lobbySlots.Length; i++)
        {
            slots.GetChild(i).Find("NotConnected").gameObject.SetActive(lobbySlots[i] == null);
            if (lobbySlots[i] == null)
                continue;
            LanPlayer lanPlayer = lobbySlots[i].GetComponent<LanPlayer>();
            Text readyText = slots.GetChild(i).Find("ReadyText").GetComponent<Text>();
            if (lanPlayer.readyToBegin)
            {
                readyText.text = "Ready";
                readyText.color = new Color(1, 95 / 255f, 95 / 255f);
            }
            else
            {
                readyText.text = "Not Ready";
                readyText.color = new Color(50 / 255f, 50 / 255f, 50 / 255f);
            }

            Transform classButtons = GameObject.Find("CharacterSlots").transform.GetChild(i).Find("ClassButtons");
            foreach (Transform button in classButtons)
            {
                button.GetComponent<Button>().interactable = button != classButtons.GetChild((int) lanPlayer.PlayerClass);
            }
        }
    }

    public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lanPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<Player>().playerClass = lanPlayer.GetComponent<LanPlayer>().PlayerClass;
        Debug.Log("OnLobbyServerSceneLoadedForPlayer");
        return base.OnLobbyServerSceneLoadedForPlayer(lanPlayer, gamePlayer);
    }
    
}