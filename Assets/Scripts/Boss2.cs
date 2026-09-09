using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2 : MonoBehaviour, BossPerformer
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

    public bool ENDDDD;

    [Header("Specs")]
    public GameObject spinner;
    public GameObject antiSpinGear;
    bool currentlySpinning;
    float currentAbility1YAdd = 0;
    float currentAbility2YAdd = 0;


    private void Start()
    {
        cam = Manager.m.playerCamera.gameObject.transform;
    }
    // Update is called once per frame
    void Update()
    {
        if (ENDDDD)
        {
            ENDDDD= false;
            StartCoroutine(End());
        }
        if (Manager.m.gameplayManager.currentState == GameState.Menu) currentlySpinning = false;
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
            default: { break; }
        }
        return true;
    }
    public void Notify(int abilityPos)
    {
        switch (abilityPos)
        {
            case 0:
                List<float> indicationTimes1 = new List<float> { 0.8f, 0.75f, 0.85f };
                PickN(indicationTimes1, 3);
                for (int i = 0; i < indicationTimes1.Count; i++)
                {
                    boss.abilities[abilityPos].data[i].indicationTime = indicationTimes1[i];
                }
                break;
            case 1:
            case 2:
                List<float> indicationTimes2 = new List<float> { 0.985f, 0.990f, 0.995f, 1.000f, 1.005f, 1.010f, 1.015f };
                PickN(indicationTimes2, 7);
                var ability12Data = boss.abilities[abilityPos].data;
                for (int i = 0; i < indicationTimes2.Count; i++)
                {
                    boss.abilities[abilityPos].data[i].indicationTime = indicationTimes2[i];
                }
                float yAdd = UnityEngine.Random.Range(-1.25f, 1.25f);
                for (int i = 0; i < ability12Data.Length; i++)
                {
                    var pos = ability12Data[i].obsSpace.gameObject.transform.localPosition;
                    ability12Data[i].obsSpace.gameObject.transform.localPosition = new Vector3(pos.x, pos.y - (abilityPos == 1 ? currentAbility1YAdd : currentAbility2YAdd) + yAdd, pos.z);
                }
                if (abilityPos == 1) currentAbility1YAdd = yAdd; else currentAbility2YAdd = yAdd;
                break;
            case 3:
            case 4:
                int toRemovePos = -1;
                int toRemovePos2 = -1;
                if (currentlySpinning == false)
                    toRemovePos = (new System.Random()).Next(0,abilityPos == 3 ? 8 : 9); //0 inclusice, 8/9 exclusive
                else
                {
                    toRemovePos = (new System.Random()).Next(0, abilityPos == 3 ? 3 : 4);
                    toRemovePos2 = (new System.Random()).Next(abilityPos == 3 ? 5 : 5, abilityPos == 3 ? 8 : 9);
                }
                var ability34Data = boss.abilities[abilityPos].data;
                for (int i = 0; i < ability34Data.Length; i++)
                {
                    if (i != toRemovePos && i != toRemovePos2) ability34Data[i].executeable = true;
                    else ability34Data[i].executeable = false;
                }
                break;
        }
        return;
    }


    IEnumerator SetStage1()
    {
        boss.InitializeAbilities(new List<int> {0, 1, 2, 3, 4});
        boss.ExecuteAbility(boss.abilities[10], 3);
        boss.ExecuteAbility(boss.abilities[12], 5);
        StartCoroutine(Spin());
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
        currentlySpinning = false;

        boss.ExecuteAbility(boss.abilities[13], 5, true);
        yield return new WaitForSeconds(5f);
        if (boss.runGeneral == false) yield break;
        //boss.InitializeAbilities(2);

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
        boss.abilitySpeed = 2f;
        boss.globalCooldownMultiplier = 1 / 2.5f;
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

    void PickN<T>(IList<T> list, int n)
    {
        System.Random rng = new System.Random();
        for (int i = 0; i < n; i++)
        {
            int randomIndex = rng.Next(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    IEnumerator Spin()
    {
        currentlySpinning = true;
        spinner.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -80));
        yield return new WaitForSeconds(3);
        if (currentlySpinning == false)
        {
            yield break;
        }
        float maxSpinSpeed = 100;
        float currentSpinSpeed = 0;
        float spinAcceleration = 2;
        while (currentSpinSpeed < maxSpinSpeed && currentlySpinning)
        {
            spinner.transform.Rotate(0, 0, currentSpinSpeed * Time.deltaTime);
            currentSpinSpeed += spinAcceleration * Time.deltaTime;
            antiSpinGear.transform.Rotate(0, 0, -currentSpinSpeed * Time.deltaTime);
            yield return null;
        }
        currentSpinSpeed = maxSpinSpeed;
        while (currentlySpinning)
        {
            spinner.transform.Rotate(0, 0, currentSpinSpeed * Time.deltaTime);
            antiSpinGear.transform.Rotate(0, 0, -currentSpinSpeed * Time.deltaTime * 2);
            yield return null;
        }
        yield break;
    }
}
