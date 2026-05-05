using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
/// Make buildings.
/// </summary>
internal sealed class BuildingMaker : BaseInfrastructureMaker
{
    private Material beigeMat = Resources.Load<Material>("Beige");
    private Material brownMat = Resources.Load<Material>("Brown");
    private Material creamMat = Resources.Load<Material>("Cream");
    private Material greyMat = Resources.Load<Material>("Grey");
    private Material peachMat = Resources.Load<Material>("Peach");
    private Material pinkMat = Resources.Load<Material>("Pink");
    private Material redMat = Resources.Load<Material>("Red");
    private Material whiteMat = Resources.Load<Material>("White Wall");

    private Terrain terrain = Terrain.activeTerrain;
    private Window window;
    private Roof roof;

    public override int NodeCount
    {
        get
        {
            return map.ways.FindAll((w) =>
                                    {
                                        return w.IsBuilding && w.NodeIDs.Count > 1;
                                    }).Count;
        }
    }

    public BuildingMaker(MapReader mapReader)
        : base(mapReader)
    {
        window = new Window(map);
        roof = new Roof(map);
    }

public override IEnumerable<int> Process()
    {
        var parent = new GameObject("Buildings");
        int count = 0;

        // List of materials (colors)
        List<Material> buildingColors = new List<Material>() { beigeMat, brownMat, creamMat, greyMat, peachMat, pinkMat, redMat, whiteMat };

        // Corresponding weights for each color
        List<int> colorWeights = new List<int>() { 7, 5, 1, 7, 1, 1, 3, 10 };

        // Total weight
        int totalWeight = colorWeights.Sum();

        // Material prevSelectedColor = null;

        // Iterate through all the buildings in the 'ways' list
        List<OsmWay> buildingWays = map.ways.FindAll((w) => { return w.IsBuilding && w.NodeIDs.Count > 1; });

        // Add the buildings in the relation nodes containing outer way
        var relations = map.relations.FindAll((r) => r.IsBuilding);
        List<ulong> outerWayIDs = new List<ulong>();
        foreach (var rel in relations) { outerWayIDs.Add(rel.outerWayID); };
        buildingWays.AddRange(map.ways.FindAll((w) => outerWayIDs.Contains(w.ID)));

        foreach (var way in buildingWays)
        {
            string name = "Building";
            if (way.Name != "")
            {
                name = way.Name;
            }

            Material selectedColor = null;
            if (way.BuildingType == "university")
            {
                selectedColor = greyMat;
            }
            else
            {
                // Choose a random color based on weighted probabilities
                int randomWeight = Random.Range(0, totalWeight);
                int cumulativeWeight = 0;

                for (int i = 0; i < buildingColors.Count; i++)
                {
                    cumulativeWeight += colorWeights[i];
                    if (randomWeight < cumulativeWeight)
                    {
                        selectedColor = buildingColors[i];

                        /*
                        // Ensure consecutive building dont have the same colour
                        if (selectedColor == prevSelectedColor)
                        {
                            if (i != buildingColors.Count - 1)
                            {
                                selectedColor = buildingColors[i + 1];
                            }
                            else
                            {
                                selectedColor = buildingColors[i - 1];
                            }
                        }
                        */

                        break;
                    }
                }

            }

            // prevSelectedColor = selectedColor;

            // Create the object
            CreateObject(way, selectedColor, name, parent);

            count++;
            yield return count;
        }
    }


    /// <summary>
    /// Build the object using the data from the OsmWay instance.
    /// </summary>
    /// <param name="way">OsmWay instance</param>
    /// <param name="origin">The origin of the structure</param>
    /// <param name="vectors">The vectors (vertices) list</param>
    /// <param name="normals">The normals list</param>
    /// <param name="uvs">The UVs list</param>
    /// <param name="indices">The indices list</param>
    protected override void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices, GameObject parent)
    {
        // Get the centre of the roof
        Vector3 oTop = new Vector3(0, way.Height, 0);
        // Convert to world space and get terrain height
        oTop.y += Utils.GetTerrainAlignedPosition(oTop + origin - map.bounds.Centre).y;

        // First vector is the middle point in the roof
        vectors.Add(oTop);
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        List<Vector2> buildingNodes = new List<Vector2>();
        foreach (ulong nodeId in way.NodeIDs)
        {
            OsmNode p = map.nodes[nodeId];
            buildingNodes.Add(new Vector2(p.Longitude, p.Latitude)); // Convert OSM nodes to Vector2
        }

        // Windows meshing considers an anti-clockwise orientation of the building nodes array
        bool isClockwise = IsClockwise(buildingNodes);
        if (!isClockwise)
        {
            way.NodeIDs.Reverse();
        }

        // Random type of windows
        var type = Random.Range(0, 2);

        // Meshes both the inner and outer walls of the buildings (4 traingles/wall)
        for (int i = 1; i < way.NodeIDs.Count; i++)
        {
            OsmNode p1 = map.nodes[way.NodeIDs[i - 1]];
            OsmNode p2 = map.nodes[way.NodeIDs[i]];

            Vector3 v1 = p1 - origin;
            Vector3 v2 = p2 - origin;

            // Convert to world space and get terrain height
            v1.y = Utils.GetTerrainAlignedPosition(v1 + origin - map.bounds.Centre).y;
            v2.y = Utils.GetTerrainAlignedPosition(v2 + origin - map.bounds.Centre).y;

            Vector3 v3 = v1 + new Vector3(0, way.Height, 0);
            Vector3 v4 = v2 + new Vector3(0, way.Height, 0);

            vectors.Add(v1);
            vectors.Add(v2);
            vectors.Add(v3);
            vectors.Add(v4);

            // Assign UV coordinates
            /*
            float uvY1 = (float)(i - 1) / (way.NodeIDs.Count - 1);
            float uvY2 = (float)i / (way.NodeIDs.Count - 1);
            */

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);

            int idx1, idx2, idx3, idx4;
            idx4 = vectors.Count - 1;
            idx3 = vectors.Count - 2;
            idx2 = vectors.Count - 3;
            idx1 = vectors.Count - 4;

            // Walls
            // first triangle v1, v3, v2
            indices.Add(idx1);
            indices.Add(idx3);
            indices.Add(idx2);

            // second         v3, v4, v2
            indices.Add(idx3);
            indices.Add(idx4);
            indices.Add(idx2);

            // Inside mesh counter-clockwise
            // third          v2, v3, v1
            indices.Add(idx2);
            indices.Add(idx3);
            indices.Add(idx1);

            // fourth         v2, v4, v3
            indices.Add(idx2);
            indices.Add(idx4);
            indices.Add(idx3);

            // Roof
            // Debug.Log(way.RoofShape);
            if (way.RoofShape == "flat" || way.NodeIDs.Count > 5)
            {
                indices.Add(0);
                indices.Add(idx3);
                indices.Add(idx4);
            
                // The upside down one
                indices.Add(idx4);
                indices.Add(idx3);
                indices.Add(0);
            }

            // For each wall place windows on each level of the building
            window.WindowsPlacer(way, origin, v1, v2, v3, v4, parent, type);
        }

        if ((way.RoofShape == "gabled" || way.RoofShape == null) && way.NodeIDs.Count == 5)
        {
            Vector3 localOrigin = GetCentre(way);
            roof.MeshGabled(way, origin, localOrigin - map.bounds.Centre, parent);
        }

        // Align with the terrain height
        // parent.transform.position = Utils.GetTerrainAlignedPosition(parent.transform.position);
    }

    /// <summary>
    /// Determines whether an OSM building's nodes are arranged in a clockwise or counterclockwise order.
    /// Uses the Shoelace algorithm.
    /// </summary>
    /// <param name="nodes">List of Vector2 (OSM node positions in 2D)</param>
    /// <returns>True if nodes are clockwise, False if counterclockwise</returns>
    private bool IsClockwise(List<Vector2> nodes)
    {
        float sum = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector2 current = nodes[i];
            Vector2 next = nodes[(i + 1) % nodes.Count]; // Wrap around for the last edge

            sum += (next.x - current.x) * (next.y + current.y);
        }

        return sum > 0; // If the sum is positive, it's clockwise; otherwise, counterclockwise
    }
}

