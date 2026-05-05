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

internal sealed class ImportMapWrapper
{
    private ImportMapDataEditorWindow _window;
    private string _mapFile;
    private bool _realTerrain;

    public ImportMapWrapper(ImportMapDataEditorWindow window, 
                            string mapFile,
                            bool realTerrain)
    {
        _window = window;
        _mapFile = mapFile;
        _realTerrain = realTerrain;
    }

    public void Import()
    {
        var mapReader = new MapReader();
        mapReader.Read(_mapFile);

        var buildingMaker = new BuildingMaker(mapReader);
        var roadMaker = new RoadMaker(mapReader);
        var terrainMaker = new TerrainMaker(mapReader, _realTerrain);

        Process(terrainMaker, "Creating Terrain");
        Process(roadMaker, "Generating road network");
        Process(buildingMaker, "Constructing buildings");
    }

    private void Process(BaseInfrastructureMaker maker, string progressText)
    {
        float nodeCount = maker.NodeCount;
        var progress = 0f;

        foreach (var node in maker.Process())
        {
            progress = node / nodeCount;
            _window.UpdateProgress(progress, progressText, false);
        }
        _window.UpdateProgress(0, string.Empty, true);
    }
}
