using System.Collections.Generic;
using System.IO;
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

internal sealed class MapReader
{
    [HideInInspector]
    public Dictionary<ulong, OsmNode> nodes;

    [HideInInspector]
    public List<OsmWay> ways;

    [HideInInspector]
    public List<OsmRelation> relations;

    [HideInInspector]
    public OsmBounds bounds;

    public float minx;
    public float maxx;
    public float miny;
    public float maxy;

    /// <summary>
    /// Load the OpenMap data resource file.
    /// </summary>
    /// <param name="resourceFile">Path to the resource file. The file must exist.</param>
	public void Read(string resourceFile)
    {
        nodes = new Dictionary<ulong, OsmNode>();
        ways = new List<OsmWay>();
        relations = new List<OsmRelation>();

        var xmlText = File.ReadAllText(resourceFile);

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlText);

        SetBounds(doc.SelectSingleNode("/osm/bounds"));
        GetNodes(doc.SelectNodes("/osm/node"));
        GetWays(doc.SelectNodes("/osm/way"));
        GetRelations(doc.SelectNodes("/osm/relation"));

        minx = (float)MercatorProjection.lonToX(bounds.MinLon);
        maxx = (float)MercatorProjection.lonToX(bounds.MaxLon);
        miny = (float)MercatorProjection.latToY(bounds.MinLat);
        maxy = (float)MercatorProjection.latToY(bounds.MaxLat);
    }

    void GetWays(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode node in xmlNodeList)
        {
            OsmWay way = new OsmWay(node);
            ways.Add(way);
        }
    }

    void GetNodes(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode n in xmlNodeList)
        {
            OsmNode node = new OsmNode(n);
            nodes[node.ID] = node;
        }
    }

    void SetBounds(XmlNode xmlNode)
    {
        bounds = new OsmBounds(xmlNode);
    }

    void GetRelations(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode node in xmlNodeList)
        {
            OsmRelation relation = new OsmRelation(node);
            relations.Add(relation);
        }
    }
}
