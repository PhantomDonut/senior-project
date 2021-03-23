using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Powerup : Collectible {
    public float maxTime = -1;
    public Material capeMaterial;
    public abstract void ApplyStatus(Player player);
}
