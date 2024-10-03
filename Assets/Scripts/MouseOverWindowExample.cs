using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class MouseOverWindowExample : EditorWindow
{
    string mouseOver = "Nothing...";
    Label label;

    [MenuItem("Examples/Mouse Over Example")]
    static void Init()
    {
        GetWindow<MouseOverWindowExample>("mouseOver");
    }

    void CreateGUI()
    {
        label = new Label($"Mouse over: {mouseOver}");
        rootVisualElement.Add(label);
    }

    void Update()
    {
        label.schedule.Execute(() =>
        {
            mouseOver = EditorWindow.mouseOverWindow ?
                        EditorWindow.mouseOverWindow.ToString() : "Nothing...";
            label.text = $"Mouse over: {mouseOver}";
        }).Every(10);
    }
}