using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss1 : MonoBehaviour, BossPerformer
{
    public Boss boss;
    public int level;
    public float section1Start;
    public float section2Start;
    public float section3Start;
    public float end;

    bool startedSection1;
    bool startedSection2;
    bool startedSection3;
    bool ended;

    Transform cam;

    private void Start()
    {
        cam = Manager.m.playerCamera.gameObject.transform;
    }
    // Update is called once per frame
    void Update()
    {
        if (Manager.m.gameplayManager.currentLevel == level && (Manager.m.gameplayManager.currentState != GameState.Menu))
        {
            boss.runGeneral = true;
            if (cam.localPosition.y > section1Start && startedSection1 == false)
            {
                startedSection1 = true;
                StartCoroutine(SetStage1());
            }
            if (cam.localPosition.y > section2Start && startedSection2 == false)
            {
                startedSection2 = true;
                StartCoroutine(SetStage2());
            }
            if (cam.localPosition.y > section3Start && startedSection3 == false)
            {
                startedSection3 = true;
                StartCoroutine(SetStage3());
            }
            if (cam.localPosition.y > end && ended == false)
            {
                ended = true;
                StartCoroutine(End());
            }
        }
        else
        {
            boss.runGeneral = false;
            boss.runAbilities = false;
            boss.runCooldowns = false;
            startedSection1 = false;
            startedSection2 = false;
            startedSection3 = false;
        }
    }

    public bool AllowAbility(int abilityPos, float[] durations)
    {
        switch (abilityPos)
        {
            case 0:
                if (durations[1] > 0)
                    return false;
                break;
            case 1:
                if (durations[0] > 0)
                    return false;
                break;
            case 2:
                if (durations[3] > 0)
                {
                    float otherPercentageTimeLeftAfterOwnInitiationTime = (durations[3] - boss.abilities[2].data[0].indicationTime / boss.abilitySpeed) / (boss.abilities[3].cooldown / boss.abilitySpeed);
                    if (otherPercentageTimeLeftAfterOwnInitiationTime > 0)
                    {
                        return false;
                    }

                }
                break;
            case 3:
                if (durations[2] > 0)
                {
                    float otherPercentageTimeLeftAfterOwnInitiationTime = (durations[2] - boss.abilities[3].data[0].indicationTime / boss.abilitySpeed) / (boss.abilities[2].cooldown / boss.abilitySpeed);
                    if (otherPercentageTimeLeftAfterOwnInitiationTime > 0)
                    {
                        return false;
                    }
                }
                break;
            case 4:
                break;
            case 5:
                if (durations[0] > 0 || durations[1] > 0 || durations[2] > 0 || durations[3] > 0 || durations[4] > 0)
                    return false;
                break;
        }
        return true;
    }


    IEnumerator SetStage1()
    {
        boss.InitializeAbilities(4);
        boss.cooldownSpeed = 1f;
        boss.abilitySpeed = 1f;
        boss.globalCooldownMultiplier = 1f;
        boss.runAbilities = true;
        boss.runCooldowns = true;
        yield break;
    }

    IEnumerator SetStage2()
    {
        boss.runAbilities = false;
        boss.runCooldowns = false;

        yield return new WaitForSeconds(5f);
        if (boss.runGeneral == false) yield break;
        boss.InitializeAbilities(5);

        boss.cooldownSpeed = 1f;
        boss.abilitySpeed = 2f;
        boss.globalCooldownMultiplier = 1f;
        boss.runAbilities = true;
        boss.runCooldowns = true;
        yield break;
    }

    IEnumerator SetStage3()
    {
        boss.runAbilities = false;
        boss.runCooldowns = false;

        yield return new WaitForSeconds(5f);
        if (boss.runGeneral == false) yield break;
        boss.InitializeAbilities(6);

        boss.cooldownSpeed = 5f;
        boss.abilitySpeed = 2.5f;
        boss.globalCooldownMultiplier = 1/2.5f;
        boss.runAbilities = true;
        boss.runCooldowns = true;
        yield break;
    }
    IEnumerator End()
    {
        boss.runAbilities = false;
        boss.runCooldowns = false;
        yield break;
    }
}

public interface BossPerformer
{
    public bool AllowAbility(int abilityPos, float[] durations);
}
