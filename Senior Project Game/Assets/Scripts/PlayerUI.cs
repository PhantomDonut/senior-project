using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUI : MonoBehaviour {

    private GameState lastGameState = GameState.Regular;

    [SerializeField] private Animator pauseBlurAnimator = default;
    [SerializeField] private Animator playerAnimator = default;

    public void MenuPressed(Player player) {
        if (GameManager.Instance.gameState == GameState.Paused) {
            GameManager.Instance.gameState = lastGameState;
            pauseBlurAnimator.SetBool("Pause", false);
            playerAnimator.speed = 1;
            player.ToggleMomentumPause(false);
            GameManager.Instance.ToggleCursorVisibility(false);
        }
        else {
            if (GameManager.Instance.gameState != GameState.TitleScreen) {
                lastGameState = GameManager.Instance.gameState;
                GameManager.Instance.gameState = GameState.Paused;
                pauseBlurAnimator.gameObject.SetActive(true);
                pauseBlurAnimator.SetBool("Pause", true);
                playerAnimator.speed = 0;
                player.ToggleMomentumPause(true);
                GameManager.Instance.ToggleCursorVisibility(true);
            }
        }
    }

    public void ExitToHub() {
        if (GameManager.CurrentSceneID != "Hub") {
            GameManager.Instance.SwitchToLevel("Hub", true);
        } else {
            GameManager.Instance.FullGameQuit();
        }
    }
}
