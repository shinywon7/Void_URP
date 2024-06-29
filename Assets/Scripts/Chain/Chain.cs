using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chain : MonoBehaviour
{
    public int iterations = 5;
    public float gravity = 10;
    public float damping = 0.7f;
    public PathAutoEndPoints pathInfo;
    public int numPoints = 20;
    public float meshThickness = 1;
    public MeshFilter meshFilter;
    public int cylinderResolution = 5;

    float pathLength;
    public float pointSpacing;
    point[] points;
    point start, end;

    bool pinStart = true;
    bool pinEnd = true;

    public LayerMask inMask;
    public LayerMask outMask;

    public GameObject ChainUnit;
    public GameObject ChainAnchor;
    public GameObject ChainCollider;

    void Start()
    {

        numPoints = Mathf.RoundToInt(pathInfo.pathCreator.path.length/pointSpacing);
        points = new point[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            float t = i / (numPoints - 1f);
            points[i].pos = pathInfo.pathCreator.path.GetPointAtTime(t, PathCreation.EndOfPathInstruction.Stop);
            points[i].lastPos = points[i].pos;
            points[i].sameSide = 1;
            points[i].collider = Instantiate(ChainCollider);
            points[i].collider.transform.parent = transform;
            points[i].collider.transform.position = points[i].pos;
            points[i].collider.GetComponent<SphereCollider>().radius = meshThickness;
            points[i].chainCollider = points[i].collider.GetComponent<ChainCollider>();
            if (i != 0) Physics.IgnoreCollision(points[i-1].collider.GetComponent<SphereCollider>(), points[i].collider.GetComponent<SphereCollider>(), true);
            points[i].rb = points[i].collider.GetComponent<Rigidbody>();
        }
        for(int i = 0; i < numPoints - 1; i++)
        {
            points[i].init(ChainUnit, transform);

            float t = i / (numPoints - 1f);
            Vector3 tan = points[i + 1].pos - points[i].pos;
            Vector3 nor = pathInfo.pathCreator.path.GetNormal(t, PathCreation.EndOfPathInstruction.Stop);
            points[i].nor = i % 2 == 0 ? nor : Vector3.Cross(tan,nor);
        }
        terminalInit();
        
        for (int i = 0; i < numPoints - 1; i++)
        {
            pathLength += Vector3.Distance(points[i].pos, points[i + 1].pos);
        }
        //pointSpacing = pathLength / points.Length;


        Player.flip += SideFlip;
        VoidManager.voidSet += VoidSet;
        VoidManager.voidFade += VoidFade;
        VoidManager.voidRearrange += VoidRearrange;
        VoidManager.setLimit += CheckLimit;
    }
    void terminalInit()
    {
        start = new point();
        end = new point();
        start.init(ChainAnchor, transform);
        start.pos = pathInfo.origin.position;
        start.tan = pathInfo.origin.forward;
        start.nor = Vector3.Cross(start.tan, pathInfo.pathCreator.path.GetNormal(0, PathCreation.EndOfPathInstruction.Stop));

        end.init(ChainAnchor, transform);
        end.pos = pathInfo.target.position;
        end.tan = pathInfo.target.forward;
        Vector3 nor = pathInfo.pathCreator.path.GetNormal(1, PathCreation.EndOfPathInstruction.Stop);
        end.nor = (numPoints-1) % 2 == 0 ? nor : Vector3.Cross(end.tan, nor);
    }

    void FixedUpdate()
    {
        Extract();
        for (int i = 0; i < points.Length; i++)
        {
            bool pinned = (i == 0 && pinStart) || (i == points.Length - 1 && pinEnd);
            if (!pinned)
            {
                Vector3 curr = points[i].pos;
                points[i].pos = points[i].pos + (points[i].pos - points[i].lastPos) * damping + Vector3.down * gravity * Time.deltaTime * Time.deltaTime;
                points[i].lastPos = curr;
            }
        }


        for (int i = 0; i < iterations; i++)
        {
            ConstrainConnections();
            ConstrainNormal();
        }
        ConstrainFlip();
        //developer
        points[0].pos = pathInfo.origin.position+ pathInfo.origin.forward * pathInfo.terminalGap;

        Inject();
    }
    void CheckLimit()
    {
        int now = 0;
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].chainCollider.onCollision|| i == points.Length-1)
            {
                if (points[now].sameSide != points[i].sameSide)
                {
                    float maxLength = (i - now) * (pointSpacing);
                    Vector3 dist = VoidManager.voidTransform.InverseTransformVector(points[i].pos - points[now].pos);
                    float limitWidth = Mathf.Sqrt(maxLength * maxLength - dist.x * dist.x - dist.z * dist.z) - Mathf.Abs(dist.y);
                    VoidManager.limitVoidWidth = Mathf.Min(VoidManager.limitVoidWidth, limitWidth);
                }
                now = i;
            }
        }
    }
    void Extract()
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].Extract();
        }
    }
    void Inject()
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].Inject();
        }
    }
    void LateUpdate()
    {
        MoveRenderer();
    }
    void MoveRenderer()
    {
        for (int i = 0; i < points.Length-1; i++)
        {
            points[i].MoveRenderer();
        }
        start.MoveRenderer();
        end.MoveRenderer();
    }
    void ConstrainNormal()
    {
        Vector3 shift = points[0].sameSide == points[1].sameSide ? Vector3.zero : VoidManager.voidTransform.up * VoidManager.voidWidth * points[0].sameSide;
        points[0].tan = points[1].pos - points[0].pos-shift;
        points[0].nor = -Vector3.Cross(points[0].tan, start.nor + points[1].nor).normalized;
        for (int i = 1; i < points.Length-1; i++)
        {
            shift = points[i].sameSide == points[i + 1].sameSide ? Vector3.zero : VoidManager.voidTransform.up * VoidManager.voidWidth * points[i].sameSide;
            points[i].tan = points[i + 1].pos - points[i].pos-shift;
            if (i % 2 == 1)
            {
                points[i].nor = Vector3.Cross(points[i].tan, points[i - 1].nor + points[i + 1].nor).normalized;
            }
            else
            {
                points[i].nor = -Vector3.Cross(points[i].tan, points[i - 1].nor + points[i + 1].nor).normalized;
            }
        }
    }
    void ConstrainFlip()
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (VoidManager.isVoidGenerated)
            {
                points[i].voidDist = VoidManager.voidPlane.GetDistanceToPoint(points[i].pos) * points[i].sameSide;
                if (points[i].voidDist < -VoidManager.halfVoidWidth)
                {
                    Vector3 shift = VoidManager.voidTransform.up * VoidManager.voidWidth * points[i].sameSide;
                    points[i].pos += shift;
                    points[i].lastPos += shift;
                    points[i].sameSide *= -1;
                    points[i].SideSet(points[i].sameSide >0);
                }
            }
        }
    }
    public void VoidRearrange()
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].VoidRearrange();
        }
    }
    public void SideFlip()
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].SideFlip();
        }
        start.SideFlip();
        end.SideFlip();
    }
    public void VoidSet()
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].VoidSet();
        }
        start.VoidSet();
        end.VoidSet();
    }
    void VoidFade()
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].SideSet(true);
        }
        start.sameSide = 1f;
        end.sameSide = 1f;
    }
    void ConstrainConnections()
    {
        for(int i = 0; i <points.Length - 1; i++)
        {
            Vector3 shift = points[i].sameSide == points[i + 1].sameSide ? Vector3.zero : VoidManager.voidTransform.up * VoidManager.voidWidth* points[i].sameSide;
            Vector3 center = (points[i].pos + shift + points[i + 1].pos) / 2;
            Vector3 offset = points[i].pos + shift - points[i + 1].pos;
            float length = offset.magnitude;
            Vector3 dir = offset / length;

            if (i != 0 || !pinStart)
            {
                points[i].pos = center + dir * pointSpacing / 2 - shift;
            }
            if (i + 1 != points.Length - 1 || !pinEnd)
            {
                points[i + 1].pos = center - dir * pointSpacing / 2;
            }
        }
    }
    void OnDrawGizmos()
    {
        if (points != null)
        {

            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(points[i].pos, 0.05f);
                Gizmos.DrawLine(points[i].pos, points[i].pos + points[i].nor);
                Gizmos.color = Color.blue;

                Gizmos.DrawLine(points[i].pos, points[i].pos + points[i].tan);
            }
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start.pos, start.pos + start.nor);

        }
    }
}
struct point
{
    public Vector3 pos;
    public Vector3 tan;
    public Vector3 nor;
    public Vector3 lastPos;
    public float sameSide;
    public float voidDist;
    public bool collided;
    public GameObject origin, clone, collider;
    public Rigidbody rb;
    public ChainCollider chainCollider;
    public void init(GameObject ChainRenderer, Transform parent)
    {
        origin = GameObject.Instantiate(ChainRenderer);
        clone = GameObject.Instantiate(origin);
        Renderer originRen = origin.GetComponent<Renderer>();
        Renderer cloneRen = clone.GetComponent<Renderer>();
        //MaterialUtils.GetClone(ref originRen, ref cloneRen, ref VoidManager.realOriginMats, ref VoidManager.fakeOriginMats);

        origin.transform.parent = parent;
        clone.transform.parent = parent;
        clone.layer = LayerMask.NameToLayer("TravellerInFake");
    }
    public void Extract()
    {
        pos = rb.position;
    }
    public void Inject()
    {
        rb.MovePosition(pos);
        //rb.position = pos;
    }
    public void SideFlip()
    {
        sameSide *= -1;
        SideSet(sameSide > 0);
    }
    public void VoidSet()
    {
        SideSet(VoidManager.voidPlane.GetDistanceToPoint(pos) > 0);
    }
    public void SideSet(bool flag)
    {
        if (flag)
        {
            sameSide = 1;
            if(collider) collider.layer = LayerMask.NameToLayer("TravellerInReal");
        }
        else
        {
            sameSide = -1;
            if(collider) collider.layer = LayerMask.NameToLayer("TravellerOutReal");
        }
    }

    public void VoidRearrange()
    {
        // if(voidDist < 0)
        // {
        //     float rate = -voidDist / VoidManager.halfVoidWidthOld;
        //     pos -= VoidManager.voidTransform.up * (VoidManager.halfVoidWidth - VoidManager.halfVoidWidthOld) * rate * sameSide;
        // }
    }
    public void MoveRenderer()
    {
        GameObject o,c;
        if (sameSide>0) { o = origin; c = clone; }
        else { c = origin; o = clone; }

        o.transform.position = pos;
        o.transform.LookAt(pos+tan,nor);
        c.transform.SetPositionAndRotation(o.transform.position+ VoidManager.voidTransform.up * VoidManager.voidWidth * sameSide,o.transform.rotation);
    }
}
