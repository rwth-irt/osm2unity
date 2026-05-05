using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */


public static class Utils
{
    public static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0; // (1-t)^2 * p0
        p += 2 * u * t * p1; // 2*(1-t)*t * p1
        p += tt * p2; // t^2 * p2

        return p;
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0; // (1-t)^3 * p0
        p += 3 * uu * t * p1; // 3*(1-t)^2 * t * p1
        p += 3 * u * tt * p2; // 3*(1-t) * t^2 * p2
        p += ttt * p3; // t^3 * p3

        return p;
    }

    private static Vector3 CatmullRomSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2 * p1) +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t3
        );
    }

    public static Vector3 GetTerrainAlignedPosition(Vector3 position, float yoffset = 0)
    {
        Terrain terrain = Terrain.activeTerrain;

        // if (terrain == null) terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("Terrain reference is null.");
            return position;
        }

        float y = terrain.SampleHeight(new Vector3(position.x, 0, position.z)) + terrain.transform.position.y;
        return new Vector3(position.x, y + yoffset, position.z);
    }

    public static List<Vector3> RemoveMisalignedPoints(List<Vector3> points)
    {
        if (points == null || points.Count < 3)
            return new List<Vector3>(points);

        List<Vector3> cleaned = new List<Vector3>();
        cleaned.Add(points[0]);

        int i = 1;
        while (i < points.Count - 1)
        {
            Vector3 prev = points[i - 1];
            Vector3 curr = points[i];
            Vector3 next = points[i + 1];

            Vector3 toCurr = (curr - prev).normalized;
            Vector3 toNext = (next - prev).normalized;

            // If the current point is in the opposite direction to the intended path,
            if (Vector3.Dot(toCurr, toNext) < 0f)
            {
                // Skip this point
                i++;
                continue;
            }

            cleaned.Add(curr);
            i++;
        }

        cleaned.Add(points[points.Count - 1]);

        return cleaned;
    }
    
    public static void RemoveCollidingObjects(Transform parent, MeshCollider[] roadColliders)
    {
        if (roadColliders == null || roadColliders.Length == 0)
        {
            Debug.LogWarning("No road colliders found!");
            return;
        }

        // Loop in reverse to safely remove elements while iterating
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            Collider objCollider = child.GetComponent<Collider>();
            if (objCollider == null)
            {
                continue; // Skip objects without Collider
            }


            foreach (MeshCollider roadCollider in roadColliders)
            {
                // Check if the object's collider collides with the road's MeshCollider
                if (IsCollidingWithRoad(objCollider, roadCollider))
                {
                    // Debug.Log("Removing colliding object: " + child.gameObject.name);
                    UnityEngine.GameObject.DestroyImmediate(child.gameObject); // Remove the object if it collides with a road
                    break;
                }
            }
        }
    }
    
    public static bool IsCollidingWithRoad(Collider objCollider, MeshCollider roadCollider)
    {
        // Check for collision between the sphere collider and the road's mesh collider
        Vector3 direction;
        float distance;

        return Physics.ComputePenetration(
            objCollider,
            objCollider.transform.position,
            objCollider.transform.rotation,
            roadCollider,
            roadCollider.transform.position,
            roadCollider.transform.rotation,
            out direction,
            out distance
        );
    }

    public static List<Vector3> SortAndRemoveDuplicates(List<Vector3> controlPoints)
    {
        // Sort control points based on their distance from the first point
        Vector3 referencePoint = controlPoints[0];

        // Sort the list by distance from the reference point, ensuring they are in sequence
        controlPoints.Sort((a, b) =>
        {
            float distA = Vector3.Distance(referencePoint, a);
            float distB = Vector3.Distance(referencePoint, b);
            return distA.CompareTo(distB);
        });

        // Remove duplicate points (points with the same position)
        controlPoints = controlPoints.Distinct().ToList();

        return controlPoints;
    }

    /// <summary>
    /// Generates evenly spaced points along the path defined by the input points.
    /// </summary>
    /// <param name="interpolatedPoints">List of interpolated points defining the path.</param>
    /// <param name="spacing">Desired spacing between the evenly spaced points.</param>
    /// <returns>List of evenly spaced points.</returns>
    public static List<Vector3> CreateEvenlySpacedPoints(List<Vector3> interpolatedPoints, float spacing, bool includeOriginalPoints = false)
    {
        List<Vector3> result = new List<Vector3>();

        if (interpolatedPoints == null || interpolatedPoints.Count < 2 || spacing <= 0)
            return result;

        result.Add(interpolatedPoints[0]);
        float distanceSinceLastPoint = 0f;

        Vector3 lastPlacedPoint = interpolatedPoints[0];

        for (int i = 1; i < interpolatedPoints.Count; i++)
        {
            Vector3 start = interpolatedPoints[i - 1];
            Vector3 end = interpolatedPoints[i];
            float segmentLength = Vector3.Distance(start, end);
            Vector3 segmentDir = (end - start).normalized;

            while (distanceSinceLastPoint + segmentLength >= spacing)
            {
                float remainingDist = spacing - distanceSinceLastPoint;
                Vector3 newPoint = lastPlacedPoint + segmentDir * remainingDist;
                result.Add(newPoint);

                lastPlacedPoint = newPoint;
                segmentLength -= remainingDist;
                distanceSinceLastPoint = 0f;
            }

            distanceSinceLastPoint += segmentLength;
            lastPlacedPoint = end;

            if (includeOriginalPoints && !result.Contains(end))
            {
                result.Add(end);
            }
        }

        return result;
    }

    public static (List<Vector3>, List<Vector3>, List<Quaternion>) OffsetPoints(List<Vector3> points, float offset)
    {
        List<Vector3> rightPoints = new List<Vector3>(points.Count);
        List<Vector3> leftPoints = new List<Vector3>(points.Count);
        List<Quaternion> rotations = new List<Quaternion>(points.Count);

        Vector3 cross = Vector3.zero;
        Quaternion yRotation = Quaternion.identity;

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 s1 = points[i - 1];
            Vector3 s2 = points[i];

            Vector3 diff = (s2 - s1).normalized;

            cross = Vector3.Cross(diff, Vector3.up) * offset;

            // Calculate the rotation quaternion
            Quaternion rotation = Quaternion.LookRotation(diff, cross);

            // Convert to Euler angles, zero out x and z rotations, and convert back to quaternion
            Vector3 euler = rotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            yRotation = Quaternion.Euler(euler);

            // Create points that represent the width of the road
            rightPoints.Add(s1 + cross);
            leftPoints.Add(s1 - cross);
            rotations.Add(yRotation);
        }

        // Adding the final point
        rightPoints.Add(points[points.Count - 1] + cross);
        leftPoints.Add(points[points.Count - 1] - cross);
        rotations.Add(yRotation);

        return (rightPoints, leftPoints, rotations);
    }

    /// <summary>
    /// Splits a list into sublists at specified indices.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the list.</typeparam>
    /// <param name="list">The list to split.</param>
    /// <param name="indices">Indices where the list should be split.</param>
    /// <returns>A list of sublists.</returns>
    public static List<List<T>> SplitAtIndices<T>(List<T> list, List<int> indices)
    {
        List<List<T>> result = new List<List<T>>();
        indices.Sort(); // Ensure indices are sorted

        int startIndex = 0;

        foreach (int index in indices)
        {
            if (index > list.Count || index < 0) continue; // Skip invalid indices
            result.Add(list.GetRange(startIndex, index - startIndex)); // Add sublist
            startIndex = index;
        }

        // Add the remaining part of the list
        if (startIndex < list.Count)
        {
            result.Add(list.GetRange(startIndex, list.Count - startIndex));
        }

        return result;
    }

    public static bool IsPointBetween(Vector3 point, Vector3 startPoint, Vector3 endPoint)
    {
        // Calculate the total distance between startPoint and endPoint
        float totalDistance = Vector3.Distance(startPoint, endPoint);

        // Calculate the distance between startPoint and the point, and the point and endPoint
        float distanceToPoint1 = Vector3.Distance(startPoint, point);
        float distanceToPoint2 = Vector3.Distance(point, endPoint);

        // Check if the sum of these distances is approximately equal to the total distance
        // Allowing a small margin for floating-point precision errors
        float epsilon = 1f;
        return Mathf.Abs(distanceToPoint1 + distanceToPoint2 - totalDistance) < epsilon;
    }

    public static (List<Vector3>, List<Vector3>) CutoffIntersection(List<Vector3> intersections, List<Vector3> points, bool isIntersection = true)
    {
        // Debug.Log("Number of intersections: " + intersections.Count / 2);
        var removedPoints = new List<Vector3>();
        Vector3 intersection = Vector3.zero;

        {
            var prevIntersection = intersections[0];
            var nextIntersection = intersections[1];

            // Iterate from the end of the list to the beginning
            for (int j = points.Count - 1; j >= 0; j--)
            {
                if (Utils.IsPointBetween(points[j], prevIntersection, nextIntersection))
                {
                    removedPoints.Add(points[j]);
                    points.RemoveAt(j);

                    intersection = prevIntersection;
                }
            }
        }

        if (isIntersection && intersection != Vector3.zero)
        {
            points.Add(intersection);
        }

        return (points, removedPoints);
    }

    public static Vector3 ComputeOffset(float angleDeg, float adjWidth, float wayWidth, float cutoff, Vector3 direction)
    {
        float angleRad = Mathf.Abs(angleDeg * Mathf.Deg2Rad);
        float offsetDist = (adjWidth / Mathf.Sin(angleRad)) + (wayWidth * Mathf.Tan(Mathf.Deg2Rad * 90f - angleRad)) + cutoff;
        Vector3 offset = offsetDist * direction;

        return offset;
    }
}
