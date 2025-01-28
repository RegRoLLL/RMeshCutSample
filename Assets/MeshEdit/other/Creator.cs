using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RME = RegUtil.RegMeshEdit_v3;
using RSE = RegUtil.RegSpriteEditing;

public class Creator : MonoBehaviour
{
    [SerializeField] string name_;
    [SerializeField] Sprite sprite;
    [SerializeField] Material material_;
    [SerializeField] Transform container;
    [SerializeField] Vector3[] verticles;
    [SerializeField] int[] indexes;
    [SerializeField] int[] polygon_indexes;

    void Start()
    {
        //GetComponent<Cutter>().objects.Add(Create());
        GetComponent<Cutter>().objects.AddRange(CreateFromSprite());
        //GetComponent<Cutter>().objects.Add(CreateFromPolygon());
    }

    public GameObject Create()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verticles;
        mesh.triangles = indexes;

        var polygon = System.Array.ConvertAll(polygon_indexes, (i) => (Vector2)verticles[i]);

        return RME.CreateMeshObject.Create2D(polygon, container, name_, material_,typeof(DragableObject2D));
    }

    public List<GameObject> CreateFromSprite()
    {
        var objs = RSE.Convert.SpriteToMeshObjects(sprite, material_, container.position, typeof(DragableObject2D));

        objs.ForEach((obj) => obj.transform.localScale = Vector3.one * 0.3f);

        return objs;
    }

    public GameObject CreateFromPolygon()
    {
        var physicsShape = new List<Vector2>();
        sprite.GetPhysicsShape(0, physicsShape);
        var mesh = RME.Convert.PolygonToMesh(physicsShape);

        var obj= RME.CreateMeshObject.Create2D(physicsShape.ToArray(), container, name_, material_,typeof(DragableObject2D));
        obj.transform.localScale = Vector3.one * 0.3f;

        return obj;
    }
}
