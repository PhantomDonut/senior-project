using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public class EnvironmentTime : MonoBehaviour {

    [Header("Environmental Objects")]
    public Light mainLight;
    public Material skyboxMaterial;
    public bool sceneHasWater;
    [ShowIf("sceneHasWater", true)] public Material waterMaterial;
    public bool sceneHasClouds;
    [ShowIf("sceneHasClouds", true)] public Material cloudsMaterial;

    [Header("Selected Time")]
    public TimeColor selectedTimeColorSet;
    public bool triggerFunction = false;

    void Update() {
        if(triggerFunction) {
            triggerFunction = false;
            UpdateSceneTime(selectedTimeColorSet);
        }
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
