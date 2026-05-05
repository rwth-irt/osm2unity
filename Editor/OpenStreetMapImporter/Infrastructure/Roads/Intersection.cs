using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

internal class Intersection
{
    private MapReader map;
    private Material asphaltMaterial = Resources.Load<Material>("Asphalt");
    private Dictionary<ulong, List<OsmWay>> NodeWays
    {
        get
        {
            var nodeWays = new Dictionary<ulong, List<OsmWay>>();
            foreach (var way in map.ways.FindAll(w => w.IsRoad))
            {
                foreach (ulong nodeID in way.NodeIDs)
                {
                    if (!nodeWays.ContainsKey(nodeID))
                    {
                        nodeWays[nodeID] = new List<OsmWay>();
                    }
                    nodeWays[nodeID].Add(way);
                }
            }
            return nodeWays;
        }
    }
    private Dictionary<ulong, List<Vector3>> IntersectionWays = new Dictionary<ulong, List<Vector3>>();

    public Intersection(MapReader map)
    {
        this.map = map;
    }

    public (List<Vector3>, List<Vector3>) FindWayIntersections(OsmWay way, int nodeIdx)
    {
        // Find the intersection points for the given node in the way

        var rightIntersection = new List<Vector3>();
        var leftIntersection = new List<Vector3>();

        if (!(way.IsDirt || way.IsPavement || way.IsCrossing))
        {
            CheckNodeIntersections(way, nodeIdx, rightIntersection, leftIntersection);
        }

        /*
        // Add the intersecting point to mesh the intersection corner
        if ((nodeIdx == 0 || nodeIdx == way.NodeIDs.Count - 1) && isIntersection && !string.IsNullOrEmpty(way.Sidewalk) && way.Sidewalk != "no" && way.Sidewalk != "none")
        {
            if (rightIntersection.Count > 0 && leftIntersection.Count > 0)
            {
                AddIntersectionPoint(way.NodeIDs[nodeIdx], rightIntersection[0] - map.bounds.Centre);
                AddIntersectionPoint(way.NodeIDs[nodeIdx], leftIntersection[0] - map.bounds.Centre);
            }
        }
        else if (!isIntersection && !string.IsNullOrEmpty(way.Sidewalk) && way.Sidewalk != "no" && way.Sidewalk != "none")
        {
            if (leftIntersection.Count == 2)
            {
                AddIntersectionPoint(way.NodeIDs[nodeIdx], leftIntersection[0] - map.bounds.Centre);
                AddIntersectionPoint(way.NodeIDs[nodeIdx], leftIntersection[1] - map.bounds.Centre);
            }
            if (rightIntersection.Count == 2)
            {
                // if (way.Name == "Sedanstra�e") { Debug.Log(way.NodeIDs[i]); }
                AddIntersectionPoint(way.NodeIDs[nodeIdx], rightIntersection[0] - map.bounds.Centre);
                AddIntersectionPoint(way.NodeIDs[nodeIdx], rightIntersection[1] - map.bounds.Centre);
            }
        }
        */

        /*
        Debug.Log(rightIntersections.Count);
        Debug.Log(leftIntersections.Count);
        Debug.Log("-----------");
        */

        return (rightIntersection, leftIntersection);
    }

    private void CheckNodeIntersections(OsmWay way, int nodeIndex, List<Vector3> rightIntersections, List<Vector3> leftIntersections)
    {
        ulong currNode = way.NodeIDs[nodeIndex];

        if (!NodeWays.TryGetValue(currNode, out var junctionWays) || junctionWays.Count < 2) return;

        // Vector pointing towards the intersection
        var points = FindWayPoints(way, nodeIndex);
        var (rightPoints, leftPoints, _) = Utils.OffsetPoints(points, way.Width);

        // Calculate the direction of the current way
        Vector3 wayDirection = (points[1] - points[0]).normalized;

        // Sort and filter junctionWays by signed angle relative to the current way direction
        var sortedWays = junctionWays
            .Where(_way => _way != way && !(_way.IsDirt || _way.IsPavement || _way.IsCrossing) && !(_way.Lanes == 1 && way.Lanes == 1)) // Exclude the current way and other ways without asphalt surface and intersection b/w one lane ways
            .Select(_way =>
            {
                var otherPoints = FindWayPoints(_way, _way.NodeIDs.FindIndex(id => id == currNode));
                Vector3 otherDirection = (otherPoints[1] - otherPoints[0]).normalized;

                // Calculate signed angle
                float angle = Vector3.SignedAngle(wayDirection, otherDirection, Vector3.up);
                return (angle, _way);
            })
            .Where(t => Mathf.Abs(t.angle) > 30 && Mathf.Abs(t.angle) < 150) // Ensure the intersection is not between two consecutive ways
            .OrderBy(t => t.angle) // Sort by signed angle
            .ToList();

        float cutoff = 5f;

        if (sortedWays.Count != 0)
        {
            // Find the least positive angle and the least negative angle ways
            var positiveCandidates = sortedWays.Where(t => t.angle > 0).ToList();
            var negativeCandidates = sortedWays.Where(t => t.angle < 0).ToList();

            var leastPositiveWay = positiveCandidates.FirstOrDefault();
            var leastNegativeWay = negativeCandidates.LastOrDefault();

            // var leastPositiveWay = sortedWays.FirstOrDefault(t => t.angle > 0);
            // var leastNegativeWay = sortedWays.LastOrDefault(t => t.angle < 0);
            /*
            if (way.ID == 97827085)
            {
                Debug.Log($"Sorted count: {sortedWays.Count}");
                Debug.Log($"Least Positive Way: angle={leastPositiveWay.angle}, _way={(leastPositiveWay._way != null ? leastPositiveWay._way.ToString() : "null")}");
                Debug.Log($"Least Negative Way: angle={leastNegativeWay.angle}, _way={(leastNegativeWay._way != null ? leastNegativeWay._way.ToString() : "null")}");
            }*/

            // In cases where there is only one adjacent way
            if (sortedWays.Count == 1)
            {
                var otherNodeIdx = sortedWays[0]._way.NodeIDs.IndexOf(way.NodeIDs[nodeIndex]);

                // Current road joins another road midway
                if (otherNodeIdx != 0 && otherNodeIdx != sortedWays[0]._way.NodeIDs.Count - 1)
                {
                    // Set both positve and negative ways equal
                    if (leastPositiveWay._way == null) leastPositiveWay = leastNegativeWay;
                    else if (leastNegativeWay._way == null) leastNegativeWay = leastPositiveWay;
                }

                /*
                var adjWay = sortedWays[0];
                if (adjWay.angle > 0)
                {
                    leastPositiveWay = adjWay;
                }
                else
                {
                    leastNegativeWay = adjWay;
                }*/
            }

            // Intersection is at the start of the way
            if (nodeIndex == 0)
            {
                if (leastPositiveWay._way != null)
                {
                    if (!string.IsNullOrEmpty(leastPositiveWay._way.Sidewalk) && leastPositiveWay._way.Sidewalk != "no" && leastPositiveWay._way.Sidewalk != "none")
                    {
                        leftIntersections.Add(rightPoints[1] - Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, cutoff, wayDirection));
                        AddIntersectionPoint(way.NodeIDs[nodeIndex], leftIntersections[0] - map.bounds.Centre);
                    }
                    else
                    {
                        leftIntersections.Add(rightPoints[1] - Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, 0f, wayDirection));
                    }
                }

                if (leastNegativeWay._way != null)
                {
                    if (!string.IsNullOrEmpty(leastNegativeWay._way.Sidewalk) && leastNegativeWay._way.Sidewalk != "no" && leastNegativeWay._way.Sidewalk != "none")
                    {
                        rightIntersections.Add(leftPoints[1] - Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, cutoff, wayDirection));
                        AddIntersectionPoint(way.NodeIDs[nodeIndex], rightIntersections[0] - map.bounds.Centre);
                    }
                    else
                    {
                        rightIntersections.Add(leftPoints[1] - Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, 0f, wayDirection));
                    }
                }
            }
            // Intersection is at the end of the way
            else if (nodeIndex == way.NodeIDs.Count - 1)
            {
                if (leastPositiveWay._way != null)
                {
                    if (!string.IsNullOrEmpty(leastPositiveWay._way.Sidewalk) && leastPositiveWay._way.Sidewalk != "no" && leastPositiveWay._way.Sidewalk != "none")
                    {
                        rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, cutoff, wayDirection));
                        AddIntersectionPoint(way.NodeIDs[nodeIndex], rightIntersections[0] - map.bounds.Centre);
                    }
                    else
                    {
                        rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, 0f, wayDirection));
                    }
                }

                if (leastNegativeWay._way != null)
                {
                    if (!string.IsNullOrEmpty(leastNegativeWay._way.Sidewalk) && leastNegativeWay._way.Sidewalk != "no" && leastNegativeWay._way.Sidewalk != "none")
                    {
                        leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, cutoff, wayDirection));
                        AddIntersectionPoint(way.NodeIDs[nodeIndex], leftIntersections[0] - map.bounds.Centre);
                    }
                    else
                    {
                        leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, 0f, wayDirection));
                    }
                }
            }
            // Other roads joins current road
            else
            {
                if (leastPositiveWay._way != null)
                {
                    if (!string.IsNullOrEmpty(leastPositiveWay._way.Sidewalk) && leastPositiveWay._way.Sidewalk != "no" && leastPositiveWay._way.Sidewalk != "none")
                    {
                        rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, cutoff, wayDirection));
                        rightIntersections.Add(rightPoints[1] + Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, cutoff, wayDirection));

                        AddIntersectionPoint(way.NodeIDs[nodeIndex], rightIntersections[0] - map.bounds.Centre);
                        AddIntersectionPoint(way.NodeIDs[nodeIndex], rightIntersections[1] - map.bounds.Centre);
                    }
                    else
                    {
                        rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, 0f, wayDirection));
                        rightIntersections.Add(rightPoints[1] + Utils.ComputeOffset(leastPositiveWay.angle, leastPositiveWay._way.Width, way.Width, 0f, wayDirection));
                    }
                }

                if (leastNegativeWay._way != null)
                {
                    if (!string.IsNullOrEmpty(leastNegativeWay._way.Sidewalk) && leastNegativeWay._way.Sidewalk != "no" && leastNegativeWay._way.Sidewalk != "none")
                    {
                        leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, cutoff, wayDirection));
                        leftIntersections.Add(leftPoints[1] + Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, cutoff, wayDirection));

                        AddIntersectionPoint(way.NodeIDs[nodeIndex], leftIntersections[0] - map.bounds.Centre);
                        AddIntersectionPoint(way.NodeIDs[nodeIndex], leftIntersections[1] - map.bounds.Centre);
                    }
                    else
                    {
                        leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, 0f, wayDirection));
                        leftIntersections.Add(leftPoints[1] + Utils.ComputeOffset(leastNegativeWay.angle, leastNegativeWay._way.Width, way.Width, 0f, wayDirection));
                    }
                }
            }

            /*
            else if (sortedWays.Count == 1)
            {
                var otherNodeIdx = sortedWays[0]._way.NodeIDs.FindIndex(id => id == currNode);

                var adjWay = sortedWays[0];

                // If the intersection is at a crossroad
                // Another road joins current road
                if (nodeIndex != 0 && nodeIndex != way.NodeIDs.Count - 1)
                {
                    // Add 2 right or left intersection points
                    if (adjWay.angle > 0)
                    {
                        if (!string.IsNullOrEmpty(adjWay._way.Sidewalk))
                        {
                            rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, cutoff, wayDirection));
                            rightIntersections.Add(rightPoints[1] + Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, cutoff, wayDirection));

                            AddIntersectionPoint(way.NodeIDs[nodeIndex], rightIntersections[0] - map.bounds.Centre);
                            AddIntersectionPoint(way.NodeIDs[nodeIndex], rightIntersections[1] - map.bounds.Centre);
                        }
                        else
                        {
                            rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, 0f, wayDirection));
                            rightIntersections.Add(rightPoints[1] + Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, 0f, wayDirection));
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(adjWay._way.Sidewalk))
                        {
                            leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, cutoff, wayDirection));
                            leftIntersections.Add(leftPoints[1] + Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, cutoff, wayDirection));

                            AddIntersectionPoint(way.NodeIDs[nodeIndex], leftIntersections[0] - map.bounds.Centre);
                            AddIntersectionPoint(way.NodeIDs[nodeIndex], leftIntersections[1] - map.bounds.Centre);
                        }
                        else
                        {
                            leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, 0f, wayDirection));
                            leftIntersections.Add(leftPoints[1] + Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, 0f, wayDirection));
                        }
                    }
                }
                // if (otherNodeIdx != 0 && otherNodeIdx != sortedWays[0]._way.NodeIDs.Count - 1)
                // Current road joins another road
                else
                {
                    rightIntersections.Add(rightPoints[1] - Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, cutoff, wayDirection));
                    leftIntersections.Add(leftPoints[1] - Utils.ComputeOffset(adjWay.angle, adjWay._way.Width, way.Width, cutoff, wayDirection));
                }
            }
            */
        }
    }
    
    private void AddIntersectionPoint(ulong nodeID, Vector3 point)
    {
        // Add the intersection point to the dictionary
        if (!IntersectionWays.ContainsKey(nodeID))
        {
            IntersectionWays[nodeID] = new List<Vector3>();
        }

        // Add the last point in the points list to the IntersectionWays dictionary
        IntersectionWays[nodeID].Add(point);
    }

    private List<Vector3> FindWayPoints(OsmWay way, int nodeIdx)
    {
        var points = new List<Vector3>();
        ulong currNode = way.NodeIDs[nodeIdx];

        // Vector should always point towards the intersection
        if (nodeIdx == 0)
        {
            points.Add(map.nodes[way.NodeIDs[nodeIdx + 1]]);
            points.Add(map.nodes[currNode]);
        }
        else if (nodeIdx == way.NodeIDs.Count - 1)
        {
            points.Add(map.nodes[way.NodeIDs[nodeIdx - 1]]);
            points.Add(map.nodes[currNode]);
        }
        else // Node is at a crossroad
        {
            /*
            Vector3 prevPoint = map.nodes[way.NodeIDs[nodeIdx - 1]];
            Vector3 nextPoint = map.nodes[way.NodeIDs[nodeIdx + 1]];

            points.Add(prevPoint);
            points.Add(nextPoint);
            */

            points.Add(map.nodes[way.NodeIDs[nodeIdx - 1]]);
            points.Add(map.nodes[currNode]);
        }

        return points;
    }

    public void Mesh()
    {
        var parent = new GameObject("Intersections");
        // var lineParent = new GameObject("Markings");
        // lineParent.transform.parent = parent.transform;

        foreach (var intersection in IntersectionWays)
        {
            var groupedPoints = GroupClosestVectors(map.nodes[intersection.Key] - map.bounds.Centre, intersection.Value);

            // Intersection of atleast 2 ways
            if (groupedPoints.Count >= 2)
            {
                foreach (var points in groupedPoints)
                {
                    // CreateLineRenderers(points, 0, 0.1f, white, lineParent);

                    // Create an instance of the object and place it in the centre of its points
                    GameObject go = new GameObject("Intersection");
                    Vector3 origin = map.nodes[intersection.Key];
                    go.transform.position = origin - map.bounds.Centre;

                    // Add the mesh filter and renderer components to the object
                    MeshFilter mf = go.AddComponent<MeshFilter>();
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    mr.material = asphaltMaterial;

                    // Create collections for the object's vertices, indices, UVs, etc.
                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector2> uvs = new List<Vector2>();
                    List<int> indices = new List<int>();

                    Vector3 p0 = Vector3.zero;
                    Vector3 p1 = points[0] - origin + map.bounds.Centre;
                    Vector3 p2 = points[1] - origin + map.bounds.Centre;

                    p0.y = Utils.GetTerrainAlignedPosition(origin - map.bounds.Centre).y;
                    p1.y = Utils.GetTerrainAlignedPosition(points[0]).y;
                    p2.y = Utils.GetTerrainAlignedPosition(points[1]).y;

                    Debug.Log(p0);
                    // Add the ground and top vertices
                    vertices.Add(p0);
                    vertices.Add(p1);
                    vertices.Add(p2);

                    // Assign UV coordinates
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));

                    // Normals (upward or outward depending on the face)
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);

                    // Road triangles
                    indices.Add(0);
                    indices.Add(1);
                    indices.Add(2);

                    indices.Add(0);
                    indices.Add(2);
                    indices.Add(1);

                    // Apply the data to the mesh
                    Mesh mesh = new Mesh();
                    mf.sharedMesh = mesh;
                    mesh.vertices = vertices.ToArray();
                    mesh.normals = normals.ToArray();
                    mesh.triangles = indices.ToArray();
                    mesh.uv = uvs.ToArray();

                    MeshCollider collider = go.AddComponent<MeshCollider>();

                    go.transform.parent = parent.transform;

                    // Mesh the corner sidewalk
                    Sidewalk.Mesh(points, origin, parent, false, true);
                }
            }
        }

        // lineParent.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
    
    private static List<List<Vector3>> GroupClosestVectors(Vector3 origin, List<Vector3> points)
    {
        var groupedPairs = new List<List<Vector3>>();

        // Track original index of each point
        Dictionary<Vector3, int> originalIndexMap = new Dictionary<Vector3, int>();
        for (int i = 0; i < points.Count; i++)
            originalIndexMap[points[i]] = i;

        while (points.Count > 1)
        {
            Vector3 point1 = points[0];
            int index1 = originalIndexMap[point1];

            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.MaxValue;
            int closestIdx = -1;

            for (int i = 1; i < points.Count; i++)
            {
                Vector3 point2 = points[i];
                int index2 = originalIndexMap[point2];

                // Skip if they were originally a pair
                if ((index1 % 2 == 0 && index2 == index1 + 1) || (index2 % 2 == 0 && index1 == index2 + 1))
                    continue;

                float distance = Vector3.Distance(point1, point2);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point2;
                    closestIdx = i;
                }
            }

            // If no valid match found (only original pairs remain)
            if (closestIdx == -1)
            {
                points.RemoveAt(0);
                continue;
            }

            // Adjust direction to ensure outward-facing normal
            Vector3 direction = (closestPoint - point1).normalized;
            Vector3 cross = Vector3.Cross(direction, Vector3.up);
            Vector3 vectorToOrigin = (origin - point1).normalized;

            if (Vector3.Dot(cross, vectorToOrigin) >= 0)
            {
                (point1, closestPoint) = (closestPoint, point1);
            }

            groupedPairs.Add(new List<Vector3> { point1, closestPoint });

            // Remove both points from list
            points.Remove(point1);
            points.Remove(closestPoint);
        }

        return groupedPairs;
    }
}
