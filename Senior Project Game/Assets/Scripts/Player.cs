using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    public bool justTeleported;

    public IEnumerator JustTeleported() {
        justTeleported = true;
        yield return new WaitForSeconds(Teleporter.TELEPORT_COOLDOWN);
        justTeleported = false;
    }
}
