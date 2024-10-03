using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tool3DGrid_Tilemap", menuName = "Scriptable Objects/Tool3DGrid_Tilemap")]
public class Tool3DGrid_Tilemap : ScriptableObject
{
    public List<GameObject> tileList = new List<GameObject>();
}
