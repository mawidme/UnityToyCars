using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using TMPro;

using Photon.Realtime;
using Photon.Pun;

// public class MpLauncher : MonoBehaviour
public class MpLauncher : MonoBehaviourPunCallbacks
{
    string gameVersion = "1";
    
    /// <summary>
    /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
    /// </summary>
    [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
    [SerializeField]
    private byte maxPlayersPerRoom = 6;

    // Store the PlayerPref Key to avoid typos
    const string playerNamePrefKey = "PlayerName";
    
    TMP_InputField _inputField;
    Image _mpStartButtonImage;

    Control _gameControl;

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    void Awake()
    {
        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        // PhotonNetwork.AutomaticallySyncScene = true;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        StartUi();

        _gameControl = GameObject.Find("Ground").GetComponent<Control>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Start the connection process.
    /// - If already connected, we attempt joining a random room
    /// - if not yet connected, Connect this application instance to Photon Cloud Network
    /// </summary>
    public void Connect()
    {
        // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("PUN disconnect");

            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("PUN connect");

            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    public void JoinRoom()
    {
        Debug.Log("JoinRoom");
        PhotonNetwork.JoinOrCreateRoom("TheOnlyRoom", new RoomOptions { MaxPlayers = maxPlayersPerRoom  }, null);
    }
    
    // UI elements
    private void StartUi()
    {
        string defaultName = "Player" + Random.Range(1, 1000);
        _inputField = GameObject.Find("PlayerNameInput").GetComponent<TMP_InputField>();
        if (_inputField != null)
        {
            if (PlayerPrefs.HasKey(playerNamePrefKey))
            {
                defaultName = PlayerPrefs.GetString(playerNamePrefKey);
            }
            _inputField.text = defaultName;

            _inputField.onValueChanged.AddListener(delegate { SetPlayerName(); });
        }

        PhotonNetwork.NickName =  defaultName;

        _mpStartButtonImage = GameObject.Find("MpStartButton").GetComponent<Image>();
    }

    // public void SetPlayerName(string value)
    public void SetPlayerName()
    {
        var value = _inputField.text;
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("Player Name is null or empty");
            return;
        }

        Debug.Log($"Set MP player name: {PhotonNetwork.NickName} -> {value}");
        PhotonNetwork.NickName = value;

        PlayerPrefs.SetString(playerNamePrefKey, value);
    }

#region EventHandlers
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster");
        
        JoinRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");

        // default: 30
        Debug.Log($"PhotonNetwork.sendRate: {PhotonNetwork.SendRate}");

        // default: 10
        Debug.Log($"PhotonNetwork.sendRateOnSerialize: {PhotonNetwork.SerializationRate}");

        _mpStartButtonImage.color = Color.green;

        _gameControl.ScheduleStartMp();
    }
    

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("OnJoinRoomFailed");

        _mpStartButtonImage.color = Color.red;
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        var str = $"OnDisconnected, reason {cause}";
        if (cause == DisconnectCause.DisconnectByClientLogic) {
            Debug.Log(str);
        } else {
            Debug.LogWarningFormat(str);
        }

        _mpStartButtonImage.color = cause == DisconnectCause.DisconnectByClientLogic ? Color.white : Color.red;
    }

    public override void OnPlayerEnteredRoom(Player remotePlayer)
    {
        Debug.Log("OnPlayerEnteredRoom");
        _gameControl.TriggerCarResync();
    }


    public override void OnPlayerLeftRoom(Player remotePlayer)
    {
        Debug.Log($"OnPlayerLeftRoom: {remotePlayer.ActorNumber}");
        _gameControl.HandleMpPlayerLeft(remotePlayer.ActorNumber);
    }

    public override void OnPlayerPropertiesUpdate(Player remotePlayer, ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("OnPlayerPropertiesUpdate");
        _gameControl.HandleMpPlayerPropertiesUpdate(remotePlayer, propertiesThatChanged);
        base.OnPlayerPropertiesUpdate(remotePlayer, propertiesThatChanged);
    }
#endregion
}
