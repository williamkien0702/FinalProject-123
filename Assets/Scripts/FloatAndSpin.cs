using UnityEngine;

public class FloatAndSpin : MonoBehaviour
{
    [Header("Spin")]
    public float degreesPerSecond = 90f;

    [Header("Float")]
    public float floatHeight = 0.3f;    // How high it bobs up and down
    public float floatSpeed = 2f;       // How fast it bobs

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Spin on Y axis
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);

        // Bob up and down using a sine wave
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}