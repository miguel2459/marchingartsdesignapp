using UnityEngine;

public class AppFocusManager : MonoBehaviour
{
    private static AppFocusManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void OnBrowserFocusChange(string state)
    {
        Debug.Log($"🌐 Browser focus state changed: {state}");

        if (state == "PAUSE")
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        else if (state == "RESUME")
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}