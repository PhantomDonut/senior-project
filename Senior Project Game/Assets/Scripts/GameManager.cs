using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

#pragma warning disable 0649
public enum GameState {Regular, Paused}
public class GameManager : Singleton<GameManager> {

    [HideInInspector] public GameState gameState = GameState.Regular;

    public static float GameTime;
    public static float GamePhysicsTime;
    public static float PlayTime;
    [HideInInspector] public bool countPlayTime = true;
    public static bool IsGamePaused = true;

    public Player player;
    public LevelManager currentLevelManager;

    private void Start() {
        StartCoroutine(LoadComponentObjects());
        LevelSwitch();
    }

    IEnumerator LoadComponentObjects() {
        yield return SceneManager.LoadSceneAsync("Components", LoadSceneMode.Additive);
        player = GameObject.FindObjectOfType<Player>();
        currentLevelManager.SetPlayer(player);
    }

    private void Update() {
        if(gameState == GameState.Regular) {
            GameTime += Time.deltaTime;
        } 
        if(gameState != GameState.Paused && countPlayTime) {
            PlayTime += Time.deltaTime;
        }
    }

    public void TogglePause(bool invert = true, bool state = false) {
        IsGamePaused = invert ? !IsGamePaused : state;
        if(IsGamePaused) {

        } else {

        }
    }

    public void ToggleCursorVisibility(bool visible) {
        Cursor.lockState = visible ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    private void FixedUpdate() {
        if (gameState == GameState.Regular) {
            GamePhysicsTime += Time.fixedDeltaTime;
        }
        if (gameState != GameState.Paused && countPlayTime) {
            PlayTime += Time.deltaTime;
        }
    }

    public void LevelSwitch() {
        currentLevelManager = FindObjectOfType<LevelManager>();
    }

    public void FullGameQuit() {
        Application.Quit();
    }
}