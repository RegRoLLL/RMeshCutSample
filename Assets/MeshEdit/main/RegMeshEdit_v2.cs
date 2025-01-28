using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace RegUtil
{
    namespace RegMeshEdit_v2
    {
        public class MeshCut
        {
            public static List<GameObject> Cut(GameObject obj, Vector3 lineStart, Vector3 lineEnd)
            {
                var line = new Line_P() { start = lineStart, end = lineEnd };
                var results = new List<GameObject>();

                var polygon = obj.GetComponent<PolygonCollider2D>();
                var polygons = CutPolygon(polygon, line);

                foreach (var part in polygons)
                {
                    var obj_ = CreateMeshObject.Create2D(PolygonToMesh(part),
                                                         part.ToArray(),
                                                         obj.transform,
                                                         obj.GetComponent<MeshRenderer>().material);
                    results.Add(obj_);
                }

                GameObject.Destroy(obj);

                return results;
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

                    //反時計回りの場合、配列を反転させる
                    if (IsClockWise(positions[0], positions[1], positions[2])) Array.Reverse(positions);

                    foreach (var pos in positions)
                    {
                        if (!verticles.Contains(pos)) verticles.Add(pos);

                        indexes.Add(verticles.IndexOf(pos));
                    }
                }

                public Mesh toMesh()
                {
                    var mesh = new Mesh();

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
            /// ※線の終点は図形内でもいいが線の始点は必ず図形外であること
            /// </summary>
            /// <param name="polygonCol">PolygonCollider2D</param>
            /// <param name="line">線分</param>
            /// <returns>pointsにあたるVector2の二次元リスト</returns>
            static public List<List<Vector2>> CutPolygon(PolygonCollider2D polygonCol, Line_P line)
            {
                var tra = polygonCol.transform;
                var resultsLocal = new List<List<Vector2>>();

                //ワールド座標に変換した頂点データ配列
                var polygonWorld = Array.ConvertAll(polygonCol.points, (vec2) => (Vector2)tra.TransformPoint(vec2));


                var crossingPairIndexList = new List<(int a, int b, int count)>();
                var polygonWorldWithCrossingPoints = new List<Vector2>();
                var crossingPointsIndex = new List<int>();


                var side = new Line_P();//ひとつ前の点→現在の点　の線分
                for (int i = 0; i < polygonWorld.Length; i++)
                {
                    side.end = polygonWorld[i];
                    side.start = (i == 0) ? polygonWorld[polygonWorld.Length - 1] : polygonWorld[i - 1];

                    if (IsCrossing_Separate(line, side))
                    {
                        polygonWorldWithCrossingPoints.Add(GetCrossingPoint(line, side));
                        crossingPointsIndex.Add(polygonWorldWithCrossingPoints.Count - 1);
                    }
                    //交点は前の点から今の点の間にあるので[交点→今の点]の順で追加すべき
                    polygonWorldWithCrossingPoints.Add(polygonWorld[i]);
                }//交点を挿入した頂点配列と交点のインデックス番号リストを作成

                Debug.Log("処理頂点数：" + polygonWorldWithCrossingPoints.Count);
                Debug.Log("交点数：" + crossingPointsIndex.Count);
                Debug.Log(string.Join(",", crossingPointsIndex.Select(n => n.ToString())));

                if (crossingPointsIndex.Count == 0)
                {
                    resultsLocal.Add(new List<Vector2>(polygonCol.points));
                    return resultsLocal;
                }//交差しない場合


                Func<float, float, int> CompareFloat = (a, b) => a.CompareTo(b);
                Func<int, float> GetLerpValue = (targetIndex)
                    => Vector2.Distance(line.start, polygonWorldWithCrossingPoints[targetIndex]) / Vector2.Distance(line.start, line.end);
                //交点インデックス番号リストを切断線上のstart→endの順に並べ替える
                crossingPointsIndex.Sort((a, b) => CompareFloat(GetLerpValue(a), GetLerpValue(b)));

                if (crossingPointsIndex.Count % 2 != 0) crossingPointsIndex.RemoveAt(crossingPointsIndex.Count - 1);

                //交点のペア(図形に入出の２点)をつくる
                for (int i = 0; i < crossingPointsIndex.Count; i += 2)
                    crossingPairIndexList.Add((crossingPointsIndex[i], crossingPointsIndex[i+1], 0));

                Debug.Log(string.Join(",", crossingPairIndexList.Select(n => n.ToString())));


                var resultsWorld = new List<List<Vector2>>();
                var currentResult = new List<Vector2>();
                var clearIndex = new List<int>();//処理済みの頂点のインデックス番号リスト
                Func<int, (int a,int b,int count)> FindPair = (n) =>
                 {
                     for (int i=0; i<crossingPairIndexList.Count;i++)
                     {
                         var point = crossingPairIndexList[i];

                         if (point.a == n || point.b == n)
                         {
                             //Debug.Log("pair founded." + point);
                             return point;
                         }
                     }

                     //Debug.Log("pair not founded: "+n);

                     return (-1,-1,-1);
                 };
                Func<int, int> GetOtherSidePair = (n) =>
                 {
                     for (int i=0; i<crossingPairIndexList.Count;i++)
                     {
                         var point = crossingPairIndexList[i];

                         if (point.a == n || point.b == n)
                         {
                             crossingPairIndexList[i] = (point.a, point.b, point.count + 1);

                             Debug.Log("pair otherside founded. "+ n + "=>" + crossingPairIndexList[i]);

                             if (point.a == n) return point.b;
                             if (point.b == n) return point.a;
                         }
                     }

                     Debug.Log("pair otherside not founded.");

                     return -1;
                 };
                Func<bool> clossingPairRemaining = () =>
                {
                    bool remains = false;

                    foreach (var sLine in crossingPairIndexList)
                    {
                        if (sLine.count < 2){
                            remains = true;
                            break;
                        }//切断時、ペアは必ず最終的に2回使われる
                    }
                    return remains;
                };


                int loopCount = 0;
                bool lastLoopWentOtherSideOfPair = false;//前回の時にペア間移動をしたか

                //for (int i = 0; clossingPairRemaining();)
                for (int i = 0; clearIndex.Count < polygonWorldWithCrossingPoints.Count;)
                {

                    var point = polygonWorldWithCrossingPoints[i];
                    var pair = FindPair(i);

                    Debug.Log(i+" , "+point+"\r\n"+Time.deltaTime);

                    //輪がつながったときの処理
                    if (currentResult.Contains(point))
                    {
                        Debug.Log(i + " is contain in currentresult.");

                        if (currentResult.Count >= 3)
                        {
                            resultsWorld.Add(new List<Vector2>(currentResult));
                            Debug.LogWarning("result" + resultsWorld.Count + ": " + string.Join(",", currentResult.Select(n => n.ToString())));
                        }

                        currentResult = new List<Vector2>();
                    }


                    currentResult.Add(point);


                    if (loopCount++ >= 1000) { Debug.LogError("endress detected."); break; }



                    if (!lastLoopWentOtherSideOfPair && pair.count>=0 && pair.count< 2 && currentResult.Count != 1)//交点だった場合
                    {
                        i = GetOtherSidePair(i);
                        lastLoopWentOtherSideOfPair = true;
                    }
                    else
                    {
                        if(!crossingPointsIndex.Contains(i))
                            if(!clearIndex.Contains(i))clearIndex.Add(i);

                        if (pair.count >= 2)
                        {
                            Debug.Log("pair cleared." + pair);
                            //Debug.DrawLine(polygonWorldWithCrossingPoints[pair.a], polygonWorldWithCrossingPoints[pair.b], Color.cyan,1000f,false);

                            if (!clearIndex.Contains(pair.a)) clearIndex.Add(pair.a);
                            if (!clearIndex.Contains(pair.b)) clearIndex.Add(pair.b);
                            crossingPointsIndex.Remove(pair.a);
                            crossingPointsIndex.Remove(pair.b);
                            crossingPairIndexList.Remove(pair);

                        }//２回使ったペアを除外する

                        

                        if (clearIndex.Count < polygonWorldWithCrossingPoints.Count)
                        {
                            do
                            {
                                i++;
                                if (i >= polygonWorldWithCrossingPoints.Count) i = 0;//インデックスをループさせる処理

                                point = polygonWorldWithCrossingPoints[i];

                            } while (clearIndex.Contains(i));//処理済みの頂点はスルー
                        }

                        if(!crossingPointsIndex.Contains(i)) lastLoopWentOtherSideOfPair = false;
                    }

                    
                }//切り分ける

                if (currentResult.Count >= 3)
                {
                    resultsWorld.Add(new List<Vector2>(currentResult));
                    Debug.LogWarning("result" + resultsWorld.Count + ": " + string.Join(",", currentResult.Select(n => n.ToString())));
                }

                clearIndex.Sort();
                Debug.LogWarning("clears: " + string.Join(",", clearIndex.Select(n => n.ToString())));
                Debug.LogWarning("clearsCount: " + clearIndex.Count);
                Debug.LogWarning("polygonCount: "+polygonWorldWithCrossingPoints.Count);


                foreach (var part in resultsWorld)
                {
                    var localArray = Array.ConvertAll(part.ToArray(), (world) => (Vector2)tra.InverseTransformPoint(world));
                    resultsLocal.Add(new List<Vector2>(localArray));
                }//ローカル座標に変換

                return resultsLocal;
            }

            /// <summary>
            /// 多角形を三角形の集合に分割しmeshにして返す
            /// </summary>
            /// <param name="polygon">多角形の頂点リスト(時計回り)</param>
            static public Mesh PolygonToMesh(List<Vector2> polygon)
            {
                var list = new List<Vector2>(polygon);
                var meshdata = new MeshDataLists();

                var origin = Vector2.zero;
                while (list.Count > 2)
                {
                    var far = (index: -1, distance: 0f);
                    foreach (var point in list)
                    {
                        float distance = Vector2.Distance(origin, point);
                        if (distance > far.distance)
                            far = (list.IndexOf(point), distance);
                    }//最も原点から遠い点を探す

                    var (a, b, c) = (Vector2.zero, Vector2.zero, Vector2.zero);
                    Action SetTriangle = () =>
                    {
                        //Debug.Log("far.index=" + far.index + "  list.count=" + list.Count);

                        if (far.index >= list.Count)
                        {
                            far.index = 0;
                            (a, b, c) = (list[list.Count - 1], list[far.index], list[far.index + 1]);
                        }
                        else if (far.index == list.Count - 1)
                        {
                            (a, b, c) = (list[far.index - 1], list[far.index], list[0]);
                        }
                        else if(far.index <= 0)
                        {
                            (a, b, c) = (list[list.Count - 1], list[far.index], list[far.index + 1]);
                        }
                        else (a, b, c) = (list[far.index - 1], list[far.index], list[far.index + 1]); 
                    };
                    SetTriangle();
                    var lastTriangleDirection = CrossFrom2Vec2(a - b, c - b) > 0;

                    int loopCount = 0;
                    while (true)
                    {
                        bool noPointsInTriangle = true;

                        foreach (var point in list)
                        {
                            if (IsinTriangle(a, b, c, point))
                            {
                                noPointsInTriangle = false;

                                lastTriangleDirection = CrossFrom2Vec2(a - b, c - b) > 0;

                                far.index++;
                                SetTriangle();

                                break;
                            }//三角形に内包点がある場合、採用点を1つずらす
                        }//三角形の中に内包点がない場合、そのまま続行

                        //採用点をずらした場合その採用点のまま次のループに入る(noPointsInTriangleはfalseになっているため)

                        if (noPointsInTriangle)
                        {
                            if (lastTriangleDirection == CrossFrom2Vec2(a - b, c - b) > 0) { 
                                Debug.Log("三角形確定(loop:"+loopCount+")");
                                break; 
                            }
                            
                            //Debug.Log("no points in triangle(loop:"+loopCount+")");
                        }

                        if (loopCount++ >= polygon.Count + 5)
                        {
                            Debug.LogError("無限ループに陥ったため強制的に処理を中断しました");
                            break;
                        }

                    }//内包点チェック→三角形確定

                    meshdata.AddTriangle(a, b, c);
                    list.RemoveAt(far.index);
                }

                return meshdata.toMesh();
            }

            //========================================================================================================

            static public bool IsinTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 target)
            {
                var abt = IsClockWise(a, b, target);
                var bct = IsClockWise(b, c, target);
                var cat = IsClockWise(c, a, target);

                return (abt == bct && bct == cat);
            }

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
        }
    }
}