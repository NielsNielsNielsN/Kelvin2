using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class SnapSocket : MonoBehaviour
{
    [Tooltip("Visual radius gizmo in Scene view")]
    [SerializeField] private float detectionRadius = 0.5f;

    [Header("Preview")]
    [Tooltip("Material used for the preview (should support transparency)")]
    [SerializeField] private Material previewMaterial;
    [Tooltip("Minimum alpha when pulsating")]
    [SerializeField] private float previewAlphaMin = 0.15f;
    [Tooltip("Maximum alpha when pulsating")]
    [SerializeField] private float previewAlphaMax = 0.9f;
    [Tooltip("Seconds for one pulse cycle")]
    [SerializeField] private float previewPulseInterval = 1f;

    private GameObject previewInstance;
    private Coroutine previewPulseCoroutine;

    // Called by tractor when object snaps here
    public void SnapObject(Transform objTransform)
    {
        // Hide preview immediately on snap
        HidePreview();

        objTransform.SetParent(transform);
        objTransform.localPosition = Vector3.zero;
        objTransform.localRotation = Quaternion.Euler(objTransform.GetComponent<TransportObjective>().SnapRotationOffset);

        // Optional: add effects/sound
        Debug.Log(objTransform.name + " snapped to socket!");
    }

    // Public: create a preview mesh based on the source object's mesh/renderer
    public void ShowPreview(GameObject sourceObject)
    {
        HidePreview();

        if (sourceObject == null) return;

        MeshFilter mf = sourceObject.GetComponentInChildren<MeshFilter>();
        Renderer srcRenderer = sourceObject.GetComponentInChildren<Renderer>();
        if (mf == null || srcRenderer == null)
            return;

        previewInstance = new GameObject("SocketPreview_") { hideFlags = HideFlags.DontSave };
        previewInstance.transform.SetParent(transform, false);
        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;
        previewInstance.transform.localScale = Vector3.one;

        MeshFilter newMf = previewInstance.AddComponent<MeshFilter>();
        newMf.sharedMesh = mf.sharedMesh;

        MeshRenderer mr = previewInstance.AddComponent<MeshRenderer>();

        if (previewMaterial != null)
        {
            mr.material = new Material(previewMaterial);
        }
        else
        {
            // fallback: clone source material and force transparent
            mr.material = new Material(srcRenderer.sharedMaterial);
            Color c = mr.material.color; c.a = previewAlphaMax; mr.material.color = c;
            mr.material.SetFloat("_Mode", 3f);
        }

        // Disable shadows and reflection probes for the preview so it doesn't cast/receive lighting
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

        // start pulse coroutine
        previewPulseCoroutine = StartCoroutine(PulsePreviewAlpha(mr));
    }

    public void HidePreview()
    {
        if (previewPulseCoroutine != null)
        {
            StopCoroutine(previewPulseCoroutine);
            previewPulseCoroutine = null;
        }
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    private IEnumerator PulsePreviewAlpha(Renderer r)
    {
        if (r == null) yield break;
        Material mat = r.material;
        Color baseColor = mat.HasProperty("_Color") ? mat.color : Color.white;
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed / previewPulseInterval, 1f);
            float a = Mathf.Lerp(previewAlphaMin, previewAlphaMax, t);
            if (mat.HasProperty("_Color"))
            {
                Color c = baseColor; c.a = a; mat.color = c;
            }
            yield return null;
        }
    }

    // Scene view gizmo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
