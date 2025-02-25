using UnityEngine;
using SimpleJSON;

public class SimpleJSONTest : MonoBehaviour
{
    void Start()
    {
        string json = "{\"name\":\"Test\", \"value\":42}";
        var parsedData = JSON.Parse(json);
        Debug.Log($"Name: {parsedData["name"]}");
        Debug.Log($"Value: {parsedData["value"]}");
    }
}
