using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUI : MonoBehaviour {

    private GameState lastGameState = GameState.Regular;

    [SerializeField] private Animator pauseBlurAnimator = default;
    [SerializeField] private Animator playerAnimator = default;
    [SerializeField] private AudioSource pauseNoise = default;

    public void MenuPressed(Player player) {
        pauseNoise.Play();
        if (GameManager.Instance.gameState == GameState.Paused) {
            GameManager.Instance.gameState = lastGameState;
            pauseBlurAnimator.SetBool("Pause", false);
            player.ToggleVisualFreeze(false);
            player.ToggleMomentumPause(false);
            GameManager.Instance.ToggleCursorVisibility(false);
        }
        else {
            if (GameManager.Instance.gameState != GameState.TitleScreen) {
                lastGameState = GameManager.Instance.gameState;
                GameManager.Instance.gameState = GameState.Paused;
                pauseBlurAnimator.gameObject.SetActive(true);
                pauseBlurAnimator.SetBool("Pause", true);
                player.ToggleVisualFreeze(true);
                player.ToggleMomentumPause(true);
                GameManager.Instance.ToggleCursorVisibility(true);
            }
        }
    }

    public void ExitToHub() {
        if (GameManager.CurrentSceneID != "Hub") {
            GameManager.Instance.SwitchToLevel("Hub", true, CelestialTime.Day);
        } else {
            GameManager.Instance.FullGameQuit();
        }
    }
}
