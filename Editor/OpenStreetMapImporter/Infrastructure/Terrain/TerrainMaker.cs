using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

/// <summary>
/// Create Terrain - Flat or Uneven.
/// </summary>
internal sealed class TerrainMaker : BaseInfrastructureMaker
{
    private Texture2D terrainTexture = Resources.Load<Texture2D>("Grass Surface");
    private TextAsset ascFile = Resources.Load<TextAsset>("dgm200_utm32s");

    private Texture2D grassTexture = Resources.Load<Texture2D>("Grass");
    private Texture2D grassFlowerTexture = Resources.Load<Texture2D>("Grass Flower");

    // Import real heightmap
    private bool realTerrain = false;
    // Max Elevation
    private float terrainHeight = 1000f;
    // Terrain heightmap res
    private int terrainResolution = 1025;
    // Extension value to the lat and lon of map
    private float extension = 0.01f;

    public override int NodeCount
    {
        // override node count to one
        get { return 1; }
    }

    public TerrainMaker(MapReader mapReader, bool _realTerrain)
    : base(mapReader)
    {
        realTerrain = _realTerrain;
    }

    public override IEnumerable<int> Process()
    {
        CreateTerrain(realTerrain);
        yield return 1;
    }

    DetailPrototype CreateGrassPrototype(Texture2D grassTexture)
    {
        DetailPrototype prototype = new DetailPrototype();
        prototype.prototypeTexture = grassTexture;
        prototype.renderMode = DetailRenderMode.GrassBillboard;
        prototype.healthyColor = new Color(0.87f, 0.93f, 0.51f);
        prototype.dryColor = new Color(0.15f, 0.23f, 0.10f);
        prototype.minWidth = 1.0f;
        prototype.maxWidth = 2.0f;
        prototype.minHeight = 0.5f;
        prototype.maxHeight = 1.0f;

        return prototype;
    }

    DetailPrototype CreateGrassFlowerPrototype(Texture2D grassFlowerTexture)
    {
        DetailPrototype prototype = new DetailPrototype();
        prototype.prototypeTexture = grassFlowerTexture;
        prototype.renderMode = DetailRenderMode.GrassBillboard;
        prototype.healthyColor = new Color(1f, 1f, 1f);
        prototype.dryColor = new Color(0.84f, 0.84f, 0.53f);
        prototype.minWidth = 1.0f;
        prototype.maxWidth = 1.0f;
        prototype.minHeight = 1.0f;
        prototype.maxHeight = 1.0f;

        return prototype;
    }

    public void SetupGrassLayer(Terrain terrain, Texture2D grassTexture)
    {
        TerrainData td = terrain.terrainData;

        // DetailPrototype grass = CreateGrassPrototype(grassTexture);
        DetailPrototype grass = CreateGrassFlowerPrototype(grassFlowerTexture);
        td.detailPrototypes = new DetailPrototype[] { grass };

        terrain.terrainData.wavingGrassTint = new Color(0.57f, 0.6f, 0.48f);
    }

    public void ApplyGrass(Terrain terrain, List<Vector3> grassPolygonWorld, int layerIndex = 0, int density = 8)
    {
        TerrainData td = terrain.terrainData;
        int detailWidth = td.detailWidth;
        int detailHeight = td.detailHeight;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = td.size;

        int[,] detailMap = td.GetDetailLayer(0, 0, detailWidth, detailHeight, layerIndex);

        // Compute bounding box of polygon
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var pt in grassPolygonWorld)
        {
            minX = Mathf.Min(minX, pt.x);
            maxX = Mathf.Max(maxX, pt.x);
            minZ = Mathf.Min(minZ, pt.z);
            maxZ = Mathf.Max(maxZ, pt.z);
        }

        // Convert bounding box to detail map range
        int minDetailX = Mathf.Clamp(Mathf.FloorToInt((minX - terrainPos.x) / terrainSize.x * detailWidth), 0, detailWidth - 1);
        int maxDetailX = Mathf.Clamp(Mathf.CeilToInt((maxX - terrainPos.x) / terrainSize.x * detailWidth), 0, detailWidth - 1);
        int minDetailY = Mathf.Clamp(Mathf.FloorToInt((minZ - terrainPos.z) / terrainSize.z * detailHeight), 0, detailHeight - 1);
        int maxDetailY = Mathf.Clamp(Mathf.CeilToInt((maxZ - terrainPos.z) / terrainSize.z * detailHeight), 0, detailHeight - 1);

        // Loop only inside bounding box
        for (int y = minDetailY; y <= maxDetailY; y++)
        {
            for (int x = minDetailX; x <= maxDetailX; x++)
            {
                // Convert detail coords back to world space
                float worldX = terrainPos.x + ((float)x / detailWidth) * terrainSize.x;
                float worldZ = terrainPos.z + ((float)y / detailHeight) * terrainSize.z;
                Vector3 worldPoint = new Vector3(worldX, 0, worldZ);

                if (PointInPolygon(worldPoint, grassPolygonWorld))
                {
                    detailMap[y, x] = density;
                }
            }
        }

        td.SetDetailLayer(0, 0, layerIndex, detailMap);
    }

    public void CreateTerrain(bool realTerrain)
    {
        // Extend the lat and lon of map bounds for the terrain
        double minLon = map.bounds.MinLon - extension;
        double maxLon = map.bounds.MaxLon + extension;
        double minLat = map.bounds.MinLat - extension;
        double maxLat = map.bounds.MaxLat + extension;

        // Convert to mercator position
        float minx = (float)MercatorProjection.lonToX(minLon);
        float maxx = (float)MercatorProjection.lonToX(maxLon);
        float miny = (float)MercatorProjection.latToY(minLat);
        float maxy = (float)MercatorProjection.latToY(maxLat);

        // Longitude
        float terrainWidth = maxx - minx;
        // Latitude
        float terrainLength = maxy - miny;

        // Create TerrainData
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = terrainResolution;
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        // Flat heightmap
        float[,] heightmap = new float[terrainResolution, terrainResolution];
        
        // Uneven heightmap
        if (realTerrain)
        {
            // Convert the lat and lon to UTM coordinates
            UTMCoordinates minUTM = LatLonToUTM(minLat, minLon);
            UTMCoordinates maxUTM = LatLonToUTM(maxLat, maxLon);

            heightmap = LoadRealHeightmap((float)minUTM.Easting, (float)maxUTM.Easting, (float)minUTM.Northing, (float)maxUTM.Northing, terrainResolution);
        }

        LogMatrix(heightmap);
        terrainData.SetHeights(0, 0, heightmap);

        // Create Terrain GameObject
        GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "Terrain";

        // Set position so center matches (X,Z)
        terrainGO.transform.position = new Vector3(-terrainWidth/2f, 0f, -terrainLength/2f);

        // Create a new TerrainLayer
        TerrainLayer terrainLayer = new TerrainLayer();
        terrainLayer.diffuseTexture = terrainTexture;
        terrainLayer.tileSize = new Vector2(5, 5);

        // Apply to terrain
        terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };

        AddTerrainDetail();
    }

    public void AddTerrainDetail()
    {
        Terrain terrain = Terrain.activeTerrain;

        // Set the detail res for grass
        terrain.terrainData.SetDetailResolution(2048, 128); // 2048x2048 map, 128 samples per patch

        SetupGrassLayer(terrain, grassTexture);

        // Iterate through the polygons with grass
        foreach (var way in map.ways.FindAll(w => w.IsGrass))
        {
            List<Vector3> grassPolygon = new List<Vector3>();

            for (int i = 0; i < way.NodeIDs.Count; i++)
            {
                OsmNode p = map.nodes[way.NodeIDs[i]];
                grassPolygon.Add(p - map.bounds.Centre);
            }

            ApplyGrass(terrain, grassPolygon);
        }
    }

    public struct UTMCoordinates
    {
        public double Easting;
        public double Northing;
        public int Zone;
        public char Hemisphere;
    }

    public UTMCoordinates LatLonToUTM(double latitude, double longitude)
    {
        // WGS84 constants
        double a = 6378137.0;                // Equatorial radius
        double f = 1 / 298.257223563;         // Flattening
        double eSquared = f * (2 - f);        // First eccentricity squared
        double ePrimeSquared = eSquared / (1 - eSquared);

        const double k0 = 0.9996;             // UTM scale factor

        // Calculate zone
        int zoneNumber = (int)((longitude + 180) / 6) + 1;
        double lonOrigin = (zoneNumber - 1) * 6 - 180 + 3;

        // Convert to radians
        double latRad = latitude * Math.PI / 180.0;
        double lonRad = longitude * Math.PI / 180.0;
        double lonOriginRad = lonOrigin * Math.PI / 180.0;

        double N = a / Math.Sqrt(1 - eSquared * Math.Sin(latRad) * Math.Sin(latRad));
        double T = Math.Tan(latRad) * Math.Tan(latRad);
        double C = ePrimeSquared * Math.Cos(latRad) * Math.Cos(latRad);
        double A = Math.Cos(latRad) * (lonRad - lonOriginRad);

        // Meridional Arc
        double M = a * (
            (1 - eSquared / 4 - 3 * eSquared * eSquared / 64 - 5 * Math.Pow(eSquared, 3) / 256) * latRad
            - (3 * eSquared / 8 + 3 * eSquared * eSquared / 32 + 45 * Math.Pow(eSquared, 3) / 1024) * Math.Sin(2 * latRad)
            + (15 * eSquared * eSquared / 256 + 45 * Math.Pow(eSquared, 3) / 1024) * Math.Sin(4 * latRad)
            - (35 * Math.Pow(eSquared, 3) / 3072) * Math.Sin(6 * latRad)
        );

        // Calculate Easting and Northing
        double easting = (k0 * N * (A + (1 - T + C) * Math.Pow(A, 3) / 6
            + (5 - 18 * T + T * T + 72 * C - 58 * ePrimeSquared) * Math.Pow(A, 5) / 120) + 500000.0);

        double northing = (k0 * (M + N * Math.Tan(latRad) * (A * A / 2
            + (5 - T + 9 * C + 4 * C * C) * Math.Pow(A, 4) / 24
            + (61 - 58 * T + T * T + 600 * C - 330 * ePrimeSquared) * Math.Pow(A, 6) / 720)));

        // Correct for southern hemisphere
        char hemisphere = (latitude >= 0) ? 'N' : 'S';
        if (latitude < 0)
            northing += 10000000.0;

        return new UTMCoordinates
        {
            Easting = easting,
            Northing = northing,
            Zone = zoneNumber,
            Hemisphere = hemisphere
        };
    }
    
    private float[,] LoadRealHeightmap(float E_min, float E_max, float N_min, float N_max, int targetResolution)
    {
        string[] lines = ascFile.text.Split('\n');

        // ASC Header
        int ncols = int.Parse(lines[0].Split()[1]);
        int nrows = int.Parse(lines[1].Split()[1]);
        float xllcenter = float.Parse(lines[2].Split()[1]);
        float yllcenter = float.Parse(lines[3].Split()[1]);
        float cellsize = float.Parse(lines[4].Split()[1]);
        float noData = float.Parse(lines[5].Split()[1]);

        // Lower-left corner UTM coordinates
        float xllcorner = xllcenter - (cellsize / 2.0f);
        float yllcorner = yllcenter - (cellsize / 2.0f);

        // Crop bounds to grid
        int xStart = Mathf.Clamp(Mathf.RoundToInt((float)((E_min - xllcorner) / cellsize)), 0, ncols - 1);
        int xEnd = Mathf.Clamp(Mathf.RoundToInt((float)((E_max - xllcorner) / cellsize)), 0, ncols - 1);
        int yStart = Mathf.Clamp(Mathf.RoundToInt((float)((N_min - yllcorner) / cellsize)), 0, nrows - 1);
        int yEnd = Mathf.Clamp(Mathf.RoundToInt((float)((N_max - yllcorner) / cellsize)), 0, nrows - 1);

        int croppedWidth = xEnd - xStart + 1;
        int croppedHeight = yEnd - yStart + 1;

        float[,] croppedHeights = new float[croppedHeight, croppedWidth];

        for (int y = 0; y < croppedHeight; y++)
        {
            int srcY = nrows - 1 - (yStart + y); // Flip Y
            string[] row = lines[6 + srcY].Trim().Split(' ');

            for (int x = 0; x < croppedWidth; x++)
            {
                int srcX = xStart + x;
                if (srcX < row.Length && float.TryParse(row[srcX], out float val))
                {
                    if (val == noData) val = 0f;
                    croppedHeights[y, x] = val / terrainHeight; // Between 0-1
                }
                else
                {
                    croppedHeights[y, x] = 0f;
                }
            }
        }

        // Resample to Unity-compatible resolution
        return ResampleHeightmap(croppedHeights, croppedWidth, croppedHeight, targetResolution, targetResolution);
    }

    private float[,] ResampleHeightmap(float[,] src, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        float[,] dst = new float[dstHeight, dstWidth];

        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                float srcX = (float)x / (dstWidth - 1) * (srcWidth - 1);
                float srcY = (float)y / (dstHeight - 1) * (srcHeight - 1);

                int x0 = Mathf.FloorToInt(srcX);
                int y0 = Mathf.FloorToInt(srcY);

                int x1 = Mathf.Min(x0 + 1, srcWidth - 1);
                int y1 = Mathf.Min(y0 + 1, srcHeight - 1);

                float dx = srcX - x0;
                float dy = srcY - y0;

                // Interpolate between 4 points
                float top = Mathf.Lerp(src[y0, x0], src[y0, x1], dx);
                float bottom = Mathf.Lerp(src[y1, x0], src[y1, x1], dx);
                dst[y, x] = Mathf.Lerp(top, bottom, dy);
            }
        }

        return dst;
    }

    private void LogMatrix(float[,] heightmap)
    {
        StringBuilder sb = new StringBuilder();

        int width = heightmap.GetLength(1);
        int height = heightmap.GetLength(0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(heightmap[y, x].ToString("F3")).Append(" ");
            }
            sb.AppendLine();
        }

        // Debug.Log(sb.ToString());
    }

    public bool PointInPolygon(Vector3 point, List<Vector3> polygon)
    {
        // Debug.Log(point);
        // Debug.Log(polygon[0]);
        // Debug.Log("--------------");

        int crossingNumber = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 a = polygon[i];
            Vector3 b = polygon[(i + 1) % polygon.Count];

            if (((a.z > point.z) != (b.z > point.z)) &&
                (point.x < (b.x - a.x) * (point.z - a.z) / (b.z - a.z + 0.0001f) + a.x))
            {
                crossingNumber++;
            }
        }

        return (crossingNumber % 2 == 1);
    }
}
