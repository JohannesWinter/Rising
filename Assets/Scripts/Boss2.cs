using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Specs")]
    public GameObject spinner;
    public GameObject antiSpinGear;
    bool currentlySpinning;
    float currentAbility1YAdd = 0;
    float currentAbility2YAdd = 0;
    float currentAbility0XAdd = 0;


    private void Start()
    {
        cam = Manager.m.playerCameraSpace.gameObject.transform;
    }
    // Update is called once per frame
    void Update()
    {
        if (Manager.m.gameplayManager.currentState == GameState.Menu)
        {
            if (currentlySpinning)
            {
                currentlySpinning = false;
            }
            
            spinner.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -80));
            antiSpinGear.transform.rotation = Quaternion.Euler(Vector3.zero);
            var indicators = boss?.abilities?
                .Where(a => a?.data != null)
                .SelectMany(a => a.data)
                .Select(d => d?.abilityIndicator)
                .Where(i => i != null);

            if (indicators != null)
            {
                foreach (var indicator in indicators)
                {
                    indicator.EndExecution();
                }
            }
        }
        if (Manager.m.gameplayManager.currentLevel == level && Manager.m.gameplayManager.currentState != GameState.Menu)
        {
            boss.runGeneral = true;
            if (Manager.m.gameplayManager.currentState == GameState.Resetting)
            {
                boss.runAbilities = false;
            }
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
            ended = false;
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

                var ability0Data = boss.abilities[abilityPos].data;

                for (int i = 0; i < indicationTimes1.Count; i++)
                {
                    ability0Data[i].indicationTime = indicationTimes1[i];
                }

                {
                    Vector3 playerLocalPosition = Manager.m.playerController.playerObject.transform.localPosition;
                    float xAdd = UnityEngine.Random.Range(playerLocalPosition.x - 0.5f, playerLocalPosition.x + 0.5f);
                    xAdd = Mathf.Clamp(xAdd, -0.75f, 0.75f);
                    xAdd = UnityEngine.Random.Range(xAdd - 0.25f, xAdd + 0.25f);

                    var pos = ability0Data[0].obsSpace.gameObject.transform.localPosition;

                    ability0Data[0].obsSpace.gameObject.transform.localPosition =
                        new Vector3(
                            pos.x - currentAbility0XAdd + xAdd,
                            pos.y,
                            pos.z
                        );

                    currentAbility0XAdd = xAdd;
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
                {
                    toRemovePos = (new System.Random()).Next(0, abilityPos == 3 ? 8 : 9); //0 inclusice, 8/9 exclusive
                }
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
        boss.InitializeAbilities(new List<int> {0, 1, 2});
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

        yield return new WaitForSeconds(3f);
        boss.ExecuteAbility(boss.abilities[10], 3);
        yield return new WaitForSeconds(5f);
        if (boss.runGeneral == false) yield break;
        boss.InitializeAbilities(new List<int> { 0, 1, 2, 3, 4 });

        boss.cooldownSpeed = 1.25f;
        boss.abilitySpeed = 1.15f;
        boss.globalCooldownMultiplier = 1/1.25f;
        boss.runAbilities = true;
        boss.runCooldowns = true;
        yield break;
    }

    IEnumerator SetStage3()
    {
        boss.runAbilities = false;
        boss.runCooldowns = false;

        yield return new WaitForSeconds(2f);
        if (boss.runGeneral == false) yield break;
        Manager.m.playerController.CameraShake(
            0.07f,
            6f,
            10f,
            0f,
            0.25f,
            6f,
            6f
            );
        boss.ExecuteAbility(boss.abilities[12], 5);
        yield return new WaitForSeconds(5f);
        if (boss.runGeneral == false) yield break;
        Manager.m.playerController.CameraShake(
            0.4f,
            2.5f,
            20f,
            0f,
            2f,
            0.5f,
            15f
            );
        yield return new WaitForSeconds(3.5f);
        if (boss.runGeneral == false) yield break;
        spinner.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -80));
        antiSpinGear.transform.rotation = Quaternion.Euler(Vector3.zero);
        StartCoroutine(Spin());
        yield return new WaitForSeconds(4.5f);
        if (boss.runGeneral == false) yield break;

        boss.cooldownSpeed = 5f;
        boss.abilitySpeed = 0.5f;
        boss.globalCooldownMultiplier = 0.5f;
        boss.runAbilities = true;
        boss.runCooldowns = true;
        yield break;
    }
    IEnumerator End()
    {
        boss.runAbilities = false;
        boss.runCooldowns = false;
        yield return new WaitForSeconds(5);
        Manager.m.playerController.CameraShake(
            intensity: 0.5f,
            duration: 5f,
            frequenzy: 15f,
            decreasePower: 0.2f
            );
        boss.ExecuteAbility(boss.abilities[13], 5);
        Manager.DisableAllColliders(spinner);
        yield return new WaitForSeconds(1.5f);
        if (boss.runGeneral == false) yield break;
        boss.ExecuteAbility(boss.abilities[11], 3);
        yield return new WaitForSeconds(3.5f);
        if (boss.runGeneral == false) yield break;
        currentlySpinning = false;
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
        antiSpinGear.transform.rotation = Quaternion.Euler(Vector3.zero);
        yield return new WaitForSeconds(3);
        if (currentlySpinning == false)
        {
            yield break;
        }
        float maxSpinSpeed = 100;
        float currentSpinSpeed = 0;
        float spinAcceleration = 2;
        float finalSpinAcceleration = 40;
        while (currentSpinSpeed < maxSpinSpeed && currentlySpinning)
        {
            if (ended) currentSpinSpeed += finalSpinAcceleration * Time.deltaTime;
            else currentSpinSpeed += spinAcceleration * Time.deltaTime;

            spinner.transform.Rotate(0, 0, currentSpinSpeed * Time.deltaTime);
            antiSpinGear.transform.Rotate(0, 0, -currentSpinSpeed * Time.deltaTime * 2);

            boss.abilitySpeed = 0.7f + (currentSpinSpeed / maxSpinSpeed) * 0.55f;
            boss.globalCooldownMultiplier = 1 / boss.abilitySpeed;

            yield return null;
        }
        while (currentlySpinning)
        {
            if (ended) currentSpinSpeed += finalSpinAcceleration * Time.deltaTime;
            spinner.transform.Rotate(0, 0, currentSpinSpeed * Time.deltaTime);
            antiSpinGear.transform.Rotate(0, 0, -currentSpinSpeed * Time.deltaTime * 2);
            yield return null;
        }
        yield break;
    }
}
