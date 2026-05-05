using System.Collections.Generic;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

internal class Roof
{
    private MapReader map;
    private Material roofMat = Resources.Load<Material>("Roof");

    public Roof(MapReader map)
    {
        this.map = map;
    }

    public void MeshGabled(OsmWay way, Vector3 origin, Vector3 position, GameObject parent)
    {
        // Create an instance of the object and place it in the centre of its points
        GameObject go = new GameObject("Roof");
        go.transform.position = position;

        // Add the mesh filter and renderer components to the object
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        // Apply the material
        mr.material = roofMat;

        // Create the collections for the object's vertices, indices, UVs etc.
        List<Vector3> vectors = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        // Iterate through the nodes
        for (int i = 0; i < way.NodeIDs.Count - 3; i += 1)
        {
            OsmNode p1 = map.nodes[way.NodeIDs[i]];
            OsmNode p2 = map.nodes[way.NodeIDs[i + 1]];
            OsmNode p3 = map.nodes[way.NodeIDs[i + 2]];
            OsmNode p4 = map.nodes[way.NodeIDs[i + 3]];

            Vector3 v1 = p1 - origin + new Vector3(0, way.Height, 0);
            Vector3 v2 = p2 - origin + new Vector3(0, way.Height, 0);
            Vector3 v3 = p3 - origin + new Vector3(0, way.Height, 0);
            Vector3 v4 = p4 - origin + new Vector3(0, way.Height, 0);

            // Convert to world space and get terrain height
            v1.y += Utils.GetTerrainAlignedPosition(v1 + origin - map.bounds.Centre).y;
            v2.y += Utils.GetTerrainAlignedPosition(v2 + origin - map.bounds.Centre).y;
            v3.y += Utils.GetTerrainAlignedPosition(v3 + origin - map.bounds.Centre).y;
            v4.y += Utils.GetTerrainAlignedPosition(v4 + origin - map.bounds.Centre).y;

            if (Vector3.Distance(v1, v2) < Vector3.Distance(v2, v3))
            {
                Vector3 v5 = (v1 + v2) / 2;
                Vector3 v6 = (v3 + v4) / 2;

                // Set roof height here
                v5.y += 5f;
                v6.y += 5f;

                // Add the vertices
                vectors.Add(v1);
                vectors.Add(v2);
                vectors.Add(v3);
                vectors.Add(v4);
                vectors.Add(v5);
                vectors.Add(v6);

                // Set UVs based on vertex positions
                uvs.Add(new Vector2(0, 0)); // v1
                uvs.Add(new Vector2(1, 0)); // v2
                uvs.Add(new Vector2(1, 1)); // v4
                uvs.Add(new Vector2(0, 1)); // v3
                uvs.Add(new Vector2(0.5f, 0)); // v5 (centered along the x-axis)
                uvs.Add(new Vector2(0.5f, 1)); // v6 (centered along the x-axis)

                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);

                // first triangle v1, v2, v5
                indices.Add(0);
                indices.Add(1);
                indices.Add(4);

                indices.Add(0);
                indices.Add(4);
                indices.Add(1);

                // second         v2, v3, v5
                indices.Add(1);
                indices.Add(2);
                indices.Add(4);

                indices.Add(1);
                indices.Add(4);
                indices.Add(2);

                // third          v3, v6, v5
                indices.Add(2);
                indices.Add(5);
                indices.Add(4);

                indices.Add(2);
                indices.Add(4);
                indices.Add(5);

                // fourth         v3, v4, v6
                indices.Add(2);
                indices.Add(3);
                indices.Add(5);

                indices.Add(2);
                indices.Add(5);
                indices.Add(3);

                // fifth         v4, v1, v6
                indices.Add(3);
                indices.Add(0);
                indices.Add(5);

                indices.Add(3);
                indices.Add(5);
                indices.Add(0);

                // sixth         v1, v5, v6
                indices.Add(0);
                indices.Add(4);
                indices.Add(5);

                indices.Add(0);
                indices.Add(5);
                indices.Add(4);
            }
        }

        // Apply the data to the mesh
        Mesh roofMesh = new Mesh();
        mf.sharedMesh = roofMesh;
        roofMesh.vertices = vectors.ToArray();
        roofMesh.triangles = indices.ToArray();
        roofMesh.normals = normals.ToArray();
        roofMesh.uv = uvs.ToArray();

        go.transform.parent = parent.transform;
    }
}
