using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using TMPro;
using System;

#pragma warning disable 0649
public enum GameState {Regular, Paused, TitleScreen}
public class GameManager : Singleton<GameManager> {

    [HideInInspector] public GameState gameState = GameState.Regular;

    public static float GameTime;
    public static float GamePhysicsTime;
    public static float PlayTime;
    [HideInInspector] public bool countPlayTime = true;

    public Player player;
    public LevelManager currentLevelManager;

    private AsyncOperation loadingLevel;
    public static bool LoadedScene;
    public static string CurrentSceneID;

    public Camera uiCamera;
    public Animator pauseAnimator;
    public GameObject levelTransitionScreen;
    public Animator permanentUIAnimator;

    static bool ExistingGameManager = false;
    new public static GameManager Instance;

    private int levelLoadWait = 6;
    public bool autoLoad;
    [ShowIf("autoLoad", true)] public string autoLevelToLoad;

    public static bool RequestDebug;
    [SerializeField] private TextMeshProUGUI debugTextField;
    private const string debugTextString = "Version A.1\n\nLevel: {0}\nFPS: {1}/60\nGrounded: {2} {3}\nGround: {4}\nFriction: {5}\nBounce: {6}\nVelocity: {7}\nVertical Velocity: {8}\nSliding: {9} {10} {11}u/s\nVelocity Control: {12}\n\nPowerup: {13} for {14}s\n\nBase Jump: {15}\nRemaining Jumps: {16}\n\nHolding Wall: {17}\nWall Acceleration: {18}u/s";
    private string[] debugArgs;

    private void Awake() {
        if(ExistingGameManager) {
            Destroy(gameObject);
            return;
        }
        ExistingGameManager = true;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        debugArgs = new string[17];
    }

    private void Start() {
        if (autoLoad) {
            SwitchToLevel(autoLevelToLoad, true);
        } else {
            //Debug.Log("Start Init");
            Initalize(false);
            CurrentSceneID = "Hub";
        }
    }

    private void Initalize(bool delayTime) {
        //Debug.Log("Initalized");
        StartCoroutine(LoadComponentObjects(delayTime));
        LevelSwitch();
    }

    IEnumerator LoadComponentObjects(bool delayTime) {
        yield return SceneManager.LoadSceneAsync("Components", LoadSceneMode.Additive);
        var cameraData = uiCamera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Overlay;
        player = GameObject.FindObjectOfType<Player>();
        player.InitalizePlayer();
        currentLevelManager.SetPlayer(player);
        LoadedScene = true;
        if(delayTime) yield return new WaitForSeconds(levelLoadWait);
        //levelTransitionScreen.SetActive(false);
        Debug.Log("Fading in FADING IN");
        permanentUIAnimator.SetTrigger("Fade In");
        gameState = GameState.Regular;
    }

    public void SwitchToLevel(string levelName, bool fade) {
        StartCoroutine(SwitchLevelOutroAnimation(levelName, fade));
    }

    IEnumerator SwitchLevelOutroAnimation(string levelName, bool fade) {
        Debug.Log("Here and fade is " + fade);
        if (fade) {
            permanentUIAnimator.SetTrigger("Fade Out");
        } else {
            permanentUIAnimator.SetTrigger("Portal");
            yield return new WaitForSeconds(1.0f);
        }
        yield return new WaitForSeconds(1.0f);
        LoadedScene = false;
        //levelTransitionScreen.SetActive(true);
        var cameraData = uiCamera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Base;
        loadingLevel = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);
        CurrentSceneID = levelName;
        StartCoroutine(WaitForSceneLoad());
    }

    IEnumerator WaitForSceneLoad() {
        while(!loadingLevel.isDone) {
            yield return new WaitForEndOfFrame();
        }
        Initalize(true);
    }

    private void Update() {
        if(gameState == GameState.Regular) {
            GameTime += Time.deltaTime;
        } 
        if(gameState != GameState.Paused && countPlayTime) {
            PlayTime += Time.deltaTime;
        }
        if(Input.GetKeyDown(KeyCode.Alpha0)) {
            RequestDebug = !RequestDebug;
            debugTextField.gameObject.SetActive(RequestDebug);
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

    private void LateUpdate() {
        UpdateDebugUI();
    }

    public void LevelSwitch() {
        currentLevelManager = FindObjectOfType<LevelManager>();
    }

    public void FullGameQuit() {
        Application.Quit();
    }

    public void LoadDebugArgs(bool arg1, float arg2, Vector3 arg3, bool arg4, float arg5, string arg6, float arg7, bool arg8, int arg9, bool arg10) {
        debugArgs[0] = Convert.ToString(arg1);
        debugArgs[1] = Convert.ToString(arg2);
        debugArgs[5] = Convert.ToString(arg3);
        debugArgs[7] = Convert.ToString(arg4);
        debugArgs[10] = Convert.ToString(arg5);
        debugArgs[11] = Convert.ToString(arg6);
        debugArgs[12] = Convert.ToString(arg7);
        debugArgs[13] = Convert.ToString(arg8);
        debugArgs[14] = Convert.ToString(arg9);
        debugArgs[15] = Convert.ToString(arg10);
    }

    public void LoadDebugArgs(string arg1, string arg2, string arg3, Vector3 arg4, Vector3 arg5, float arg6, float arg7) {
        debugArgs[2] = Convert.ToString(arg1);
        debugArgs[3] = Convert.ToString(arg2);
        debugArgs[4] = Convert.ToString(arg3);
        debugArgs[6] = Convert.ToString(arg4);
        debugArgs[8] = Convert.ToString(arg5);
        debugArgs[9] = Convert.ToString(arg6);
        debugArgs[16] = Convert.ToString(arg7);
    }

    //public void LoadDebugArts()
    private void UpdateDebugUI() {
        debugTextField.text = string.Format(debugTextString, CurrentSceneID, (int)(1 / Time.smoothDeltaTime), debugArgs[0], debugArgs[1], debugArgs[2], debugArgs[3], debugArgs[4], debugArgs[5], debugArgs[6], debugArgs[7], debugArgs[8], debugArgs[9], debugArgs[10], debugArgs[11], debugArgs[12], debugArgs[13], debugArgs[14], debugArgs[15], debugArgs[16]);
    }


}