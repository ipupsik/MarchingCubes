using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public int worldChunkXMin;
    public int worldChunkXMax;
    public int worldChunkZMin;
    public int worldChunkZMax;
    public int worldChunkYMin;
    public int worldChunkYMax;

    float value = 0.0f;

    Dictionary<Vector3, MarchingChunk> chunks = new Dictionary<Vector3, MarchingChunk>();
    // Start is called before the first frame update
    void Start()
    {
        generate();
    }

    void generate()
    {
        clearData();

        Vector3 generatorPosition = GetComponent<Transform>().position;

        for (int x = worldChunkXMin; x <= worldChunkXMax; x++)
        {
            for (int z = worldChunkZMin; z <= worldChunkZMax; z++)
            {
                for (int y = worldChunkYMin; y <= worldChunkYMax; y++)
                {
                    Vector3 currentPosition = new Vector3(x * MarchingSettings.chunkLength * MarchingSettings.chunkResX,
                                                          y * MarchingSettings.chunkHeight * MarchingSettings.chunkResY,
                                                          z * MarchingSettings.chunkWidth * MarchingSettings.chunkResZ);

                    currentPosition += generatorPosition;

                    chunks.Add(currentPosition, new MarchingChunk(currentPosition));
                }
            }
        }
    }

    void clearData()
    {
        chunks.Clear();
    }

    private void Update()
    {
        value += Time.deltaTime;

        foreach (KeyValuePair<Vector3, MarchingChunk> entry in chunks)
        {
            entry.Value.updateChunk(value);
        }
    }
}
