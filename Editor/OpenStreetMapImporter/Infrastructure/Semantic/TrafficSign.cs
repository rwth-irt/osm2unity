using System.Collections.Generic;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

internal class TrafficSign
{
    private MapReader map;
    
    private Material white = Resources.Load<Material>("White");
    
    private GameObject stopSign = Resources.Load<GameObject>("Stop Sign");
    private GameObject yieldSign = Resources.Load<GameObject>("Yield Sign");
    private GameObject walkSign = Resources.Load<GameObject>("239 Sign");
    private GameObject walkBiSign = Resources.Load<GameObject>("240 Sign");
    private GameObject driveSign = Resources.Load<GameObject>("331.1 Sign");
    private GameObject noDriveSign = Resources.Load<GameObject>("331.2 Sign");
    private GameObject citySign = Resources.Load<GameObject>("City Limit Sign");

    public TrafficSign(MapReader map)
    {
        this.map = map;
    }

    public void SignsPlacer(OsmWay way, Dictionary<OsmNode, string> trafficSigns, GameObject parent)
    {
        foreach (var item in trafficSigns)
        {
            if (item.Value.Contains("DE:239"))
            {
                SignPlacer(way, item.Key, walkSign, parent);
            }
            else if (item.Value == "DE:240")
            {
                SignPlacer(way, item.Key, walkBiSign, parent);
            }
            else if (item.Value == "DE:331.1")
            {
                SignPlacer(way, item.Key, driveSign, parent);
            }
            else if (item.Value == "DE:331.2")
            {
                SignPlacer(way, item.Key, noDriveSign, parent);
            }
            else if (item.Value == "city_limit")
            {
                SignPlacer(way, item.Key, citySign, parent);
            }
            else if (item.Value == "give_way")
            {
                SignPlacer(way, item.Key, yieldSign, parent);
            }

            /*
            private GameObject walkSign = Resources.Load<GameObject>("239 Sign");
            private GameObject walkBiSign = Resources.Load<GameObject>("240 Sign");
            private GameObject driveSign = Resources.Load<GameObject>("331.1 Sign");
            private GameObject noDriveSign = Resources.Load<GameObject>("331.2 Sign");
            private GameObject citySign = Resources.Load<GameObject>("City Limit Sign");
            */

        }
    }

    private void SignPlacer(OsmWay way, OsmNode signNode, GameObject sign, GameObject parent)
    {
        var intersectingPoints = new List<Vector3>();
        var signDirection = signNode.SignalDirection;
        int i = way.NodeIDs.IndexOf(signNode.ID);

        // Handle reversal of heading
        int next_i = i + 1;
        Vector3 intersection = map.nodes[way.NodeIDs[0]] - map.bounds.Centre;
        if (signDirection == "forward" || i == way.NodeIDs.Count - 1)
        {
            next_i = i - 1;
            intersection = map.nodes[way.NodeIDs[way.NodeIDs.Count - 1]] - map.bounds.Centre;
        }

        Vector3 s1 = map.nodes[signNode.ID] - map.bounds.Centre;
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

        ObjectPlacer.PlaceObject(sign, s1 + cross, yRotation, parent);
    }
}
