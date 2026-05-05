using System.Collections.Generic;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

internal class Lane
{
    private MapReader map;
    
    private Material white = Resources.Load<Material>("White");
    
    private GameObject bicycleMarking = Resources.Load<GameObject>("Bicycle Road Marking");

    public Lane(MapReader map)
    {
        this.map = map;
    }
    public void CreateLaneMarkings(OsmWay way, List<Vector3> centreLane, List<Vector3> rightBorder, List<Vector3> leftBorder, List<Vector3> centreLaneCutOff, List<Vector3> intersectionPoints, GameObject parent)
    {
        GameObject lane = new GameObject("Lane");
        lane.transform.parent = parent.transform;

        CreateLineRenderers(leftBorder, 0, 0.1f, white, lane);
        if (!way.IsOneway)
        {
            CreateLineRenderers(rightBorder, 0, 0.1f, white, lane);
        }

        for (int i = 1; i < way.Lanes / 2; i++)
        {
            var (rightLanePts, leftLanePts, _) = Utils.OffsetPoints(Utils.CreateEvenlySpacedPoints(centreLane, 2f), 3.5f * i);
            CreateLineRenderers(rightLanePts, 3, 0.1f, white, lane);
            CreateLineRenderers(leftLanePts, 3, 0.1f, white, lane);
        }

        // Create bicycle lane markings
        if (way.IsCycleway)
        {
            var (rightLanePts, leftLanePts, rotations) = Utils.OffsetPoints(Utils.CreateEvenlySpacedPoints(centreLane, 1f), way.Width - 2);
            CreateLineRenderers(leftLanePts, 2, 0.1f, white, lane);
            // One way roads have only one lane
            if (way.Lanes != 1)
            {
                CreateLineRenderers(rightLanePts, 2, 0.1f, white, lane);
            }

            // Place bicycle markings
            List<GameObject> gameObjects = new List<GameObject>(1);
            gameObjects.Add(bicycleMarking);
            List<GameObject> parents = new List<GameObject>(1);
            parents.Add(parent);

            (rightLanePts, leftLanePts, rotations) = Utils.OffsetPoints(Utils.CreateEvenlySpacedPoints(centreLane, 30f), way.Width - 1);
            ObjectPlacer.PlaceObjects(gameObjects, leftLanePts, rotations, true, parents);
            if (way.Lanes != 1)
            {
                ObjectPlacer.PlaceObjects(gameObjects, rightLanePts, rotations, false, parents);
            }

            // EvenlyPlaceObjects(gameObjects, centreLane, 30f, (3.5f * way.Lanes / 2) + 1, parents);
        }

        if (way.Lanes != 1)
        {
            // Solid line if more than or equal to 4 lanes else broken line
            if (way.Lanes >= 4)
            {
                CreateLineRenderers(centreLaneCutOff, 0, 0.1f, white, lane);
            }
            else
            {
                CreateLineRenderers(Utils.CreateEvenlySpacedPoints(centreLaneCutOff, 2f), 3, 0.1f, white, lane);
            }

            if (intersectionPoints.Count > 0)
            {
                CreateLineRenderers(Utils.CreateEvenlySpacedPoints(intersectionPoints, 2f), 3, 0.1f, white, lane);
            }
        }

        // Rotate the parent to fix line render
        lane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public static void CreateLineRenderers(List<Vector3> positions, int gap, float lineWidth, Material material, GameObject parent)
    {
        if (gap == 0)
        {
            var points = new Vector3[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 point = Utils.GetTerrainAlignedPosition(positions[i]);
                points[i] = new Vector3(point.x, point.y + 0.01f, point.z);
            }
            var newRenderer = new GameObject("Lane Marking");

            var lineRenderer = newRenderer.AddComponent<LineRenderer>();
            lineRenderer.widthCurve = new AnimationCurve
            {

                keys = new Keyframe[2]
                {
                    new Keyframe(0, lineWidth),
                    new Keyframe(1, lineWidth)
                }
            };
            lineRenderer.colorGradient = new Gradient
            {
                colorKeys = new GradientColorKey[1]
                {
                    new GradientColorKey(Color.white, 0)
                }
            };

            lineRenderer.material = material;
            lineRenderer.loop = false;
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.generateLightingData = true;

            newRenderer.transform.parent = parent.transform;
        }
        else
        {
            var dashPts = new Vector3[2];

            for (int i = 0; i < positions.Count - gap; i = i + gap)
            {
                Vector3 point1 = Utils.GetTerrainAlignedPosition(positions[i]);
                Vector3 point2 = Utils.GetTerrainAlignedPosition(positions[i + 1]);

                dashPts[0] = new Vector3(point1.x, point1.y + 0.01f, point1.z);
                dashPts[1] = new Vector3(point2.x, point2.y + 0.01f, point2.z);

                var newRenderer = new GameObject("Lane Marking");

                var lineRenderer = newRenderer.AddComponent<LineRenderer>();
                lineRenderer.widthCurve = new AnimationCurve
                {

                    keys = new Keyframe[2]
                    {
                    new Keyframe(0, lineWidth),
                    new Keyframe(1, lineWidth)
                    }
                };
                lineRenderer.colorGradient = new Gradient
                {
                    colorKeys = new GradientColorKey[1]
                    {
                    new GradientColorKey(Color.white, 0)
                    }
                };

                lineRenderer.material = material;
                lineRenderer.loop = false;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(dashPts);
                lineRenderer.alignment = LineAlignment.TransformZ;
                lineRenderer.generateLightingData = true;

                newRenderer.transform.parent = parent.transform;
            }
        }
    }
}
