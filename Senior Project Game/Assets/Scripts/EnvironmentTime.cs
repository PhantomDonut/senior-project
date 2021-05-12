using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public enum CelestialTime { Day = 0, Sunset = 1, Night = 2 }
public class EnvironmentTime : MonoBehaviour {
    [Header("Time Objects")]
    public static CelestialTime CurrentTime = CelestialTime.Day;
    TimeObject[] gameColorObjects;
    [ColorUsageAttribute(true, true)] public Color dayColor;
    [ColorUsageAttribute(true, true)] public Color sunsetColor;
    [ColorUsageAttribute(true, true)] public Color nightColor;
    private static Color[] timeColors;


    [Header("Environmental Objects")]
    public Light mainLight;
    public Material skyboxMaterial;
    public bool sceneHasWater;
    [ShowIf("sceneHasWater", true)] public Material waterMaterial;
    public bool sceneHasClouds;
    [ShowIf("sceneHasClouds", true)] public Material cloudsMaterial;

    [Header("Selected Time")]
    public TimeColor[] timeColorSets;
    public CelestialTime setTimeValue;
    public bool triggerFunction = false;


    private void Start() {
        gameColorObjects = FindObjectsOfType<TimeObject>();
        timeColors = new Color[] { dayColor, sunsetColor, nightColor};
    }

    void Update() {
        if(triggerFunction) {
            triggerFunction = false;
            TriggerTimeChange(setTimeValue);
        }
    }

    public void TriggerTimeChange(CelestialTime time) {
        CurrentTime = time;
        if(Application.isPlaying) UpdateAllTimeObjects((int)CurrentTime);
        UpdateSceneTime(timeColorSets[(int)CurrentTime]);
    }

    public void UpdateAllTimeObjects(int time) {
        CurrentTime = (CelestialTime)time;
        for (int i = 0; i < gameColorObjects.Length; i++) {
            gameColorObjects[i].TimeUpdate();
        }
    }

    public static Color FetchTimeColor(int time) {
        return timeColors[time];
    }

    void UpdateSceneTime(TimeColor timeColorSet) {
        RenderSettings.fogColor = timeColorSet.fogColor;
        RenderSettings.ambientEquatorColor = timeColorSet.ambientHorizon;
        RenderSettings.ambientSkyColor = timeColorSet.ambientSky;
        skyboxMaterial.SetVector("_SunDirection", timeColorSet.sunDirection);
        skyboxMaterial.SetColor("_HorizonColor", timeColorSet.skyboxHorizon);
        skyboxMaterial.SetColor("_SkyColor", timeColorSet.skyboxSky);
        if (sceneHasWater) {
            waterMaterial.SetColor("Color_F01C36BF", timeColorSet.waterShallow);
            waterMaterial.SetColor("Color_7D9A58EC", timeColorSet.waterDeep);
        }
        if(sceneHasClouds) {
            cloudsMaterial.SetColor("Color_AD16EE5E", timeColorSet.cloudPeak);
            cloudsMaterial.SetColor("Color_6D35BD30", timeColorSet.cloudValley);
            cloudsMaterial.SetFloat("Vector1_3BED982A", timeColorSet.cloudFresnelOpacity);
        }
        mainLight.transform.rotation = Quaternion.Euler(timeColorSet.lightRotation);
        mainLight.color = timeColorSet.lightColor;
        mainLight.intensity = timeColorSet.lightIntensity;
    }
}
