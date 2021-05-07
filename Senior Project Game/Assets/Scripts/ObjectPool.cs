using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool {
    public Queue<IPoolable> pooledObjects;
    public int amount;
    public GameObject prefab;

    public IPoolable FetchPooledObject() {
        return pooledObjects.Dequeue();
    }

    public void ReturnToPool(IPoolable poolObject) {
        pooledObjects.Enqueue(poolObject);
    }
}
