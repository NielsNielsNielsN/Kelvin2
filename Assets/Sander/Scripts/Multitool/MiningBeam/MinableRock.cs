using UnityEngine;
using UnityEngine.Events;

public class MinableRock : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject resourceDropPrefab;
    [SerializeField] private ParticleSystem breakParticlesPrefab;
    [SerializeField] private AudioClip breakSound;
    public UnityEvent OnMined = new UnityEvent();

    [SerializeField] public UnityEngine.UI.Slider miningProgressSlider;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // Update slider if we have one
        if (miningProgressSlider != null)
        {
            miningProgressSlider.value = 1f - (currentHealth / maxHealth); // 0 → full health, 1 → dead
        }

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

    public void ResetHealth()
    {
        currentHealth = maxHealth;

        // Reset slider too
        if (miningProgressSlider != null)
        {
            miningProgressSlider.value = 0f;
        }
    }
}