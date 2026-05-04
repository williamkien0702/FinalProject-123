using UnityEngine;

public class CoinSpin : MonoBehaviour
{
    public float degreesPerSecond = 360f;

    void Start()
    {
        // Stand the coin upright
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void Update()
    {
        // Spin on Y — since coin is upright this looks like a spinning top
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}