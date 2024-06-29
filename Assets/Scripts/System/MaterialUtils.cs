using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialUtils
{
    static Dictionary<Material[], Material[]> dict = new Dictionary<Material[], Material[]>();
    //public static GameObject GetClone (GameObject origin)
    //{ 
    //    GameObject clone = GameObject.Instantiate(origin);
    //    if (!dict.ContainsKey(origin.GetComponent<MeshRenderer>().sharedMaterials))
    //    {
    //        clone.GetComponent<MeshRenderer>().sharedMaterials = origin.GetComponent<MeshRenderer>().sharedMaterials;
    //        Material[] originMaterials = origin.GetComponent<MeshRenderer>().sharedMaterials;
    //        Material[] cloneMaterials = clone.GetComponent<MeshRenderer>().materials;
    //        dict.Add(originMaterials, cloneMaterials);
    //        VoidManager.inTravellers.AddRange(originMaterials);
    //        VoidManager.outTravellers.AddRange(cloneMaterials);
    //    }
    //    else
    //    {
    //        clone.GetComponent<MeshRenderer>().sharedMaterials = dict[origin.GetComponent<MeshRenderer>().sharedMaterials];
    //    }
    //    return clone;
    //}
    public static void GetClone(ref Renderer origin, ref Renderer clone, ref List<Material> originMats, ref List<Material> cloneMats)
    {
        if (!dict.ContainsKey(origin.sharedMaterials))
        {
            clone.sharedMaterials = origin.sharedMaterials;
            Material[] originMaterials = origin.sharedMaterials;
            Material[] cloneMaterials = clone.materials;
            dict.Add(originMaterials, cloneMaterials);
            originMats.AddRange(originMaterials);
            cloneMats.AddRange(cloneMaterials);
        }
        else
        {
            clone.sharedMaterials = dict[origin.sharedMaterials];
        }
    }
    public static Material[] GetChildMaterials(GameObject g)
    {
        var renderers = g.GetComponentsInChildren<MeshRenderer>();
        var matList = new List<Material>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                matList.Add(mat);
            }
        }
        return matList.ToArray();
    }
    public static Material[] GetMaterials(GameObject g)
    {
        var renderer = g.GetComponent<MeshRenderer>();
        var matList = new List<Material>();
        foreach (var mat in renderer.materials)
        {
            matList.Add(mat);
        }
        return matList.ToArray();
    }
}
