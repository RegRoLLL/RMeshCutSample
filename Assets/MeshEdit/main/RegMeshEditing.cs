using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RegUtil
{
    namespace RegMeshEditing
    {
        public class MeshCut
        {
            public static (GameObject clockwise, GameObject anticlockwise) Cut(GameObject obj, Vector3 lineStart, Vector3 lineEnd)
            {   
                //そもそも線がちゃんと貫通しているか(どこかで三角形を貫通していたらtrueにする。三角形に刺さっていたらfalseにしてbreak)
                bool isClossingPolygonAndLine = false;

                var mesh = obj.GetComponent<MeshFilter>().mesh;
                var line = new Line_P() { start = lineStart, end = lineEnd };

                (MeshDataLists clockwiseResult, MeshDataLists anticlockwiseResult) = (new MeshDataLists(), new MeshDataLists());

                //mesh.trianglesを利用して各三角形それぞれで処理する
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    var index = (a: mesh.triangles[i],
                                 b: mesh.triangles[i + 1],
                                 c: mesh.triangles[i + 2]);

                    var pos = (a: obj.transform.TransformPoint(mesh.vertices[index.a]),
                               b: obj.transform.TransformPoint(mesh.vertices[index.b]),
                               c: obj.transform.TransformPoint(mesh.vertices[index.c]));

                    var (aSide, bSide, cSide) = (IsClockWise(lineStart, lineEnd, pos.a),
                                                 IsClockWise(lineStart, lineEnd, pos.b),
                                                 IsClockWise(lineStart, lineEnd, pos.c));

                    var (ab, bc, ca) = (new Line_P() { start = pos.a, end = pos.b },
                                        new Line_P() { start = pos.b, end = pos.c },
                                        new Line_P() { start = pos.c, end = pos.a });

                    var (abCross, bcCross, caCross) = (IsCrossing_Separate(line, ab),
                                                       IsCrossing_Separate(line, bc),
                                                       IsCrossing_Separate(line, ca));

                    /*
                     * Tを切ったときに独立する頂点として、時計回りにL、Rの頂点を渡す。
                     * 独立する頂点の時計回り判定をtSideとして渡す。
                     */
                    System.Action<Vector3, Vector3, Vector3, bool> TriangleMethod =
                    (Vector3 T, Vector3 L, Vector3 R, bool tSide) =>
                    {
                        isClossingPolygonAndLine = true;

                        Line_P tlLine = new Line_P() { start = T, end = L };
                        Line_P trLine = new Line_P() { start = T, end = R };

                        var tlCross = GetCrossingPoint(line, tlLine);
                        var trCross = GetCrossingPoint(line, trLine);

                        var triangleResult = tSide ? clockwiseResult : anticlockwiseResult;
                        var rectangleResult = tSide ? anticlockwiseResult : clockwiseResult;

                        triangleResult.AddTriangle(T, tlCross, trCross);
                        rectangleResult.AddRectangle(tlCross, L, R, trCross);
                    };

                    //交わらない場合
                    System.Action DefaultMethod = () =>
                    {
                        var triangleResult = aSide ? clockwiseResult : anticlockwiseResult;
                        triangleResult.AddTriangle(pos.a, pos.b, pos.c);
                    };

                    //三角形の中で切断線が切れる場合
                    System.Action StopMethod = () =>
                    {
                        isClossingPolygonAndLine = false;
                    };

                    //頂点aと頂点b,cに分かれる
                    if (aSide != bSide && aSide != cSide)
                    {
                        if (abCross && caCross)
                            TriangleMethod(pos.a, pos.b, pos.c, aSide);
                        else if (!abCross && !caCross)
                            DefaultMethod();
                        else{
                            StopMethod();
                            break;
                        }
                    }
                    //頂点bと頂点a,cに分かれる
                    else if (bSide != aSide && bSide != cSide)
                    {
                        if (abCross && bcCross)
                            TriangleMethod(pos.b, pos.c, pos.a, bSide);
                        else if(!abCross && !bcCross)
                            DefaultMethod();
                        else{
                            StopMethod();
                            break;
                        }
                    }
                    //頂点cと頂点a,bに分かれる
                    else if (cSide != aSide && cSide != bSide)
                    {
                        if (caCross && bcCross)
                            TriangleMethod(pos.c, pos.a, pos.b, cSide);
                        else if(caCross && bcCross)
                            DefaultMethod();
                        else{
                            StopMethod();
                            break;
                        }
                    }
                    //切断線と三角形が交わらない
                    else if (aSide == bSide && aSide == cSide)
                    {
                        DefaultMethod();
                    }
                }

                GameObject clockwiseObject, anticlockwiseObject;

                if (isClossingPolygonAndLine)
                {

                    for (int i = 0; i < clockwiseResult.verticles.Count; i++)
                        clockwiseResult.verticles[i] = obj.transform.InverseTransformPoint(clockwiseResult.verticles[i]);
                    for (int i = 0; i < anticlockwiseResult.verticles.Count; i++)
                        anticlockwiseResult.verticles[i] = obj.transform.InverseTransformPoint(anticlockwiseResult.verticles[i]);



                    var cut_polygon = CutPolygon(obj.GetComponent<PolygonCollider2D>(), line);

                    clockwiseObject = CreateMeshObject.Create2D(clockwiseResult.toMesh(),
                                                                    cut_polygon.clockwise,
                                                                    obj.transform,
                                                                    obj.GetComponent<MeshRenderer>().material);

                    anticlockwiseObject = CreateMeshObject.Create2D(anticlockwiseResult.toMesh(),
                                                                    cut_polygon.anticlockwise,
                                                                    obj.transform,
                                                                    obj.GetComponent<MeshRenderer>().material);
                }
                else
                {
                    clockwiseObject = CreateMeshObject.Create2D(mesh,
                                                                obj.GetComponent<PolygonCollider2D>().points,
                                                                obj.transform,
                                                                obj.GetComponent<MeshRenderer>().material);
                    anticlockwiseObject = null;
                }

                GameObject.Destroy(obj);

                return (clockwiseObject, anticlockwiseObject);
            }
            //========================================================================================================
            class MeshDataLists
            {
                public List<Vector3> verticles = new List<Vector3>();
                public List<int> indexes = new List<int>();

                /// <summary>
                /// リストに三角形を追加
                /// </summary>
                /// <param name="positions">三角形の各頂点( 時計回りに！！！ )</param>
                public void AddTriangle(params Vector3[] positions)
                {
                    if (positions.Length != 3) Debug.LogError("Not Triangle!!");

                    foreach (var pos in positions)
                    {
                        if (!verticles.Contains(pos)) verticles.Add(pos);

                        indexes.Add(verticles.IndexOf(pos));
                    }
                }

                /// <summary>
                /// リストに四角形を追加
                /// </summary>
                /// <param name="positions">四角形の各頂点( 時計回りに！！！ )</param>
                public void AddRectangle(params Vector3[] positions)
                {
                    if (positions.Length != 4) Debug.LogError("Not Rectngle!!");

                    //今回の四角形の各頂点のverticlesにおけるインデックス番号のリスト
                    var indexList = new List<int>();

                    foreach (var pos in positions)
                    {
                        if (!verticles.Contains(pos)) verticles.Add(pos);

                        indexList.Add(verticles.IndexOf(pos));
                    }

                    indexes.Add(indexList[0]);
                    indexes.Add(indexList[1]);
                    indexes.Add(indexList[2]);

                    indexes.Add(indexList[0]);
                    indexes.Add(indexList[2]);
                    indexes.Add(indexList[3]);
                }

                public Mesh toMesh()
                {
                    var mesh = new Mesh();

                    //foreach (var a in indexes) Debug.Log(a);

                    mesh.vertices = verticles.ToArray();
                    mesh.triangles = indexes.ToArray();

                    return mesh;
                }
            }

            //========================================================================================================

            /// <summary>
            /// 線分aと線分bが交差しているかどうかを判定する
            /// </summary>
            /// <param name="a">線分a</param>
            /// <param name="b">線分b</param>
            /// <returns></returns>
            static bool IsCrossing_Separate(Line_T a, Line_T b)
            {
                return IsCrossing_Separate(a.toLineP(), b.toLineP());
            }

            /// <summary>
            /// 線分aと線分bが交差しているかどうかを判定する
            /// </summary>
            /// <param name="a">線分a</param>
            /// <param name="b">線分b</param>
            /// <returns></returns>
            static bool IsCrossing_Separate(Line_P a, Line_P b)
            {
                bool crossA = IsClockWise(a.start, a.end, b.start) != IsClockWise(a.start, a.end, b.end);
                bool crossB = IsClockWise(b.start, b.end, a.start) != IsClockWise(b.start, b.end, a.end);

                return crossA && crossB;
            }

            /// <summary>
            /// 線分aと線分bの交点(交差しない場合はVector2.zeroを返す)
            /// </summary>
            /// <param name="a">線分a</param>
            /// <param name="b">線分b</param>
            /// <returns></returns>
            static Vector2 GetCrossingPoint(Line_T a, Line_T b)
            {
                return GetCrossingPoint(a.toLineP(), b.toLineP());
            }

            /// <summary>
            /// 線分aと線分bの交点(交差しない場合はVector2.zeroを返す)
            /// </summary>
            /// <param name="a">線分a</param>
            /// <param name="b">線分b</param>
            /// <returns></returns>
            static Vector2 GetCrossingPoint(Line_P a, Line_P b)
            {
                if (!IsCrossing_Separate(a, b)) { Debug.LogWarning("not crossing!"); return Vector2.zero; }

                var s1 = Mathf.Abs(CrossFrom3Point(a.start, a.end, b.end));

                var s2 = Mathf.Abs(CrossFrom3Point(a.start, a.end, b.start));

                float sum = s1 + s2;

                //Debug.Log("s1=" + s1 + " | s2=" + s2 + " | sum=" + sum + " ratio=" + (s1 / sum));

                return Vector2.Lerp(b.start, b.end, s2 / sum);
            }

            /// <summary>
            /// PolygonColliderを線分で切る
            /// </summary>
            /// <param name="polygonCol">PolygonCollider2D</param>
            /// <param name="line">線分</param>
            /// <returns>pointsにあたるVector2配列</returns>
            static public (Vector2[] clockwise, Vector2[] anticlockwise) CutPolygon(PolygonCollider2D polygonCol, Line_P line)
            {
                var tra = polygonCol.transform;

                //ワールド座標に変換した頂点データ配列
                var polygonWorld = Array.ConvertAll(polygonCol.points, (vec2) => (Vector2)tra.TransformPoint(vec2));

                var clockwiseList = new List<Vector2>();
                var anticlockwiseList = new List<Vector2>();

                var side = new Line_P();//ひとつ前の点→現在の点　の線分

                bool lastStateClockwise = IsClockWise(line.start, line.end, polygonWorld[polygonWorld.Length - 1]);
                bool currentStateClockwise;

                for (int i = 0; i < polygonWorld.Length; i++)
                {
                    side.end = polygonWorld[i];

                    side.start = (i == 0) ? polygonWorld[polygonWorld.Length - 1] : polygonWorld[i - 1];

                    currentStateClockwise = IsClockWise(line.start, line.end, side.end);

                    if (currentStateClockwise != lastStateClockwise)
                    {
                        //時計回り→反時計回り
                        if (lastStateClockwise) {
                            clockwiseList.Add(GetCrossingPoint(line, side));
                            anticlockwiseList.Add(GetCrossingPoint(line, side));
                        }
                        //反時計回り→時計回り
                        else
                        {
                            clockwiseList.Add(GetCrossingPoint(line, side));
                            anticlockwiseList.Add(GetCrossingPoint(line, side));
                        }
                    }

                    (currentStateClockwise ? clockwiseList : anticlockwiseList).Add(side.end);

                    lastStateClockwise = currentStateClockwise;
                }

                var clockwiseArray = Array.ConvertAll(clockwiseList.ToArray(), (worldVec) => (Vector2)tra.InverseTransformPoint(worldVec));
                var anticlockwiseArray = Array.ConvertAll(anticlockwiseList.ToArray(), (worldVec) => (Vector2)tra.InverseTransformPoint(worldVec));

                return (clockwiseArray, anticlockwiseArray);
            }

            //========================================================================================================

            /// <summary>
            /// 線分の始点と終点、目標の座標から目標が線分に対して時計回りの位置にあるかどうかを判定する
            /// </summary>
            /// <param name="start"></param>
            /// <param name="end"></param>
            /// <param name="target"></param>
            /// <returns></returns>
            static bool IsClockWise(Vector2 start, Vector2 end, Vector2 target)
            {
                return CrossFrom3Point(start, end, target) > 0;
            }

            /// <summary>
            /// 3つの点からベクトルの外積を求める
            /// a→b→c
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="c"></param>
            /// <returns></returns>
            static float CrossFrom3Point(Vector2 a, Vector2 b, Vector2 c)
            {
                var A = b - a;
                var B = c - b;

                return CrossFrom2Vec2(A, B);
            }

            /// <summary>
            /// 2つの二次元ベクトルから外積の大きさを求める
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            static float CrossFrom2Vec2(Vector2 a, Vector2 b)
            {
                return a.x * b.y - a.y * b.x;
            }

            //========================================================================================================

            public struct Line_T
            {
                public Transform start, end;

                public Line_P toLineP()
                {
                    Line_P result;
                    result.start = start.position; result.end = end.position;

                    return result;
                }
            }

            public struct Line_P
            {
                public Vector3 start, end;
            }
        }

        public class CreateMeshObject
        {
            public static GameObject Create2D(Mesh mesh, Vector2[] polygon, Transform tra, Material material, params System.Type[] components)
            {
                return Create2D(mesh, polygon, tra, "meshObject", material, components);
            }
            public static GameObject Create2D(Mesh mesh, Vector2[] polygon, Transform tra, string name, Material material, params System.Type[] components)
            {
                if (mesh.vertices.Length < 3) return null;

                var obj = new GameObject(name);
                obj.name = name;

                obj.AddComponent<MeshFilter>().mesh = mesh;
                obj.AddComponent<MeshRenderer>().material = material;
                obj.AddComponent<PolygonCollider2D>().points = polygon;

                obj.transform.position = tra.position;
                obj.transform.rotation = tra.rotation;
                obj.transform.localScale = tra.localScale;

                foreach (var comp in components) obj.AddComponent(comp);

                return obj;
            }

            static List<Vector2> Sort2DPointsToClockwise(List<Vector2> points)
            {
                return Sort2DPointsToClockwise(points.ToArray());
            }
            static List<Vector2> Sort2DPointsToClockwise(Vector2[] points)
            {
                var center = GetAveragePoint2D(points);
                var sortedPoints = new List<Vector2>();

                while (sortedPoints.Count < points.Length)
                {

                    var min = (vec: Vector2.zero, angle: 1000f);

                    foreach (var p in points)
                    {
                        if (sortedPoints.Contains(p)) continue;

                        float angle = GetAngle2D(center, p);
                        if (angle < min.angle)
                        {
                            min.vec = p;
                            min.angle = angle;
                        }
                    }

                    sortedPoints.Add(new Vector2(min.vec.x, min.vec.y));
                }

                return sortedPoints;
            }

            static Vector2 GetAveragePoint2D(Vector2[] points)
            {
                var sum = new Vector2();

                foreach (var p in points) sum += p;

                return sum / (points.Length - 1);
            }

            static float GetAngle2D(Vector2 from, Vector2 to)
            {
                var direction = to - from;
                var signed_rad = Mathf.Atan2(direction.y, direction.x);
                var signed_degree = signed_rad * Mathf.Rad2Deg;

                return (signed_degree < 0) ? (signed_degree + 360) : signed_degree;
            }

             static Vector2[] Vec3ArrayToVec2Array(Vector3[] vec3array)
            {
                return Array.ConvertAll(vec3array, (vec3) => (Vector2)vec3);
            }

            static Vector2[] LocalToWorldVec2Array(Transform tra, Vector2[] localArray)
            {
                return Array.ConvertAll(localArray, (vec2) => (Vector2)tra.transform.TransformPoint(vec2));
            }
        }
    }
}
