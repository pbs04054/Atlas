using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class Lobby : NetworkLobbyManager
{

    //빠른 프로토타이핑을 위해 Find함수를 썼음


    #region UI

    [SerializeField] GameObject mainPanel, matchlistPanel, createLobbyPanel, lobbyPanel, matchScrollGrid, matchContent;

    #endregion

    #region Button

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

    public void ExitButton()
    {
        Application.Quit();
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

    #endregion

    void Start()
    {
        if(!isLocal)
            MMStart();
    }
    
    #region MM

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


    public void UpdateLobbySlot()
    {
        Transform slots = lobbyPanel.transform.Find("CharacterSlots");
        for (var i = 0; i < lobbySlots.Length; i++)
        {            
            slots.GetChild(i).Find("NotConnected").gameObject.SetActive(lobbySlots[i] == null);
            if (lobbySlots[i] == null)
                continue;
            LobbyPlayer lobbyPlayer = lobbySlots[i].GetComponent<LobbyPlayer>();
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
                button.GetComponent<Button>().interactable = button != classButtons.GetChild((int)lobbyPlayer.PlayerClass);
            }
        }
    }

    public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<Player>().playerClass = lobbyPlayer.GetComponent<LobbyPlayer>().PlayerClass;
        Debug.Log("OnLobbyServerSceneLoadedForPlayer");
        return base.OnLobbyServerSceneLoadedForPlayer(lobbyPlayer, gamePlayer);
    }
    
    #endregion
    
    #region Local
    
    public bool isLocal = true;
    
    NetworkClient myClient;
    
    void Update()
    {
        if (isLocal)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SetupServer();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                SetupClient();
            }
            
            if (Input.GetKeyDown(KeyCode.B))
            {
                SetupServer();
                SetupLocalClient();
            }
        }
    }
    
    void SetupServer()
    {
        NetworkServer.Listen(4444);
        isLocal = false;
    }
    
    void SetupClient()
    {
        myClient = new NetworkClient();
        myClient.RegisterHandler(MsgType.Connect, GotoLobby);     
        myClient.Connect("127.0.0.1", 4444);
        isLocal = false;
    }
    
    void SetupLocalClient()
    {
        myClient = ClientScene.ConnectLocalServer();
        myClient.RegisterHandler(MsgType.Connect, GotoLobby);     
        isLocal = false;
    }

    void GotoLobby(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }
    
    #endregion
    
}
