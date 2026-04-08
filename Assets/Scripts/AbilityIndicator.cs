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
    
    public void ExecuteIndication(float duration)
    {
        switch (type)
        {
            case AbilityIndicationType.None:
                break;
            case AbilityIndicationType.ParticleSystem:
                StartCoroutine(ExecuteIndicationParticleSystem(duration));
                break;
        }
    }

    public void StopExecution()
    {
        return; //todo
    }

    public IEnumerator ExecuteIndicationParticleSystem(float duration)
    {
        var emission = particle_particleSystem.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(particle_amount);

        yield return new WaitForSeconds(duration);

        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0);
    } 
}

public enum AbilityIndicationType
{
    None,
    ParticleSystem,
}
