﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour, Assets.scripts.IPoolable
{

    public float distancePerSecond = 1.0f;

    private bool falling = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called 50 times per second
    void FixedUpdate()
    {
        if (falling)
        {
            fall();
        }
    }

    private void fall()
    {
        // Called 50 times a second (every 0.02 seconds)
        float amountToFall = distancePerSecond * 0.02f;
        Vector3 newPosition = transform.position + Vector3.down * amountToFall;
        transform.position = newPosition;
    }

    public void LeavePool()
    {
        falling = true; 
    }

    public void ReturnToPool()
    {
        falling = false;
    }
}
