using System.Collections.Generic;
using System.Xml;

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
/// An OSM object that describes an arrangement of OsmNodes into a shape or road.
/// </summary>
public class OsmWay : BaseOsm
{
    /// <summary>
    /// Way ID.
    /// </summary>
    public ulong ID { get; private set; }

    /// <summary>
    /// True if visible.
    /// </summary>
    public bool Visible { get; private set; }

    /// <summary>
    /// List of node IDs.
    /// </summary>
    public List<ulong> NodeIDs { get; private set; }

    /// <summary>
    /// True if the way is a boundary.
    /// </summary>
    public bool IsBoundary { get; private set; }

    /// <summary>
    /// True if the way is a building.
    /// </summary>
    public bool IsBuilding { get; private set; }

    /// <summary>
    /// The number of floors in building.
    /// </summary>
    public float BuildingLevels { get; private set; }

    /// <summary>
    /// The type of building.
    /// </summary>
    public string BuildingType { get; private set; }

    /// <summary>
    /// The shape of building roof.
    /// </summary>
    public string RoofShape { get; private set; }

    /// <summary>
    /// True if the way is a road.
    /// </summary>
    public bool IsRoad { get; private set; }

    /// <summary>
    /// True if the way is a resedential road.
    /// </summary>
    public bool IsResidentialRoad { get; private set; }

    /// <summary>
    /// True if the way is an area or a square.
    /// </summary>
    public bool IsArea { get; private set; }

    /// <summary>
    /// True if the road has cycleway.
    /// </summary>
    public bool IsCycleway { get; private set; }

    /// <summary>
    /// True if the road has dirt surface.
    /// </summary>
    public bool IsDirt { get; private set; }

    /// <summary>
    /// True if the road is a pavement.
    /// </summary>
    public bool IsPavement { get; private set; }

    /// <summary>
    /// True if the road is a crossing path.
    /// </summary>
    public bool IsCrossing { get; private set; }

    /// <summary>
    /// True if the road contains lane markings.
    /// </summary>
    public bool IsLaneMarking { get; private set; }

    /// <summary>
    /// True if the road is a oneway.
    /// </summary>
    public bool IsOneway { get; private set; }

    /// <summary>
    /// Sidewalk type.
    /// </summary>
    public string Sidewalk { get; private set; }

    /// <summary>
    /// Height of the structure.
    /// </summary>
    public float Height { get; private set; }

    /// <summary>
    /// The name of the object.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The number of lanes on the road.
    /// </summary>
    public int Lanes { get; private set; }

    /// <summary>
    /// The half width of the road.
    /// </summary>
    public float Width { get; private set; }

    /// <summary>
    /// Traffic sign of the road.
    /// </summary>
    public string TrafficSign { get; private set; }

    /// <summary>
    /// True if the landuse is grass.
    /// </summary>
    public bool IsGrass { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="node"></param>
    public OsmWay(XmlNode node)
    {
        NodeIDs = new List<ulong>();
        Height = 3.5f * 3; // Default height for structures is 3 stories
        BuildingLevels = 3;
        Lanes = 1;      // Number of lanes either side of the divide 
        Name = "";

        // List of all highway types to add lane markings
        List<string> laneMarkingTypes = new List<string>();
        laneMarkingTypes.Add("motorway");
        laneMarkingTypes.Add("trunk");
        laneMarkingTypes.Add("primary");
        laneMarkingTypes.Add("secondary");
        laneMarkingTypes.Add("tertiary");

        // List of all surface types for dirt road
        List<string> dirtSurfaceTypes = new List<string>();
        dirtSurfaceTypes.Add("unpaved");
        dirtSurfaceTypes.Add("dirt");
        dirtSurfaceTypes.Add("fine_gravel");
        dirtSurfaceTypes.Add("gravel");
        dirtSurfaceTypes.Add("ground");

        // List of all surface types for pavement road
        List<string> pavementSurfaceTypes = new List<string>();
        pavementSurfaceTypes.Add("paving_stones");
        pavementSurfaceTypes.Add("sett");

        // Get the data from the attributes
        ID = GetAttribute<ulong>("id", node.Attributes);
        Visible = GetAttribute<bool>("visible", node.Attributes);

        // Get the nodes
        XmlNodeList nds = node.SelectNodes("nd");
        foreach(XmlNode n in nds)
        {
            ulong refNo = GetAttribute<ulong>("ref", n.Attributes);
            NodeIDs.Add(refNo);
        }

        if (NodeIDs.Count > 1)
        {
            IsBoundary = NodeIDs[0] == NodeIDs[NodeIDs.Count - 1];
        }

        // Read the tags
        XmlNodeList tags = node.SelectNodes("tag");
        foreach (XmlNode t in tags)
        {
            string key = GetAttribute<string>("k", t.Attributes);
            if (key == "building:levels")
            {
                Height = 3.5f * GetAttribute<float>("v", t.Attributes);
                BuildingLevels = GetAttribute<float>("v", t.Attributes);
            }
            else if (key == "height")
            {
                Height = GetAttribute<float>("v", t.Attributes);
            }
            else if (key == "building")
            {
                IsBuilding = true;
                BuildingType = GetAttribute<string>("v", t.Attributes);
            }
            else if (key == "roof:shape")
            {
                RoofShape = GetAttribute<string>("v", t.Attributes);
            }
            else if (key == "highway")
            {
                if (GetAttribute<string>("v", t.Attributes) != "crossing")
                {
                    IsRoad = true;
                    IsLaneMarking = laneMarkingTypes.Contains(GetAttribute<string>("v", t.Attributes));

                    if (GetAttribute<string>("v", t.Attributes) == "residential")
                    {
                        IsResidentialRoad = true;
                    }
                }
            }
            else if (key == "cycleway")
            {
                IsCycleway = GetAttribute<string>("v", t.Attributes) == "lane";
            }
            else if (key == "cycleway:both")
            {
                IsCycleway = GetAttribute<string>("v", t.Attributes) == "lane";
            }
            else if (key == "cycleway:right")
            {
                IsCycleway = GetAttribute<string>("v", t.Attributes) == "track";
                IsCycleway = GetAttribute<string>("v", t.Attributes) == "lane";
            }
            else if (key == "lanes")
            {
                Lanes = GetAttribute<int>("v", t.Attributes);
            }
            else if (key == "name")
            {
                Name = GetAttribute<string>("v", t.Attributes);
            }
            else if (key == "surface")
            {
                IsDirt = dirtSurfaceTypes.Contains(GetAttribute<string>("v", t.Attributes));
                IsPavement = pavementSurfaceTypes.Contains(GetAttribute<string>("v", t.Attributes));
            }
            else if (key == "path")
            {
                IsCrossing = GetAttribute<string>("v", t.Attributes) == "crossing";
            }
            else if (key == "footway")
            {
                IsCrossing = GetAttribute<string>("v", t.Attributes) == "crossing";
                IsPavement = GetAttribute<string>("v", t.Attributes) == "sidewalk";
            }
            else if (key == "oneway")
            {
                IsOneway = GetAttribute<string>("v", t.Attributes) == "yes";
            }
            else if (key == "sidewalk")
            {
                Sidewalk = GetAttribute<string>("v", t.Attributes);
            }
            else if (key == "sidewalk:right")
            {
                Sidewalk = GetAttribute<string>("v", t.Attributes) == "yes" ? "right":"";
            }
            else if (key == "sidewalk:left")
            {
                Sidewalk = GetAttribute<string>("v", t.Attributes) == "yes" ? "left" : "";
            }
            else if (key.Contains("traffic_sign"))
            {
                TrafficSign = GetAttribute<string>("v", t.Attributes);
            }
            else if (key == "area")
            {
                IsArea = GetAttribute<string>("v", t.Attributes) == "yes";
            }
            else if (key == "place")
            {
                IsArea = GetAttribute<string>("v", t.Attributes) == "square";
            }
            else if (key == "landuse")
            {
                IsGrass = GetAttribute<string>("v", t.Attributes) == "grass";
            }
        }

        if ((IsResidentialRoad || IsOneway) && Lanes == 1)
        {
            Width = 3.75f * (Lanes + 1) / 2;
        }
        else
        {
            Width = 3.75f * Lanes / 2;
        }

        if (IsCycleway)
        {
            Width += 2;
        }
    }
}

