using System.Collections.Generic;
using UnityEngine;
using Poly2Tri;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * Contains portions of code from: Copyright (c) 2017 Sloan Kelly 
 * [Licensed under the MIT License]
 *
 * See LICENSE file for full license text.
 */

/// <summary>
/// Road infrastructure maker.
/// </summary>
internal sealed class RoadMaker : BaseInfrastructureMaker
{
    private Material asphaltMaterial = Resources.Load<Material>("Asphalt");
    private Material dirtMaterial = Resources.Load<Material>("Dirt");
    private Material pavementMaterial = Resources.Load<Material>("Pavement");
    private Material cobblestoneMaterial = Resources.Load<Material>("Cobblestone");

    private GameObject tree = Resources.Load<GameObject>("Tree9_5");
    private GameObject streetLight = Resources.Load<GameObject>("Street Light");

    private GameObject treeParent;
    private GameObject streetLightParent;
    private GameObject sidewalkParent;
    private GameObject signalParent;

    private Lane lane;
    private Intersection intersections;
    private TrafficSignal trafficSignal;
    private TrafficSign trafficSign;

    public override int NodeCount
    {
        get { return map.ways.FindAll(w => w.IsRoad).Count; }
    }

    public RoadMaker(MapReader mapReader)
        : base(mapReader)
    {
        treeParent = new GameObject("Trees");
        streetLightParent = new GameObject("Street Lights");
        sidewalkParent = new GameObject("Sidewalks");
        signalParent = new GameObject("Traffic Signs");

        lane = new Lane(map);
        intersections = new Intersection(map);
        trafficSignal = new TrafficSignal(map);
        trafficSign = new TrafficSign(map);
    }

    /// <summary>
    /// Create the roads.
    /// </summary>
    /// <returns></returns>
    public override IEnumerable<int> Process()
    {
        var parent = new GameObject("Roads");

        int count = 0;
        Material roadMaterial;

        // Iterate through the roads and build each one
        foreach (var way in map.ways.FindAll(w => w.IsRoad))
        {
            string name = "Road " + way.ID.ToString();
            if (way.Name != "")
            {
                name = way.Name + " " + way.ID.ToString();
            }
            if (way.IsDirt)
            {
                roadMaterial = dirtMaterial;
            }
            else if (way.IsPavement)
            {
                if (way.IsArea)
                {
                    roadMaterial = cobblestoneMaterial;
                }
                else
                {
                    roadMaterial = pavementMaterial;
                }
            }
            else
            {
                roadMaterial = asphaltMaterial;
            }

            CreateObject(way, roadMaterial, name, parent);

            count++;
            yield return count;
        }

        intersections.Mesh();

        MeshCollider[] roadColliders;
        MeshCollider[] intersectionColliders;

        // Manually get all MeshColliders for roads and intersections
        GameObject roadsParent = GameObject.Find("Roads");
        GameObject intersectionsParent = GameObject.Find("Intersections");
        roadColliders = roadsParent.GetComponentsInChildren<MeshCollider>();
        intersectionColliders = intersectionsParent.GetComponentsInChildren<MeshCollider>();

        // Check for collisions with roads and remove colliding objects
        Utils.RemoveCollidingObjects(treeParent.transform, roadColliders);
        Utils.RemoveCollidingObjects(streetLightParent.transform, roadColliders);
        Utils.RemoveCollidingObjects(sidewalkParent.transform, roadColliders);

        Utils.RemoveCollidingObjects(treeParent.transform, intersectionColliders);
        Utils.RemoveCollidingObjects(streetLightParent.transform, intersectionColliders);

        // Randomly initialize the bicycle pos on a road
        Bicycle.InitializeRandomPos();

        // Offset Terrain y pos for avoiding clash with road elements
        Terrain terrain = Terrain.activeTerrain;
        Vector3 terrainPos = terrain.transform.position;
        terrain.transform.position = new Vector3(terrainPos.x, terrainPos.y - 0.05f, terrainPos.z);
    }

    protected override void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices, GameObject parent)
    {
        List<Vector3> controlPoints = new List<Vector3>();
        List<OsmNode> signalNodes = new List<OsmNode>();
        List<OsmNode> crossingNodes = new List<OsmNode>();
        List<OsmNode> stopNodes = new List<OsmNode>();
        Dictionary<OsmNode, string> trafficSigns = new Dictionary<OsmNode, string>();

        for (int i = 0; i < way.NodeIDs.Count; i++)
        {
            OsmNode p = map.nodes[way.NodeIDs[i]];
            controlPoints.Add(p - origin);

            // Add signal points if node is a signal
            if (p.IsSignal)
            {
                signalNodes.Add(p);
            }
            if (p.IsCrossing)
            {
                crossingNodes.Add(p);
            }
            if (p.IsStop)
            {
                stopNodes.Add(p);
            }
            if (p.TrafficSign != null)
            {
                trafficSigns.Add(p, p.TrafficSign);
            }
        }

        // Add traffic sign node if way contains traffic sign attr
        if (way.TrafficSign != null && way.TrafficSign != "none" && !trafficSigns.ContainsKey(map.nodes[way.NodeIDs[0]]))
        {
            trafficSigns.Add(map.nodes[way.NodeIDs[0]], way.TrafficSign);
        }

        // Sort control points to ensure they are in the correct order
        // controlPoints = SortAndRemoveDuplicates(controlPoints);

        if (controlPoints.Count > 1)
        {
            if (way.IsArea)
            {
                if (IsClockwise(controlPoints))
                {
                    controlPoints.Reverse(); // Ensures the polygon is counterclockwise
                }

                // Convert Vector3 list to Vector2 for triangulation
                List<PolygonPoint> points2D = new List<PolygonPoint>();
                foreach (Vector3 point in controlPoints)
                {
                    points2D.Add(new PolygonPoint(point.x, point.z)); // Convert XZ plane to 2D
                }

                Polygon poly = new Polygon(points2D);

                // Triangulate it!
                var tcx = new DTSweepContext();
                tcx.PrepareTriangulation(poly);
                DTSweep.Triangulate(tcx);

                var codeToIndex = new Dictionary<uint, int>();
                var vertexList = new List<Vector3>();

                foreach (DelaunayTriangle t in poly.Triangles)
                {
                    foreach (var p in t.Points)
                    {
                        if (codeToIndex.ContainsKey(p.VertexCode)) continue;
                        codeToIndex[p.VertexCode] = vertexList.Count;
                        var pos = new Vector3(p.Xf, 0, p.Yf);
                        pos.y = Utils.GetTerrainAlignedPosition(pos + origin - map.bounds.Centre).y;
                        vertexList.Add(pos);
                    }
                }
                vectors.AddRange(vertexList);

                // Create the indices array
                {
                    foreach (DelaunayTriangle t in poly.Triangles)
                    {
                        indices.Add(codeToIndex[t.Points[2].VertexCode]);
                        indices.Add(codeToIndex[t.Points[1].VertexCode]);
                        indices.Add(codeToIndex[t.Points[0].VertexCode]);
                    }
                }

                // Create UV's
                for (int i = 0; i < vertexList.Count; i++)
                {
                    var v = vertexList[i];
                    uvs.Add(new Vector2(v.x, v.z));
                }

                parent.transform.position = new Vector3(parent.transform.position.x, -0.02f, parent.transform.position.z);
            }
            else
            {
                List<Vector3> leftBorderPoints = new List<Vector3>();
                List<Vector3> rightBorderPoints = new List<Vector3>();
                
                Vector3 prev_v3 = new Vector3();
                Vector3 prev_v4 = new Vector3();

                List<int> leftJunctions = new List<int>();
                List<int> rightJunctions = new List<int>();

                // Skip assigning border points if no intersection found for all points in the way
                bool addBorder = true;

                // Loop for finding border points
                for (int i = 1; i < controlPoints.Count; i++)
                {
                    Vector3 s1 = controlPoints[i - 1];
                    Vector3 s2 = controlPoints[i];

                    Vector3 diff = (s2 - s1).normalized;

                    // Always use the same reference for the cross product to ensure consistency
                    Vector3 cross = Vector3.Cross(diff, Vector3.up) * way.Width;

                    if (i == 1)
                    {
                        prev_v3 = s1 + cross;
                        prev_v4 = s1 - cross;

                        // Convert to world space and get terrain height
                        prev_v3.y = Utils.GetTerrainAlignedPosition(prev_v3 + origin - map.bounds.Centre).y;
                        prev_v4.y = Utils.GetTerrainAlignedPosition(prev_v4 + origin - map.bounds.Centre).y;
                    }

                    // Create points that represent the width of the road
                    Vector3 v1 = prev_v3;
                    Vector3 v2 = prev_v4;
                    Vector3 v3 = s2 + cross;
                    Vector3 v4 = s2 - cross;

                    // Convert to world space and get terrain height
                    v3.y = Utils.GetTerrainAlignedPosition(v3 + origin - map.bounds.Centre).y;
                    v4.y = Utils.GetTerrainAlignedPosition(v4 + origin - map.bounds.Centre).y;

                    // Find the intersections of right and left border lanes if the node is part of an intersection
                    var (rightIntersection, leftIntersection) = intersections.FindWayIntersections(way, i - 1);

                    if (rightIntersection.Count != 0)
                    {
                        // Intersection is at first node
                        if (i == 1)
                        {
                            // The way points are flipped? Check this!
                            rightBorderPoints.Add(rightIntersection[0] - origin);
                        }
                        // Intersection is at a middle node
                        else
                        {
                            rightBorderPoints.Add(rightIntersection[0] - origin);
                            rightBorderPoints.Add(rightIntersection[1] - origin);
                                
                            rightJunctions.Add(rightBorderPoints.IndexOf(rightIntersection[1] - origin));
                        }
                        addBorder = false;
                    }
                    // No intersection at current node
                    else
                    {
                        // Add the previous points to the border lists
                        rightBorderPoints.Add(prev_v3);
                    }
                    if (leftIntersection.Count != 0)
                    {
                        // Intersection is at first node
                        if (i == 1)
                        {
                            leftBorderPoints.Add(leftIntersection[0] - origin);
                        }
                        // Intersection is at a middle node
                        else
                        {
                            leftBorderPoints.Add(leftIntersection[0] - origin);
                            leftBorderPoints.Add(leftIntersection[1] - origin);

                            leftJunctions.Add(leftBorderPoints.IndexOf(leftIntersection[1] - origin));
                        }
                        addBorder = false;
                    }
                    else
                    {
                        leftBorderPoints.Add(prev_v4);
                    }

                    if (i == controlPoints.Count - 1)
                    {
                        (rightIntersection, leftIntersection) = intersections.FindWayIntersections(way, i);

                        // Intersection is at last node
                        if (rightIntersection.Count!=0)
                        {
                            rightBorderPoints.Add(rightIntersection[0] - origin);
                            addBorder = false;
                        }
                        else
                        {
                            // Add the current points to the border lists
                            rightBorderPoints.Add(v3);
                        }

                        if (leftIntersection.Count != 0)
                        {
                            leftBorderPoints.Add(leftIntersection[0] - origin);
                            addBorder = false;
                        }
                        else
                        {
                            leftBorderPoints.Add(v4);
                        }
                    }

                    prev_v3 = v3;
                    prev_v4 = v4;
                }

                List<Vector3> interpolatedPoints = new List<Vector3>();
                int interpolationSteps = controlPoints.Count * 5;

                // Create evenly spaced points for meshing
                float meshSpacing = 10f;
                float wayExtend = way.IsOneway ? 0f : 0.5f;
                interpolatedPoints = Utils.CreateEvenlySpacedPoints(controlPoints, meshSpacing, true);

                /*
                if (controlPoints.Count == 2)
                {
                    // Interpolate linearly between two points
                    for (int j = 0; j <= interpolationSteps; j++)
                    {
                        float t = j / (float)interpolationSteps;
                        Vector3 point = Vector3.Lerp(controlPoints[0], controlPoints[1], t);
                        interpolatedPoints.Add(point);
                    }
                }
                else if (controlPoints.Count == 3)
                {
                    // Use a quadratic Bézier curve
                    for (int j = 0; j <= interpolationSteps; j++)
                    {
                        float t = j / (float)interpolationSteps;
                        Vector3 point = QuadraticBezier(controlPoints[0], controlPoints[1], controlPoints[2], t);
                        interpolatedPoints.Add(point);
                    }
                }
                else if (controlPoints.Count >= 4)
                {
                    // Use cubic Bézier curves, processing control points in groups of four
                    for (int i = 0; i < controlPoints.Count - 3; i += 3)
                    {
                        Vector3 p0 = controlPoints[i];
                        Vector3 p1 = controlPoints[i + 1];
                        Vector3 p2 = controlPoints[i + 2];
                        Vector3 p3 = controlPoints[i + 3];

                        for (int j = 0; j <= interpolationSteps; j++)
                        {
                            float t = j / (float)interpolationSteps;
                            Vector3 point = CubicBezier(p0, p1, p2, p3, t);
                            interpolatedPoints.Add(point);
                        }
                    }
                }
                */

                // Add the last point for continuity
                // interpolatedPoints.Add(controlPoints[controlPoints.Count - 1]);

                // Remove duplicate points (points with the same position)
                // interpolatedPoints = interpolatedPoints.Distinct().ToList();

                // Clear the border lists
                if (addBorder)
                {
                    rightBorderPoints.Clear();
                    leftBorderPoints.Clear();
                }

                // Loop for finding road mesh points and (optionally) border points
                for (int i = 1; i < interpolatedPoints.Count; i++)
                {
                    Vector3 s1 = interpolatedPoints[i - 1];
                    Vector3 s2 = interpolatedPoints[i];

                    Vector3 diff = (s2 - s1).normalized;

                    // Always use the same reference for the cross product to ensure consistency
                    Vector3 cross = Vector3.Cross(diff, Vector3.up) * (way.Width + wayExtend);

                    if (i == 1)
                    {
                        prev_v3 = s1 + cross;
                        prev_v4 = s1 - cross;

                        // Convert to world space and get terrain height
                        prev_v3.y = Utils.GetTerrainAlignedPosition(prev_v3 + origin - map.bounds.Centre).y;
                        prev_v4.y = Utils.GetTerrainAlignedPosition(prev_v4 + origin - map.bounds.Centre).y;
                    }

                    // Create points that represent the width of the road
                    Vector3 v1 = prev_v3;
                    Vector3 v2 = prev_v4;
                    Vector3 v3 = s2 + cross;
                    Vector3 v4 = s2 - cross;

                    // Convert to world space and get terrain height
                    v3.y = Utils.GetTerrainAlignedPosition(v3 + origin - map.bounds.Centre).y;
                    v4.y = Utils.GetTerrainAlignedPosition(v4 + origin - map.bounds.Centre).y;

                    vectors.Add(v1);
                    vectors.Add(v2);
                    vectors.Add(v3);
                    vectors.Add(v4);

                    // Assign UV coordinates
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));

                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);

                    int idx1, idx2, idx3, idx4;
                    idx4 = vectors.Count - 1;
                    idx3 = vectors.Count - 2;
                    idx2 = vectors.Count - 3;
                    idx1 = vectors.Count - 4;

                    // first triangle v1, v3, v2
                    indices.Add(idx1);
                    indices.Add(idx3);
                    indices.Add(idx2);

                    // second triangle v3, v4, v2
                    indices.Add(idx3);
                    indices.Add(idx4);
                    indices.Add(idx2);

                    if (addBorder)
                    {
                        rightBorderPoints.Add(prev_v3);
                        leftBorderPoints.Add(prev_v4);

                        if (i == interpolatedPoints.Count - 1)
                        {
                            rightBorderPoints.Add(v3);
                            leftBorderPoints.Add(v4);
                        }
                    }

                    prev_v3 = v3;
                    prev_v4 = v4;
                }

                // Convert all the points to global coords and evenly place
                var centrePoints = LocalToGlobal(interpolatedPoints, origin);
                rightBorderPoints = LocalToGlobal(rightBorderPoints, origin);
                leftBorderPoints = LocalToGlobal(leftBorderPoints, origin);

                // rightBorderPoints = Utils.CreateEvenlySpacedPoints(LocalToGlobal(rightBorderPoints, origin), 1f);
                // leftBorderPoints = Utils.CreateEvenlySpacedPoints(LocalToGlobal(leftBorderPoints, origin), 1f);

                if (!(way.IsDirt || way.IsPavement || way.IsCrossing))
                {
                    // Place traffic signals and add stop line
                    var intersectionPoints = new List<Vector3>();
                    var centreLane = centrePoints;
                    if (signalNodes.Count != 0)
                    {
                        for (int i = 0; i < signalNodes.Count; i++)
                        {
                            var intersectingPoints = trafficSignal.SignalPlacer(way, signalNodes[i], signalParent);
                            (centrePoints, intersectionPoints) = Utils.CutoffIntersection(intersectingPoints, Utils.CreateEvenlySpacedPoints(centreLane, 1f), false);
                        }
                    }

                    if (crossingNodes.Count != 0)
                    {
                        for (int i = 0; i < crossingNodes.Count; i++)
                        {
                            var intersectingPoints = trafficSignal.CrossingPlacer(way, crossingNodes[i], signalParent);
                            (_, intersectionPoints) = Utils.CutoffIntersection(intersectingPoints, intersectionPoints, false);
                        }
                    }

                    if (stopNodes.Count != 0)
                    {
                        for (int i = 0; i < stopNodes.Count; i++)
                        {
                            var intersectingPoints = trafficSignal.StopPlacer(way, stopNodes[i], signalParent);
                            (_, intersectionPoints) = Utils.CutoffIntersection(intersectingPoints, intersectionPoints, false);
                        }
                    }

                    if (trafficSigns.Count != 0)
                    {
                        trafficSign.SignsPlacer(way, trafficSigns, signalParent);
                    }

                    // Create lane markings for this way
                    // Skip lane markings if way type is excluded 
                    if (way.IsLaneMarking)
                    {
                        lane.CreateLaneMarkings(way, centreLane, Utils.CreateEvenlySpacedPoints(rightBorderPoints, meshSpacing, true), Utils.CreateEvenlySpacedPoints(leftBorderPoints, meshSpacing, true), centrePoints, intersectionPoints, parent);
                    }

                    if (centrePoints.Count > 1)
                    {
                        // Evenly place trees and street lights
                        List<GameObject> gameObjects = new List<GameObject>(2);
                        gameObjects.Add(tree);
                        gameObjects.Add(streetLight);

                        List<GameObject> parents = new List<GameObject>(2);
                        parents.Add(treeParent);
                        parents.Add(streetLightParent);

                        // Set tree and streetlight spacing and offset here
                        ObjectPlacer.EvenlyPlaceObjects(gameObjects, centrePoints, 10f, way.Width + 1.5f, parents);

                        if (!string.IsNullOrEmpty(way.Sidewalk))
                        {
                            // Handle meshing of sidewalk at mid junctions
                            if (rightJunctions.Count > 0)
                            {
                                // Debug.Log(string.Join(", ", rightJunctions));
                                var splitBorderPoints = Utils.SplitAtIndices(rightBorderPoints, rightJunctions);

                                for (int i = 0; i < splitBorderPoints.Count; i++)
                                {
                                    Sidewalk.Mesh(splitBorderPoints[i], origin - map.bounds.Centre, sidewalkParent);
                                }
                            }
                            else if (way.Sidewalk == "both" || way.Sidewalk == "left")
                            {
                                Sidewalk.Mesh(rightBorderPoints, origin - map.bounds.Centre, sidewalkParent);
                            }

                            if (leftJunctions.Count > 0)
                            {
                                // Debug.Log(string.Join(", ", leftJunctions));
                                var splitBorderPoints = Utils.SplitAtIndices(leftBorderPoints, leftJunctions);

                                for (int i = 0; i < splitBorderPoints.Count; i++)
                                {
                                    Sidewalk.Mesh(splitBorderPoints[i], origin - map.bounds.Centre, sidewalkParent, true);
                                }
                            }
                            else if (way.Sidewalk == "both" || way.Sidewalk == "right")
                            {
                                Sidewalk.Mesh(leftBorderPoints, origin - map.bounds.Centre, sidewalkParent, true);
                            }
                        }
                    }
                }
                // Layer road based on roadMaterial
                else if (way.IsPavement)
                {
                    parent.transform.position = new Vector3(parent.transform.position.x, -0.02f, parent.transform.position.z);
                }
                else if (way.IsDirt)
                {
                    parent.transform.position = new Vector3(parent.transform.position.x, -0.01f, parent.transform.position.z);
                }
            }
        }

        // Align with the terrain height
        // parent.transform.position = Utils.GetTerrainAlignedPosition(parent.transform.position);
    }

    private List<Vector3> LocalToGlobal(List<Vector3> points, Vector3 origin)
    {
        List<Vector3> globalPoints = new List<Vector3>();
        points.ForEach(x => {
            globalPoints.Add(x + origin - map.bounds.Centre);
        });

        return globalPoints;
    }

    public static bool IsClockwise(List<Vector3> points)
    {
        float sum = 0;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[(i + 1) % points.Count]; // Wrap around to the first point

            sum += (p2.x - p1.x) * (p2.z + p1.z);
        }

        return sum > 0; // CW if sum > 0, CCW if sum < 0
    }
}
