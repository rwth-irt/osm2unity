using System.Collections.Generic;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

internal class Window
{
    private MapReader map;

    private static Material woodMat = Resources.Load<Material>("White Wood");
    private static Material glassMat = Resources.Load<Material>("Dark Glass");
    
    private GameObject aptDoor = Resources.Load<GameObject>("Apartment Door");

    public Window(MapReader map)
    {
        this.map = map;
    }

    public void WindowsPlacer(OsmWay way, Vector3 origin, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, GameObject parent, int type)
    {
        float floorHeight = way.Height / way.BuildingLevels;
        float windowWidth = 1.5f;
        float windowHeight = 1.5f;
        float frameWidth = 0.1f;
        float frameThickness = 0.01f;
        float windowSpacing = 3f; // spacing between windows

        if (way.BuildingType == "university")
        {
            windowWidth = 3f;
            windowHeight = 2f;

            if (type == 0 || way.BuildingLevels == 1) windowSpacing = 1.5f;
            else if (type == 1) windowSpacing = 0.01f;
        }
        // else if (way.BuildingType == "apartments")

        // Calculate the distance between v1 and v2 (wall length)
        float wallLength = Vector3.Distance(v1, v2);

        // Check if the wall length is the longest
        bool maxLength = true;
        for (int i = 1; i < way.NodeIDs.Count; i++)
        {
            OsmNode p1 = map.nodes[way.NodeIDs[i - 1]];
            OsmNode p2 = map.nodes[way.NodeIDs[i]];

            Vector3 v1_ = p1 - origin;
            Vector3 v2_ = p2 - origin;

            if (Vector3.Distance(v1_, v2_) > wallLength) { maxLength = false; break; }
        }

        // Calculate how many windows can fit on the wall based on the desired width
        int numWindows = Mathf.FloorToInt(wallLength / (windowSpacing + windowWidth));
        numWindows = Mathf.Max(1, numWindows); // Ensure at least 1 window

        // Lists to collect submesh data for both materials
        List<CombineInstance> glassCombineInstances = new List<CombineInstance>();
        List<CombineInstance> frameCombineInstances = new List<CombineInstance>();

        for (int i = 1; i <= way.BuildingLevels; i++)
        {
            float yHeight = (i * floorHeight) - (floorHeight / 2);

            for (int j = 0; j < numWindows; j++)
            {
                float t = (float)(j + 1) / (numWindows + 1); // Even spacing between edges
                Vector3 windowPos = Vector3.Lerp(v1, v2, t);
                windowPos.y = yHeight + Utils.GetTerrainAlignedPosition(windowPos + origin - map.bounds.Centre).y;

                // Handle doors for the first floor
                if (maxLength && i == 1 && (j == numWindows / 2 || j == numWindows - 1 / 2))
                {
                    GameObject door = GameObject.Instantiate(aptDoor, LocalToGlobal(windowPos, origin), Quaternion.identity);

                    Vector3 fwdDirection = (v2 - v1).normalized; // Align with the wall
                    door.transform.rotation = Quaternion.LookRotation(fwdDirection, Vector3.up);
                    door.transform.parent = parent.transform;

                    continue;
                }

                // Create a new window GameObject
                GameObject window = new GameObject("Window");
                GameObject glass = new GameObject("Glass");
                Window.Mesh(window, glass, windowWidth, windowHeight, frameWidth, frameThickness);

                // Set the window position
                window.transform.position = LocalToGlobal(windowPos, origin);
                glass.transform.position = LocalToGlobal(windowPos, origin);

                // Set rotation to align with the wall
                Vector3 forwardDirection = (v2 - v1).normalized;
                window.transform.rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
                glass.transform.rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);

                Vector3 newRotation = window.transform.localEulerAngles;
                newRotation.y -= 90f;
                window.transform.localEulerAngles = newRotation;
                glass.transform.localEulerAngles = newRotation;

                // Offset the window slightly forward
                window.transform.position += window.transform.forward * 0.05f;
                glass.transform.position += window.transform.forward * 0.05f;

                // Collect this window's submeshes for combination
                MeshFilter windowMeshFilter = window.GetComponent<MeshFilter>();
                MeshFilter glassMeshFilter = glass.GetComponent<MeshFilter>();

                if (windowMeshFilter != null && glassMeshFilter != null)
                {
                    Mesh glassMesh = glassMeshFilter.sharedMesh;
                    Mesh windowMesh = windowMeshFilter.sharedMesh;

                    // Submesh 0: Glass
                    CombineInstance glassCombine = new CombineInstance
                    {
                        mesh = glassMesh,
                        subMeshIndex = 0, // Glass submesh
                        transform = glassMeshFilter.transform.localToWorldMatrix
                    };
                    glassCombineInstances.Add(glassCombine);

                    // Submesh 1: Frame
                    CombineInstance frameCombine = new CombineInstance
                    {
                        mesh = windowMesh,
                        subMeshIndex = 0, // Frame submesh
                        transform = windowMeshFilter.transform.localToWorldMatrix
                    };
                    frameCombineInstances.Add(frameCombine);
                }

                // Destroy the temporary window GameObject
                GameObject.DestroyImmediate(window);
                GameObject.DestroyImmediate(glass);
            }
        }

        // Combine all collected window submeshes into two separate meshes (glass and frame)
        if (glassCombineInstances.Count > 0 || frameCombineInstances.Count > 0)
        {
            GameObject combinedGlass = new GameObject("CombinedGlass");
            combinedGlass.transform.parent = parent.transform;

            GameObject combinedWindows = new GameObject("CombinedWindows");
            combinedWindows.transform.parent = parent.transform;

            Mesh combinedMesh = new Mesh();
            Mesh combinedMesh2 = new Mesh();

            // Combine glass submesh
            Mesh glassMesh = new Mesh();
            glassMesh.CombineMeshes(glassCombineInstances.ToArray(), true, true);

            // Combine frame submesh
            Mesh frameMesh = new Mesh();
            frameMesh.CombineMeshes(frameCombineInstances.ToArray(), true, true);

            // Merge both submeshes into the combined mesh
            combinedMesh.vertices = glassMesh.vertices;
            combinedMesh2.vertices = frameMesh.vertices;

            combinedMesh.triangles = glassMesh.triangles; // Glass submesh
            combinedMesh2.triangles = frameMesh.triangles; // Frame submesh

            combinedMesh.RecalculateNormals();
            combinedMesh2.RecalculateNormals();

            // Assign the combined mesh to the MeshFilter
            MeshFilter mf = combinedGlass.AddComponent<MeshFilter>();
            MeshRenderer mr = combinedGlass.AddComponent<MeshRenderer>();
            mf.mesh = combinedMesh;

            MeshFilter mf2 = combinedWindows.AddComponent<MeshFilter>();
            MeshRenderer mr2 = combinedWindows.AddComponent<MeshRenderer>();
            mf2.mesh = combinedMesh2;

            // Assign materials for each submesh
            mr.material = glassMat; // Assign the appropriate materials
            mr2.material = woodMat;
        }
    }

    public static void Mesh(GameObject window, GameObject glass, float windowWidth, float windowHeight, float frameWidth, float frameThickness)
    {
        // Create Mesh and MeshFilter
        // Add the mesh filter and renderer components to the object
        MeshFilter meshFilter = glass.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = glass.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();

        MeshFilter meshFilter2 = window.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer2 = window.AddComponent<MeshRenderer>();
        Mesh mesh2 = new Mesh();

        // Half dimensions
        float halfWidth = windowWidth / 2;
        float halfHeight = windowHeight / 2;
        float halfFrame = frameWidth / 2;

        // Vertices for two panes and the frame
        Vector3[] vertices = new Vector3[]
        {
            // Glass panes
            new Vector3(-halfWidth, -halfHeight, 0),  // Bottom-left
            new Vector3(-halfFrame, -halfHeight, 0), // Bottom-right
            new Vector3(-halfWidth, halfHeight, 0),  // Top-left
            new Vector3(-halfFrame, halfHeight, 0),  // Top-right

            new Vector3(halfFrame, -halfHeight, 0),  // Bottom-left
            new Vector3(halfWidth, -halfHeight, 0), // Bottom-right
            new Vector3(halfFrame, halfHeight, 0),  // Top-left
            new Vector3(halfWidth, halfHeight, 0),  // Top-right
        };

        Vector3[] vertices2 = new Vector3[]
        {
            // Frame
            new Vector3(-halfWidth - 0.1f, -halfHeight - 0.1f, -frameThickness), // Frame Bottom-left
            new Vector3(halfWidth + 0.1f, -halfHeight - 0.1f, -frameThickness),  // Frame Bottom-right
            new Vector3(-halfWidth - 0.1f, halfHeight + 0.1f, -frameThickness),  // Frame Top-left
            new Vector3(halfWidth + 0.1f, halfHeight + 0.1f, -frameThickness),   // Frame Top-right
        };

        // Triangles for the glass panes (first submesh)
        int[] glassTriangles = new int[]
        {
            // Left pane
            0, 1, 2,
            1, 3, 2,

            // Right pane
            4, 5, 6,
            5, 7, 6
        };

        // Triangles for the frame (second submesh)
        int[] frameTriangles = new int[]
        {
            // Frame quad
            0, 1, 2,
            1, 3, 2
        };

        // UVs for texture mapping
        Vector2[] uvs = new Vector2[]
        {
            // Glass panes
            new Vector2(0, 0), new Vector2(0.5f, 0),
            new Vector2(0, 1), new Vector2(0.5f, 1),

            new Vector2(0.5f, 0), new Vector2(1, 0),
            new Vector2(0.5f, 1), new Vector2(1, 1),
        };

        Vector2[] uvs2 = new Vector2[]
        {
            // Frame
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1)
        };

        // Assign data to the mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = glassTriangles;

        mesh2.vertices = vertices2;
        mesh2.uv = uvs2;
        mesh2.triangles = frameTriangles;

        // Recalculate normals for lighting
        mesh.RecalculateNormals();
        mesh2.RecalculateNormals();

        // Assign the mesh to the MeshFilter
        meshFilter.mesh = mesh;
        meshFilter2.mesh = mesh2;

        // Assign materials to the MeshRenderer
        meshRenderer.material = glassMat;
        meshRenderer2.material = woodMat;
    }

    private Vector3 LocalToGlobal(Vector3 point, Vector3 origin)
    {
        return point + origin - map.bounds.Centre;
    }
}
