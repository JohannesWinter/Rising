using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    public GameObject healthBarObject;
    public float barDistance;
    int currentDisplayedHealth;
    int currentDisplayedMaxHealth;
    List<GameObject> currentHealthBars;
    public List<float> privateAlphas;
    public float lowLifeVibrateHz;
    float lowLifeVibrateValue;

    public int testHealth;
    public int testMaxHealth;

    public float publicAlpha;
    // Start is called before the first frame update
    void Start()
    {
        currentHealthBars = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (testHealth != currentDisplayedHealth || testMaxHealth != currentDisplayedMaxHealth)
        {
            UpdateHealth(testHealth, testMaxHealth);
        }
        for (int i = 0; i < currentDisplayedMaxHealth; i++)
        {
            currentHealthBars[i].GetComponent<Image>().color = new Color(1, 1, 1, privateAlphas[i] * publicAlpha);
        }
        if (currentDisplayedHealth == 0)
        {
            lowLifeVibrateValue += Time.unscaledDeltaTime;
            if (lowLifeVibrateValue > lowLifeVibrateHz)
            {
                lowLifeVibrateValue = 0;
                for (int i = 0; i < currentDisplayedMaxHealth; i++)
                {
                    currentHealthBars[i].transform.localPosition = new Vector3(currentHealthBars[i].transform.localPosition.x, Random.Range(-3.5f, 3.5f), 0);
                }
            }
        }
        else
        {
            for (int i = 0; i < currentDisplayedMaxHealth; i++)
            {
                currentHealthBars[i].transform.localPosition = new Vector3(currentHealthBars[i].transform.localPosition.x, 0, 0);
            }
        }
    }

    public void UpdateHealth(int newHealth, int newMaxHealth)
    {
        newMaxHealth = Mathf.Max(newMaxHealth, 0);
        newHealth = Mathf.Max(newHealth, 0);
        if (newHealth > newMaxHealth) newHealth = newMaxHealth;
        ChangeBarAmount(newMaxHealth);
        if (newMaxHealth != currentDisplayedMaxHealth)
        {
            currentDisplayedHealth = newMaxHealth;
            currentDisplayedMaxHealth = newMaxHealth;
        }

        for (int i = 0; i < currentDisplayedMaxHealth; i++)
        {
            if (i < newHealth && i >= currentDisplayedHealth)
            {
                StartCoroutine(GainBar(currentHealthBars[i], i));
            }
            else if (i >= newHealth && i < currentDisplayedHealth)
            {
                StartCoroutine(LoseBar(currentHealthBars[i], i));
            }
            else if (i >= newHealth)
            {
                privateAlphas[i] = 0.5f;
            }
        }
        currentDisplayedHealth = newHealth;
        currentDisplayedMaxHealth = newMaxHealth;
    }

    void ChangeBarAmount(int newMaxHealth)
    {
        while (currentHealthBars.Count > 0)
        {
            Destroy(currentHealthBars[currentHealthBars.Count - 1]);
            currentHealthBars.RemoveAt(currentHealthBars.Count - 1);
        }
        for (int i = 0; i < newMaxHealth; i++)
        {
            var newBar = Instantiate(healthBarObject);
            newBar.transform.SetParent(this.gameObject.transform);
            newBar.transform.localPosition = Vector3.zero;
            currentHealthBars.Add(newBar);
        }
        if (newMaxHealth == 0) return;
        float xDistance = barDistance;
        float leftMostX = -(newMaxHealth / 2f - 0.5f) * xDistance;
        privateAlphas = new List<float>();
        for (int i = 0; i < newMaxHealth; i++)
        {
            GameObject curBar = currentHealthBars[i];
            float curBarX = leftMostX + xDistance * i;
            curBar.transform.localPosition = new Vector3(curBarX, 0, 0);
            privateAlphas.Add(1);
        }
    }


    IEnumerator LoseBar(GameObject bar, int pos)
    {
        privateAlphas[pos] = 1f;
        for (int i = 0; i < 3; i++)
        {
            if (bar == null) yield break;
            privateAlphas[pos] = 0.5f;
            yield return new WaitForSecondsRealtime(0.2f);
            if (bar == null) yield break;
            privateAlphas[pos] = 1f;
            yield return new WaitForSecondsRealtime(0.2f);
        }
        if (bar == null) yield break;
        privateAlphas[pos] = 0.5f;
        yield return null;
    }
    IEnumerator GainBar(GameObject bar, int pos)
    {
        while (privateAlphas[pos] < 1)
        {
            if (bar == null) yield break;
            privateAlphas[pos] += Time.deltaTime;
            yield return null;
            if (bar == null) yield break;
        }
        if (bar == null) yield break;
        privateAlphas[pos] = 1f;
    }
}
