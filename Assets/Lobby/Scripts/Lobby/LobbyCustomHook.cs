﻿using UnityEngine;
using System.Collections;
using Prototype.NetworkLobby;
using UnityEngine.Networking;

public class LobbyCustomHook : LobbyHook
{
    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<SpawnPlayer>().ShipId = lobbyPlayer.GetComponent<LobbyPlayer>().Ship;
        gamePlayer.GetComponent<SpawnPlayer>().Pseudo = lobbyPlayer.GetComponent<LobbyPlayer>().Pseudo;
        gamePlayer.GetComponent<SpawnPlayer>().BotLevel = lobbyPlayer.GetComponent<LobbyPlayer>().BotLevel;
    }
}
