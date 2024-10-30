using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using NetworkTransform = Fusion.NetworkTransform;


// The class is dedicated to controlling the Player's Firing
public class PlayerFireController : NetworkBehaviour
{
    // Game Session AGNOSTIC Settings
    [SerializeField] private float _delayBetweenShots = 0.2f;
    [SerializeField] private NetworkPrefabRef _bullet = NetworkPrefabRef.Empty;

    // Local Runtime references
    private Rigidbody2D _rigidbody = null;
    private PlayerController _playerController = null;

    // Game Session SPECIFIC Settings
    [Networked] private NetworkButtons _buttonsPrevious { get; set; }
    [Networked] private TickTimer _shootCooldown { get; set; }

    private Vector2 _lastFacingDirection = Vector2.up;
    public override void Spawned()
    {
        // --- Host & Client
        // Set the local runtime references.
        _rigidbody = GetComponent<Rigidbody2D>();
        _playerController = GetComponent<PlayerController>();
    }

    public override void FixedUpdateNetwork()
    {
        // Bail out of FUN() if this player does not currently accept input
        if (_playerController.AcceptInput == false) return;

        // Bail out of FUN() if this Client does not have InputAuthority over this object or
        // if no PlayerInput struct is available for this tick
        if (GetInput<PlayerInput>(out var input) == false) return;

        Fire(input);
    }

    // Checks the Buttons in the input struct against their previous state to check
    // if the fire button was just pressed.
    private void Fire(PlayerInput input)
    {
        if (input.Buttons.WasPressed(_buttonsPrevious, PlayerButtons.Fire))
        {
            SpawnBullet();
        }
        Vector2 newDirection = new Vector2(input.HorizontalInput, input.VerticalInput);
        if (newDirection != Vector2.zero)
        {
            _lastFacingDirection = newDirection.normalized;
        }

        _buttonsPrevious = input.Buttons;
    }

    // Spawns a bullet which will be travelling in the direction the player is facing
    private void SpawnBullet()
    {
        if (_shootCooldown.ExpiredOrNotRunning(Runner) == false || !Runner.CanSpawn) return;

        // Determine the bullet direction based on player¡¯s current movement direction
        Vector2 bulletDirection = _lastFacingDirection;
        Debug.Log(bulletDirection);

        // Spawn the bullet at the player¡¯s position with no rotation
        BulletBehaviour bullet = Runner.Spawn(_bullet, _rigidbody.position, Quaternion.identity, Object.InputAuthority).GetComponent<BulletBehaviour>();

        // Set the bullet¡¯s direction
        bullet.SetDirection(bulletDirection);

        // Set bullet's authority
        bullet.SetAuthority(Object.InputAuthority);

        _shootCooldown = TickTimer.CreateFromSeconds(Runner, _delayBetweenShots);
    }



}
