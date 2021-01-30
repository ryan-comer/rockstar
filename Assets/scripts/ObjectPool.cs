using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{

    public GameObject[] PoolableObjects;

    private Dictionary<int, Queue<GameObject>> objectPool = new Dictionary<int, Queue<GameObject>>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public bool ReturnToPool(int objectIndex, GameObject gameObject)
    {
        if (objectIndex > PoolableObjects.Length)
        {
            return false;
        } 


        // No pool for the object
        if (!objectPool.ContainsKey(objectIndex))
        {
            return false;
        }

        Assets.scripts.IPoolable poolable = gameObject.GetComponent<Assets.scripts.IPoolable>();
        poolable.ReturnToPool();
        gameObject.SetActive(false);
        objectPool[objectIndex].Enqueue(gameObject);

        return true;
    }

    public GameObject[] GetFromPool(int objectIndex, int amount)
    {
        if(objectIndex > PoolableObjects.Length)
        {
            return null;
        }

        // No pool for the object
        if (!objectPool.ContainsKey(objectIndex))
        {
            return null;
        }

        List<GameObject> objectsToReturn = new List<GameObject>();
        for(var i = 0; i < amount; i++)
        {
            GameObject newObject = objectPool[objectIndex].Dequeue();
            newObject.SetActive(true);
            Assets.scripts.IPoolable poolable = newObject.GetComponent<Assets.scripts.IPoolable>();
            poolable.LeavePool();
            objectsToReturn.Add(newObject);
        }

        return objectsToReturn.ToArray();
    }

    public bool AddToPool(int objectIndex, int amount)
    {
        if(objectIndex > PoolableObjects.Length)
        {
            return false;
        }

        // Check for first
        if (!objectPool.ContainsKey(objectIndex))
        {
            objectPool[objectIndex] = new Queue<GameObject>();
        }

        for(var i = 0; i < amount; i++)
        {
            GameObject newObject = Instantiate(PoolableObjects[objectIndex]);
            Assets.scripts.IPoolable poolable = newObject.GetComponent<Assets.scripts.IPoolable>();

            // Check if interface exists
            if(poolable == null)
            {
                return false;
            }

            poolable.ReturnToPool();
            newObject.SetActive(false);
            objectPool[objectIndex].Enqueue(newObject);
        }

        return true;
    }

}
