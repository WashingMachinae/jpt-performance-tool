using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;
[Serializable]
public class TestManager : EditorWindow {
    [Serializable]
    class sceneItem
    {
        [SerializeField]
        private bool highlighted;
        [SerializeField]
        private SceneAsset scene;
        [SerializeField]
        private string scenePath;

        public bool GetHighlighted()
        {
            return highlighted;
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
        }

        public SceneAsset GetScene()
        {
            return scene;
        }

        public void SetScene(SceneAsset value)
        {
            if (value != null)
                scene = value;
            else Debug.LogError("Tried to set scene to null");
        }

        public string GetPath ()
        {
            return scenePath;
        }

        public void SetPath (string value)
        {
            if (value != null)
                scenePath = value;
            else Debug.LogError("Tried to set scene path to null");
        }
    }
    [Serializable]
    class testSuite
    {
        [SerializeField]
        private string SuiteName;
        [SerializeField]
        private List<sceneItem> testsInSuite;
        [SerializeField]
        private bool beingEdited = false;
        [SerializeField]
        private bool isHighlighted = false;

        public void SetSuiteName(string value)
        {
            if (value != null)
                SuiteName = value;
        }

        public string GetSuiteName()
        {
            return SuiteName;
        }

        public List<sceneItem> GetScenesInSuite()
        {
            return testsInSuite;
        }

        public void SetScenes(List<sceneItem> list)
        {
            if (list != null)
            testsInSuite = list;
            else { Debug.Log("Trying to set scenes in suite to null"); }
        }

        public bool GetBeingEdited()
        {
            return beingEdited;
        }

        public void SetBeingEdited(bool value)
        {
            beingEdited = value;
        }

        public bool GetHighlighted()
        {
            return isHighlighted;
        }

        public void SetHighlighted(bool value)
        {
            isHighlighted = value;
        }
    }

    [MenuItem("TestManager/Show GUI")]
    public static void ShowWindow()
    {
        TestManager window = (TestManager)EditorWindow.GetWindow(typeof(TestManager), false, "TestManager");
        window.minSize = new Vector2(350, 350);
        window.Show();
    }

    string filename, fullPath;

    void Awake()
    {
        filename = "TestSuites.json";
        fullPath = Application.streamingAssetsPath + "/" + filename;
    }
    [SerializeField]
    List<testSuite> suitesInProject = new List<testSuite>();
    List<sceneItem> testsInSuite = new List<sceneItem>();

    Vector2 scrollPosAll, scrollPosSelected;
    GUIStyle alignCenterStyle, selectedItemStyle, listItemStyle;

    Texture2D selectedTex, normalTex;

    string AddSuiteName;

    void OnGUI()
    {
        float widthOfScrollArea = position.width / 2f - 30;
        if (selectedTex == null) selectedTex = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.8f, 0.8f));
        if (normalTex == null) normalTex = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        if (listItemStyle == null)
        {
            listItemStyle = new GUIStyle(GUI.skin.textField);
            listItemStyle.fontSize = 15;
            listItemStyle.normal.textColor = Color.white;
            listItemStyle.padding = new RectOffset(8, 8, 8, 8);
            listItemStyle.fontStyle = FontStyle.Bold;
            listItemStyle.normal.background = normalTex;
        }
        if (selectedItemStyle == null)
        {
            selectedItemStyle = new GUIStyle(listItemStyle);
            selectedItemStyle.normal.background = selectedTex;
        }
        if (alignCenterStyle == null)
        {
            alignCenterStyle = new GUIStyle(GUI.skin.label);
            alignCenterStyle.fontSize = 15;
            alignCenterStyle.alignment = TextAnchor.MiddleCenter;
        }
        EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("", GUILayout.MinHeight(10));
            EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Test Suites", alignCenterStyle);
                GUILayout.Label("", GUILayout.Width(30));
                GUILayout.Label("Tests Contained", alignCenterStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Suite", GUILayout.Width(widthOfScrollArea / 3 )))
                    {
                        suitesInProject = AddSuite(suitesInProject);
                        SelectSuite(suitesInProject.Count - 1);
                    }
                if (GUILayout.Button("Refresh", GUILayout.Width(widthOfScrollArea / 3)))
                    {
                        suitesInProject = GetSuites();
                    }
                if (GUILayout.Button("Delete Suite", GUILayout.Width(widthOfScrollArea / 3)))
                    {
                        suitesInProject = DeleteSuite(suitesInProject);
                    }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove Tests", GUILayout.Width(widthOfScrollArea / 3)))
                    {
                        testsInSuite = RemoveTest(testsInSuite);
                    }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Name the test Suite and Press Add Suite", GUILayout.Width(widthOfScrollArea));
            AddSuiteName = EditorGUILayout.TextField(AddSuiteName, GUILayout.Width(widthOfScrollArea));
                EditorGUILayout.BeginHorizontal();
                    scrollPosAll = EditorGUILayout.BeginScrollView(scrollPosAll, GUI.skin.GetStyle("TextArea"), GUILayout.MinHeight(position.height / 1.5f), GUILayout.Width(widthOfScrollArea));
                        ShowSuites();
                    EditorGUILayout.EndScrollView();
                        EditorGUILayout.BeginVertical(GUILayout.Width(30));
                            GUILayout.FlexibleSpace();
                        EditorGUILayout.EndVertical();
                    scrollPosSelected = EditorGUILayout.BeginScrollView(scrollPosSelected, GUI.skin.GetStyle("TextArea"), GUILayout.MinHeight(position.height/1.5f), GUILayout.Width(widthOfScrollArea));
                        DropAreaGUI();
                        int selectedSuite = GetSelectedSuite();
                        if (selectedSuite >= 0)
                        {
                            ShowTestsInSuite(suitesInProject[selectedSuite]);
                        }
                    EditorGUILayout.EndScrollView();
                EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

    }

    void DropAreaGUI()
    {
        Event evt = Event.current;
        if (suitesInProject.Count == 0)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
        else
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragExited)
        {
            DragAndDrop.AcceptDrag();
            UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects != null && draggedObjects[0] is SceneAsset)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragExited)
                {
                    for (int i = 0; i < draggedObjects.Length; i++)
                    {
                        //Debug.Log(draggedObjects[i].name);
                        SceneAsset _scene = draggedObjects[i] as SceneAsset;
                        string _path = AssetDatabase.GetAssetOrScenePath(draggedObjects[i]);
                        AddTestToSelectedSuite(_scene, _path);
                    }
                }
            }

        }
    }

    void AddTestToSelectedSuite(SceneAsset scene, string scenePath)
    {
        int j = GetSelectedSuite();
        if (j < 0)
            return;
        suitesInProject = ReadFromFile();
        suitesInProject[j].SetHighlighted(true);

        List<sceneItem> tempScenes = suitesInProject[j].GetScenesInSuite();
        if (tempScenes == null)
        {
            tempScenes = new List<sceneItem>();
        }
        sceneItem tempSceneItem = new sceneItem();
        tempSceneItem.SetScene(scene);
        tempSceneItem.SetPath(scenePath);
        tempSceneItem.SetHighlighted(false);
        tempScenes.Add(tempSceneItem);

        suitesInProject[j].SetScenes(tempScenes);
        WriteToFile(suitesInProject);
    }

    void SelectSuite (int selection)
    {
        selection = Mathf.Clamp(selection, 0, suitesInProject.Count - 1);
        if (suitesInProject.Count > 0)
        {
            for (int i = 0; i < suitesInProject.Count; i++)
            {
                suitesInProject[i].SetHighlighted(false);
            }
            suitesInProject[selection].SetHighlighted(true);
        }
    }

    List<testSuite> AddSuite(List<testSuite> _testSuites)
    {
        _testSuites = GetSuites();

        testSuite temp = new testSuite();
        temp.SetSuiteName(AddSuiteName);
        temp.SetBeingEdited(false);
        temp.SetHighlighted(false);

        _testSuites.Add(temp);

        SelectSuite(suitesInProject.Count - 1);
        WriteToFile(_testSuites);
        return _testSuites;   
    }

    List<testSuite> DeleteSuite(List<testSuite> _list)
    {
        int selected = GetSelectedSuite();
        if (selected >= 0)
        {
            _list.RemoveAt(selected);
            SelectSuite(Mathf.Clamp(selected, 0, _list.Count - 1));
            WriteToFile(_list);
        }
        return _list;
    }

    List<testSuite> GetSuites()
    {
        if (File.Exists(fullPath))
            return ReadFromFile();
        else return new List<testSuite>();
    }

    void WriteToFile (List<testSuite> _testSuites)
    {
        string json = JsonHelper.ToJson(_testSuites.ToArray(), true);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        //Debug.Log(json);
        StreamWriter writer = new StreamWriter(fullPath, true);
        writer.WriteLine(json);
        writer.Close();
    }

    List<testSuite> ReadFromFile()
    {
        StreamReader reader = new StreamReader(fullPath);
        string output = (reader.ReadToEnd());
        reader.Close();
        return JsonHelper.FromJson<testSuite>(output).ToList();
    }

    void ShowSuites()
    {
        for (int i = 0; i < suitesInProject.Count; i++)
        {
            GUIStyle ButtonStyle;
            if (!suitesInProject[i].GetHighlighted())
                ButtonStyle = listItemStyle;
            else ButtonStyle = selectedItemStyle;

                if (GUILayout.Button(suitesInProject[i].GetSuiteName(), ButtonStyle))
                {
                    for (int j = 0; j < suitesInProject.Count; j++)
                    {
                        suitesInProject[j].SetHighlighted(false);
                    }
                    suitesInProject[i].SetHighlighted(!suitesInProject[i].GetHighlighted());
                    WriteToFile(suitesInProject);
                }
        }
    }


    int GetSelectedSuite()
    {
        for (int i = 0; i < suitesInProject.Count; i++)
        {
            if (suitesInProject[i].GetHighlighted())
                return i;
        }
        return -1;
    }

    void ShowTestsInSuite(testSuite suite)
    {
        testsInSuite = suite.GetScenesInSuite();
        if (testsInSuite == null)
            return;
        for (int i = 0; i < testsInSuite.Count; i++)
        {
            GUIStyle ButtonStyle;
            if (testsInSuite[i].GetHighlighted())
                ButtonStyle = selectedItemStyle;
            else ButtonStyle = listItemStyle;
            if (testsInSuite[i].GetScene() != null)
            {
                if (GUILayout.Button(testsInSuite[i].GetScene().name, ButtonStyle))
                {
                    testsInSuite[i].SetHighlighted(!testsInSuite[i].GetHighlighted());
                }
            }
        }
    }

    List<sceneItem> RemoveTest(List<sceneItem> _list)
    {
        List<int> selection = GetSelectedTests();
        if (selection.Count == 0)
        {
            return new List<sceneItem>();
        }
        else
        {
            for (int i = 0; i < selection.Count; i++)
            {
                testsInSuite.RemoveAt(selection[i]);
                for (int j = 0; j < selection.Count; j++)
                {
                    selection[j]--;
                }
            }
            suitesInProject[GetSelectedSuite()].SetScenes(testsInSuite);
            WriteToFile(suitesInProject);
            return testsInSuite;
        }
    }

    List<int> GetSelectedTests()
    {
        List<int> selected = new List<int>();
        if (testsInSuite != null)
        {
            for (int i = 0; i < testsInSuite.Count; i++)
            {
                if (testsInSuite[i].GetHighlighted())
                {
                    selected.Add(i);
                }
            }
            return selected;
        }
        return new List<int>();
    }

    /* static void RecursiveAdding(string path, List<sceneItem> list)
     {

         DirectoryInfo dir = new DirectoryInfo(path);
         DirectoryInfo[] dirs = dir.GetDirectories();

         if (dirs.Length > 0)
         {
             for (int i = 0; i < dirs.Length; i++)
             {
                 RecursiveAdding(path + "/" + dirs[i].Name, list);
             }
         }
         else
         {
             FileInfo[] info = dir.GetFiles();
             for (int i = 0; i < info.Length; i++)
             {
                 FileInfo f = info[i];
                 if (f.Name.Contains(".unity") && !f.Name.Contains(".meta"))
                 {
                     SceneAsset sA = (SceneAsset)AssetDatabase.LoadAssetAtPath(path + "/" + f.Name, typeof(SceneAsset));
                     if (sA != null)
                     {
                         sceneItem temp = new sceneItem();
                         temp.SetScene(sA);
                         temp.SetHighlighted(false);
                         temp.SetPath(path + "/" + f.Name);
                         list.Add(temp);

                     }
                 }
             }
         }
     }*/

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
