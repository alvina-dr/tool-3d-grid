#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEditor;

public class Tool3DGrid_EditorWindow : EditorWindow
{
    public enum ToolType
    {
        None = 0,
        Paint = 1,
        Rectangle = 2,
        Erase = 3
    }

    private ToolType SelectedTool = ToolType.None;
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private DropdownField _editionMod;
    private GroupBox _gridGroupBox;
    private GameObject CurrentObject;

    private GameObject CurrentPreviewObject;

    // Rectangle variables
    private Vector3 _startDrag;
    private Vector3 _currentDrag;
    private Vector3 _currentRectangleSize;
    private Plane _currentPlane;
    private List<GameObject> CurrentPreviewObjectList = new();

    private Tool3DGrid_Tilemap CurrentTilemap;
    private ObjectField CurrentTilemapField;

    private Transform CurrentBuildParent;
    private ObjectField CurrentBuildParentField;

    private List<VisualElement> gridButtonList = new List<VisualElement>();

    private float CurrentRotationY = 0;

    [Header("Tool buttons")]
    VisualElement ToolGroup;
    Button noneButton;
    Button paintButton;
    Button rectangleButton;
    Button eraseButton;

    bool CanErase = false;
    bool IsMouseDrag = false;

    [SerializeField]
    private int maxPrefabInstantiation = 200;
    private IntegerField maxPrefabInstantiationField;

    private LayerMask _layerMask;

    [MenuItem("Window/UI Toolkit/Tool3DGrid_EditorWindow")]
    public static void ShowExample()
    {
        Tool3DGrid_EditorWindow wnd = GetWindow<Tool3DGrid_EditorWindow>();
        wnd.titleContent = new GUIContent("Tool3DGrid_EditorWindow");
    }

    public void CreateGUI()
    {
        CurrentObject = null;

        _layerMask = ~((1 << (LayerMask.NameToLayer("Audio")) | (1 << LayerMask.NameToLayer("Ignore Raycast"))));

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        _editionMod = (DropdownField)rootVisualElement.Query("editionMod").First();

        ToolGroup = new GroupBox();
        ToolGroup.style.flexDirection = FlexDirection.Row;

        noneButton = new Button(() => { ChangeTool(ToolType.None, null); }) { text = "None" };
        paintButton = new Button(() => { ChangeTool(ToolType.Paint, paintButton); }) { text = "Paint" };
        rectangleButton = new Button(() => { ChangeTool(ToolType.Rectangle, rectangleButton); }) { text = "Rectangle" };
        eraseButton = new Button(() => { ChangeTool(ToolType.Erase, eraseButton); }) { text = "Erase" };
        ToolGroup.Add(noneButton);
        ToolGroup.Add(paintButton);
        ToolGroup.Add(rectangleButton);
        ToolGroup.Add(eraseButton);
        root.Add(ToolGroup);

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        //Current Tilemap
        CurrentTilemapField = new ObjectField();
        CurrentTilemapField.objectType = typeof(Tool3DGrid_Tilemap);
        CurrentTilemapField.allowSceneObjects = false;
        CurrentTilemapField.label = "Select an object:";

        CurrentTilemapField.RegisterValueChangedCallback(
        evt =>
        {
            CurrentObject = null;
            CurrentTilemap = (Tool3DGrid_Tilemap)evt.newValue;
            DestroyList(CurrentPreviewObjectList);
            CurrentPreviewObjectList.Clear();
            //generate ui depending on new tilemap
            GenerateTilemapMenu();
        });
        root.Add(CurrentTilemapField);

        //Current Build Parent
        CurrentBuildParentField = new ObjectField();
        CurrentBuildParentField.objectType = typeof(Transform);
        CurrentBuildParentField.allowSceneObjects = true;
        CurrentBuildParentField.label = "Build parent";

        CurrentBuildParentField.RegisterValueChangedCallback(
        evt =>
        {
            CurrentBuildParent = (Transform)evt.newValue;
        });
        root.Add(CurrentBuildParentField);

        maxPrefabInstantiationField = new IntegerField();
        maxPrefabInstantiationField.label = "Max Prefab Instantiation Num";
        maxPrefabInstantiationField.RegisterValueChangedCallback(
        evt =>
        {
            maxPrefabInstantiation = evt.newValue;
        });
        root.Add(maxPrefabInstantiationField);

        _gridGroupBox = new GroupBox();
        _gridGroupBox.style.flexDirection = FlexDirection.Row;
        _gridGroupBox.style.flexWrap = Wrap.Wrap;
        root.Add(_gridGroupBox);

        DestroyList(CurrentPreviewObjectList);
        CurrentPreviewObjectList.Clear();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += SceneGUI;
    }

    void SceneGUI(SceneView sceneView)
    {
        Event cur = Event.current;

        //if (SelectedTool == ToolType.None)
        //    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Keyboard));
        //else
        //    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (mouseOverWindow is SceneView && !(focusedWindow is SceneView))
        {
            FocusWindowIfItsOpen<SceneView>();
        }

        if (mouseOverWindow is SceneView && !CanErase)
        {
            switch (SelectedTool)
            {
                case ToolType.Paint:
                    PreviewPrefabInWorld();
                    break;
                case ToolType.Rectangle:
                    if (IsMouseDrag)
                    {
                        PreviewRectangle();
                    }
                    else 
                    {
                        PreviewPrefabInWorld();
                    }
                    break;
                case ToolType.Erase:
                    break;
            }
        } 
        else // If the cursor goes out of the screen, the preview prefab object is destroyed
        {
            DestroyImmediate(CurrentPreviewObject);
            DestroyList(CurrentPreviewObjectList);
            CurrentPreviewObjectList.Clear();
            IsMouseDrag = false;
        }

        // Erase with any tool, just by pushing the R key
        if (cur.keyCode == KeyCode.R && cur.type == EventType.KeyDown)
        {
            CanErase = true;
            DestroyImmediate(CurrentPreviewObject);
        }
        else if (cur.keyCode == KeyCode.R && cur.type == EventType.KeyUp)
        {
            CanErase = false;
        }

        // Rotate the object they want to paint
        if (cur.keyCode == KeyCode.T && cur.type == EventType.KeyDown)
        {
            CurrentRotationY += 90;
            if (CurrentRotationY >= 360) CurrentRotationY -= 360;

            if (CurrentPreviewObject != null) // If there's a preview, rotate it to the wanted Y rotation to show how it would look if painted
            {
                CurrentPreviewObject.transform.eulerAngles = new Vector3(CurrentPreviewObject.transform.eulerAngles.x, CurrentRotationY, CurrentPreviewObject.transform.eulerAngles.z);
            }
        }

        // When user simple click
        if (cur.type == EventType.MouseDown && cur.button == 0)
        {
            if (CanErase)
            {
                Erase();
            }
            else
            {
                switch (SelectedTool)
                {
                    case ToolType.Paint:
                        Build();
                        break;
                    case ToolType.Rectangle:
                        IsMouseDrag = true;
                        StartRectangle();
                        break;
                    case ToolType.Erase:
                        Erase();
                        break;
                }
            }
        }

        // When player click up
        if (cur.type == EventType.MouseUp && cur.button == 0 && IsMouseDrag)
        {
            switch (SelectedTool)
            {
                case ToolType.Paint:
                    break;
                case ToolType.Rectangle:
                    IsMouseDrag = false;
                    BuildRectangle();
                    break;
                case ToolType.Erase:
                    break;
            }
        }
    }

    // Builds the object at the point indicated by the mouse
    public void Build()
    {
        if (CurrentObject == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(CurrentObject);
        instance.transform.position = CurrentPreviewObject.transform.position;
        instance.transform.eulerAngles = new Vector3(instance.transform.eulerAngles.x, CurrentRotationY, instance.transform.eulerAngles.z);
        instance.transform.parent = CurrentBuildParent;
        Undo.RegisterCreatedObjectUndo(instance, "Created go");
        Tools.current = Tool.None;
    }

    public void StartRectangle()
    {
        //choose first drag
        Vector3 buildPoint = Vector3.zero;
        RaycastHit hit;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMask))
        {
            buildPoint = new Vector3(hit.point.x - Mathf.Sign(ray.direction.x) * .1f, hit.point.y - Mathf.Sign(ray.direction.y) * .1f, hit.point.z - Mathf.Sign(ray.direction.z) * .1f); // Collision point
            _currentPlane = new Plane(Vector3.up, Mathf.RoundToInt(-buildPoint.y));
        }
        else
        {
            _currentPlane = new Plane(Vector3.up, Vector3.zero);
            float distance = 0;
            if (_currentPlane.Raycast(ray, out distance))
            {
                buildPoint = ray.GetPoint(distance) + new Vector3(0, .1f, 0); // Collision point with the ground
            }
        }
        _currentRectangleSize = Vector3.zero;
        _startDrag = new Vector3(Mathf.Round(buildPoint.x + 0.5f) - 0.5f, Mathf.Round(buildPoint.y - 0.5F), Mathf.Round(buildPoint.z - 0.5f) + 0.5f);
        Event.current.Use();
    }

    public void PreviewRectangle()
    {
        if (CurrentObject == null)
        {
            DestroyList(CurrentPreviewObjectList);
            CurrentPreviewObjectList.Clear();
            return;
        }
        if (CurrentPreviewObject != null) DestroyImmediate(CurrentPreviewObject);

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        float distance = 0;
        if (_currentPlane.Raycast(ray, out distance))
        {
            _currentDrag = ray.GetPoint(distance) + new Vector3(0, .1f, 0); // Collision point with the ground
        }

        Vector3Int rectangleSize = new Vector3Int(Mathf.RoundToInt(_currentDrag.x - _startDrag.x), 0, Mathf.RoundToInt(_currentDrag.z - _startDrag.z));
        if (_currentRectangleSize != rectangleSize)
        {
            Vector3Int firstCell = new Vector3Int(Mathf.RoundToInt(_startDrag.x + .5f), Mathf.RoundToInt(_startDrag.y), Mathf.RoundToInt(_startDrag.z - .5f));

            int startX = firstCell.x;
            int startZ = firstCell.z;

            int endX = firstCell.x + rectangleSize.x;
            int endZ = firstCell.z + rectangleSize.z;

            if (endX < startX)
            {
                startX = endX;
                endX = firstCell.x;
            }

            if (endZ < startZ)
            {
                startZ = endZ;
                endZ = firstCell.z;
            }

            int prefabNumInRectangle = (endX + 1 - startX) * (endZ + 1 - startZ);

            // If not enough prefabs for rectangle
            if (prefabNumInRectangle > CurrentPreviewObjectList.Count)
            {
                int missingPrefabNum = prefabNumInRectangle - CurrentPreviewObjectList.Count;
                for (int i = 0; i < missingPrefabNum; i++)
                {
                    if (i > maxPrefabInstantiation) break;
                    GameObject preview = (GameObject) PrefabUtility.InstantiatePrefab(CurrentObject);
                    SetGameLayerRecursive(preview, LayerMask.NameToLayer("Ignore Raycast")); // Set the layer of the preview object to "Ignore Raycast" to avoid colliding with itself
                    CurrentPreviewObjectList.Add(preview);
                }
            }
            // If too much prefabs for rectangle
            else if (prefabNumInRectangle < CurrentPreviewObjectList.Count && CurrentPreviewObjectList.Count > 1)
            {
                // Destroy all prefabs that won't be used
                for (int i = CurrentPreviewObjectList.Count - 1; i > prefabNumInRectangle; i--)
                {
                    DestroyImmediate(CurrentPreviewObjectList[i]);
                    CurrentPreviewObjectList.RemoveAt(i);
                }
            }

            int index = 0;
            for (int i = startX; i <= endX; i++)
            {
                for (int j = startZ; j <= endZ; j++)
                {
                    if (CurrentPreviewObjectList.Count - 1 < index) break;
                    Vector3 position = new Vector3(Mathf.Round(i) - .5f, Mathf.Round(_currentDrag.y), Mathf.Round(j) + .5f);
                    GameObject preview = CurrentPreviewObjectList[index];
                    preview.transform.eulerAngles = new Vector3(preview.transform.eulerAngles.x, CurrentRotationY, preview.transform.eulerAngles.z);
                    preview.transform.position = position;
                    index++;
                }
            }

            if (index < CurrentPreviewObjectList.Count)
            {
                DestroyImmediate(CurrentPreviewObjectList[index]);
                CurrentPreviewObjectList.RemoveAt(index);
            }

            if (CurrentPreviewObjectList.Count == prefabNumInRectangle)
            {
                _currentRectangleSize = rectangleSize;
            }
        }
    }

    public void BuildRectangle()
    {
        if (CurrentObject == null) return;

        for (int i = 0; i < CurrentPreviewObjectList.Count; i++)
        {

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(CurrentObject);
            instance.transform.position = CurrentPreviewObjectList[i].transform.position;
            instance.transform.eulerAngles = new Vector3(instance.transform.eulerAngles.x, CurrentRotationY, instance.transform.eulerAngles.z);
            instance.transform.parent = CurrentBuildParent;
         
            DestroyImmediate(CurrentPreviewObjectList[i].gameObject);

            Undo.RegisterCreatedObjectUndo(instance, "Created go");
        }

        CurrentPreviewObjectList.Clear();
        return;
    }

    // Erase the gameobject indicated by the mouse
    public void Erase()
    {
        RaycastHit hit;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMask))
        {
            Undo.DestroyObjectImmediate(hit.transform.gameObject);
        }
    }

    // Show the preview of the block that is going to be painted in the world
    public void PreviewPrefabInWorld()
    {
        // If we don't have a paint object selected, we don't want a preview
        if (CurrentObject == null)
        {
            DestroyImmediate(CurrentPreviewObject);
            return;
        }

        Vector3 buildPoint = Vector3.zero;
        RaycastHit hit;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMask))
        {
            buildPoint = new Vector3(hit.point.x - Mathf.Sign(ray.direction.x) * .1f,
                hit.point.y - Mathf.Sign(ray.direction.y) * .1f,
                hit.point.z - Mathf.Sign(ray.direction.z) * .1f); // Collision point
        }
        else
        {
            Plane hPlane = new Plane(Vector3.up, Vector3.zero);
            float distance = 0;
            if (hPlane.Raycast(ray, out distance))
            {
                buildPoint = ray.GetPoint(distance) + new Vector3(0, .1f, 0); // Collision point with the ground
            }
        }

        if (CurrentPreviewObject != null) // If the object prefab is already instantiated for the preview, it moves with the cursor
        {
            CurrentPreviewObject.transform.position = new Vector3(Mathf.Round(buildPoint.x + 0.5f) - 0.5f,
                Mathf.Round(buildPoint.y - 0.5F),
                Mathf.Round(buildPoint.z - 0.5f) + 0.5f);
        }
        else // Instantiates the object prefab once for the preview
        {
            CurrentPreviewObject = (GameObject)PrefabUtility.InstantiatePrefab(CurrentObject);
            SetGameLayerRecursive(CurrentPreviewObject, LayerMask.NameToLayer("Ignore Raycast")); // Set the layer of the preview object to "Ignore Raycast" to avoid self-collision
            CurrentPreviewObject.transform.position = new Vector3(Mathf.Round(buildPoint.x + 0.5f) - 0.5f,
                Mathf.Round(buildPoint.y - 0.5F),
                Mathf.Round(buildPoint.z - 0.5f) + 0.5f);
        }
    }

    public void GenerateTilemapMenu()
    {
        for (int i = _gridGroupBox.childCount - 1; i >= 0; i--)
        {
            _gridGroupBox.RemoveAt(i);
        }

        if (CurrentTilemap != null)
        {
            for (int i = 0; i < CurrentTilemap.TileList.Count; i++)
            {
                int index = i;

                Texture2D tex = AssetPreview.GetAssetPreview(CurrentTilemap.TileList[index]);
                int counter = 0;
                while (tex == null && counter < 75)
                {
                    tex = AssetPreview.GetAssetPreview(CurrentTilemap.TileList[index]);
                    counter++;
                    System.Threading.Thread.Sleep(15);
                }
                VisualElement _blockButton = new Button(() => { 
                    CurrentObject = CurrentTilemap.TileList[index];
                    DestroyList(CurrentPreviewObjectList);
                    CurrentPreviewObjectList.Clear();
                }) { iconImage = tex };
                _gridGroupBox.Add(_blockButton);
            }
        }
    }

    public void ChangeTool(ToolType tool, Button button)
    {
        SelectedTool = tool;

        DestroyList(CurrentPreviewObjectList);
        CurrentPreviewObjectList.Clear();

        Color color = new Color32(188, 188, 188, 0);
        foreach(var item in ToolGroup.Children())
        {
            item.style.backgroundColor = color;
        }
        if (button != null) button.style.backgroundColor = Color.gray;
    }

    // Change the layer of the given GameObject and all its children to the wanted layer
    private void SetGameLayerRecursive(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetGameLayerRecursive(child.gameObject, layer);
        }
    }

    private void DestroyList(List<GameObject> list)
    {
        for (int i = list.Count - 1; i >= 0 ; i--)
        {
            DestroyImmediate(list[i]);
        }
    }
}
#endif
