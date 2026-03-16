using UnityEngine;

public class Fan_Rotate : MonoBehaviour
{
    public float speed = 300f;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}
