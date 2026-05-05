using System.Collections.Generic;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

public static class ObjectPlacer
{
    public static void EvenlyPlaceObjects(List<GameObject> gameObjects, List<Vector3> centerLane, float spacing, float offset, List<GameObject> parents)
    {
        var (rightLanePts, leftLanePts, rotations) = Utils.OffsetPoints(Utils.CreateEvenlySpacedPoints(centerLane, spacing), offset);
        PlaceObjects(gameObjects, rightLanePts, rotations, false, parents);
        PlaceObjects(gameObjects, leftLanePts, rotations, true, parents);
    }

    public static void PlaceObjects(List<GameObject> gameObjects, List<Vector3> points, List<Quaternion> rotations, bool opposite, List<GameObject> parents)
    {
        for (int i = 1; i < points.Count; i++)
        {
            GameObject placedObject = null;
            var objIndx = i % gameObjects.Count;

            if (gameObjects[objIndx].name == "Street Light")
            {
                if ((i + 3) % 4 == 0 && !opposite)
                {
                    continue;
                }
                else if ((i + 1) % 4 == 0 && opposite)
                {
                    continue;
                }
            }

            placedObject = GameObject.Instantiate(gameObjects[objIndx], parents[objIndx].transform);
            placedObject.transform.position = Utils.GetTerrainAlignedPosition(points[i]);
            placedObject.transform.rotation = rotations[i];

            if (opposite && placedObject)
            {
                var currRot = placedObject.transform.rotation.eulerAngles;
                placedObject.transform.rotation = Quaternion.Euler(currRot.x, currRot.y + 180, currRot.z);
            }
        }
    }

    public static void PlaceObject(GameObject gameObject, Vector3 pose, Quaternion rotation, GameObject parent)
    {
        Vector3 position = new Vector3(pose.x, gameObject.transform.position.y, pose.z);
        var placedObject = GameObject.Instantiate(gameObject, parent.transform);
        placedObject.transform.SetPositionAndRotation(Utils.GetTerrainAlignedPosition(position), rotation);
    }
}
