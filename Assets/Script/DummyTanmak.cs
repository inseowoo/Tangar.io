using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class DummyTanmak : NetworkBehaviour
{
    // Local Runtime references
    private ChangeDetector _changeDetector;

    [Networked] public NetworkBool _isAlive { get; private set; }

    public override void Spawned()
    {
        // --- Host & Client
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

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

    public void Hit()
    {
        // Gamelogic only on the host.
        if (Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        Runner.Despawn(Object);
    }
}
