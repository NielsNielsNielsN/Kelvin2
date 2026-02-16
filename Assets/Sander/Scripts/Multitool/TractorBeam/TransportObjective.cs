using UnityEngine;

public class TransportObjective : MonoBehaviour
{
    [Tooltip("The specific snap socket this object belongs to")]
    [SerializeField] private SnapSocket targetSocket;

    [Tooltip("Proximity distance to auto-snap (Unity units)")]
    [SerializeField] private float snapDistance = 0.5f;

    [Tooltip("Optional: rotation offset when snapped")]
    [SerializeField] private Vector3 snapRotationOffset = Vector3.zero;

    public SnapSocket TargetSocket => targetSocket;
    public float SnapDistance => snapDistance;
    public Vector3 SnapRotationOffset => snapRotationOffset;
}