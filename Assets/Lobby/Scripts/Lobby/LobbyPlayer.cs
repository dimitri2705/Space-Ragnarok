using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Prototype.NetworkLobby
{
    //Player entry in the lobby. Handle selecting color/setting name & getting ready for the game
    //Any LobbyHook can then grab it and pass those value to the game player prefab (see the Pong Example in the Samples Scenes)
    public class LobbyPlayer : NetworkLobbyPlayer
    {
        public static List<string> playersPseudo = new List<string>();

        public Button shipButton;
        public InputField nameInput;
        public Button readyButton;
        public Button waitingPlayerButton;
        public Button removePlayerButton;
        public GameObject shipChoicePrefab;

        public GameObject localIcone;
        public GameObject remoteIcone;

        //OnMyName function will be invoked on clients when server change the value of playerName
        [SyncVar(hook = "OnMyName")]
        public string Pseudo = "";
        [SyncVar(hook = "OnMyShip")]
        public int Ship = 0;
        [SyncVar(hook = "OnIsBot")]
        public bool IsBot;
        [SyncVar(hook = "OnBotLevel")]
        public int BotLevel = -1;

        public Color OddRowColor = new Color(250.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f, 1.0f);
        public Color EvenRowColor = new Color(180.0f / 255.0f, 180.0f / 255.0f, 180.0f / 255.0f, 1.0f);

        static Color JoinColor = Color.white;
        static Color NotReadyColor = Color.white;
        static Color ReadyColor = Color.white;
        static Color TransparentColor = new Color(0, 0, 0, 0);


        public override void OnClientEnterLobby()
        {
            base.OnClientEnterLobby();

            if (LobbyManager.s_Singleton != null) LobbyManager.s_Singleton.OnPlayersNumberModified(1);

            LobbyPlayerList._instance.AddPlayer(this);
            LobbyPlayerList._instance.DisplayDirectServerWarning(isServer && LobbyManager.s_Singleton.matchMaker == null);


            if (isLocalPlayer)
            {
                SetupLocalPlayer();
            }
            else
            {
                SetupOtherPlayer();
            }

            //setup the player data on UI. The value are SyncVar so the player
            //will be created with the right value currently on server
            OnMyName(Pseudo);
            OnMyShip(Ship);
            OnIsBot(IsBot);
            OnBotLevel(BotLevel);
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            //if we return from a game, color of text can still be the one for "Ready"
            readyButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;

            SetupLocalPlayer();
        }

        void ChangeReadyButtonColor(Color c)
        {
            ColorBlock b = readyButton.colors;
            b.normalColor = c;
            b.pressedColor = c;
            b.highlightedColor = c;
            b.disabledColor = c;
            readyButton.colors = b;
        }

        void SetupOtherPlayer()
        {
            nameInput.interactable = false;
            shipButton.interactable = false;

            removePlayerButton.interactable = NetworkServer.active;
            if (!removePlayerButton.interactable)
            {
                removePlayerButton.gameObject.transform.Find("Label").GetComponent<Text>().color = Color.gray;
            }
            else
            {
                removePlayerButton.gameObject.transform.Find("Label").GetComponent<Text>().color = Color.white;
            }

            ChangeReadyButtonColor(NotReadyColor);

            readyButton.transform.GetChild(0).GetComponent<Text>().text = "...";
            readyButton.interactable = false;

            OnClientReady(false);
        }

        void SetupLocalPlayer()
        {
            nameInput.interactable = true;
            //remoteIcone.gameObject.SetActive(false);
            //localIcone.gameObject.SetActive(true);

            CheckRemoveButton();

            CmdShipChange(UserData.GetShipId());

            ChangeReadyButtonColor(JoinColor);

            readyButton.transform.GetChild(0).GetComponent<Text>().text = "JOIN";
            readyButton.interactable = true;

            //have to use child count of player prefab already setup as "this.slot" is not set yet
            if (Pseudo == "")
            {
                if (playerControllerId > 0)
                {
                    IsBot = true;
                    CmdNameChanged("Bot " + PlayerPrefs.GetInt("BotCount"));
                    PlayerPrefs.SetInt("BotCount", PlayerPrefs.GetInt("BotCount") + 1);
                    Ship = (int)Random.Range(0, 4);
                    BotLevel = 0;
                }
                else
                {
                    if (NetworkServer.active)
                    {
                        removePlayerButton.interactable = false;
                        if (!removePlayerButton.interactable)
                        {
                            removePlayerButton.gameObject.transform.Find("Label").GetComponent<Text>().color = Color.gray;
                        }
                        else
                        {
                            removePlayerButton.gameObject.transform.Find("Label").GetComponent<Text>().color = Color.white;
                        }
                    }
                    CmdNameChanged(Social.localUser.userName);
                }
            }
            //we switch from simple name display to name input
            shipButton.interactable = true;
            nameInput.interactable = true;

            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(OnNameChanged);

            shipButton.onClick.RemoveAllListeners();
            shipButton.onClick.AddListener(OnShipClicked);

            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyClicked);

            //when OnClientEnterLobby is called, the loval PlayerController is not yet created, so we need to redo that here to disable
            //the add button if we reach maxLocalPlayer. We pass 0, as it was already counted on OnClientEnterLobby
            if (LobbyManager.s_Singleton != null) LobbyManager.s_Singleton.OnPlayersNumberModified(0);
        }

        //This enable/disable the remove button depending on if that is the only local player or not
        public void CheckRemoveButton()
        {
            if (!isLocalPlayer)
                return;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            removePlayerButton.interactable = IsBot;
            if (!removePlayerButton.interactable)
            {
                removePlayerButton.gameObject.transform.Find("Label").GetComponent<Text>().color = Color.gray;
            }
            else
            {
                removePlayerButton.gameObject.transform.Find("Label").GetComponent<Text>().color = Color.white;
            }

        }

        public override void OnClientReady(bool readyState)
        {
            if (IsBot)
            {
                return;
            }
            if (readyState)
            {
                ChangeReadyButtonColor(TransparentColor);

                Text textComponent = readyButton.transform.GetChild(0).GetComponent<Text>();
                textComponent.text = "READY";
                textComponent.color = ReadyColor;
                readyButton.interactable = false;
                shipButton.interactable = false;
                nameInput.interactable = false;
            }
            else
            {
                ChangeReadyButtonColor(isLocalPlayer ? JoinColor : NotReadyColor);

                Text textComponent = readyButton.transform.GetChild(0).GetComponent<Text>();
                textComponent.text = isLocalPlayer ? "JOIN" : "...";
                textComponent.color = Color.white;
                readyButton.interactable = isLocalPlayer;
                shipButton.interactable = isLocalPlayer;
                nameInput.interactable = isLocalPlayer;
            }
        }

        public void OnPlayerListChanged(int idx)
        {
            GetComponent<Image>().color = (idx % 2 == 0) ? EvenRowColor : OddRowColor;
        }

        ///===== callback from sync var

        public void OnMyName(string newName)
        {
            Pseudo = newName;
            nameInput.text = Pseudo;
        }

        public void OnMyShip(int newShip)
        {
            Ship = newShip;
            shipButton.GetComponent<Image>().sprite = ShipProperties.GetShip(Ship).ShipSprite;
        }

        public void OnIsBot(bool isBot)
        {
            IsBot = isBot;
        }

        public void OnBotLevel(int botLevel)
        {
            BotLevel = botLevel;
            if (IsBot)
            {
                readyButton.GetComponent<Image>().color = GetColor(botLevel);
                readyButton.transform.GetChild(0).GetComponent<Text>().color = GetColor(botLevel);
                readyButton.transform.GetChild(0).GetComponent<Text>().text = GetText(botLevel);
            }
        }

        //===== UI Handler

        //Note that those handler use Command function, as we need to change the value on the server not locally
        //so that all client get the new value throught syncvar
        public void OnShipClicked()
        {
            var shipChoice = GameObject.Find("ShipChoice");
            shipChoice.transform.GetChild(0).gameObject.SetActive(true);
            shipChoice.transform.GetChild(1).gameObject.SetActive(true);
            shipChoice.transform.GetChild(2).gameObject.SetActive(true);
            shipChoice.transform.GetChild(3).gameObject.SetActive(true);

            for (int i = 0; i < shipChoice.transform.GetChild(3).childCount; i++)
            {
                Destroy(shipChoice.transform.GetChild(3).GetChild(i).gameObject);
            }

            for (int i = 0; i < Constants.ShipsCount; i++)
            {
                if (ShipProperties.GetShip(i).ShipName != "Error" && ShipProperties.GetClass(i) < Constants.ClassCount)
                {
                    if (UserData.HasBoughtShip(i))
                    {
                        var s = Instantiate(shipChoicePrefab) as GameObject;
                        s.transform.SetParent(shipChoice.transform.GetChild(3));
                        s.transform.localScale = Vector3.one;
                        s.transform.localRotation = Quaternion.identity;
                        s.transform.localPosition = Vector3.zero;
                        s.transform.SetAsLastSibling();
                        s.GetComponentInChildren<Text>().text = ShipProperties.GetShip(i).ShipName;
                        s.name = ShipProperties.GetShip(i).ShipName;
                        int x = i;
                        s.GetComponent<Button>().onClick.AddListener(() => OnShipId(x));
                        s.GetComponent<Button>().interactable = true;
                        s.transform.Find("Image").GetComponent<Image>().sprite = ShipProperties.GetShip(i).ShipSprite;
                        if (i == 3 || i == 7 || i == 11)
                        {
                            s = Instantiate(shipChoicePrefab) as GameObject;
                            s.transform.SetParent(shipChoice.transform.GetChild(3));
                            s.transform.localScale = Vector3.one;
                            s.transform.localRotation = Quaternion.identity;
                            s.transform.localPosition = Vector3.zero;
                            s.transform.Find("Image").GetComponent<Image>().enabled = false;
                            s.GetComponentInChildren<Text>().text = "";
                            s.transform.SetAsLastSibling();
                        }
                        if (i == 52)
                        {
                            s.transform.SetSiblingIndex(19);
                        }
                    }
                }
            }
        }

        public void OnShipId(int id)
        {
            UserData.SetShipId(id);
            var shipChoice = GameObject.Find("ShipChoice");
            shipChoice.transform.GetChild(0).gameObject.SetActive(false);
            shipChoice.transform.GetChild(1).gameObject.SetActive(false);
            shipChoice.transform.GetChild(2).gameObject.SetActive(false);
            shipChoice.transform.GetChild(3).gameObject.SetActive(false);
            Ship = id;
        }


        public void OnReadyClicked()
        {
            if (!IsBot)
            {
                SendReadyToBeginMessage();
                if (isServer)
                {
                    foreach (var lobbyPlayer in FindObjectsOfType<LobbyPlayer>())
                    {
                        if (lobbyPlayer.IsBot)
                        {
                            lobbyPlayer.SendReadyToBeginMessage();
                        }
                    }
                }
            }
            else
            {
                BotLevel++;
                if (BotLevel > Constants.TotalBotLevel)
                {
                    BotLevel = 0;
                }
            }
        }

        public void OnNameChanged(string str)
        {
            CmdNameChanged(str);
        }

        public void OnRemovePlayerClick()
        {
            if (isLocalPlayer)
            {
                RemovePlayer();
            }
            else if (isServer)
            {
                LobbyManager.s_Singleton.KickPlayer(connectionToClient);
            }
        }

        public void ToggleJoinButton(bool enabled)
        {
            readyButton.gameObject.SetActive(enabled);
            waitingPlayerButton.gameObject.SetActive(!enabled);
        }

        [ClientRpc]
        public void RpcUpdateCountdown(int countdown)
        {
            LobbyManager.s_Singleton.countdownPanel.UIText.text = "Match Starting in " + countdown;
            LobbyManager.s_Singleton.countdownPanel.gameObject.SetActive(countdown != 0);
        }

        [ClientRpc]
        public void RpcUpdateRemoveButton()
        {
            CheckRemoveButton();
        }

        //====== Server Command

        [Command]
        public void CmdShipChange(int newShip)
        {
            Ship = newShip;
        }

        [Command]
        public void CmdNameChanged(string name)
        {
            if (name == Pseudo)
            {
                return;
            }
            if (!playersPseudo.Contains(name))
            {
                playersPseudo.Add(name);
                playersPseudo.Remove(Pseudo);
                Pseudo = name;
            }
            else
            {
                playersPseudo.Remove(Pseudo);
                CmdNameChanged(name + "_");
            }
        }

        //Cleanup thing when get destroy (which happen when client kick or disconnect)
        public void OnDestroy()
        {
            LobbyPlayerList._instance.RemovePlayer(this);
            if (LobbyManager.s_Singleton != null)
            {
                LobbyManager.s_Singleton.OnPlayersNumberModified(-1);
            }
        }

        Color GetColor(int botLevel)
        {
            return Color.Lerp(Color.green, Color.black, (float)botLevel / Constants.TotalBotLevel);
        }

        string GetText(int botLevel)
        {
            return ShipProperties.GetBotProperties(botLevel).LevelName;
        }
    }
}
