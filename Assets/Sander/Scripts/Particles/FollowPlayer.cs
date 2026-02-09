using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = Vector3.zero;

    void Start()
    {
        if (player == null)
        {
            var found = GameObject.Find("player");
            if (found != null)
                player = found.transform;
        }
    }

    void LateUpdate()
    {
        if (player != null)
            transform.position = player.position + offset;
    }
}