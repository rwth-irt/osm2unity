using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

public static class Sidewalk
{
    private static Material sidewalkMaterial = Resources.Load<Material>("Sidewalk");
    private static Material borderMaterial = Resources.Load<Material>("Border");

    public static void Mesh(List<Vector3> borderPoints, Vector3 position, GameObject parent, bool reverse = false, bool rounded = false)
    {
        // Create the sidewalk GameObject and set its position
        GameObject go = new GameObject("Sidewalk");
        if (rounded) go.transform.position = new Vector3(position.x, 0.01f, position.z);
        else go.transform.position = position;

        // Align the border points with terrain (TODO: do this only once for lanes and sidewalk)
        List<Vector3> points = new List<Vector3>();
        foreach (var p in borderPoints)
        {
            Vector3 point = Utils.GetTerrainAlignedPosition(p);
            points.Add(new Vector3(point.x, point.y + 0.01f, point.z));
        }

        // Add components for mesh and material
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        // Assign two materials: one for the border and one for the sidewalk
        mr.materials = new Material[2] { borderMaterial, sidewalkMaterial };

        // Initialize lists for mesh data
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        // Submesh for the sidewalk
        List<Vector3> sidewalkVertices = new List<Vector3>();
        List<Vector2> sidewalkUVs = new List<Vector2>();
        List<int> sidewalkIndices = new List<int>();

        float sidewalkWidth = 6f;
        float sidewalkHeight = 0.2f;
        float borderWidth = 0.3f;

        // Remove points that appear in opposite directions (intersection edge cases)
        points = Utils.RemoveMisalignedPoints(points);

        // Optionally reverse the points list
        if (reverse) points.Reverse();

        // Optionally generate a rounded sidewalk corner
        if (rounded) points = GenerateRoundedCorner(points[0], points[1], 2f, 5);

        float totalDistance = 0f;

        Vector3 prevp1 = Vector3.zero, prevp1BorderCross = Vector3.zero, prevp1SidewalkCross = Vector3.zero;

        // Loop through points to create the sidewalk mesh
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 p1 = points[i - 1] - position;
            Vector3 p2 = points[i] - position;

            p1.y = Utils.GetTerrainAlignedPosition(points[i - 1]).y;
            p2.y = Utils.GetTerrainAlignedPosition(points[i]).y;

            // Calculate the direction and perpendicular for width
            Vector3 direction = (p2 - p1).normalized;
            Vector3 borderCross = Vector3.Cross(direction, Vector3.up) * borderWidth;
            Vector3 sidewalkCross = Vector3.Cross(direction, Vector3.up) * sidewalkWidth;

            // Segment length for UV mapping
            float segmentLength = Vector3.Distance(p1, p2);
            totalDistance += segmentLength;

            if (i == 1)
            {
                prevp1 = p1;
                prevp1BorderCross = p1 + borderCross;
                prevp1SidewalkCross = prevp1BorderCross + sidewalkCross;
            }

            // Define vertices for the border of current segment
            Vector3 leftGround1 = prevp1;
            Vector3 rightGround1 = prevp1BorderCross;
            Vector3 leftTop1 = leftGround1 + Vector3.up * sidewalkHeight;
            Vector3 rightTop1 = rightGround1 + Vector3.up * sidewalkHeight;

            Vector3 leftGround2 = p2;
            Vector3 rightGround2 = p2 + borderCross;
            Vector3 leftTop2 = leftGround2 + Vector3.up * sidewalkHeight;
            Vector3 rightTop2 = rightGround2 + Vector3.up * sidewalkHeight;

            // Add vertices for the ground and top layers
            vertices.Add(leftGround1); vertices.Add(rightGround1);
            vertices.Add(leftTop1); vertices.Add(rightTop1);
            vertices.Add(leftGround2); vertices.Add(rightGround2);
            vertices.Add(leftTop2); vertices.Add(rightTop2);

            // Set UVs for the border using total distance
            uvs.Add(new Vector2(totalDistance - segmentLength, 0));
            uvs.Add(new Vector2(totalDistance - segmentLength, borderWidth));
            uvs.Add(new Vector2(totalDistance - segmentLength, sidewalkHeight));
            uvs.Add(new Vector2(totalDistance - segmentLength, sidewalkHeight + borderWidth));

            uvs.Add(new Vector2(totalDistance, 0));
            uvs.Add(new Vector2(totalDistance, borderWidth));
            uvs.Add(new Vector2(totalDistance, sidewalkHeight));
            uvs.Add(new Vector2(totalDistance, sidewalkHeight + borderWidth));

            // Set normals as upward facing for all vertices
            normals.Add(Vector3.up); normals.Add(Vector3.up);
            normals.Add(Vector3.up); normals.Add(Vector3.up);
            normals.Add(Vector3.up); normals.Add(Vector3.up);
            normals.Add(Vector3.up); normals.Add(Vector3.up);

            // Define indices for border triangles
            int offset = vertices.Count - 8;
            // Side border triangles
            indices.Add(offset); indices.Add(offset + 6); indices.Add(offset + 4);
            indices.Add(offset); indices.Add(offset + 2); indices.Add(offset + 6);
            // Top border triangles
            indices.Add(offset + 2); indices.Add(offset + 3); indices.Add(offset + 7);
            indices.Add(offset + 2); indices.Add(offset + 7); indices.Add(offset + 6);
            // Front
            if (i == 1)
            {
                indices.Add(offset); indices.Add(offset + 3); indices.Add(offset + 2);
                indices.Add(offset); indices.Add(offset + 1); indices.Add(offset + 3);
            }
            // End
            if (i == points.Count - 1)
            {
                indices.Add(offset + 4); indices.Add(offset + 6); indices.Add(offset + 7);
                indices.Add(offset + 4); indices.Add(offset + 7); indices.Add(offset + 5);
            }

            // Define vertices for the sidewalk submesh
            Vector3 sidewalkGroundLeft1 = rightGround1;
            Vector3 sidewalkGroundRight1 = prevp1SidewalkCross;
            Vector3 sidewalkTopLeft1 = rightTop1;
            Vector3 sidewalkTopRight1 = prevp1SidewalkCross + Vector3.up * sidewalkHeight;

            Vector3 sidewalkGroundLeft2 = rightGround2;
            Vector3 sidewalkGroundRight2 = rightGround2 + sidewalkCross;
            Vector3 sidewalkTopLeft2 = rightTop2;
            Vector3 sidewalkTopRight2 = rightTop2 + sidewalkCross;

            sidewalkVertices.Add(sidewalkGroundLeft1); sidewalkVertices.Add(sidewalkGroundRight1);
            sidewalkVertices.Add(sidewalkTopLeft1); sidewalkVertices.Add(sidewalkTopRight1);
            sidewalkVertices.Add(sidewalkGroundLeft2); sidewalkVertices.Add(sidewalkGroundRight2);
            sidewalkVertices.Add(sidewalkTopLeft2); sidewalkVertices.Add(sidewalkTopRight2);

            // Set UVs for the sidewalk
            sidewalkUVs.Add(new Vector2(totalDistance - segmentLength, 0));
            sidewalkUVs.Add(new Vector2(totalDistance - segmentLength, sidewalkWidth));
            sidewalkUVs.Add(new Vector2(totalDistance - segmentLength, sidewalkHeight));
            sidewalkUVs.Add(new Vector2(totalDistance - segmentLength, sidewalkHeight + borderWidth));

            sidewalkUVs.Add(new Vector2(totalDistance, 0));
            sidewalkUVs.Add(new Vector2(totalDistance, sidewalkWidth));
            sidewalkUVs.Add(new Vector2(totalDistance, sidewalkHeight));
            sidewalkUVs.Add(new Vector2(totalDistance, sidewalkHeight + borderWidth));

            // Indices for the sidewalk submesh
            int sidewalkOffset = sidewalkVertices.Count - 8;
            // Top
            sidewalkIndices.Add(sidewalkOffset + 2); sidewalkIndices.Add(sidewalkOffset + 3); sidewalkIndices.Add(sidewalkOffset + 7);
            sidewalkIndices.Add(sidewalkOffset + 2); sidewalkIndices.Add(sidewalkOffset + 7); sidewalkIndices.Add(sidewalkOffset + 6);
            // Side 
            sidewalkIndices.Add(sidewalkOffset + 1); sidewalkIndices.Add(sidewalkOffset + 5); sidewalkIndices.Add(sidewalkOffset + 7);
            sidewalkIndices.Add(sidewalkOffset + 1); sidewalkIndices.Add(sidewalkOffset + 7); sidewalkIndices.Add(sidewalkOffset + 3);
            // Front
            if (i == 1)
            {
                sidewalkIndices.Add(sidewalkOffset); sidewalkIndices.Add(sidewalkOffset + 3); sidewalkIndices.Add(sidewalkOffset + 2);
                sidewalkIndices.Add(sidewalkOffset); sidewalkIndices.Add(sidewalkOffset + 1); sidewalkIndices.Add(sidewalkOffset + 3);
            }
            // End
            if (i == points.Count - 1)
            {
                sidewalkIndices.Add(sidewalkOffset + 4); sidewalkIndices.Add(sidewalkOffset + 6); sidewalkIndices.Add(sidewalkOffset + 7);
                sidewalkIndices.Add(sidewalkOffset + 4); sidewalkIndices.Add(sidewalkOffset + 7); sidewalkIndices.Add(sidewalkOffset + 5);
            }

            prevp1 = leftGround2;
            prevp1BorderCross = rightGround2;
            prevp1SidewalkCross = sidewalkGroundRight2;
        }

        // Offset sidewalk indices to align with main vertex list
        List<int> sidewalkIndicesNew = new List<int>();
        sidewalkIndices.ForEach(i => sidewalkIndicesNew.Add(i + vertices.Count));

        // Create the main mesh and assign the sidewalk and border submeshes
        Mesh mesh = new Mesh
        {
            vertices = vertices.Concat(sidewalkVertices).ToArray(),
            normals = normals.Concat(Enumerable.Repeat(Vector3.up, sidewalkVertices.Count)).ToArray(),
            uv = uvs.Concat(sidewalkUVs).ToArray()
        };

        // Set submesh indices
        mesh.subMeshCount = 2;
        mesh.SetTriangles(indices.ToArray(), 0); // Border submesh
        mesh.SetTriangles(sidewalkIndicesNew.ToArray(), 1); // Sidewalk submesh

        mf.sharedMesh = mesh;
        go.transform.parent = parent.transform;
    }
    private static List<Vector3> GenerateRoundedCorner(Vector3 p1, Vector3 p2, float radius, int segments)
    {
        List<Vector3> arcPoints = new List<Vector3>();

        // Midpoint between the points
        Vector3 mid = (p1 + p2) * 0.5f;

        // Direction of the chord 
        Vector3 chordDir = (p1 - p2).normalized;

        // Perpendicular direction
        Vector3 perp = Vector3.Cross(chordDir, Vector3.up).normalized;

        // Bulge center (control point for arc) is offset from midpoint
        Vector3 arcCenter = mid + perp * radius;

        // Generate arc using quadratic Bezier curve
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;

            // Quadratic B�zier formula: B(t) = (1 - t)^2 * p1 + 2(1 - t)t * bulge + t^2 * p2
            Vector3 point = Utils.QuadraticBezier(p1, arcCenter, p2, t);
            arcPoints.Add(point);
        }

        return arcPoints;
    }
}
