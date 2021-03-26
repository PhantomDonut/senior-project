using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour {
    public ObjectPool coinPool;
    [SerializeField] private Vector3 defaultPosition;

    public void Start() {
        coinPool.pooledObjects = new Queue<IPoolable>(coinPool.amount * 2);
        GameObject tempReference;
        for (int i = 0; i < coinPool.amount; i++) {
            tempReference = Instantiate(coinPool.prefab, defaultPosition, Quaternion.identity, transform);
            tempReference.SetActive(false);
            coinPool.pooledObjects.Enqueue(tempReference.GetComponent<IPoolable>());
        }
    }
    
    public IPoolable FetchPooled(string searchQuery, Vector3 position, Transform parent, Vector3 velocity) {
        IPoolable returnTerm = null;
        switch(searchQuery) {
            case "Coin":
                returnTerm = coinPool.FetchPooledObject();
                returnTerm.SetupPooledObject(position, parent, velocity);
                break;
            default:
                Debug.LogWarning("No such pool found");
                break;
        }

        return returnTerm;
    }

    public IPoolable[] FetchPooledMulti(string searchQuery, int count, Vector3 position, Transform parent, Vector3 velocityMin, Vector3 velocityMax) {
        IPoolable[] returnTerm = new IPoolable[count];
        switch (searchQuery) {
            case "Coin":
                for(int i = 0; i < count; i++) {
                    returnTerm[i] = coinPool.FetchPooledObject();
                    returnTerm[i].SetupPooledObject(position, parent, new Vector3(Random.Range(velocityMin.x, velocityMax.x), Random.Range(velocityMin.y, velocityMax.y), Random.Range(velocityMin.z, velocityMax.z)));
                }
                break;
            default:
                Debug.LogWarning("No such pool found");
                break;
        }

        return returnTerm;
    }

    public void ReturnToPool(string typeQuery, IPoolable poolObject, GameObject rootObject) {
        rootObject.SetActive(false);
        switch (typeQuery) {
            case "Coin":
                coinPool.ReturnToPool(poolObject);
                break;
            default:
                Debug.LogWarning("No such pool found");
                break;
        }
    }
}

public interface IPoolable {
    void SetupPooledObject(Vector3 position, Transform parent, Vector3 velocity);
}
