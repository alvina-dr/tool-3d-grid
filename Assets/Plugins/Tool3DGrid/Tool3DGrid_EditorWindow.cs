using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.IO;

public class Tool3DGrid_EditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private DropdownField _editionMod;
    private GroupBox _gridGroupBox;
    public GameObject test;
    private GameObject CurrentObject;
    private Tool3DGrid_Tilemap CurrentTilemap;
    private ObjectField CurrentTilemapField;

    private List<VisualElement> gridButtonList = new List<VisualElement>();

    [MenuItem("Window/UI Toolkit/Tool3DGrid_EditorWindow")]
    public static void ShowExample()
    {
        Tool3DGrid_EditorWindow wnd = GetWindow<Tool3DGrid_EditorWindow>();
        wnd.titleContent = new GUIContent("Tool3DGrid_EditorWindow");
    }

    public void CreateGUI()
    {
        CurrentObject = null;

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        _editionMod = (DropdownField)rootVisualElement.Query("editionMod").First();

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        CurrentTilemapField = new ObjectField();
        CurrentTilemapField.objectType = typeof(Tool3DGrid_Tilemap);
        CurrentTilemapField.allowSceneObjects = false;
        CurrentTilemapField.label = "Select an object:";

        CurrentTilemapField.RegisterValueChangedCallback(
        evt =>
        {
            if (evt.newValue == null) CurrentObject = null;
            else CurrentTilemap = (Tool3DGrid_Tilemap)evt.newValue;
            //generate ui depending on new tilemap
            GenerateTilemapMenu();
        });

        root.Add(CurrentTilemapField);
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += SceneGUI;
    }

    void SceneGUI(SceneView sceneView)
    {
        Event cur = Event.current;
        if (cur.type == EventType.MouseDown && cur.button == 0)
        {
            Build();
        }
    }

    public void Build()
    {
        if (CurrentObject != null)
        {
            Vector3 buildPoint = Vector3.zero;
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                buildPoint = new Vector3(hit.point.x - Mathf.Sign(ray.direction.x) * .1f, hit.point.y - Mathf.Sign(ray.direction.y) * .1f, hit.point.z - Mathf.Sign(ray.direction.z) * .1f);
            } 
            else
            {
                Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                float distance = 0;
                if (hPlane.Raycast(ray, out distance))
                {
                    // get the hit point:
                    buildPoint = ray.GetPoint(distance) + new Vector3(0, .1f, 0);
                }
            }
            GameObject _object = (GameObject) PrefabUtility.InstantiatePrefab(CurrentObject);
            _object.transform.position = new Vector3(Mathf.Round(buildPoint.x + 0.5f) - 0.5f, Mathf.Round(buildPoint.y - 0.5F) + 0.5f, Mathf.Round(buildPoint.z - 0.5f) + 0.5f);
        }
    }

    public void GenerateTilemapMenu()
    {
        if (_gridGroupBox != null) _gridGroupBox.RemoveFromHierarchy();
        _gridGroupBox = new GroupBox();
        _gridGroupBox.style.flexDirection = FlexDirection.Row;
        _gridGroupBox.style.flexWrap = Wrap.Wrap;

        for (int i = 0; i < gridButtonList.Count; i++)
        {
            rootVisualElement.Remove(gridButtonList[i]);
        }
        gridButtonList.Clear();
        for (int i = 0; i < CurrentTilemap.TileList.Count; i++)
        {
            int index = i;
            var editor = UnityEditor.Editor.CreateEditor(CurrentTilemap.TileList[index]);
            Texture2D tex = editor.RenderStaticPreview(AssetDatabase.GetAssetPath(CurrentTilemap.TileList[index]), null, 100, 100);
            VisualElement _blockButton = new Button(() => { CurrentObject = CurrentTilemap.TileList[index]; }) { iconImage =  tex};
            _gridGroupBox.Add(_blockButton);
            gridButtonList.Add(_blockButton);
        }

        rootVisualElement.Add(_gridGroupBox);
    }
}
