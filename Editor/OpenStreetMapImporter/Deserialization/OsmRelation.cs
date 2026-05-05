using System.Xml;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

/// <summary>
/// An OSM object that describes an arrangement of OsmNodes into a shape or road.
/// </summary>
public class OsmRelation : BaseOsm
{
    /// <summary>
    /// Way ID.
    /// </summary>
    public ulong ID { get; private set; }

    /// <summary>
    /// True if the way is a building.
    /// </summary>
    public bool IsBuilding { get; private set; }

    /// <summary>
    /// The type of building.
    /// </summary>
    public string BuildingType { get; private set; }

    /// <summary>
    /// The ID of outer way if Building.
    /// </summary>
    public ulong outerWayID { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="node"></param>
    public OsmRelation(XmlNode node)
    {
        // Get the data from the attributes
        ID = GetAttribute<ulong>("id", node.Attributes);

        // Read the tags
        XmlNodeList tags = node.SelectNodes("tag");
        foreach (XmlNode t in tags)
        {
            string key = GetAttribute<string>("k", t.Attributes);
            if (key == "building")
            {
                IsBuilding = true;
                BuildingType = GetAttribute<string>("v", t.Attributes);
            }
        }

        // Read the members and add only outer way if building
        XmlNodeList members = node.SelectNodes("member");
        foreach (XmlNode m in members)
        {
            string type = GetAttribute<string>("type", m.Attributes);
            ulong wayid = GetAttribute<ulong>("ref", m.Attributes);
            string role = GetAttribute<string>("role", m.Attributes);

            if (IsBuilding && type == "way" && role == "outer")
            {
                outerWayID = wayid;
            }
        }
    }
}

