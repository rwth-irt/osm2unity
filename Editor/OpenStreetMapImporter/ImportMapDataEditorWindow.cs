using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * Contains portions of code from: Copyright (c) 2017 Sloan Kelly 
 * [Licensed under the MIT License]
 *
 * See LICENSE file for full license text.
 */

public class ImportMapDataEditorWindow : EditorWindow
{
    private Material _roadMaterial;
    private Material _buildingMaterial;
    private GameObject _treeObject;
    private GameObject _streetLightObject;
    private string _mapFilePath = "None (Choose OpenStreetMap File)";
    private string _progressText;
    private float _progress;
    private bool _disableUI;
    private bool _validFile;
    private bool _importing;
    private bool realTerrian = false;

    [MenuItem("Window/Import OpenStreetMap Data")]
    public static void ShowEditorWindow()
    {
        var window = GetWindow<ImportMapDataEditorWindow>();
        window.titleContent = new GUIContent("Import OpenStreetMap");
        window.Show();
    }

    public void ResetProgress()
    {
        _progress = 0f;
        _progressText = "";
    }

    public void UpdateProgress(float progress, string progressText, bool done)
    {
        _progress = progress;
        _progressText = progressText;

        if (!done)
            EditorUtility.DisplayProgressBar("Importing Map",
                                             string.Format("{0} {1:%}", progressText, progress), 
                                             progress);
        else
            EditorUtility.ClearProgressBar();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(_mapFilePath);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("..."))
        {
            var filePath = EditorUtility.OpenFilePanel("Select OpenStreetMap File",
                                                       Application.dataPath,
                                                       "osm");
            if (filePath.Length > 0)
                _mapFilePath = filePath;

            _validFile = _mapFilePath.Length > 0;
        }
        
        EditorGUILayout.EndHorizontal();

        /*
        _roadMaterial = EditorGUILayout.ObjectField("Road Material",
                                                    _roadMaterial,
                                                    typeof(Material),
                                                    false) as Material;
        _buildingMaterial = EditorGUILayout.ObjectField("Building Material",
                                                        _buildingMaterial,
                                                        typeof(Material),
                                                        false) as Material;
        _treeObject = EditorGUILayout.ObjectField("Tree",
                                                _treeObject,
                                                typeof(GameObject),
                                                false) as GameObject;
        _streetLightObject = EditorGUILayout.ObjectField("Street Light",
                                        _streetLightObject,
                                        typeof(GameObject),
                                        false) as GameObject;
        */

        realTerrian = EditorGUILayout.Toggle("Import Real Terrain", realTerrian);

        EditorGUI.BeginDisabledGroup(!_validFile || _disableUI || _importing);
        if (GUILayout.Button("Import"))
        {
            _importing = true;

            var mapWrapper = new ImportMapWrapper(this, 
                                                    _mapFilePath, realTerrian);

            mapWrapper.Import();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            _importing = false;
        }

        EditorGUI.EndDisabledGroup();

        if (_disableUI)
        {
            EditorGUILayout.HelpBox("The current scene has not been saved yet!",
                                    MessageType.Warning,
                                    true);
        }

        if (GUILayout.Button("Reinitialize Bicycle"))
        {
            Bicycle.InitializeRandomPos();
        }
    }

    private void Update()
    {
        _disableUI = EditorSceneManager.GetActiveScene().isDirty;
    }
}
