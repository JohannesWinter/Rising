using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss1 : MonoBehaviour
{
    public GameObject bossObject;
    public AbilityData[] abilities;

    bool inFight;
    float currentGlobalCooldown;
    int spellRotationPosition;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Manager.m.gameplayManager.currentState == GameState.Running && Manager.m.gameplayManager.currentLevel == 8)
        {
            if (inFight == false)
            {
                OnFightStart();
            }
            inFight = true;
            UpdateBossPosition();
            UpdateAbilityUsage();
        }
        else
        {
            inFight = false;
        }
    }
    void OnFightStart()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i].currentCooldown = 0;
        }
    }

    void UpdateBossPosition()
    {
        bossObject.transform.position = new Vector3(0, Manager.m.playerCamera.transform.position.y, 0);
    }


    void UpdateAbilityUsage()
    {
        currentGlobalCooldown -= Time.deltaTime;
        if (currentGlobalCooldown < 0)
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                AbilityData current = abilities[(i + spellRotationPosition) % abilities.Length];
                if (current.currentCooldown <= 0)
                {
                    currentGlobalCooldown = current.globalCooldown;
                    ExecuteAbility(current);
                    break;
                }
            }
            spellRotationPosition++;
        }
        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i].currentCooldown -= Time.deltaTime;
        }
    }

    public IEnumerator ExecuteAbility(AbilityData ad, Vector2 startPosition)
    {
        ad.obsSpace.transform.localPosition = startPosition;
        var emission = ad.indicator.emission;
        ad.currentCooldown = ad.cooldown;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(ad.indicationEmission);
        yield return new WaitForSeconds(ad.indicationTime);
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0);
        if (!inFight) yield break;
        ad.obs.triggerd = true;
    }

    public void ExecuteAbility(AbilityData ad)
    {
        StartCoroutine(ExecuteAbility(ad, ad.obsSpace.transform.localPosition));
    }
}

[System.Serializable]
public class AbilityData
{
    [Header("Selectable")]
    public ObstacleTypedata obs;
    public Transform obsSpace;
    public ParticleSystem indicator;
    public float indicationEmission;
    public float indicationTime;
    public float cooldown;
    public float globalCooldown;
    [Header("DoNotChange")]
    public float currentCooldown;
}
