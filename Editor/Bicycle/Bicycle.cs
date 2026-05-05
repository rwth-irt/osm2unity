using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

public static class Bicycle
{
    public static void InitializeRandomPos()
    {
        GameObject roadsParent = GameObject.Find("Roads");
        GameObject bicycle = GameObject.Find("Bicycle");

        if (bicycle == null)
        {
            Debug.LogError("Missing reference 'Bicycle'.");
            return;
        }

        else if (roadsParent == null)
        {
            Debug.LogError("Missing reference 'Roads'.");
            return;
        }

        // Get all child Transforms (each road)
        List<Transform> roadSegmentsMarking = new List<Transform>();
        List<Transform> roadSegments = new List<Transform>();

        foreach (Transform road in roadsParent.transform)
        {
            // Get all children of the road (including nested)
            Transform[] allChildren = road.GetComponentsInChildren<Transform>();

            // Find all bicycle markings under this road
            List<Transform> markings = allChildren
                .Where(t => t.name == "Bicycle Road Marking(Clone)" && t != road)
                .ToList();

            if (markings.Count > 0)
                // Add the midpoint marking
                roadSegmentsMarking.Add(markings[markings.Count / 2]);
            else
                roadSegments.Add(road);

            /*
            Transform bicycleMarking = child.Find("Bicycle Road Marking(Clone)");
            if (bicycleMarking != null)
                roadSegmentsMarking.Add(bicycleMarking);
            else
                roadSegments.Add(child);
            */
        }

        // Pick a random road
        Transform randomRoad = (roadSegmentsMarking.Count != 0)
            ? roadSegmentsMarking[Random.Range(0, roadSegmentsMarking.Count)]
            : roadSegments[Random.Range(0, roadSegments.Count)];

        Vector3 pos = randomRoad.position;

        // Adjust Y based on terrain height
        pos = Utils.GetTerrainAlignedPosition(pos);

        // Apply position
        bicycle.transform.position = pos;

        // Face forward along the road
        bicycle.transform.forward = -randomRoad.forward;
    }
}
