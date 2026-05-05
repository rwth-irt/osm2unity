using System;
using System.Collections.Generic;
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
/// Base infrastructure creator.
/// </summary>
internal abstract class BaseInfrastructureMaker
{
    /// <summary>
    /// The map reader object; contains all the data to build procedural geometry.
    /// </summary>
    protected MapReader map;

    /// <summary>
    /// The number of nodes present of this type in a file.
    /// </summary>
    public abstract int NodeCount { get; }

    /// <summary>
    /// Awaken this instance.
    /// </summary>
    public BaseInfrastructureMaker(MapReader mapReader)
    {
        map = mapReader;
    }
    
    /// <summary>
    /// Process the nodes to create the geometry.
    /// </summary>
    /// <returns></returns>
    public abstract IEnumerable<int> Process();

    /// <summary>
    /// Get the centre of an object or road.
    /// </summary>
    /// <param name="way">OsmWay object</param>
    /// <returns>The centre point of the object</returns>
    protected Vector3 GetCentre(OsmWay way)
    {
        Vector3 total = Vector3.zero;

        foreach (var id in way.NodeIDs)
        {
            total += map.nodes[id];
        }

        return total / way.NodeIDs.Count;
    }

    /// <summary>
    /// Procedurally generate an object from the data given in the OsmWay instance.
    /// </summary>
    /// <param name="way">OsmWay instance</param>
    /// <param name="mat">Material to apply to the instance</param>
    /// <param name="objectName">The name of the object (building name, road etc.)</param>
    protected void CreateObject(OsmWay way, Material mat, string objectName, GameObject parent)
    {
        // Make sure we have some name to display
        objectName = string.IsNullOrEmpty(objectName) ? "OsmWay" : objectName;

        // Create an instance of the object and place it in the centre of its points
        GameObject go = new GameObject(objectName);
        Vector3 localOrigin = GetCentre(way);
        go.transform.position = localOrigin - map.bounds.Centre;

        // Add the mesh filter and renderer components to the object
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        // Apply the material
        mr.material = mat;

        // Create the collections for the object's vertices, indices, UVs etc.
        List<Vector3> vectors = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        // Call the child class' object creation code
        OnObjectCreated(way, localOrigin, vectors, normals, uvs, indices, go);

        // Apply the data to the mesh
        mf.sharedMesh = new Mesh();
        mf.sharedMesh.vertices = vectors.ToArray();
        mf.sharedMesh.normals = normals.ToArray();
        mf.sharedMesh.triangles = indices.ToArray();
        mf.sharedMesh.uv = uvs.ToArray();

        MeshCollider collider = go.AddComponent<MeshCollider>();

        go.transform.parent = parent.transform;
    }

    protected virtual void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices, GameObject parent)
    {
        throw new NotImplementedException("Subclass must implement this!");
    }
}
