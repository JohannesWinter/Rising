using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss1 : MonoBehaviour
{
    public GameObject bossObject;
    public Ability[] abilities;
    public float globalCooldownMultiplier = 1;
    public float cooldownSpeed = 1;
    public float abilitySpeed = 1;
    public float delay;

    bool inFight;
    float currentGlobalCooldown;
    float[] abilityCooldowns;
    float[] abilityDurations;
    float remainingDelay;
    LinkedList<Ability> priorityQueue;
    
    // Start is called before the first frame update
    void Start()
    {
        remainingDelay = delay;
        abilityCooldowns = new float[abilities.Length];
        abilityDurations = new float[abilities.Length];
        priorityQueue = new LinkedList<Ability>();
        for (int i = 0; i < abilities.Length; i++)
        {
            for (int x = 0; x < abilities[i].data.Length; x++)
            {
                if (abilities[i].data[x].obs.GetComponent<Renderer>()) abilities[i].data[x].obs.GetComponent<Renderer>().enabled = false;
                if (abilities[i].data[x].obs.GetComponent<Collider2D>()) abilities[i].data[x].obs.GetComponent<Collider2D>().enabled = false;
            }
            priorityQueue.AddLast(abilities[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //print(priorityQueue.First.Value.data[0].obsSpace.name + " -- " + priorityQueue.First.Next.Value.data[0].obsSpace.name + " -- " + priorityQueue.First.Next.Next.Value.data[0].obsSpace.name);
        if (Manager.m.gameplayManager.currentState != GameState.Menu && Manager.m.gameplayManager.currentLevel == 8)
        {
            if (remainingDelay > 0)
            {
                remainingDelay -= Time.deltaTime;
            }
            else
            {
                if (inFight == false)
                {
                    OnFightStart();
                }
                inFight = true;
                if (Manager.m.gameplayManager.currentState != GameState.Resetting)
                {
                    UpdateBossPosition();
                }
                UpdateAbilityUsage();
                UpdateAbilityTimer();
                UpdateAbilitySpeed();
            }
        }
        else
        {
            if (inFight == true)
            {
                OnFightEnd();
            }
            remainingDelay = delay;
            inFight = false;
            
        }
    }
    void OnFightStart()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            abilityCooldowns[i] = 0;
            abilityDurations[i] = 0;
            for (int x = 0; x < abilities[i].data.Length; x++)
            {
                abilities[i].data[x].obs.ResetObstacle();
            }
        }
    }
    void OnFightEnd()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            for (int x = 0; x < abilities[i].data.Length; x++)
            {
                if (abilities[i].data[x].obs.GetComponent<Renderer>()) abilities[i].data[x].obs.GetComponent<Renderer>().enabled = false;
                if (abilities[i].data[x].obs.GetComponent<Collider2D>()) abilities[i].data[x].obs.GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    void UpdateBossPosition()
    {
        bossObject.transform.position = new Vector3(0, Manager.m.playerCamera.transform.position.y, 0);
    }
    void UpdateAbilitySpeed()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            for (int x = 0; x < abilities[i].data.Length; x++)
            {
                abilities[i].data[x].obs.speedMultiplier = abilitySpeed;
            }
        }
    }


    void UpdateAbilityUsage()
    {
        currentGlobalCooldown -= Time.deltaTime;
        if (currentGlobalCooldown < 0)
        {
            int tryCounter = 0;
            int positionCounter = 0;
            while (tryCounter + positionCounter < abilities.Length)
            {
                LinkedListNode<Ability> toExecuteNode = GetLinkedListElement(priorityQueue.First, positionCounter);
                Ability toExecute = toExecuteNode.Value;
                int position = AbilityArrayPos(toExecute);
                if (abilityCooldowns[position] > 0)
                {
                    priorityQueue.Remove(toExecuteNode);
                    InsertInBackPercentage<Ability>(priorityQueue, toExecute, 50);
                    tryCounter++;
                    continue;
                }
                else if (abilityDurations[position] > 0)
                {
                    positionCounter++;
                    continue;
                }
                else if (AllowAbility(position, abilityDurations) == false)
                {
                    if (toExecute.blockOnWait)
                        break;

                    positionCounter++;
                    continue;

                }
                else
                {
                    priorityQueue.Remove(toExecuteNode);
                    InsertInBackPercentage<Ability>(priorityQueue, toExecute, 50);

                    abilityDurations[position] = toExecute.duration / abilitySpeed;
                    abilityCooldowns[position] = UnityEngine.Random.Range(toExecute.cooldown / toExecute.cooldownRandomizer, toExecute.cooldown * toExecute.cooldownRandomizer) / cooldownSpeed;
                    currentGlobalCooldown = toExecute.globalCooldown * globalCooldownMultiplier;
                    ExecuteAbility(toExecute);
                    break;
                }
            }

        }
    }
    public void ExecuteAbility(Ability a)
    {
        for (int i = 0; i < a.data.Length; i++)
        {
            StartCoroutine(ExecuteAbility(a.data[i], a.data[i].obsSpace.transform.localPosition));
        }
        currentGlobalCooldown = a.globalCooldown * globalCooldownMultiplier / cooldownSpeed;

    }
    public IEnumerator ExecuteAbility(AbilityData ad, Vector2 startPosition)
    {

        ad.obsSpace.transform.localPosition = startPosition;
        var emission = ad.indicator.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(ad.indicationEmission);
        float timer = 0;
        while (timer < (ad.indicationTime / abilitySpeed))
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            if (inFight == false)
            {
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(0);
                yield break;
            }
        }
        if (ad.obs.GetComponent<Renderer>()) ad.obs.GetComponent<Renderer>().enabled = true;
        if (ad.obs.GetComponent<Collider2D>()) ad.obs.GetComponent<Collider2D>().enabled = false;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0);
        if (!inFight) yield break;
        ad.obs.triggerd = true;
        
    }

    void UpdateAbilityTimer()
    {
        for (int i = 0; i < abilityDurations.Length; i++)
        {
            if (abilityDurations[i] > 0)
            {
                abilityDurations[i] -= Time.deltaTime;
            }
            else
            {
                abilityDurations[i] = 0;
                for (int x = 0; x < abilities[i].data.Length; x++)
                {
                    if (abilities[i].data[x].obs.GetComponent<Renderer>()) abilities[i].data[x].obs.GetComponent<Renderer>().enabled = false;
                    if (abilities[i].data[x].obs.GetComponent<Collider2D>()) abilities[i].data[x].obs.GetComponent<Collider2D>().enabled = false;
                }
            }
        }
        for (int i = 0; i < abilityCooldowns.Length; i++)
        {
            if (abilityCooldowns[i] > 0)
            {
                abilityCooldowns[i] -= Time.deltaTime;
            }
            else
            {
                abilityCooldowns[i] = 0;
            }
        }
    }

    bool AllowAbility(int abilityPos, float[] durations)
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
                if (durations[0] > 0 && durations[3] > 0)
                {
                    float otherPercentageTimeLeftAfterOwnInitiationTime = (durations[3] - abilities[2].data[0].indicationTime / abilitySpeed) / (abilities[3].cooldown / abilitySpeed);
                    if (otherPercentageTimeLeftAfterOwnInitiationTime > 0 && otherPercentageTimeLeftAfterOwnInitiationTime < 1)
                    {
                        return false;
                    }

                }
                break;
            case 3:
                if (durations[1] > 0 && durations[2] > 0)
                {
                    float otherPercentageTimeLeftAfterOwnInitiationTime = (durations[2] - abilities[3].data[0].indicationTime / abilitySpeed) / (abilities[2].cooldown / abilitySpeed);
                    if (otherPercentageTimeLeftAfterOwnInitiationTime > 0 && otherPercentageTimeLeftAfterOwnInitiationTime < 1)
                    {
                        return false;
                    }
                }
                break;
        }
        return true;
    }
    int AbilityArrayPos(Ability ability)
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] == ability)
            {
                return i;
            }
        }
        return -1;
    }

    public static void InsertInBackPercentage<T>(LinkedList<T> list, T value, float backPercentage)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (list.Count == 0)
        {
            list.AddFirst(value);
            return;
        }

        backPercentage = Mathf.Clamp01(backPercentage);

        int count = list.Count;

        int startIndex = Mathf.FloorToInt(count * (1f - backPercentage));

        int targetIndex = UnityEngine.Random.Range(startIndex, count);

        LinkedListNode<T> node;

        if (targetIndex < count / 2)
        {
            node = list.First;
            for (int i = 0; i < targetIndex; i++)
                node = node.Next;
        }
        else
        {
            node = list.Last;
            for (int i = count - 1; i > targetIndex; i--)
                node = node.Previous;
        }

        list.AddBefore(node, value);
    }

    public static LinkedListNode<T> GetLinkedListElement<T>(LinkedListNode<T> first, int position)
    {
        if (position == 0) return first;
        else return GetLinkedListElement(first.Next, position - 1);
    }
}

[System.Serializable]
public class AbilityData
{
    [Header("Selectable")]
    public Transform obsSpace;
    public ObstacleTypedata obs;
    public ParticleSystem indicator;
    public float indicationEmission;
    public float indicationTime;
}
[System.Serializable]
public class Ability
{
    [Header("Selectable")]
    public AbilityData[] data;
    public float cooldown;
    public float cooldownRandomizer = 1;
    public float duration;
    public float globalCooldown;
    public bool blockOnWait;
}
