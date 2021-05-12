using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TitleScreen : MonoBehaviour {

    [SerializeField] private Canvas titleScreen;
    [SerializeField] private Animator titleAnimator;
    [SerializeField] Transform playerCamera;
    private CameraController trueCamController;
    private bool manageCamera;
    private bool canActivate;
    private bool motionPulse;
    [SerializeField] private Transform titleText;
    [SerializeField] [Range(0, 10f)] private float titleBounceSpeed = 1;
    [SerializeField] private TextMeshProUGUI startText;
    private Color32 startTextColor;
    [SerializeField] [Range(0, 0.5f)] private float startTextOpacityRange = 0.2f;
    
    public void Initialize() {
        if(GameManager.FirstHubLoad) {
            GameManager.Instance.gameState = GameState.Cutscene;
            titleAnimator.SetTrigger("Title In");
            trueCamController = FindObjectOfType<CameraController>();
            trueCamController.enabled = false;
            playerCamera = trueCamController.cameraTransform;
            playerCamera.transform.position = new Vector3(0, 15, -20);
            playerCamera.transform.eulerAngles = new Vector3(-20, 0, 0);
            startTextColor = new Color32(255, 255, 255, 255);
            StartCoroutine(WaitForFadeUp());
        } else {
            titleScreen.enabled = false;
        }
    }

    // Update is called once per frame
    void Update() {
        if(canActivate && Input.GetKeyDown(KeyCode.Space)) {
            canActivate = false;
            StartCoroutine(MoveIntoGame());
        }
        if (motionPulse) MotionPulse();
    }
    
    private void MotionPulse() {
        startTextColor.a = (byte)(255f * (1f - Mathf.PingPong(Time.time, startTextOpacityRange)));
        startText.color = startTextColor;
        titleText.localScale = Vector3.one * (1 - Mathf.PingPong(Time.time / 10f * titleBounceSpeed, 0.1f));
    }

    IEnumerator WaitForFadeUp() {
        motionPulse = true;
        yield return new WaitForSeconds(2.0f);
        canActivate = true;
    }

    IEnumerator MoveIntoGame() {
        titleAnimator.SetTrigger("Title Out");
        yield return new WaitForSeconds(1.0f);
        GameManager.Instance.gameState = GameState.Regular;

        Vector3 startPos = new Vector3(0, 15, -20);
        Vector3 endPos = new Vector3(0, 6, -9);

        float startingTime = Time.time;
        while(Time.time < startingTime + 4.0) {
            playerCamera.transform.position = Vector3.Lerp(startPos, endPos, Mathf.InverseLerp(startingTime, startingTime + 4, Time.time));
            playerCamera.transform.rotation = Quaternion.Euler(Mathf.Lerp(-20, 18, Mathf.InverseLerp(startingTime, startingTime + 4, Time.time)), 0, 0);
            yield return new WaitForEndOfFrame();
        }
        trueCamController.enabled = true;
        //GameManager.Instance.gameState = GameState.Regular;
    }
}
