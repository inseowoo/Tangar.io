using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.LagCompensation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

    // This class controls the lifecycle of the spaceship
public class SpaceshipController : NetworkBehaviour
{
    // Game Session AGNOSTIC Settings
    [SerializeField] private float _respawnDelay = 4.0f;
    [SerializeField] private float _spaceshipDamageRadius = 2.5f;
    [SerializeField] private LayerMask _asteroidCollisionLayer;

    // Local Runtime references
    private ChangeDetector _changeDetector;
    private Rigidbody2D _rigidbody = null;
    private PlayerDataNetworked _playerDataNetworked = null;
    //private SpaceshipVisualController _visualController = null;

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
        //_visualController = GetComponent<SpaceshipVisualController>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        //_visualController.SetColorFromPlayerID(Object.InputAuthority.PlayerId);

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
                    //ToggleVisuals(previous, current);
                    break;
            }
        }
    }

    //private void ToggleVisuals(bool wasAlive, bool isAlive)
    //{
    //    // Check if the spaceship was just brought to life
    //    if (wasAlive == false && isAlive == true)
    //    {
    //        _visualController.TriggerSpawn();
    //    }
    //    // or whether it just got destroyed.
    //    else if (wasAlive == true && isAlive == false)
    //    {
    //        _visualController.TriggerDestruction();
    //    }
    //}

    public override void FixedUpdateNetwork()
    {
        // Checks if the spaceship is ready to be respawned.
        if (_respawnTimer.Expired(Runner))
        {
            _isAlive = true;
            _respawnTimer = default;
        }

        // Checks if the spaceship got hit by an asteroid
        if (_isAlive && HasHitAsteroid())
        {
            ShipWasHit();
        }
    }

    // Check asteroid collision using a lag compensated OverlapSphere
    private bool HasHitAsteroid()
    {
        _lagCompensatedHits.Clear();

        // Get Collision From MaskLayer
        var count = Runner.LagCompensation.OverlapSphere(_rigidbody.position, _spaceshipDamageRadius,
            Object.InputAuthority, _lagCompensatedHits,
            _asteroidCollisionLayer.value);

        if (count <= 0) return false;

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

    // Toggle the _isAlive boolean if the spaceship was hit and check whether the player has any lives left.
    // If they do, then the _respawnTimer is activated.
    private void ShipWasHit()
    {
        _isAlive = false;

        ResetShip();

        // ---- Host Only
        if (Object.HasStateAuthority == false) return;

        if (_playerDataNetworked.Lives > 1)
        {
            _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
        }
        else
        {
            _respawnTimer = default;
        }

        _playerDataNetworked.SubtractLife();

        //FindObjectOfType<GameStateController>().CheckIfGameHasEnded();
    }

    // Resets the spaceships movement velocity
    private void ResetShip()
    {
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
    }
}
