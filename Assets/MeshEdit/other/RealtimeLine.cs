using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RealtimeLine : MonoBehaviour
{
    //[SerializeField] bool sendLog = false;

    public List<Transform> point = new();
    [HideInInspector] public LineRenderer _renderer;

    private List<Vector3> point_lastframe = new();

    void Start()
    {
        _renderer = this.GetComponent<LineRenderer>();

        point_lastframe.Clear();
        point_lastframe.AddRange(point.Select((p) => p.position).ToList());

        Render();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Compare_isDifferent())
        {
            point_lastframe.Clear();
            point_lastframe.AddRange(point.Select((p) => p.position).ToList());
            return;
        }
        point_lastframe.Clear();
        point_lastframe.AddRange(point.Select((p) => p.position).ToList());

        Render();
    }

    void Render()
    {
        _renderer.positionCount = point.Count;
        _renderer.SetPositions(point.Select((p) => p.position).ToArray());

        point[point.Count - 1].rotation = Quaternion.FromToRotation(Vector3.up, point[point.Count - 1].position - point[point.Count - 2].position);
    }

    bool Compare_isDifferent()
    {
        bool state = false;

        for (int i = 0; i < point.Count; i++)
            if (point_lastframe[i] != point[i].position)
            {
                state = true;
                break;
            }

        return state;
    }
}
