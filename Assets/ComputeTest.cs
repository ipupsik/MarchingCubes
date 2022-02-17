using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeTest : MonoBehaviour
{
    public ComputeShader computeShader;

    public RenderTexture renderTexture;

    private float Size = 0.1f;
        
    // Start is called before the first frame update
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(512, 512, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("Resolution", renderTexture.width);
        computeShader.SetFloat("Size", Size);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, dest);
    }

    public void Update()
    {
        Size += Time.deltaTime * 0.02f;
        if (computeShader)
        {
            computeShader.SetFloat("Size", Size);
        }
    }
}
