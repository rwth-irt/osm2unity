using System.Xml;
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
/// OSM map bounds.
/// </summary>
public class OsmBounds : BaseOsm
{
    /// <summary>
    /// Minimum latitude (y-axis)
    /// </summary>
    public float MinLat { get; private set; }

    /// <summary>
    /// Maximum latitude (y-axis)
    /// </summary>
    public float MaxLat { get; private set; }

    /// <summary>
    /// Minimum longitude (x-axis)
    /// </summary>
    public float MinLon { get; private set; }

    /// <summary>
    /// Maximum longitude (x-axis)
    /// </summary>
    public float MaxLon { get; private set; }

    /// <summary>
    /// Centre of the map in Unity units.
    /// </summary>
    public Vector3 Centre { get; private set; }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="node">Xml node</param>
    public OsmBounds(XmlNode node)
    {
        if (node != null && node.Attributes != null)
        {
            // Get values from the node
            MinLat = GetAttribute<float>("minlat", node.Attributes);
            MaxLat = GetAttribute<float>("maxlat", node.Attributes);
            MinLon = GetAttribute<float>("minlon", node.Attributes);
            MaxLon = GetAttribute<float>("maxlon", node.Attributes);
        }
        else
        {
            // Default bounds if node is missing
            Debug.Log("No OSM bounds node found. Using default bounds.");
            MinLat = -1f;
            MaxLat = 1f;
            MinLon = -1f;
            MaxLon = 1f;
        }

        // Create the centre location
        float x = (float)((MercatorProjection.lonToX(MaxLon) + MercatorProjection.lonToX(MinLon)) / 2);
        float y = (float)((MercatorProjection.latToY(MaxLat) + MercatorProjection.latToY(MinLat)) / 2);
        Centre = new Vector3(x, 0, y);
    }
}