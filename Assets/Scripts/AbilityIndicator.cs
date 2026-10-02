using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AbilityIndicator : MonoBehaviour
{
    [Header("General")]
    public AbilityIndicationType type;

    [Header("PaticleSystem")]
    public ParticleSystem particle_particleSystem;
    public float particle_amount;

    [Header("Obstacle")]
    public ObstacleTypedata obsData;
    public bool waitForAbilityEnd;
    public int waitingMovementElementIndex;
    public float abilityDurationMultiplier = 1;

    [SerializeField] private bool locked = false;

    private void Start()
    {
        if (waitForAbilityEnd && obsData != null)
        {
            Manager.DisableAllColliders(obsData.gameObject);
            Manager.DisableAllRenderers(obsData.gameObject);
        }
    }

    public void ExecuteIndication(float duration, float abilityDuration, bool ignoreGamestate = false)
    {
        if (Manager.m.gameplayManager.currentState != GameState.Running && ignoreGamestate == false)
        {
            return;
        }
        if (locked)
        {
            Debug.LogWarning("Warning - Tried to execute ability indicator while already indicating");
            return;
        }
        locked = true;
        switch (type)
        {
            case AbilityIndicationType.None:
                locked = false;
                break;
            case AbilityIndicationType.ParticleSystem:
                StartCoroutine(ExecuteIndicationParticleSystem(duration));
                break;
            case AbilityIndicationType.Obstacle:
                StartCoroutine(ExecuteIndicationObstacle(duration, abilityDuration));
                break;
        }
    }

    public void EndExecution()
    {
        switch (type)
        {
            case AbilityIndicationType.None:
                break;
            case AbilityIndicationType.ParticleSystem:
                var emission = particle_particleSystem.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(0);
                break;
            case AbilityIndicationType.Obstacle:
                Manager.DisableAllColliders(obsData.gameObject);
                Manager.DisableAllRenderers(obsData.gameObject);
                break;
        }
        return; //todo
    }

    public IEnumerator ExecuteIndicationParticleSystem(float duration)
    {
        var emission = particle_particleSystem.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(particle_amount);

        yield return new WaitForSeconds(duration);

        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0);
        locked = false;
    } 
    public IEnumerator ExecuteIndicationObstacle(float duration, float abilityDuration)
    {
        Manager.EnableAllColliders(obsData.gameObject);
        Manager.EnableAllRenderers(obsData.gameObject);
        obsData.speedMultiplier = 1 / duration;
        if (waitForAbilityEnd) obsData.MOVING_movementTargets[waitingMovementElementIndex].duration = abilityDuration * abilityDurationMultiplier;
        obsData.triggerd = true;
        yield return new WaitForSeconds(duration);
        if (waitForAbilityEnd)
        {
            while (obsData.HasMovementTarget() && Manager.m.gameplayManager.currentState != GameState.Menu)
            {
                yield return null;
            }
        }
        Manager.DisableAllColliders(obsData.gameObject);
        Manager.DisableAllRenderers(obsData.gameObject);
        locked = false;
    }
    public bool Locked()
    {
        return locked;
    }
}

public enum AbilityIndicationType
{
    None,
    ParticleSystem,
    Obstacle,
}
