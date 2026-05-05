using System.Collections.Generic;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

internal class TrafficSignal
{
    private MapReader map;

    private Material white = Resources.Load<Material>("White");

    private GameObject stopSign = Resources.Load<GameObject>("Stop Sign");
    private GameObject signalObject = Resources.Load<GameObject>("Traffic Light");
    private GameObject crossingObject = Resources.Load<GameObject>("Crossing");

    public TrafficSignal(MapReader map)
    {
        this.map = map;
    }

    public List<Vector3> SignalPlacer(OsmWay way, OsmNode signalNode, GameObject parent)
    {
        var intersectingPoints = new List<Vector3>();
        var signalDirection = signalNode.SignalDirection;
        int i = way.NodeIDs.IndexOf(signalNode.ID);

        // Handle reversal of heading
        int next_i = i + 1;
        Vector3 intersection = map.nodes[way.NodeIDs[0]] - map.bounds.Centre;
        if (signalDirection == "forward" || i == way.NodeIDs.Count - 1)
        {
            next_i = i - 1;
            intersection = map.nodes[way.NodeIDs[way.NodeIDs.Count - 1]] - map.bounds.Centre;
        }

        Vector3 s1 = map.nodes[signalNode.ID] - map.bounds.Centre;
        Vector3 s2 = map.nodes[way.NodeIDs[next_i]] - map.bounds.Centre;

        Vector3 diff = (s2 - s1).normalized;
        var cross = Vector3.Cross(diff, Vector3.up) * way.Width;

        // Calculate the rotation quaternion
        Quaternion rotation = Quaternion.LookRotation(diff, cross);

        // Convert to Euler angles, zero out x and z rotations, and convert back to quaternion
        Vector3 euler = rotation.eulerAngles;
        euler.x = 0;
        euler.y += 90f;
        euler.z = 0;
        var yRotation = Quaternion.Euler(euler);

        ObjectPlacer.PlaceObject(signalObject, s1 - diff * 10f + cross, yRotation, parent);

        var stopPos = new List<Vector3>();
        stopPos.Add(s1);
        stopPos.Add(s1 + cross);

        ObjectPlacer.PlaceObject(stopSign, stopPos[1], yRotation, parent);

        GameObject lane = new GameObject("Lane");
        lane.transform.parent = parent.transform;
        Lane.CreateLineRenderers(stopPos, 0, 0.5f, white, lane);
        lane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        intersectingPoints.Add(intersection);
        intersectingPoints.Add(s1);

        return intersectingPoints;
    }

    public List<Vector3> StopPlacer(OsmWay way, OsmNode stopNode, GameObject parent)
    {
        var intersectingPoints = new List<Vector3>();
        var stopDirection = stopNode.SignalDirection;
        int i = way.NodeIDs.IndexOf(stopNode.ID);

        // Handle reversal of heading
        int next_i = i + 1;
        Vector3 intersection = map.nodes[way.NodeIDs[0]] - map.bounds.Centre;
        if (stopDirection == "forward" || i == way.NodeIDs.Count - 1)
        {
            next_i = i - 1;
            intersection = map.nodes[way.NodeIDs[way.NodeIDs.Count - 1]] - map.bounds.Centre;
        }

        Vector3 s1 = map.nodes[stopNode.ID] - map.bounds.Centre;
        Vector3 s2 = map.nodes[way.NodeIDs[next_i]] - map.bounds.Centre;

        Vector3 diff = (s2 - s1).normalized;
        var cross = Vector3.Cross(diff, Vector3.up) * way.Width;

        // Calculate the rotation quaternion
        Quaternion rotation = Quaternion.LookRotation(diff, cross);

        // Convert to Euler angles, zero out x and z rotations, and convert back to quaternion
        Vector3 euler = rotation.eulerAngles;
        euler.x = 0;
        euler.y += 90f;
        euler.z = 0;
        var yRotation = Quaternion.Euler(euler);

        var stopPos = new List<Vector3>();
        stopPos.Add(s1);
        stopPos.Add(s1 + cross);

        ObjectPlacer.PlaceObject(stopSign, stopPos[1], yRotation, parent);

        GameObject lane = new GameObject("Lane");
        lane.transform.parent = parent.transform;
        Lane.CreateLineRenderers(stopPos, 0, 0.5f, white, lane);
        lane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        intersectingPoints.Add(intersection);
        intersectingPoints.Add(s1);

        return intersectingPoints;
    }

    public List<Vector3> CrossingPlacer(OsmWay way, OsmNode signalNode, GameObject parent)
    {
        var intersectingPoints = new List<Vector3>();
        int i = way.NodeIDs.IndexOf(signalNode.ID);

        // Handle reversal of heading
        int next_i = i + 1;
        int prev_i = i - 1;
        Vector3 intersection = map.nodes[way.NodeIDs[0]] - map.bounds.Centre;
        if (i == way.NodeIDs.Count - 1)
        {
            next_i = i - 1;
            intersection = map.nodes[way.NodeIDs[way.NodeIDs.Count - 1]] - map.bounds.Centre;
        }

        Vector3 s1 = map.nodes[signalNode.ID] - map.bounds.Centre;
        Vector3 s2 = map.nodes[way.NodeIDs[next_i]] - map.bounds.Centre;

        Vector3 diff = (s2 - s1).normalized;
        var cross = Vector3.Cross(diff, Vector3.up) * way.Width;

        var crossingLeft = new List<Vector3>();
        var crossingRight = new List<Vector3>();
        crossingLeft.Add(s1 + cross);
        crossingLeft.Add(s1);
        crossingLeft.Add(s1 - cross);
        var c1 = s1 - diff * 5f;
        crossingRight.Add(c1 + cross);
        crossingRight.Add(c1);
        crossingRight.Add(c1 - cross);

        GameObject lane = new GameObject("Lane");
        lane.transform.parent = parent.transform;
        Lane.CreateLineRenderers(Utils.CreateEvenlySpacedPoints(crossingLeft, 0.4f), 2, 0.2f, white, lane);
        Lane.CreateLineRenderers(Utils.CreateEvenlySpacedPoints(crossingRight, 0.4f), 2, 0.2f, white, lane);
        lane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        intersectingPoints.Add(intersection);
        intersectingPoints.Add(s1);

        return intersectingPoints;
    }
}
