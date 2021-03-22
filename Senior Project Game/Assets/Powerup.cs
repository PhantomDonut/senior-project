using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Powerup {
    public float maxTime = -1;
    public abstract void ApplyStatus(Player player);
}
