using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Firefly : MonoBehaviour
{
    public VisualEffect effect;
    
    // Start is called before the first frame update
    void Start()
    {
        VoidManager.voidSet += VoidSet;
        VoidManager.voidFade += VoidFade;
        Player.flip += Flip;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void VoidSet()
    {
        transform.SetPositionAndRotation(VoidManager.voidTransform.position, VoidManager.voidTransform.rotation);
        effect.SetBool("Side", true);
        effect.SetVector3("Velocity",transform.InverseTransformVector(VoidManager.gravity));
    }
    void Flip()
    {
        effect.SetBool("Side",!effect.GetBool("Side"));
    }
    void LateUpdate()
    {
        effect.SetFloat("HalfWidth", VoidManager.halfVoidWidth);
        effect.SetFloat("Width", VoidManager.voidWidth);
    }
    void VoidFade()
    {
        transform.SetPositionAndRotation(VoidManager.voidTransform.position, VoidManager.voidTransform.rotation);
    }
}
