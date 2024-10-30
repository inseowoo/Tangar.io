using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.LagCompensation;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms.Impl;

// This class controls the lifecycle of the player
public class PlayerController : NetworkBehaviour
{
    // Game Session AGNOSTIC Settings
    [SerializeField] private float _respawnDelay = 4.0f;
    [SerializeField] private float _playerDamageRadius = 2.5f;
    [SerializeField] private LayerMask _tanmakCollisionLayer;
    [SerializeField] private LayerMask _bulletCollisionLayer;
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

    private float _minScale = 5.0f;
    private float _maxScale = 20.0f;

    public float _scaleFactor = 2.0f;

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
            FindObjectOfType<PlayerSpawner>().RespawnPlayer(Object.InputAuthority);
        }

        // Checks if the player got hit by an bullet
        if (_isAlive && HasHitBullet())
        {
            PlayerWasHit();
        }

        // Checks if the player got hit by an tanmak
        if (_isAlive && HasHitTanmak())
        {
            UpdateSize();
        }
    }

    private LagCompensatedHit GetFirstHit(LayerMask mask)
    {
        _lagCompensatedHits.Clear();

        // Get Collisions From MaskLayer
        var count = Runner.LagCompensation.OverlapSphere(_rigidbody.position, _playerDamageRadius,
            Object.InputAuthority, _lagCompensatedHits,
            mask.value);

        if (count <= 0) return default;

        // Sort by Distance
        _lagCompensatedHits.SortDistance();

        return _lagCompensatedHits[0];
    }

    // Check tanmak collision using a lag compensated OverlapSphere
    private bool HasHitTanmak()
    {
        LagCompensatedHit firstHit = GetFirstHit(_tanmakCollisionLayer);
        if (firstHit.Equals(default(LagCompensatedHit))) return false;

        // Check Collider

        var tanmak = firstHit.GameObject.GetComponent<AsteroidBehaviour>();
        if (tanmak)
        {
            if (tanmak.IsAlive == false) return false;

            tanmak.HitAsteroid(Object.InputAuthority);

            // For Debug
            DisplayCollisionDebug(firstHit.GameObject);

            return true;
        }

        return false;
    }

    private bool HasHitBullet()
    {
        LagCompensatedHit firstHit = GetFirstHit(_bulletCollisionLayer);
        if (firstHit.Equals(default(LagCompensatedHit))) return false;

        // Check Collider
        var bullet = firstHit.GameObject.GetComponent<BulletBehaviour>();
        if (bullet)
        {
            if (bullet._authority == Object.InputAuthority) return false;

            bullet.Hit();

            // For Debug
            DisplayCollisionDebug(firstHit.GameObject);

            return true;
        }

        return false;
    }

    private void DisplayCollisionDebug(GameObject colObj)
    {
        string txt = $"Collide {this.name} with {colObj.name}";
        UnityEngine.Debug.Log(txt);
        debug_text.PushDebugText(txt);
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

        _playerDataNetworked.ResetScore();
    }

    private void UpdateSize()
    {
        float scale = Mathf.Clamp(_minScale + _playerDataNetworked.Score * _scaleFactor, _minScale, _maxScale);
        int score = _playerDataNetworked.Score;
        Debug.Log(score);
        _rigidbody.transform.localScale = new Vector3(scale, scale, 1);
    }
}
