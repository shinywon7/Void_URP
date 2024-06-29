using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public ObjectPooler pooler;
    // Start is called before the first frame update
    void Start()
    {
        pooler = GameObject.Find("BarrierPooler").GetComponent<ObjectPooler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            pooler.SpawnFromPool(transform.position);
        }
    }
}
