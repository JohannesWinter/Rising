using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class GameplayManager : MonoBehaviour
{
    public GameObject pointsPanel;
    public TextMeshProUGUI pointsDisplay;

    public GameObject levelPanel;
    public TextMeshProUGUI levelDisplay;
    public HealthDisplay healthDisplay;

    public GameObject levelStopMenu;
    public Button levelCancelButton;
    public int currentTimeScale;
    public int currentLevel;
    public int currentHealth;
    public float cameraResetSpeed;
    public float maxCameraResetTime;
    public float playerDisappearDuration;
    public float playerReappearDuration;
    public float playerReapperWaitTime;
    public float headlineDisappearSpeed;
    public float headlineReappearSpeed;

    public GameState currentState;

    bool showText;
    float points = 0;
    void Start()
    {
        currentState = GameState.Menu;
        //currentLevel = 1;
        levelCancelButton.onClick.AddListener(Fail);
        levelStopMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == GameState.Menu)
        {
            Time.timeScale = 1;
            pointsPanel.gameObject.SetActive(false);
            showText = true;
            levelDisplay.text = Manager.m.worldBuilder.levelList[currentLevel - 1].name;
            if (Input.GetButtonDown("Fire1"))
            {
                StartGame();
            }
            Manager.m.playerController.dead = false;
        }
        else if (currentState == GameState.Running)
        {
            showText = false;
            Time.timeScale = currentTimeScale;
            if (PlayerOutOfBounds() || Manager.m.playerController.dead)
            {
                Fail();
            }
            if (Manager.m.worldBuilder.finished == true)
            {
                Win();
            }
            pointsDisplay.text = Mathf.Round(points) + "m";
            points += Time.deltaTime * Manager.m.playerController.currentGeneralSpeed;
            if (Input.GetButtonDown("Cancel"))
            {
                Stop();
            }
        }
        else if (currentState == GameState.Stopped)
        {
            Time.timeScale = 0;
            if (Input.GetButtonDown("Cancel") || Input.GetButtonUp("Fire1"))
            {
                Continue();
            }

        }
        else if (currentState == GameState.Resetting)
        {
            Time.timeScale = currentTimeScale;
        }
        UpdateHeadline();
        UpdateHealthDisplay();
    }

    void StartGame()
    {
        Manager.m.worldBuilder.running = true;
        Manager.m.worldBuilder.finished = false;
        currentState = GameState.Running;
        points = 0;
        pointsPanel.gameObject.SetActive(true);
    }

    void Fail()
    {
        currentState = GameState.Resetting;
        if (currentLevel > 1)
        {
            currentHealth -= 1;
            if (currentHealth < 0)
            {
                currentHealth = 3;
                currentLevel -= 1;
            }
        }
        Manager.m.playerController.dead = false;
        Manager.m.worldBuilder.Reset();
        StartCoroutine(ResetCamera());
        levelStopMenu.SetActive(false);
    }

    void Win()
    {
        currentLevel += 1;
        Manager.m.worldBuilder.Reset();
        currentState = GameState.Menu;
        Manager.m.playerCamera.transform.localPosition = new Vector3(0,0,-10);
        levelStopMenu.SetActive(false);
        currentHealth = 3;
    }
    void Stop()
    {
        levelStopMenu.SetActive(true);
        currentState = GameState.Stopped;
        Manager.m.worldBuilder.running = false;
    }

    void Continue()
    {
        levelStopMenu.SetActive(false);
        currentState = GameState.Running;
        Manager.m.worldBuilder.running = true;
    }

    bool PlayerOutOfBounds()
    {
        if (Manager.m.playerController.gameObject.transform.localPosition.y < Manager.m.playerCamera.gameObject.transform.localPosition.y - Manager.m.playerCamera.orthographicSize * 1.2)
        {
            return true;
        }
        if (Math.Abs(Manager.m.playerController.gameObject.transform.localPosition.x) > Manager.m.playerCamera.orthographicSize * Manager.m.playerCamera.aspect * 1.2)
        {
            return true;
        }
        return false;
    }

    void UpdateHeadline()
    {
        if (levelDisplay.color.a < 1 && showText == true)
        {
            if (headlineReappearSpeed == 0) levelDisplay.color = new Color(levelDisplay.color.r, levelDisplay.color.g, levelDisplay.color.b, 1);
            else levelDisplay.color = new Color(levelDisplay.color.r, levelDisplay.color.g, levelDisplay.color.b, levelDisplay.color.a + Time.unscaledDeltaTime / headlineReappearSpeed);
        }
        else if (levelDisplay.color.a > 0 && showText == false)
        {
            if (headlineDisappearSpeed == 0) levelDisplay.color = new Color(levelDisplay.color.r, levelDisplay.color.g, levelDisplay.color.b, 0);
            else levelDisplay.color = new Color(levelDisplay.color.r, levelDisplay.color.g, levelDisplay.color.b, levelDisplay.color.a - Time.unscaledDeltaTime / headlineDisappearSpeed);
        }
    }

    void UpdateHealthDisplay()
    {
        if (currentState != GameState.Resetting)
        {
            healthDisplay.publicHealth = currentHealth;
        }
        healthDisplay.publicMaxHealth = 3;
        if (healthDisplay.publicAlpha < 1 && showText == true)
        {
            if (headlineReappearSpeed == 0) healthDisplay.publicAlpha = 1;
            else healthDisplay.publicAlpha += Time.unscaledDeltaTime / headlineReappearSpeed;
        }
        if (healthDisplay.publicAlpha > 0 && showText == false)
        {
            if (headlineDisappearSpeed == 0) healthDisplay.publicAlpha = 0;
            else healthDisplay.publicAlpha -= Time.unscaledDeltaTime / headlineDisappearSpeed;
        }
    }

    public IEnumerator ResetCamera()
    {
        currentState = GameState.Resetting;
        SpriteRenderer playerSpr = Manager.m.playerController.playerObject.GetComponent<SpriteRenderer>();
        Transform playerTrf = Manager.m.playerController.playerObject.transform;
        Vector3 playerSize = playerTrf.localScale;
        Light2D playerLight = Manager.m.playerController.playerObject.GetComponent<Light2D>();
        float playerLightIntensity = playerLight.intensity;
        Transform cam = Manager.m.playerCamera.transform;
        Vector3 aimPosition = new Vector3(cam.position.x, Manager.m.worldBuilder.GetCurrentMinHeight(), cam.position.z);
        Vector3 startPosition = cam.position;
        Vector3 aimVector = aimPosition - startPosition;
        float resetTime = Mathf.Min(Mathf.Abs(cam.position.y - aimPosition.y) / Mathf.Max(1, cameraResetSpeed), maxCameraResetTime);
        float currentDuration = 0;

        while (currentDuration < resetTime)
        {
            currentDuration += Time.unscaledDeltaTime;
            float percentageDuration = currentDuration / resetTime;

            float p = Mathf.Lerp(1f, 6f, 0.5f);
            float a = Mathf.Pow(percentageDuration, p);
            float b = Mathf.Pow(1f - percentageDuration, p);
            
            float percentageDistance = a / (a + b);

            if (percentageDistance > 0.999 || percentageDistance.Equals(float.NaN)) break;

            cam.transform.position = startPosition + aimVector * percentageDistance;

            if (resetTime < playerDisappearDuration)
            {
                playerSpr.color = new Color(playerSpr.color.r, playerSpr.color.g, playerSpr.color.b, 1 - percentageDuration);
                playerTrf.localScale = playerSize * (1 - percentageDuration);
                playerLight.intensity = playerLightIntensity * (1 - percentageDuration * 2);
                if (playerLight.intensity < 0) playerLight.intensity = 0;
            }
 
            else
            {
                playerSpr.color = new Color(playerSpr.color.r, playerSpr.color.g, playerSpr.color.b, 1 - Mathf.Max(0, currentDuration / playerDisappearDuration));
                playerTrf.localScale = playerSize * (1 - currentDuration / playerDisappearDuration);
                playerLight.intensity = playerLightIntensity * (1 - currentDuration / playerDisappearDuration * 2);
                if (playerLight.intensity < 0) playerLight.intensity = 0;
            }
            yield return null;
        }
        cam.transform.position = aimPosition;
        Manager.m.playerController.playerObject.transform.localPosition = new Vector3(0, 0, -1);

        float currentReappearDuration = 0;

        while (currentReappearDuration < playerReappearDuration)
        {
            currentReappearDuration += Time.unscaledDeltaTime;
            float percentageDuration = currentReappearDuration / playerReappearDuration;

            playerSpr.color = new Color(playerSpr.color.r, playerSpr.color.g, playerSpr.color.b, percentageDuration);
            playerTrf.localScale = playerSize * percentageDuration;
            playerLight.intensity = playerLightIntensity * percentageDuration;
            yield return null;
        }
        playerLight.intensity = playerLightIntensity;
        playerTrf.localScale = playerSize;
        playerLight.intensity = playerLightIntensity;

        yield return new WaitForSecondsRealtime(playerReapperWaitTime);
        currentState = GameState.Menu;
    }
}

public enum GameState
{
    Menu,
    Running,
    Stopped,
    Resetting,
}
