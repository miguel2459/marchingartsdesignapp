using UnityEngine;

public class StartUpSceneController : MonoBehaviour
{
    public void OnStartupAnimationComplete()
    {
        Debug.Log("Startup animation complete. Loading LoginScene...");
        SceneController.instance.SwitchScene(3); // Load Login Scene
    }
}
