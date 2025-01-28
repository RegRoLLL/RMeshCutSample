using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RME = RegUtil.RegMeshEdit_v3;

public class Cutter : MonoBehaviour
{
    public Transform container;
    public List<GameObject> objects = new List<GameObject>();
    public RealtimeLine line;

    void Start()
    {
        
    }

    public void Cut_Object()
    {
        var resultList = new List<GameObject>();

        objects = objects.Where(o => o != null).ToList();

        foreach (var obj in objects)
        {
            var material = obj.GetComponent<MeshRenderer>().material;

            var start = line.point[0].position;
            var end = line.point[1].position;

            var splitedObj = RME.MeshCut.Cut(obj, start, end);

            if (splitedObj == null)
            {
                Debug.Log("cut failed.");
                continue;
            }

            Debug.Log("result count is " + splitedObj.Count);

            foreach (var result in splitedObj)
            {
                result.AddComponent(typeof(DragableObject2D));

                result.transform.parent = container;

                resultList.Add(result);
            }
        }

        objects.AddRange(resultList);
    }
}
