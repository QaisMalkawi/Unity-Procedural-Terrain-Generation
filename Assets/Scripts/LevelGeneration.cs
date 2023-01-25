using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class LevelGeneration : MonoBehaviour {


	[SerializeField] bool blur;
	[SerializeField] int textureBlur;
	[SerializeField] int textureBlurIterations;

	[SerializeField, Range(1, 2)] float fallOffEdge;

	[SerializeField] int tileResolution;


	[SerializeField]
	private int levelSizeInTiles;

	[SerializeField]
	private GameObject tilePrefab;

	[SerializeField]
	private float centerVertexZ, maxDistanceZ;


	Texture2D[,] textures;
	MeshRenderer[,] tiles;

	void Start() {
		GenerateMap ();
	}

	private void OnGUI()
	{
		if(GUILayout.Button("Generate"))
		{
			ReGenerate();
		}

		if(GUILayout.Button("Generate With Random Seed"))
		{
			tilePrefab.GetComponent<TileGeneration>().Seed = UnityEngine.Random.Range(1000, 9999);
			ReGenerate();
		}
	}

	void ReGenerate()
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();

		foreach(GameObject tile in GameObject.FindGameObjectsWithTag("GroundTerrain"))
		{
			Destroy(tile.gameObject);
		}
		GC.Collect();

		tilePrefab.GetComponent<TileGeneration>().BeginSeeding();

		GenerateMap();

		stopwatch.Stop();
		UnityEngine.Debug.Log($"Elapsed Time is {stopwatch.ElapsedMilliseconds} ms" );
	}

	void GenerateMesh()
	{
		Mesh mesh = new Mesh();

		Vector3[] verts = new Vector3[tileResolution * tileResolution];
		int[] tris = new int[(tileResolution - 1) * (tileResolution - 1) * 6];
		int triInd = 0;
		Vector2[] uvs = new Vector2[verts.Length];

		for(int x = 0;x < tileResolution;x++)
		{
			for(int y = 0;y < tileResolution;y++)
			{
				int i = x + y * tileResolution;
				verts[i] = new Vector3(x, 0, y);

				uvs[i] = new Vector2(verts[i].x, verts[i].z) / (Vector2.one * tileResolution);

				if(x != tileResolution - 1 && y != tileResolution - 1)
				{
					tris[triInd] = i;
					tris[triInd + 1] = i + tileResolution + 1;
					tris[triInd + 2] = i + tileResolution;

					tris[triInd + 3] = i;
					tris[triInd + 4] = i + 1;
					tris[triInd + 5] = i + tileResolution + 1;

					triInd += 6;
				}
			}
		}
		Array.Reverse(verts);
		Array.Reverse(tris);
		//Array.Reverse(uvs);
		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.uv = uvs;
		mesh.RecalculateUVDistributionMetrics();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		tilePrefab.GetComponent<MeshFilter>().sharedMesh = mesh;

	}

	void GenerateMap()
	{

		GenerateMesh();

		textures = new Texture2D[levelSizeInTiles, levelSizeInTiles];
		tiles = new MeshRenderer[levelSizeInTiles, levelSizeInTiles];


		// get the tile dimensions from the tile Prefab
		Vector3 tileSize = tilePrefab.GetComponent<MeshRenderer>().bounds.size;
		int tileWidth = (int) tileSize.x;
		int tileDepth = (int) tileSize.z;

		// calculate the number of vertices of the tile in each axis using its mesh
		Vector3[] tileMeshVertices = tilePrefab.GetComponent<MeshFilter>().sharedMesh.vertices;
		int tileDepthInVertices = (int) Mathf.Sqrt(tileMeshVertices.Length);

		float distanceBetweenVertices = (float) tileDepth / (float) tileDepthInVertices;

		// build an empty LevelData object, to be filled with the tiles to be generated
		LevelData levelData = new LevelData(tileDepthInVertices, tileDepthInVertices, this.levelSizeInTiles, this.levelSizeInTiles);

		float[,] fallOff = FalloffMapGenerator.Generate(tileDepthInVertices * levelSizeInTiles, fallOffEdge);

		// for each Tile, instantiate a Tile in the correct position
		for(int xTileIndex = 0;xTileIndex < levelSizeInTiles;xTileIndex++)
		{
			for(int zTileIndex = 0;zTileIndex < levelSizeInTiles;zTileIndex++)
			{
				// calculate the tile position based on the X and Z indices
				Vector3 tilePosition = new Vector3(this.gameObject.transform.position.x + xTileIndex * tileWidth,
					this.gameObject.transform.position.y,
					this.gameObject.transform.position.z + zTileIndex * tileDepth);
				// instantiate a new Tile
				GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
				tile.transform.parent = transform;

				Vector2Int Offset = new Vector2Int(levelSizeInTiles - 1 - xTileIndex, levelSizeInTiles - 1 - zTileIndex);

				// generate the Tile texture and save it in the levelData
				TileData tileData = tile.GetComponent<TileGeneration>().GenerateTile(centerVertexZ, maxDistanceZ, fallOff, Offset);
				levelData.AddTileData(tileData, zTileIndex, xTileIndex);
				textures[xTileIndex, zTileIndex] = tileData.texture;

				tiles[xTileIndex, zTileIndex] = tile.GetComponent<MeshRenderer>();
			}
		}

		Texture2D FullTexture = new Texture2D(textures.GetLength(0) * textures[0, 0].width, textures.GetLength(1) * textures[0, 0].height);

		for(int y = 0;y < textures.GetLength(1);y++)
		{
			for(int x = 0;x < textures.GetLength(0);x++)
			{
				int sX = (x * textures[x, y].width);
				int sY = (y * textures[x, y].height);

				for(int iy = 0;iy < textures[x, y].height;iy++)
				{
					for(int ix = 0;ix  < textures[x, y].width;ix++)
					{
						FullTexture.SetPixel(sX + (textures[x, y].width - ix) - 1, sY + (textures[x, y].height - iy) - 1, textures[x, y].GetPixel(ix, iy));
					}
				}
			}
		}

		if(blur)
			FullTexture = Blur.FastBlur(FullTexture, textureBlur, textureBlurIterations);


		for(int y = 0;y < textures.GetLength(1);y++)
		{
			for(int x = 0;x < textures.GetLength(0);x++)
			{
				int sX = (x * textures[x, y].width);
				int sY = (y * textures[x, y].height);

				for(int iy = 0;iy < textures[x, y].height;iy++)
				{
					for(int ix = 0;ix < textures[x, y].width;ix++)
					{
						textures[x, y].SetPixel(ix, iy, FullTexture.GetPixel(sX + (textures[x, y].width - ix) - 1, sY + (textures[x, y].height - iy) - 1));
					}
				}
				textures[x, y].Apply();
			}
		}

		for(int y = 0;y < textures.GetLength(1);y++)
		{
			for(int x = 0;x < textures.GetLength(0);x++)
			{
				tiles[x, y].material.mainTexture = textures[x, y];
			}
		}
	}
}

public enum FlipMethod { X, Y, Both, Qais };

// class to store all the merged tiles data
public class LevelData {
	private int tileDepthInVertices, tileWidthInVertices;

	public TileData[,] tilesData;

	public Texture2D texture;

	public LevelData(int tileDepthInVertices, int tileWidthInVertices, int levelDepthInTiles, int levelWidthInTiles) {
		// build the tilesData matrix based on the level depth and width
		tilesData = new TileData[tileDepthInVertices * levelDepthInTiles, tileWidthInVertices * levelWidthInTiles];

		this.tileDepthInVertices = tileDepthInVertices;
		this.tileWidthInVertices = tileWidthInVertices;
	}

	public void AddTileData(TileData tileData, int tileZIndex, int tileXIndex) {
		// save the TileData in the corresponding coordinate
		tilesData [tileZIndex, tileXIndex] = tileData;
	}

	public TileCoordinate ConvertToTileCoordinate(int zIndex, int xIndex) {
		// the tile index is calculated by dividing the index by the number of tiles in that axis
		int tileZIndex = (int)Mathf.Floor ((float)zIndex / (float)this.tileDepthInVertices);
		int tileXIndex = (int)Mathf.Floor ((float)xIndex / (float)this.tileWidthInVertices);
		// the coordinate index is calculated by getting the remainder of the division above
		// we also need to translate the origin to the bottom left corner
		int coordinateZIndex = this.tileDepthInVertices - (zIndex % this.tileDepthInVertices) - 1;
		int coordinateXIndex = this.tileWidthInVertices - (xIndex % this.tileDepthInVertices) - 1;

		TileCoordinate tileCoordinate = new TileCoordinate (tileZIndex, tileXIndex, coordinateZIndex, coordinateXIndex);
		return tileCoordinate;
	}
}

// class to represent a coordinate in the Tile Coordinate System
public class TileCoordinate
{
	public int tileZIndex;
	public int tileXIndex;
	public int coordinateZIndex;
	public int coordinateXIndex;

	public TileCoordinate(int tileZIndex, int tileXIndex, int coordinateZIndex, int coordinateXIndex)
	{
		this.tileZIndex = tileZIndex;
		this.tileXIndex = tileXIndex;
		this.coordinateZIndex = coordinateZIndex;
		this.coordinateXIndex = coordinateXIndex;
	}
}