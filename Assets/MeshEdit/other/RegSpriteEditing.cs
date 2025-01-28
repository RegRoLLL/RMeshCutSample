using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RegUtil
{
    namespace RegSpriteEditing
    {
        public class Convert
        {
            public static Mesh SpriteToMesh(Sprite sprite)
            {
                Mesh mesh = new Mesh();

                mesh.vertices = Array.ConvertAll(sprite.vertices, (vec2) => (Vector3)vec2);
                mesh.triangles = Array.ConvertAll(sprite.triangles, (ush) => (int)ush);

                return mesh;
            }

            public static List<GameObject> SpriteToMeshObjects(Sprite sprite,Material material,Vector2 pos, params System.Type[] components)
            {
                var results = new List<GameObject>();

                var physicsShape = new List<Vector2>();

                var gameobj = new GameObject();
                gameobj.transform.position = pos;

                for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
                {
                    sprite.GetPhysicsShape(i, physicsShape);

                    var obj = RegMeshEdit_v3.CreateMeshObject.Create2D(physicsShape.ToArray(), gameobj.transform, material, components);

                    results.Add(obj);
                }

                GameObject.Destroy(gameobj);

                return results;
            }
        }
    }
}