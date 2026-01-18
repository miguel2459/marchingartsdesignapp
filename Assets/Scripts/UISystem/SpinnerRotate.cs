using UnityEngine;

public sealed class SpinnerRotate : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 300;

    private void Update()
    {
        transform.Rotate(0f, 0f, -degreesPerSecond * Time.deltaTime);
    }
}