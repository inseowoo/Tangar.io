using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.LagCompensation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

    // This class controls the lifecycle of the player
public class PlayerController : NetworkBehaviour
{
    // Game Session AGNOSTIC Settings
    [SerializeField] private float _respawnDelay = 4.0f;
    [SerializeField] private float _playerDamageRadius = 2.5f;
    [SerializeField] private LayerMask _tanmakCollisionLayer;
    [SerializeField] private LayerMask _playerCollisionLayer;
    [SerializeField] private LayerMask _itemCollisionLayer;

    // Local Runtime references
    private ChangeDetector _changeDetector;
    private Rigidbody2D _rigidbody = null;
    private PlayerDataNetworked _playerDataNetworked = null;
    private PlayerVisualController _visualController = null;

    private List<LagCompensatedHit> _lagCompensatedHits = new List<LagCompensatedHit>();

    // Game Session SPECIFIC Settings
    public bool AcceptInput => _isAlive && Object.IsValid;

    [Networked] private NetworkBool _isAlive { get; set; }

    [Networked] private TickTimer _respawnTimer { get; set; }

    DebugText debug_text;

    public override void Spawned()
    {
        // --- Host & Client
        // Set the local runtime references.
        _rigidbody = GetComponent<Rigidbody2D>();
        _playerDataNetworked = GetComponent<PlayerDataNetworked>();
        _visualController = GetComponent<PlayerVisualController>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        _visualController.SetColorFromPlayerID(Object.InputAuthority.PlayerId);

        debug_text = GameObject.FindObjectOfType<DebugText>();

        // --- Host
        // The Game Session SPECIFIC settings are initialized
        if (Object.HasStateAuthority == false) return;
        _isAlive = true;

        
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            switch (change)
            {
                case nameof(_isAlive):
                    var reader = GetPropertyReader<NetworkBool>(nameof(_isAlive));
                    var (previous, current) = reader.Read(previousBuffer, currentBuffer);
                    ToggleVisuals(previous, current);
                    break;
            }
        }
    }

    private void ToggleVisuals(bool wasAlive, bool isAlive)
    {
        // Check if the player was just brought to life
        if (wasAlive == false && isAlive == true)
        {
            _visualController.TriggerSpawn();
        }
        // or whether it just got destroyed.
        else if (wasAlive == true && isAlive == false)
        {
            _visualController.TriggerDestruction();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Checks if the player is ready to be respawned.
        if (_respawnTimer.Expired(Runner))
        {
            _isAlive = true;
            _respawnTimer = default;
            FindObjectOfType<PlayerSpawner>().RespawnPlayer(gameObject);
        }

        // Checks if the player got hit by an tanmak
        if (_isAlive && HasHitTanmak())
        {
            PlayerWasHit();
        }
    }

    // Check tanmak collision using a lag compensated OverlapSphere
    private bool HasHitTanmak()
    {
        _lagCompensatedHits.Clear();

        // Get Collisions From MaskLayer
        var count = Runner.LagCompensation.OverlapSphere(_rigidbody.position, _playerDamageRadius,
            Object.InputAuthority, _lagCompensatedHits,
            _tanmakCollisionLayer.value);

        if (count <= 0) return false;

        // Sort by Distance
        _lagCompensatedHits.SortDistance();

        // For Debug
        string txt = $"Collide {this.name} with {_lagCompensatedHits[0].GameObject.name}";
        UnityEngine.Debug.Log(txt);
        debug_text.PushDebugText(txt);

        // Check Collider
        var dummyTanmak = _lagCompensatedHits[0].GameObject.GetComponent<DummyTanmak>();
        if (dummyTanmak)
        {
            if (dummyTanmak._isAlive == false) return false;

            dummyTanmak.Hit();

            return true;
        }

        return false;
    }

    // Toggle the _isAlive boolean if the player was hit and check whether the player has any lives left.
    // If they do, then the _respawnTimer is activated.
    private void PlayerWasHit()
    {
        _isAlive = false;

        ResetPlayer();

        // ---- Host Only
        if (Object.HasStateAuthority == false) return;

        _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);

        _playerDataNetworked.ResetScore();

        //if (_playerDataNetworked.Lives > 1)
        //{
        //    _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
        //}
        //else
        //{
        //    _respawnTimer = default;
        //}

        // _playerDataNetworked.SubtractLife();

        //FindObjectOfType<GameStateController>().CheckIfGameHasEnded();
    }

    // Resets the players movement velocity
    private void ResetPlayer()
    {
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
    }
}
