using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#pragma warning disable 0649
public enum GameState {Regular, Paused}
public class GameManager : Singleton<GameManager> {

    [HideInInspector] public GameState gameState = GameState.Regular;

    public static float GameTime;
    public static float GamePhysicsTime;
    public static float PlayTime;
    [HideInInspector] public bool countPlayTime = true;

    public Player player;

    [SerializeField] private SurfaceType defaultSurface;
    public SurfaceType GetDefaultSurface() { return defaultSurface; }

    private void Start() {
        Application.targetFrameRate = -1;
    }

    private void Update() {
        if(gameState == GameState.Regular) {
            GameTime += Time.deltaTime;
        } 
        if(gameState != GameState.Paused && countPlayTime) {
            PlayTime += Time.deltaTime;
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

    public void FullGameQuit() {
        Application.Quit();
    }
}