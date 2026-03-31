using UnityEngine;
using UnityEngine.Events;

public class RepairableObject : MonoBehaviour
{
    [SerializeField] private float maxRepairTime = 8f;          // Time needed to fully repair
    [SerializeField] private Mesh repairedMesh;                 // Drag the repaired mesh here
    [SerializeField] private Material repairedMaterial;         // Optional: different material when repaired
    [SerializeField] private ParticleSystem repairCompleteParticlesPrefab; // Optional effect on finish
    [SerializeField] private AudioClip repairCompleteSound;

    public UnityEvent OnRepaired = new UnityEvent();

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

        enabled = false;

        OnRepaired.Invoke();
    }

    public float GetRepairProgressNormalized() => currentRepairProgress / GetMaxRepairTime();

    public float GetMaxRepairTime() => Mathf.Max(0.0001f, maxRepairTime);

    // Allows external systems to modify repair progress (positive to add, negative to remove)
    public void ModifyRepairProgress(float delta)
    {
        currentRepairProgress = Mathf.Clamp(currentRepairProgress + delta, 0f, maxRepairTime);
    }
}