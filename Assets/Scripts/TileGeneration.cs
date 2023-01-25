using UnityEngine;

public class TileGeneration : MonoBehaviour {

	[Header("References:")]
	[SerializeField] MeshRenderer tileRenderer;
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] MeshCollider meshCollider;

	[Header("Settings:")]
	[SerializeField] bool deleteIfAllUnderWater;
	public int Seed;
	[SerializeField] float levelScale;
	[SerializeField] float heightMultiplier;
	[SerializeField] AnimationCurve heightCurve;
	[SerializeField] Wave[] terrainWaves;
	[SerializeField] BiomeRow[] biomes;

	[Header("Terrain Coloring:")]
	[SerializeField] TerrainType[] heightTerrainTypes;
	[SerializeField] TerrainType[] heatTerrainTypes;
	[SerializeField] TerrainType[] moistureTerrainTypes;
	[SerializeField] Color waterColor;
	[SerializeField] VisualizationMode visualizationMode;
	[SerializeField, Range(0, 1)] float biomEffect;

	public void BeginSeeding()
	{
		foreach(Wave wave in terrainWaves)
		{
			wave.setSeed(Seed);
		}
	}
	public TileData GenerateTile(float centerVertexZ, float maxDistanceZ, float[,] falloff, Vector2Int tileOffset) {
		// calculate tile depth and width based on the mesh vertices
		Vector3[] meshVertices = this.meshFilter.mesh.vertices;
		int tileDepth = (int)Mathf.Sqrt (meshVertices.Length);
		int tileWidth = tileDepth;

		// calculate the offsets based on the tile position
		float offsetX = -this.gameObject.transform.position.x;
		float offsetZ = -this.gameObject.transform.position.z;

		// generate a heightMap using Perlin Noise
		float[,] heightMap = NoiseMapGeneration.GeneratePerlinNoiseMap (tileDepth, tileWidth, this.levelScale, offsetX, offsetZ, this.terrainWaves);

		// calculate vertex offset based on the Tile position and the distance between vertices
		Vector3 tileDimensions = this.meshFilter.mesh.bounds.size;
		float distanceBetweenVertices = tileDimensions.z / (float)tileDepth;
		float vertexOffsetZ = this.gameObject.transform.position.z / distanceBetweenVertices;

		// generate a heatMap using uniform noise
		float[,] uniformHeatMap = NoiseMapGeneration.GenerateUniformNoiseMap (tileDepth, tileWidth, centerVertexZ, maxDistanceZ, vertexOffsetZ);
		// generate a heatMap using Perlin Noise
		float[,] randomHeatMap = NoiseMapGeneration.GeneratePerlinNoiseMap (tileDepth, tileWidth, this.levelScale, offsetX, offsetZ, this.terrainWaves);
		float[,] heatMap = new float[tileDepth, tileWidth];
		for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
			for (int xIndex = 0; xIndex < tileWidth; xIndex++) {

				int xProg = (tileOffset.x * tileWidth);
				int yProg = (tileOffset.y * tileDepth);

				float fallOffValue = falloff[xProg + xIndex, yProg + zIndex];

				heightMap[zIndex, xIndex] *= fallOffValue;

				// mix both heat maps together by multiplying their values
				heatMap[zIndex, xIndex] = uniformHeatMap [zIndex, xIndex] * randomHeatMap [zIndex, xIndex];
				// makes higher regions colder, by adding the height value to the heat map
				heatMap [zIndex, xIndex] += this.heightCurve.Evaluate(heightMap [zIndex, xIndex]) * heightMap [zIndex, xIndex];
			}
		}

		// generate a moistureMap using Perlin Noise
		float[,] moistureMap = NoiseMapGeneration.GeneratePerlinNoiseMap (tileDepth, tileWidth, this.levelScale, offsetX, offsetZ, this.terrainWaves);
		for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
			for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
				// makes higher regions dryer, by reducing the height value from the heat map
				moistureMap [zIndex, xIndex] -= this.heightCurve.Evaluate(heightMap [zIndex, xIndex]) * heightMap [zIndex, xIndex];
			}
		}

		// build a Texture2D from the height map
		TerrainType[,] chosenHeightTerrainTypes = new TerrainType[tileDepth, tileWidth];
		Texture2D heightTexture = BuildTexture (heightMap, this.heightTerrainTypes, chosenHeightTerrainTypes);
		// build a Texture2D from the heat map
		TerrainType[,] chosenHeatTerrainTypes = new TerrainType[tileDepth, tileWidth];
		Texture2D heatTexture = BuildTexture (heatMap, this.heatTerrainTypes, chosenHeatTerrainTypes);
		// build a Texture2D from the moisture map
		TerrainType[,] chosenMoistureTerrainTypes = new TerrainType[tileDepth, tileWidth];
		Texture2D moistureTexture = BuildTexture (moistureMap, this.moistureTerrainTypes, chosenMoistureTerrainTypes);

		// build a biomes Texture2D from the three other noise variables
		Biome[,] chosenBiomes = new Biome[tileDepth, tileWidth];
		Texture2D biomeTexture = BuildBiomeTexture(chosenHeightTerrainTypes, chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes);

		Texture2D texturedTerrain = TerrainTexture(heatMap, heightTexture, biomeTexture, biomEffect);


		Texture2D ChosenTexture = null;
		switch(this.visualizationMode) {
		case VisualizationMode.Height:
				// assign material texture to be the heightTexture
				ChosenTexture = heightTexture;
			break;
		case VisualizationMode.Heat:
				// assign material texture to be the heatTexture
				ChosenTexture = heatTexture;
			break;
		case VisualizationMode.Moisture:
				// assign material texture to be the moistureTexture
				ChosenTexture = moistureTexture;
			break;
		case VisualizationMode.Biome:
				// assign material texture to be the moistureTexture
				ChosenTexture = biomeTexture;
			break;
		case VisualizationMode.Textured:
				// assign material texture to be the moistureTexture
				ChosenTexture = texturedTerrain;
				break;
		}

		// update the tile mesh vertices according to the height map
		UpdateMeshVertices(heightMap);

		TileData tileData = new TileData (heightMap, heatMap, moistureMap, 
			chosenHeightTerrainTypes, chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes, 
			this.meshFilter.mesh, ChosenTexture);

		return tileData;
	}

	private Texture2D TerrainTexture(float[,] heightMap, Texture2D heightTex, Texture2D biomTex, float biomEff)
	{
		int tileDepth = heightMap.GetLength(0);
		int tileWidth = heightMap.GetLength(1);

		Color[] colorMap = new Color[tileDepth * tileWidth];
		for(int zIndex = 0;zIndex < tileDepth;zIndex++)
		{
			for(int xIndex = 0;xIndex < tileWidth;xIndex++)
			{
				int colorIndex = zIndex * tileWidth + xIndex;

				// assign the color according to the terrain type
				colorMap[colorIndex] = (heightTex.GetPixel(xIndex, zIndex) * (1 - biomEff)) + (biomTex.GetPixel(xIndex, zIndex) * (biomEff));
			}
		}
		Texture2D tileTexture = new Texture2D(tileWidth, tileDepth);
		tileTexture.wrapMode = TextureWrapMode.Clamp;
		tileTexture.filterMode = FilterMode.Bilinear;
		tileTexture.SetPixels(colorMap);
		tileTexture.Apply();

		return tileTexture;
	}

	private Texture2D BuildTexture(float[,] heightMap, TerrainType[] terrainTypes, TerrainType[,] chosenTerrainTypes) {
		int tileDepth = heightMap.GetLength (0);
		int tileWidth = heightMap.GetLength (1);

		Color[] colorMap = new Color[tileDepth * tileWidth];
		for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
			for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
				// transform the 2D map index is an Array index
				int colorIndex = zIndex * tileWidth + xIndex;
				float height = heightMap [zIndex, xIndex];
				// choose a terrain type according to the height value
				TerrainType terrainType = ChooseTerrainType (height, terrainTypes);
				// assign the color according to the terrain type
				colorMap[colorIndex] = (terrainType.color);
				// save the chosen terrain type
				chosenTerrainTypes [zIndex, xIndex] = terrainType;
			}
		}

		// create a new texture and set its pixel colors
		Texture2D tileTexture = new Texture2D (tileWidth, tileDepth);
		tileTexture.wrapMode = TextureWrapMode.Clamp;
		tileTexture.filterMode = FilterMode.Bilinear;
		tileTexture.SetPixels (colorMap);
		tileTexture.Apply();

		return tileTexture;
	}

	TerrainType ChooseTerrainType(float noise, TerrainType[] terrainTypes) {
		// for each terrain type, check if the height is lower than the one for the terrain type
		foreach (TerrainType terrainType in terrainTypes) {
			// return the first terrain type whose height is higher than the generated one
			if (noise < terrainType.threshold) {
				return terrainType;
			}
		}
		return terrainTypes [terrainTypes.Length - 1];
	}

	private void UpdateMeshVertices(float[,] heightMap) {
		int tileDepth = heightMap.GetLength (0);
		int tileWidth = heightMap.GetLength (1);

		Vector3[] meshVertices = this.meshFilter.mesh.vertices;

		// iterate through all the heightMap coordinates, updating the vertex index
		int vertexIndex = 0;
		for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
			for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
				float height = heightMap [zIndex, xIndex];

				Vector3 vertex = meshVertices [vertexIndex];
				// change the vertex Y coordinate, proportional to the height value. The height value is evaluated by the heightCurve function, in order to correct it.
				meshVertices[vertexIndex] = new Vector3(vertex.x, this.heightCurve.Evaluate(height) * this.heightMultiplier, vertex.z);

				vertexIndex++;
			}
		}

		// update the vertices in the mesh and update its properties
		this.meshFilter.mesh.vertices = meshVertices;
		this.meshFilter.mesh.RecalculateBounds ();
		this.meshFilter.mesh.RecalculateNormals ();
		// update the mesh collider
		this.meshCollider.sharedMesh = this.meshFilter.mesh;

		if(deleteIfAllUnderWater)
		{
			if(this.meshCollider.bounds.max.y <= 1)
			{
				Destroy(this.gameObject);
			}
		}
		gameObject.tag = "GroundTerrain";
		Destroy(this);
	}

	private Texture2D BuildBiomeTexture(TerrainType[,] heightTerrainTypes, TerrainType[,] heatTerrainTypes, TerrainType[,] moistureTerrainTypes, Biome[,] chosenBiomes) {
		int tileDepth = heatTerrainTypes.GetLength (0);
		int tileWidth = heatTerrainTypes.GetLength (1);

		Color[] colorMap = new Color[tileDepth * tileWidth];
		for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
			for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
				int colorIndex = zIndex * tileWidth + xIndex;

				TerrainType heightTerrainType = heightTerrainTypes [zIndex, xIndex];
				// check if the current coordinate is a water region
				if (heightTerrainType.name != "water") {
					// if a coordinate is not water, its biome will be defined by the heat and moisture values
					TerrainType heatTerrainType = heatTerrainTypes [zIndex, xIndex];
					TerrainType moistureTerrainType = moistureTerrainTypes [zIndex, xIndex];

					// terrain type index is used to access the biomes table
					Biome biome = this.biomes [moistureTerrainType.index].biomes [heatTerrainType.index];
					// assign the color according to the selected biome
					colorMap [colorIndex] = biome.color;

					// save biome in chosenBiomes matrix only when it is not water
					chosenBiomes [zIndex, xIndex] = biome;
				} else {
					// water regions don't have biomes, they always have the same color
					colorMap [colorIndex] = this.waterColor;
				}
			}
		}

		// create a new texture and set its pixel colors
		Texture2D tileTexture = new Texture2D (tileWidth, tileDepth);
		tileTexture.wrapMode = TextureWrapMode.Clamp;
		tileTexture.SetPixels (colorMap);
		tileTexture.Apply ();

		return tileTexture;
	}
}

[System.Serializable]
public class TerrainType {
	public string name;
	public float threshold;
	public Color color;
	public int index;
}

[System.Serializable]
public class Biome {
	public string name;
	public Color color;
	public int index;
}

[System.Serializable]
public class BiomeRow {
	public Biome[] biomes;
}

// class to store all data for a single tile
public class TileData {
	public float[,]  heightMap;
	public float[,]  heatMap;
	public float[,]  moistureMap;
	public TerrainType[,] chosenHeightTerrainTypes;
	public TerrainType[,] chosenHeatTerrainTypes;
	public TerrainType[,] chosenMoistureTerrainTypes;
	public Biome[,] chosenBiomes;
	public Mesh mesh;
	public Texture2D texture;

	public TileData(float[,]  heightMap, float[,]  heatMap, float[,]  moistureMap, 
		TerrainType[,] chosenHeightTerrainTypes, TerrainType[,] chosenHeatTerrainTypes, TerrainType[,] chosenMoistureTerrainTypes,
		Biome[,] chosenBiomes, Mesh mesh, Texture2D texture) {
		this.heightMap = heightMap;
		this.heatMap = heatMap;
		this.moistureMap = moistureMap;
		this.chosenHeightTerrainTypes = chosenHeightTerrainTypes;
		this.chosenHeatTerrainTypes = chosenHeatTerrainTypes;
		this.chosenMoistureTerrainTypes = chosenMoistureTerrainTypes;
		this.chosenBiomes = chosenBiomes;
		this.mesh = mesh;
		this.texture = texture;
	}
}

enum VisualizationMode { Height, Heat, Moisture, Biome, Textured }