using UnityEngine;

public class CoinSpin : MonoBehaviour
{
    public float degreesPerSecond = 360f;
    int spinDirection = 1;

    void Start()
    {
        // Stand the coin upright, then give each instance a different starting angle.
        transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        spinDirection = CompareTag("FakeCoin") ? -1 : 1;
    }

    void Update()
    {
        // Spin on Y — since coin is upright this looks like a spinning top
        transform.Rotate(0f, spinDirection * degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
