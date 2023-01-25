using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCompiner : MonoBehaviour
{
    // Start is called before the first frame update
    public void CompineMeshes()
	{
		MeshFilter[] meshes = FindObjectsOfType<MeshFilter>();

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();
		for(int i = 0;i < meshes.Length;i++)
		{
			for(int v = 0;v < meshes[i].mesh.vertexCount;v++)
			{
				verts.Add(meshes[i].mesh.vertices[v]);
			}
			for(int v = 0;v < meshes[i].mesh.triangles.Length;v++)
			{
				tris.Add(meshes[i].mesh.triangles[v]);
			}
			for(int v = 0;v < meshes[i].mesh.normals.Length;v++)
			{
				normals.Add(meshes[i].mesh.normals[v]);
			}
			//Destroy(meshes[i].gameObject);
		}
		Mesh mesh = new Mesh();
		mesh.vertices = verts.ToArray();
		mesh.triangles = tris.ToArray();
		mesh.normals = normals.ToArray();

		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
	}
}
