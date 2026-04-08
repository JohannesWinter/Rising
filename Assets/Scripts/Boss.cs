using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public BossPerformer performer;
    public GameObject bossObject;
    public Ability[] abilities;
    public float globalCooldownMultiplier = 1;
    public float cooldownSpeed = 1;
    public float abilitySpeed = 1;
    public float delay;
    public bool runAbilities;
    public bool runCooldowns;
    public bool runGeneral;

    protected bool inFight;
    protected float currentGlobalCooldown;
    protected float[] abilityCooldowns;
    protected float[] abilityDurations;
    float remainingDelay;
    protected LinkedList<Ability> priorityQueue;

    public int testAbility;
    
    // Start is called before the first frame update
    void Start()
    {
        performer = gameObject.GetComponent<BossPerformer>();
        testAbility = -1;
        remainingDelay = delay;
        InitializeAbilities(abilities.Length);
    }

    public void InitializeAbilities(int amount)
    {
        amount = Mathf.Min(amount, abilities.Length);
        abilityCooldowns = new float[amount];
        abilityDurations = new float[amount];
        priorityQueue = new LinkedList<Ability>();
        for (int i = 0; i < amount; i++)
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
        if (testAbility != -1)
        {
            ExecuteAbility(abilities[testAbility], true);
            testAbility = -1;
        }
        //print(AbilityListToString(priorityQueue));
        if (runGeneral)
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
                if (runAbilities)
                {
                    UpdateAbilityUsage();
                }
                if (runCooldowns)
                {
                    UpdateAbilityTimer();
                }
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
    void FixedUpdate()
    {
        if (runGeneral && Manager.m.gameplayManager.currentState != GameState.Resetting)
        {
            UpdateBossPosition();
        }
    }
    void OnFightStart()
    {
        for (int i = 0; i < abilityCooldowns.Length; i++)
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
        bossObject.GetComponent<Rigidbody2D>().MovePosition(new Vector3(0, Manager.m.playerCamera.transform.position.y, 0));
    }
    void UpdateAbilitySpeed()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            for (int x = 0; x < abilities[i].data.Length; x++)
            {
                abilities[i].data[x].obs.speedMultiplier = Mathf.Lerp(1, abilitySpeed, abilities[i].durationScaling);
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
            while (tryCounter + positionCounter < priorityQueue.Count)
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
                else if (performer.AllowAbility(position, abilityDurations) == false)
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

                    abilityDurations[position] = toExecute.duration / Mathf.Lerp(1, abilitySpeed, toExecute.durationScaling);
                    abilityCooldowns[position] = UnityEngine.Random.Range(toExecute.cooldown / toExecute.cooldownRandomizer, toExecute.cooldown * toExecute.cooldownRandomizer) / Mathf.Lerp(1,cooldownSpeed,toExecute.cooldownScaling);
                    currentGlobalCooldown = toExecute.globalCooldown * Mathf.Lerp(1, globalCooldownMultiplier, toExecute.globalCooldownScaling);
                    ExecuteAbility(toExecute);
                    break;
                }
            }
        }
    }

    public static string AbilityListToString(LinkedList<Ability> list)
    {
        if (list == null || list.Count == 0)
            return string.Empty;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        var node = list.First;

        while (node != null)
        {
            var ability = node.Value;

            string name = "null";

            if (ability != null &&
                ability.data != null &&
                ability.data.Length > 0 &&
                ability.data[0] != null &&
                ability.data[0].obsSpace != null)
            {
                name = ability.data[0].obsSpace.name;
            }

            sb.Append(name);

            if (node.Next != null)
                sb.Append(" -> ");

            node = node.Next;
        }

        return sb.ToString();
    }
    public void ExecuteAbility(Ability a, bool test = false)
    {
        for (int i = 0; i < a.data.Length; i++)
        {
            StartCoroutine(ExecuteAbility(a.data[i], a.data[i].obsSpace.transform.localPosition, test, a.durationScaling));
        }

    }
    public IEnumerator ExecuteAbility(AbilityData ad, Vector2 startPosition, bool test, float abilityDurationScaling)
    {

        ad.obsSpace.transform.localPosition = startPosition;
        float indicationTime = ad.indicationTime / Mathf.Lerp(1, abilitySpeed, abilityDurationScaling);
        if (ad.abilityIndicator != null)
        {
            ad.abilityIndicator.ExecuteIndication(indicationTime);
        }
        float timer = 0;
        while (timer < indicationTime)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            if (inFight == false && test == false)
            {
                yield break;
            }
        }
        if (ad.obs.GetComponent<Renderer>()) ad.obs.GetComponent<Renderer>().enabled = true;
        if (ad.obs.GetComponent<Collider2D>()) ad.obs.GetComponent<Collider2D>().enabled = true;

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

        list.AddAfter(node, value);
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
    public float indicationTime;

    public AbilityIndicator abilityIndicator;
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

    [Header("Scalings")]
    public float globalCooldownScaling = 1;
    public float cooldownScaling = 1;
    public float durationScaling = 1;
}
