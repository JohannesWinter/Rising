using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]

public class ParticleTriggerAccess : MonoBehaviour
{
    public ParticleSystem ps;

    List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();

    void OnParticleTrigger()
    {
        if (ps == null) ps = gameObject.GetComponent<ParticleSystem>();
        int count = ps.GetTriggerParticles(
            ParticleSystemTriggerEventType.Enter,
            enter
        );

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = enter[i];

            p.remainingLifetime = 0.5f;

            enter[i] = p;
        }

        ps.SetTriggerParticles(
            ParticleSystemTriggerEventType.Enter,
            enter
        );
    }
}