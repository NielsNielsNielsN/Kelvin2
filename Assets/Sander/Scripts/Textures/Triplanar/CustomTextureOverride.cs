using UnityEngine;

public class CustomTextureOverride : MonoBehaviour
{
    public Texture2D secondTexture;
    public Texture2D normalTexture;

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        if (secondTexture != null)
        {
            block.SetTexture("_SecondTexture", secondTexture);
        }
        if (normalTexture != null)
        {
            block.SetTexture("_NormalTexture", normalTexture);
        }

        renderer.SetPropertyBlock(block);
    }
}