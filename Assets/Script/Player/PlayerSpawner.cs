using Fusion;
using System;
using UnityEngine;


    // The PlayerSpawner, just like the AsteroidSpawner, only executes on the Host.
    // Therefore none of its parameters need to be [Networked].
public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    // References to the NetworkObject prefab to be used for the players' players.
    [SerializeField] private NetworkPrefabRef _playerNetworkPrefab = NetworkPrefabRef.Empty;

    private bool _gameIsReady = false;
    private GameStateController _gameStateController = null;

    private SpawnPoint[] _spawnPoints = null;

    public override void Spawned()
    {
        if (Object.HasStateAuthority == false) return;
        // Collect all spawn points in the scene.
        _spawnPoints = FindObjectsOfType<SpawnPoint>();
    }

    // The spawner is started when the GameStateController switches to GameState.Running.
    public void StartPlayerSpawner(GameStateController gameStateController)
    {
        _gameIsReady = true;
        _gameStateController = gameStateController;
        foreach (var player in Runner.ActivePlayers)
        {
            SpawnPlayer(player);
        }
    }

    // Spawns a new player if a client joined after the game already started
    public void PlayerJoined(PlayerRef player)
    {
        if (_gameIsReady == false) return;
        SpawnPlayer(player);
    }

    // Spawns a player for a player.
    // The spawn point is chosen in the _spawnPoints array using the implicit playerRef to int conversion 
    private void SpawnPlayer(PlayerRef player)
    {
        // Modulo is used in case there are more players than spawn points.
        int index = UnityEngine.Random.Range(0, _spawnPoints.Length);
        var spawnPosition = _spawnPoints[index].transform.position;

        var playerObject = Runner.Spawn(_playerNetworkPrefab, spawnPosition, Quaternion.identity, player);
        // Set Player Object to facilitate access across systems.
        Runner.SetPlayerObject(player, playerObject);

        // Add the new player to the players to be tracked for the game end check.
        _gameStateController.TrackNewPlayer(playerObject.GetComponent<PlayerDataNetworked>().Id);
    }

    // Despawns the player associated with a player when their client leaves the game session.
    public void PlayerLeft(PlayerRef player)
    {
        DespawnPlayer(player);
    }

    private void DespawnPlayer(PlayerRef player)
    {
        if (Runner.TryGetPlayerObject(player, out var playerNetworkObject))
        {
            Runner.Despawn(playerNetworkObject);
        }

        // Reset Player Object
        Runner.SetPlayerObject(player, null);
    }

    public void RespawnPlayer(PlayerRef player)
    {
        if (Runner.TryGetPlayerObject(player, out var playerObject) == false) return;

        int index = UnityEngine.Random.Range(0, _spawnPoints.Length);
        var spawnPosition = _spawnPoints[index].transform.position;

        playerObject.transform.position = spawnPosition;
    }
}
