using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class MultiLobby : NetworkLobbyManager
{

    [SerializeField] GameObject mainPanel, selectPanel, lobbyPanel;
    [SerializeField] GameObject matchPanel, lanPanel;
    [SerializeField] GameObject clientPanel;
    [SerializeField] GameObject matchlistPanel, createLobbyPanel, matchScrollGrid, matchContent;

    string externalIP;
    string internalIP;

    #region MM

    public void SelectMatchButton()
    {
        MMStart();
        selectPanel.SetActive(false);
        matchPanel.SetActive(true);
    }

    public void MatchBackButton()
    {
        matchPanel.SetActive(false);
        selectPanel.SetActive(true);
        StopMatchMaker();
    }
    
    //Main
    public void StartMatchButton()
    {
        mainPanel.SetActive(false);
        createLobbyPanel.SetActive(true);
    }

    public void FindMatchButton()
    {
        mainPanel.SetActive(false);
        matchlistPanel.SetActive(true);
        MMListMathces();
    }

    //CreateLobby
    public void CreateLobbyButton()
    {
        string lobbyText = createLobbyPanel.transform.Find("Panel").Find("LobbyNameHolder").Find("InputField")
            .GetComponent<InputField>().text;
        if (string.IsNullOrEmpty(lobbyText))
            return;
        MMCreateMatch(lobbyText);
    }

    void MMStart()
    {
        Debug.Log("매치 메이커 시작");
        StartMatchMaker();
    }

    void MMListMathces()
    {
        matchMaker.ListMatches(0, 10, "", true, 0, 0, OnMatchList);
    }

    public override void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        base.OnMatchList(success, extendedInfo, matchList);

        if (!success)
            Debug.LogError("매치 리스트 실패 : " + extendedInfo);
        else
        {
            if (matchList.Count > 0)
            {
                Debug.Log("매치 찾기 완료");
                CreateMatchContents(matchList);
                matchlistPanel.transform.Find("NoMatchFound").gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("방이 없습니다.");
                matchlistPanel.transform.Find("NoMatchFound").gameObject.SetActive(true);
            }
        }
    }

    void CreateMatchContents(List<MatchInfoSnapshot> matchList)
    {
        foreach (MatchInfoSnapshot snapshot in matchList)
        {
            MatchInfoSnapshot tempSnapshot = snapshot;
            GameObject content = Instantiate(matchContent);
            content.transform.Find("Title").GetComponent<Text>().text = snapshot.name;
            content.transform.Find("Size").GetComponent<Text>().text = snapshot.currentSize + "/" + snapshot.maxSize;
            content.transform.Find("JoinButton").GetComponent<Button>().onClick
                .AddListener(() => { MMJoinMatch(tempSnapshot); });
            content.transform.SetParent(matchScrollGrid.transform, false);
        }
    }

    void MMJoinMatch(MatchInfoSnapshot firstMatch)
    {
        Debug.Log("매치 Join");
        matchMaker.JoinMatch(firstMatch.networkId, "", "", "", 0, 0, OnMatchJoined);
    }

    public override void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        base.OnMatchJoined(success, extendedInfo, matchInfo);

        if (!success)
            Debug.LogError("매치 접속 실패 : " + extendedInfo);
        else
        {
            // Success
            Debug.Log("매치 접속 성공 : " + matchInfo.networkId);
            GotoLobby(matchInfo);
        }
    }

    void MMCreateMatch(string lobbyName)
    {
        Debug.Log("매치 Create");
        matchMaker.CreateMatch(lobbyName, 4, true, "", "", "", 0, 0, OnMatchCreate);
    }

    public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        base.OnMatchCreate(success, extendedInfo, matchInfo);

        if (!success)
            Debug.LogError("매치 생성 실패 : " + extendedInfo);
        else
        {
            // Success
            Debug.Log("매치 생성 성공 : " + matchInfo.networkId);
            GotoLobby(matchInfo);
        }
    }

    void GotoLobby(MatchInfo matchInfo)
    {
        createLobbyPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    #endregion

    #region Lan

    public bool IsServer { get { return NetworkServer.active; } }
    bool flag { get { return client == null || client.connection == null || client.connection.connectionId == -1; } }

    #region Buttons

    public void SelectLanButton()
    {
        selectPanel.SetActive(false);
        lanPanel.SetActive(true);
    }

    public void LanBackButton()
    {
        lanPanel.SetActive(false);
        selectPanel.SetActive(true);
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
        GotoMainPanel();
    }

    public void GotoMainPanel()
    {
        clientPanel.SetActive(false);
        lanPanel.SetActive(true);
    }

    
    #endregion
    
    void GotoClientPanel()
    {
        lanPanel.SetActive(false);
        clientPanel.SetActive(true);
    }

    void GotoLobbyPanel()
    {
        clientPanel.SetActive(false);
        lanPanel.SetActive(false);
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

    #endregion

    #region Common
    
    IEnumerator Start () { 

        Network.Connect("127.0.0.1");
        float time = 0;

        while (Network.player.externalIP == "UNASSIGNED_SYSTEM_ADDRESS")
        {
            time += Time.deltaTime;

            if (time > 10)
            {
                Debug.LogError(" Unable to obtain external ip: Are you sure your connected to the internet");
            }

            yield return null;
        }

        internalIP = Network.player.ipAddress;
        externalIP = Network.player.externalIP;
        GameObject.Find("IP").GetComponent<Text>().text = externalIP;
        Network.Disconnect();
    }

    public void SelectBackButton()
    {
        mainPanel.SetActive(true);
        selectPanel.SetActive(false);
    }
    
    public void MainPlayButton()
    {
        mainPanel.SetActive(false);
        selectPanel.SetActive(true);
    }
    
    public void ExitButton()
    {
        Application.Quit();
    }
    
    public void UpdateLobbySlot()
    {
        Transform slots = lobbyPanel.transform.Find("CharacterSlots");
        for (var i = 0; i < lobbySlots.Length; i++)
        {
            slots.GetChild(i).Find("NotConnected").gameObject.SetActive(lobbySlots[i] == null);
            if (lobbySlots[i] == null)
                continue;
            MultiLobbyPlayer lobbyPlayer = lobbySlots[i].GetComponent<MultiLobbyPlayer>();
            Text readyText = slots.GetChild(i).Find("ReadyText").GetComponent<Text>();
            if (lobbyPlayer.readyToBegin)
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
                button.GetComponent<Button>().interactable = button != classButtons.GetChild((int) lobbyPlayer.PlayerClass);
            }
        }
    }

    public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<Player>().playerClass = lobbyPlayer.GetComponent<MultiLobbyPlayer>().PlayerClass;
        Debug.Log("OnLobbyServerSceneLoadedForPlayer");
        return base.OnLobbyServerSceneLoadedForPlayer(lobbyPlayer, gamePlayer);
    }

    #endregion


}
