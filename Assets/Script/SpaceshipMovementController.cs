using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

// The class is dedicated to controlling the Spaceship's movement in 2D
public class SpaceshipMovementController : NetworkBehaviour
{
    // Game Session AGNOSTIC Settings
    [SerializeField] private float _rotationSpeed = 90.0f;
    [SerializeField] private float _movementSpeed = 2000.0f;
    [SerializeField] private float _maxSpeed = 200.0f;

    // Local Runtime references
    private Rigidbody2D _rigidbody = null;  // The Unity Rigidbody2D (RB) is automatically synchronized across the network thanks to the NetworkRigidbody2D (NRB) component.

    private SpaceshipController _spaceshipController = null;

    // Game Session SPECIFIC Settings
    [Networked] private float _screenBoundaryX { get; set; }
    [Networked] private float _screenBoundaryY { get; set; }

    public override void Spawned()
    {
        // --- Host & Client
        // Set the local runtime references.
        _rigidbody = GetComponent<Rigidbody2D>();
        _spaceshipController = GetComponent<SpaceshipController>();

        // --- Host
        // The Game Session SPECIFIC settings are initialized
        if (Object.HasStateAuthority == false) return;

        _screenBoundaryX = Camera.main.orthographicSize * Camera.main.aspect;
        _screenBoundaryY = Camera.main.orthographicSize;
    }

    public override void FixedUpdateNetwork()
    {
        // Bail out of FUN() if this spaceship does not currently accept input
        if (_spaceshipController.AcceptInput == false) return;

        // GetInput() can only be called from NetworkBehaviours.
        // In SimulationBehaviours, either TryGetInputForPlayer<T>() or GetInputForPlayer<T>() has to be called.
        // This will only return true on the Client with InputAuthority for this Object and the Host.
        if (Runner.TryGetInputForPlayer<PlayerInput>(Object.InputAuthority, out var input))
        {
            Move(input);
        }

        CheckExitScreen();
    }

    // Moves the spaceship RB using the input for the client with InputAuthority over the object
    private void Move(PlayerInput input)
    {
        // Adjust rotation on the 2D plane (rotation around the Z-axis)

        // Calculate translation based on forward direction and input
        Vector2 direction =  (transform.up * input.VerticalInput + transform.right * input.HorizontalInput);

        // Apply direct translation without sliding
        _rigidbody.velocity = direction.normalized * _movementSpeed * Runner.DeltaTime;

        // Clamp the velocity to the maximum speed, if necessary
        if (_rigidbody.velocity.magnitude > _maxSpeed)
        {
            _rigidbody.velocity = _rigidbody.velocity.normalized * _maxSpeed;
        }
    }


    // Moves the ship to the opposite side of the screen if it exits the screen boundaries.
    private void CheckExitScreen()
    {
        var position = _rigidbody.position;

        if (Mathf.Abs(position.x) < _screenBoundaryX && Mathf.Abs(position.y) < _screenBoundaryY) return;

        // Wrap around on the X and Y axes if the spaceship goes beyond screen boundaries
        if (Mathf.Abs(position.x) > _screenBoundaryX)
        {
            position.x = -Mathf.Sign(position.x) * _screenBoundaryX;
        }

        if (Mathf.Abs(position.y) > _screenBoundaryY)
        {
            position.y = -Mathf.Sign(position.y) * _screenBoundaryY;
        }

        // Offset slightly to avoid looping back and forth between the edges
        position -= position.normalized * 0.1f;
        _rigidbody.position = position;
    }
}
