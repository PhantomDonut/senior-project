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
    [SerializeField] [Range(0, 10f)] private float textBounceSpeed = 1;
    [SerializeField] private Transform startText;
    
    public void Initialize() {
        if(GameManager.FirstHubLoad) {
            GameManager.Instance.gameState = GameState.Cutscene;
            titleAnimator.SetTrigger("Title In");
            trueCamController = FindObjectOfType<CameraController>();
            trueCamController.enabled = false;
            playerCamera = trueCamController.cameraTransform;
            playerCamera.transform.position = new Vector3(0, 15, -20);
            playerCamera.transform.eulerAngles = new Vector3(-85, 0, 0);
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
    }

    private void LateUpdate() {
        if (motionPulse) MotionPulse();
    }
    
    private void MotionPulse() {
        //titleText.localScale = Vector3.one * (1 - Mathf.PingPong(Time.time / 10f * titleBounceSpeed, 0.1f));
        startText.localScale = Vector3.one * (1 - Mathf.PingPong(Time.time / 5f * textBounceSpeed, 0.1f));
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
            playerCamera.transform.rotation = Quaternion.Euler(Mathf.Lerp(-85, 18, Mathf.InverseLerp(startingTime, startingTime + 4, Time.time)), 0, 0);
            yield return new WaitForEndOfFrame();
        }
        trueCamController.enabled = true;
        //GameManager.Instance.gameState = GameState.Regular;
    }
}
