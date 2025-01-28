using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RME = RegUtil.RegMeshEdit_v3;

public class TrianlePointChecker : MonoBehaviour
{
    public Transform a, b, c, point;
    public Color out_, in_;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool flag =  RME.Convert.IsinTriangle(a.position, b.position, c.position, point.position);

        point.GetComponent<SpriteRenderer>().color = flag ? in_ : out_;
    }
}
