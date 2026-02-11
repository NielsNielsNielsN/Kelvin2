using UnityEngine;

public class RepairableObject : MonoBehaviour
{
    [SerializeField] private float maxRepairTime = 8f;          // Time needed to fully repair
    [SerializeField] private Mesh repairedMesh;                 // Drag the repaired mesh here
    [SerializeField] private Material repairedMaterial;         // Optional: different material when repaired
    [SerializeField] private ParticleSystem repairCompleteParticlesPrefab; // Optional effect on finish
    [SerializeField] private AudioClip repairCompleteSound;

    private float currentRepairProgress = 0f;
    private MeshFilter meshFilter;
    private Renderer objectRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        objectRenderer = GetComponent<Renderer>();
    }

    public void Repair(float deltaTime)
    {
        if (currentRepairProgress >= maxRepairTime) return;

        currentRepairProgress += deltaTime;

        if (currentRepairProgress >= maxRepairTime)
        {
            CompleteRepair();
        }
    }

    private void CompleteRepair()
    {
        if (meshFilter != null && repairedMesh != null)
        {
            meshFilter.mesh = repairedMesh;
        }

        if (objectRenderer != null && repairedMaterial != null)
        {
            objectRenderer.material = repairedMaterial;
        }

        if (repairCompleteParticlesPrefab != null)
        {
            Instantiate(repairCompleteParticlesPrefab, transform.position, transform.rotation);
        }

        if (repairCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(repairCompleteSound, transform.position);
        }

        // Optional: disable script after repair
        enabled = false;
    }

    // For visual feedback (optional): change color or scale slightly during repair
    public float GetRepairProgressNormalized() => currentRepairProgress / maxRepairTime;
}