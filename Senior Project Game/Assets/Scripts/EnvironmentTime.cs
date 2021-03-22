using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EnvironmentTime : MonoBehaviour {

    public Light mainLight;
    public bool sceneHasWater;
    public Material waterMaterial;
    public Material skyboxMaterial;

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
        mainLight.transform.rotation = Quaternion.Euler(timeColorSet.lightRotation);
        mainLight.color = timeColorSet.lightColor;
        mainLight.intensity = timeColorSet.lightIntensity;
    }
}
