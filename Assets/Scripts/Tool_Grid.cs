using UnityEngine;

public class Tool_Grid : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    [ExecuteInEditMode]
    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            // whatever
            Debug.Log("click");
        }
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;

        if (Input.GetMouseButtonDown(0))
        {
            // whatever
            Debug.Log("click");
        }
    }
}
