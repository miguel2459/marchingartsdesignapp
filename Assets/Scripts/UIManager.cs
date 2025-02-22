using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public Animator fadeAnimator; // Assign this in the Inspector

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep UIManager persistent across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeToBlack()
    {
        if (fadeAnimator != null)
        {
            fadeAnimator.SetTrigger("FadeOut");
        }
    }

    public void FadeFromBlack()
    {
        if (fadeAnimator != null)
        {
            fadeAnimator.SetTrigger("FadeIn");
        }
    }
}
