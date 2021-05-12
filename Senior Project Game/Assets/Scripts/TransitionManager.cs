using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour {
    [SerializeField] Image black;
    [SerializeField] Image portal;
    [SerializeField] float portalBaseScale = 3000f;
    [SerializeField] AnimationCurve speedCurve;

    public void Fade(float time, bool fadeOut) {
        StartCoroutine(FadeFunction(time, fadeOut));
    }

    IEnumerator FadeFunction(float time, bool fadeOut) {
        Color blackColor = new Color(0, 0, 0, fadeOut ? 0 : 1);
        black.color = blackColor;
        float startingTime = Time.time;
        while(Time.time < startingTime + time) {
            blackColor.a = fadeOut ? Mathf.InverseLerp(startingTime, startingTime + time, Time.time) : Mathf.InverseLerp(startingTime + time, startingTime, Time.time);
            black.color = blackColor;
            yield return new WaitForEndOfFrame();
        }
        blackColor.a = fadeOut ? 1 : 0;
        black.color = blackColor;
    }

    public void Portal(float time, bool outwards) {
        StartCoroutine(PortalFunction(time, outwards));
    }

    IEnumerator PortalFunction(float time, bool outwards) {
        Debug.Log("portal");
        black.color = Color.clear;
        Vector2 portalScale = Vector2.one * (outwards ? portalBaseScale : 0);
        portal.rectTransform.sizeDelta = portalScale;
        float startingTime = Time.time;
        while (Time.time < startingTime + time) {
            portal.rectTransform.sizeDelta = Vector2.one * portalBaseScale * speedCurve.Evaluate(outwards ? Mathf.InverseLerp(startingTime, startingTime + time, Time.time) : Mathf.InverseLerp(startingTime + time, startingTime, Time.time));
            portal.rectTransform.localRotation = Quaternion.Euler(0, 0, -90 * (outwards ? Mathf.InverseLerp(startingTime, startingTime + time, Time.time) : Mathf.InverseLerp(startingTime + time, startingTime, Time.time)));
            //Debug.Log(Mathf.InverseLerp(startingTime, startingTime + time, Time.time));
            yield return new WaitForEndOfFrame();
        }
        portal.rectTransform.sizeDelta = Vector2.one * portalBaseScale;
        black.color = new Color(0, 0, 0, 1);
    }
}
