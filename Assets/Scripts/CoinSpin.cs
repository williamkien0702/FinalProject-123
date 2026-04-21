using UnityEngine;

public class CoinSpin : MonoBehaviour
{
    public float degreesPerSecond = 120f;

    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
    }
}