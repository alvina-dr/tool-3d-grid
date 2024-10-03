using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;

public class Tool3DGrid_EditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private DropdownField _editionMod;
    public GameObject test;
    private GameObject CurrentObject;
    private ObjectField CurrentObjectField;

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

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        _editionMod = (DropdownField)rootVisualElement.Query("editionMod").First();

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        var CurrentObjectField = new ObjectField();
        CurrentObjectField.objectType = typeof(GameObject);
        CurrentObjectField.allowSceneObjects = false;
        CurrentObjectField.label = "Select an object:";

        CurrentObjectField.RegisterValueChangedCallback(
        evt =>
        {
            if (evt.newValue == null) CurrentObject = null;
            else CurrentObject = (GameObject) evt.newValue;
        });
        root.Add(CurrentObjectField);

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
                    buildPoint = ray.GetPoint(distance);
                }
            }
            GameObject _object = (GameObject) PrefabUtility.InstantiatePrefab(CurrentObject);
            _object.transform.position = new Vector3(Mathf.Round(buildPoint.x + 0.5f) - 0.5f, Mathf.Round(buildPoint.y - 0.5F) + 0.5f, Mathf.Round(buildPoint.z - 0.5f) + 0.5f);
        }
    }
}
