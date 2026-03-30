using UnityEngine;

public class HelmetLight : MonoBehaviour
{
    [Header("Light Settings")]
    public Light helmetLight;          // Assign your helmet light here
    public KeyCode toggleKey = KeyCode.L;  // Default toggle key

    private bool isOn = false;

    void Start()
    {
        if (helmetLight != null)
            helmetLight.enabled = isOn;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOn = !isOn;
            if (helmetLight != null)
                helmetLight.enabled = isOn;
        }
    }
}
