using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class Tool3DGrid_BlockButton : Button
{
    public GameObject SelectedGameObject;

    public void Test()
    {
        var editor = UnityEditor.Editor.CreateEditor(SelectedGameObject);
        //Texture2D tex = editor.RenderStaticPreview(path, null, 200, 200);
    }
}
