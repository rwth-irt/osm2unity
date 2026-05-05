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
/// OSM node.
/// </summary>
public class OsmNode : BaseOsm
{
    /// <summary>
    /// Node ID.
    /// </summary>
    public ulong ID { get; private set; }

    /// <summary>
    /// Latitude position of the node.
    /// </summary>
    public float Latitude { get; private set; }

    /// <summary>
    /// Longitude position of the node.
    /// </summary>
    public float Longitude { get; private set; }

    /// <summary>
    /// Unity unit X-co-ordinate.
    /// </summary>
    public float X { get; private set; }

    /// <summary>
    /// Unity unit Y-co-ordinate.
    /// </summary>
    public float Y { get; private set; }

    /// <summary>
    /// True if the node is a traffic signal.
    /// </summary>
    public bool IsSignal { get; private set; }
    public string SignalDirection { get; private set; }

    /// <summary>
    /// True if the node is a crossing.
    /// </summary>
    public bool IsCrossing { get; private set; }

    /// <summary>
    /// True if the node is a stop sign.
    /// </summary>
    public bool IsStop { get; private set; }

    /// <summary>
    /// Traffic Sign Name
    /// </summary>
    public string TrafficSign { get; private set; }

    /// <summary>
    /// Implicit conversion between OsmNode and Vector3.
    /// </summary>
    /// <param name="node">OsmNode instance</param>
    public static implicit operator Vector3 (OsmNode node)
    {
        return new Vector3(node.X, 0, node.Y);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="node">Xml node</param>
    public OsmNode(XmlNode node)
    {
        // Get the attribute values
        ID = GetAttribute<ulong>("id", node.Attributes);
        Latitude = GetAttribute<float>("lat", node.Attributes);
        Longitude = GetAttribute<float>("lon", node.Attributes);

        // Calculate the position in Unity units
        X = (float)MercatorProjection.lonToX(Longitude);
        Y = (float)MercatorProjection.latToY(Latitude);

        // Read the tags
        XmlNodeList tags = node.SelectNodes("tag");
        foreach (XmlNode t in tags)
        {
            string key = GetAttribute<string>("k", t.Attributes);
            if (key == "highway")
            {
                IsSignal = GetAttribute<string>("v", t.Attributes) == "traffic_signals";
                IsCrossing = GetAttribute<string>("v", t.Attributes) == "crossing";
                IsStop = GetAttribute<string>("v", t.Attributes) == "stop";

                if (GetAttribute<string>("v", t.Attributes) == "give_way")
                {
                    TrafficSign = GetAttribute<string>("v", t.Attributes);
                }
            }
            if (key == "traffic_signals:direction")
            {
                SignalDirection = GetAttribute<string>("v", t.Attributes);
            }
            if (key == "traffic_sign")
            {
                TrafficSign = GetAttribute<string>("v", t.Attributes);
            }
        }
    }
}

