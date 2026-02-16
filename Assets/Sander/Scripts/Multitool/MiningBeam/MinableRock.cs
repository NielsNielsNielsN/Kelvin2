using UnityEngine;
using UnityEngine.Events;

public class MinableRock : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject resourceDropPrefab;
    [SerializeField] private ParticleSystem breakParticlesPrefab;
    [SerializeField] private AudioClip breakSound;
    public UnityEvent OnMined = new UnityEvent();

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            BreakRock();
        }
    }

    private void BreakRock()
    {
        if (resourceDropPrefab != null)
        {
            Instantiate(resourceDropPrefab, transform.position, Quaternion.identity);
        }

        if (breakParticlesPrefab != null)
        {
            ParticleSystem particles = Instantiate(breakParticlesPrefab, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + 1f);
        }

        if (breakSound != null && AudioListener.volume > 0)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        OnMined.Invoke();

        Destroy(gameObject);
    }
}