using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierModel : MonoBehaviour
{
    public ObjectPooler pooler;

    void OnEnable()
    {
        if (!pooler)
        {
            pooler = GameObject.Find("BarrierModels").GetComponent<ObjectPooler>();
            transform.parent = pooler.transform;
            VoidManager.voidFade += VoidFade;
        }
    }
    public void Setup(Vector2 p1, Vector2 p2)
    {
        transform.localPosition = (p1 + p2) / 2;
        transform.LookAt(pooler.transform.TransformPoint(p2), VoidManager.voidTransform.up);
        float length = (p2 - p1).magnitude;
        transform.localScale = new Vector3(1, 1, length);
    }
    public void VoidFade()
    {
        gameObject.SetActive(false);
    }
    void OnDisable()
    {
        pooler.ReturnToPool(gameObject);
    }
}
