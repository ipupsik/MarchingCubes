using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingChunk
{
	ComputeShader marchingCubes;
	ComputeShader terrainGenerator;
	// Buffers
	ComputeBuffer triangleBuffer;
	ComputeBuffer pointsBuffer;
	ComputeBuffer triCountBuffer;

	struct Triangle
	{
		public Vector3 c;
		public Vector3 b;
		public Vector3 a;
	}

	public Material chunkMaterial;

	Vector3[] vertices;
	Color[] colors;
	int[] triangles;

	GameObject chunkObject;
	MeshFilter meshFilter;
	MeshRenderer meshRenderer;
	Mesh mesh;

	Vector3 chunkPosition;

	int resX;
	int resZ;
	int resY;
	float length;
	float width;
	float height;

	float value;

	public float noiseLimit = 200.0f;
	public int functionEnum = 2;

	const int threadGroupSize = 8;

	int numPoints;
	int numVoxelsPerXAxis;
	int numVoxelsPerYAxis;
	int numVoxelsPerZAxis;
	int numVoxels;
	int maxTriangleCount;

	public MarchingChunk(Vector3 position)
    {
		chunkObject = new GameObject();
		chunkPosition = position;

		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();
		chunkObject.transform.position = chunkPosition;
		chunkObject.transform.tag = "Terrain";

		chunkMaterial = Resources.Load<Material>("Materials/Terrain");
		meshRenderer.material = chunkMaterial;

		marchingCubes = Resources.Load<ComputeShader>("Scripts/March");

		if (functionEnum == 1)
			terrainGenerator = Resources.Load<ComputeShader>("Scripts/Terrain_Perlin2d");
		else if (functionEnum == 2)
			terrainGenerator = Resources.Load<ComputeShader>("Scripts/Terrain_F1");
		else if (functionEnum == 3)
			terrainGenerator = Resources.Load<ComputeShader>("Scripts/Terrain_Perlin3d");

		mesh = new Mesh();
		meshFilter.mesh = mesh;

		initializeVariables();

		numPoints = (resX + 1) * (resY + 1) * (resZ + 1);
		numVoxelsPerXAxis = resX - 1;
		numVoxelsPerYAxis = resY - 1;
		numVoxelsPerZAxis = resZ - 1;
		numVoxels = numVoxelsPerXAxis * numVoxelsPerYAxis * numVoxelsPerXAxis;

		maxTriangleCount = numVoxels * 5;

		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3 * 3, ComputeBufferType.Append);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

		pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));

		process();
	}

	void initializeVariables()
	{
		resX = MarchingSettings.chunkResX;
		resY = MarchingSettings.chunkResY;
		resZ = MarchingSettings.chunkResZ;

		width = MarchingSettings.chunkWidth;
		height = MarchingSettings.chunkHeight;
		length = MarchingSettings.chunkLength;
	}

	public void updateChunk(float _value)
    {
		value = _value;
		process();
    }

	void process()
	{
		PopulateTerrainMap();
		CreateMeshData();
	}

	void PopulateTerrainMap()
	{
        int numThreadsPerXAxis = Mathf.CeilToInt((resX + 1) / (float)threadGroupSize);
        int numThreadsPerYAxis = Mathf.CeilToInt((resY + 1) / (float)threadGroupSize);
        int numThreadsPerZAxis = Mathf.CeilToInt((resZ + 1) / (float)threadGroupSize);

        terrainGenerator.SetBuffer(0, "points", pointsBuffer);

        terrainGenerator.SetInt("numPointsPerXAxis", resX + 1);
        terrainGenerator.SetInt("numPointsPerYAxis", resY + 1);
        terrainGenerator.SetInt("numPointsPerZAxis", resZ + 1);

        terrainGenerator.SetFloat("height", height);
        terrainGenerator.SetFloat("width", width);
        terrainGenerator.SetFloat("length", length);

        terrainGenerator.SetFloat("value", value);

        if (functionEnum == 3)
            terrainGenerator.SetFloat("noiseLimit", noiseLimit);

        float[] pos = new float[3];
        pos[0] = chunkPosition.x;
        pos[1] = chunkPosition.y;
        pos[2] = chunkPosition.z;

        terrainGenerator.SetFloats("chunkPosition", pos);

        terrainGenerator.Dispatch(0, numThreadsPerXAxis, numThreadsPerYAxis, numThreadsPerZAxis);

        float[] p = new float[numPoints];
        pointsBuffer.GetData(p, 0, 0, numPoints);
    }

    void CreateMeshData()
	{
		int numThreadsPerXAxis = Mathf.CeilToInt(numVoxelsPerXAxis / (float)threadGroupSize);
		int numThreadsPerYAxis = Mathf.CeilToInt(numVoxelsPerXAxis / (float)threadGroupSize);
		int numThreadsPerZAxis = Mathf.CeilToInt(numVoxelsPerXAxis / (float)threadGroupSize);

		triangleBuffer.SetCounterValue(0);
		marchingCubes.SetBuffer(0, "points", pointsBuffer);
		marchingCubes.SetBuffer(0, "triangles", triangleBuffer);

		marchingCubes.SetInt("numPointsPerXAxis", resX + 1);
		marchingCubes.SetInt("numPointsPerYAxis", resY + 1);
		marchingCubes.SetInt("numPointsPerZAxis", resZ + 1);

		marchingCubes.SetFloat("height", height);
		marchingCubes.SetFloat("width", width);
		marchingCubes.SetFloat("length", length);

		marchingCubes.Dispatch(0, numThreadsPerXAxis, numThreadsPerYAxis, numThreadsPerZAxis);

		// Get number of triangles in the triangle buffer
		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];

		// Get triangle data from shader
		Triangle[] tris = new Triangle[numTris];
		triangleBuffer.GetData(tris, 0, 0, numTris);

		vertices = new Vector3[numTris * 3];
		triangles = new int[numTris * 3];
		colors = new Color[numTris * 3];

		for (int i = 0; i < numTris; i++)
		{
			triangles[i * 3] = i * 3;
			triangles[i * 3 + 1] = i * 3 + 1;
			triangles[i * 3 + 2] = i * 3 + 2;

			vertices[i * 3] = tris[i].a;
			vertices[i * 3 + 1] = tris[i].b;
			vertices[i * 3 + 2] = tris[i].c;

			colors[i * 3] = new Color(Mathf.Abs(vertices[i * 3].x) / 32.0f, Mathf.Abs(vertices[i * 3].y), Mathf.Abs(vertices[i * 3].z), 1.0f);
			colors[i * 3 + 1] = new Color(Mathf.Abs(vertices[i * 3 + 1].x) / 32.0f, Mathf.Abs(vertices[i * 3 + 1].y), Mathf.Abs(vertices[i * 3 + 1].z), 1.0f);
			colors[i * 3 + 2] = new Color(Mathf.Abs(vertices[i * 3 + 2].x) / 32.0f, Mathf.Abs(vertices[i * 3 + 2].y), Mathf.Abs(vertices[i * 3 + 2].z), 1.0f);
		}

		//vertices.Clear();
		//triangles.Clear();

		//for (int i = 0; i < numTris; i++)
		//{
		//	triangles.Add(AddVert(tris[i].a));
		//	triangles.Add(AddVert(tris[i].b));
		//	triangles.Add(AddVert(tris[i].c));
		//}

		BuildMesh();
	}

	//int AddVert(Vector3 position)
	//{
	//	for (int i = 0; i < vertices.Count; i++)
	//	{
	//		if (position == vertices[i])
	//			return i;
	//	}

	//	vertices.Add(position);
	//	return vertices.Count - 1;
	//}

	void BuildMesh() {
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		
		mesh.RecalculateNormals();

		//mesh.colors = colors;
	}
}
