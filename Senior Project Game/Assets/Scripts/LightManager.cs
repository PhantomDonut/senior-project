using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour {
    private void Update() {
        Shader.SetGlobalVector("_SunDirection", transform.forward);
    }

}
