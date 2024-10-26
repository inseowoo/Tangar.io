using Fusion;
using UnityEngine;

public class BulletBehaviour : NetworkBehaviour
{
    // Settings
    [SerializeField] private float _maxLifetime = 3.0f;
    [SerializeField] private float _speed = 200.0f;
    [SerializeField] private LayerMask _asteroidLayer;

    // The countdown for a bullet's lifetime.
    [Networked] private TickTimer _currentLifetime { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority == false) return;

        // Initialize lifetime on host
        _currentLifetime = TickTimer.CreateFromSeconds(Runner, _maxLifetime);
    }

    public override void FixedUpdateNetwork()
    {
        // Move the bullet in the direction it was spawned (player's facing direction)
        transform.Translate(transform.up * _speed * Runner.DeltaTime, Space.World);

        CheckLifetime();
    }

    // If the bullet exceeds its lifetime, it gets destroyed
    private void CheckLifetime()
    {
        if (_currentLifetime.Expired(Runner) == false) return;

        Runner.Despawn(Object);
    }
    // Check if the bullet will hit an asteroid in the next tick.
    //private bool HasHitAsteroid()
    //{
    //    var hitAsteroid = Runner.LagCompensation.Raycast(transform.position, transform.forward, _speed * Runner.DeltaTime,
    //        Object.InputAuthority, out var hit, _asteroidLayer);

    //    if (hitAsteroid == false) return false;

    //    var asteroidBehaviour = hit.GameObject.GetComponent<AsteroidBehaviour>();

    //    if (asteroidBehaviour.IsAlive == false)
    //        return false;

    //    asteroidBehaviour.HitAsteroid(Object.InputAuthority);

    //    return true;
    //}
}




