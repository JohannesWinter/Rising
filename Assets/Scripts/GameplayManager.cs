using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameplayManager : MonoBehaviour
{
    public PlayerController pc;
    public Worldbuilder wb;
    public GameObject pointsPanel;
    public TextMeshProUGUI pointsDisplay;

    public GameObject levelPanel;
    public TextMeshProUGUI levelDisplay;

    public GameObject levelStopMenu;
    public Button levelCancelButton;
    public Camera cam;
    public int currentTimeScale;
    int currentLevel;

    GameState currentState;
    float points = 0;
    void Start()
    {
        currentState = GameState.Menu;
        currentLevel = 1;
        levelCancelButton.onClick.AddListener(Fail);
        levelStopMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        pc.gamestate = currentState;
        if (currentState == GameState.Menu)
        {
            Time.timeScale = 0;
            pointsPanel.gameObject.SetActive(false);
            levelPanel.gameObject.SetActive(true);
            levelDisplay.text = "Level " + currentLevel;
            if (Input.GetButtonDown("Fire1"))
            {
                StartGame();
            }
        }
        else if (currentState == GameState.Running)
        {
            Time.timeScale = currentTimeScale;
            if (PlayerOutOfBounds() || pc.dead)
            {
                Fail();
            }
            if (wb.finished == true)
            {
                Win();
            }
            pointsDisplay.text = Mathf.Round(points) + "m";
            points += Time.deltaTime * pc.currentGeneralSpeed;
            if (Input.GetButtonDown("Cancel"))
            {
                Stop();
            }
        }
        else if (currentState == GameState.Stopped)
        {
            Time.timeScale = 0;
            if (Input.GetButtonDown("Cancel"))
            {
                Continue();
            }
        }
    }

    void StartGame()
    {
        wb.running = true;
        currentState = GameState.Running;
        wb.generate = 1;
        points = 0;
        levelPanel.gameObject.SetActive(false);
        pointsPanel.gameObject.SetActive(true);
    }

    void Fail()
    {
        pc.dead = false;
        wb.running = false;
        wb.reset = true;
        currentState = GameState.Menu;
        pc.playerObject.transform.localPosition = new Vector3(0,0,0);
        pc.cam.transform.position = new Vector3(0,0,-10);
        levelStopMenu.SetActive(false);
    }

    void Win()
    {
        wb.running = false;
        wb.reset = true;
        currentLevel += 1;
        currentState = GameState.Menu;
        pc.playerObject.transform.localPosition = new Vector3(0,0,0);
        pc.cam.transform.position = new Vector3(0,0,-10);
        levelStopMenu.SetActive(false);
    }
    void Stop()
    {
        levelStopMenu.SetActive(true);
        currentState = GameState.Stopped;
        wb.running = false;
    }

    void Continue()
    {
        levelStopMenu.SetActive(false);
        currentState = GameState.Running;
        wb.running = true;
    }

    bool PlayerOutOfBounds()
    {
        if (pc.gameObject.transform.localPosition.y < cam.gameObject.transform.position.y - cam.orthographicSize * 1.2)
        {
            return true;
        }
        if (Math.Abs(pc.gameObject.transform.localPosition.x) > cam.orthographicSize * cam.aspect * 1.2)
        {
            return true;
        }
        return false;
    }
}

public enum GameState
{
    Menu,
    Running,
    Stopped
}
