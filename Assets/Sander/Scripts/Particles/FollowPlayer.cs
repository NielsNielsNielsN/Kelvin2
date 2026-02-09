using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;

    void Start()
    {
        player = GameObject.Find("player").transform;
    }

    void LateUpdate()
    {
        if (player != null)
            transform.position = player.position;
    }
}