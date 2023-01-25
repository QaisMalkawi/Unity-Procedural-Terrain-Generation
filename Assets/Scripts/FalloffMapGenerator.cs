using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FalloffMapGenerator
{
	public static float[,] Generate(int resolution, float edge)
	{
		float[,] map = new float[resolution, resolution];

		for (int y = 0; y < map.GetLength(1); y++)
		{
			for (int x = 0; x < map.GetLength(0); x++)
			{

				Vector2 pos = new Vector2(x, y);
				Vector2 center = new Vector2(map.GetLength(1) / 2, map.GetLength(0) / 2);

				float distance = Vector2.Distance(pos, center);
				distance /= (map.GetLength(1) / 2);

				map[x, y] = edge - distance;

			}
		}
		return map;
	}
}
