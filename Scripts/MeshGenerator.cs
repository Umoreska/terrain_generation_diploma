
using UnityEngine;
public static class MeshGenerator {

	public static MeshData GenerateTerrainMesh(float[,] heightMap, float height_multiplier, AnimationCurve _height_curve, int level_of_detail, bool use_LOD=true) {
		// height_curve.Evaluate returns weird results when is used from differrent threads at the same time
		// so i create new one
		AnimationCurve height_curve = new AnimationCurve(_height_curve.keys);

        int meshSimplificationIncrement = (level_of_detail == 0)? 1: level_of_detail * 2; 

		int borderedSize = heightMap.GetLength (0);
		int meshSize = borderedSize - 2 * meshSimplificationIncrement;
		int meshSizeUnsimplified = borderedSize - 2;
        
		float topLeftX = (meshSizeUnsimplified - 1) / -2f; // makes posititon not dependent on LOD
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;
        
        int verticesPerLine = (meshSize-1) / meshSimplificationIncrement + 1;
        Debug.Log(verticesPerLine);

		if(use_LOD==false) {
			meshSimplificationIncrement = 1;
			verticesPerLine = meshSize;
		}

		MeshData meshData = new MeshData (verticesPerLine);
		//int vertexIndex = 0;

		int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;
		
		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {

				bool is_border_vertex = y == 0 || y == borderedSize-1 || x == 0 || x == borderedSize-1;

				if(is_border_vertex) {
					vertexIndicesMap[x,y] = borderVertexIndex;
					borderVertexIndex--;
				}else {
					vertexIndicesMap[x,y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {

				int vertexIndex = vertexIndicesMap[x,y];

				Vector2 percent = new Vector2 ((x-meshSimplificationIncrement) / (float)meshSize, (y-meshSimplificationIncrement) / (float)meshSize);
				float height = height_curve.Evaluate(heightMap[x, y])*height_multiplier;
				Vector3 vertex_position = new Vector3 (topLeftX + percent.x*meshSizeUnsimplified, height, topLeftZ - percent.y*meshSizeUnsimplified);

				meshData.AddVertex(vertex_position, percent, vertexIndex);
				// віднімаємо meshSimplificationIncrement, тому що при 0 воно визначає кордони, borders, а меш починаєтсья при 0+meshSimplificationIncrement

				/*meshData.vertices [vertexIndex] = new Vector3 (topLeftX + x, height_curve.Evaluate(heightMap[x, y])*height_multiplier, topLeftZ - y);
				meshData.uvs [vertexIndex] = new Vector2 (x / (float)borderedSize, y / (float)borderedSize);*/

				if (x < borderedSize - 1 && y < borderedSize - 1) {
					int a = vertexIndicesMap[x,y];
					int b = vertexIndicesMap[x+meshSimplificationIncrement, y];
					int c = vertexIndicesMap[x, y+meshSimplificationIncrement];
					int d = vertexIndicesMap[x+meshSimplificationIncrement, y+meshSimplificationIncrement];
					meshData.AddTriangle (a,d,c);
					meshData.AddTriangle (d,a,b);
				}

				vertexIndex++;
			}
		}

		return meshData;

	}
}

public class MeshData {
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;

	Vector3[] border_vertices;
	int[] border_triangles;

	int triangleIndex;
	int borderTrianleIndex;

	public MeshData(int vertices_per_line) {
		vertices = new Vector3[vertices_per_line * vertices_per_line];
		uvs = new Vector2[vertices_per_line * vertices_per_line];
		triangles = new int[(vertices_per_line-1)*(vertices_per_line-1)*6];

		border_vertices = new Vector3[vertices_per_line*4+4];
		border_triangles = new int[24*vertices_per_line];
	}

	public void AddVertex(Vector3 vertex_position, Vector2 uv, int vertex_index) {
		if(vertex_index < 0) {
			border_vertices[-vertex_index-1] = vertex_position;
		}else {
			vertices[vertex_index] = vertex_position;
			uvs[vertex_index] = uv;
		}
	}

	public void AddTriangle(int a, int b, int c) {

		if(a < 0 || b < 0 || c < 0) {
			// then it triangle belonging to the border
			border_triangles[borderTrianleIndex] = a;
			border_triangles[borderTrianleIndex + 1] = b;
			border_triangles[borderTrianleIndex + 2] = c;
			borderTrianleIndex += 3;
		}else {
			triangles[triangleIndex] = a;
			triangles[triangleIndex + 1] = b;
			triangles[triangleIndex + 2] = c;
			triangleIndex += 3;
		}

	}

	Vector3[] CalculateNormals() { // i ahve no idea how this works
		Vector3[] vertex_normals = new Vector3[vertices.Length];
		int traingle_count = triangles.Length/3;

		for(int i = 0; i < traingle_count; i++) {
			int normal_triangle_id = i*3;
			int vertex_id_A = triangles[normal_triangle_id];
			int vertex_id_B = triangles[normal_triangle_id+1];
			int vertex_id_C = triangles[normal_triangle_id+2];

			Vector3 triangle_normal = SurfaceNormalFromIndices(vertex_id_A, vertex_id_B, vertex_id_C);
			vertex_normals[vertex_id_A] += triangle_normal;
			vertex_normals[vertex_id_B] += triangle_normal;
			vertex_normals[vertex_id_C] += triangle_normal;
		}

		int border_triangle_count = border_triangles.Length/3;
		for(int i = 0; i < border_triangle_count; i++) {
			int normal_triangle_id = i*3;
			int vertex_id_A = border_triangles[normal_triangle_id];
			int vertex_id_B = border_triangles[normal_triangle_id+1];
			int vertex_id_C = border_triangles[normal_triangle_id+2];

			Vector3 triangle_normal = SurfaceNormalFromIndices(vertex_id_A, vertex_id_B, vertex_id_C);
			if(vertex_id_A >= 0) {
				vertex_normals[vertex_id_A] += triangle_normal;
			}
			if(vertex_id_B >= 0) {
				vertex_normals[vertex_id_B] += triangle_normal;				
			}
			if(vertex_id_C >= 0) {
				vertex_normals[vertex_id_C] += triangle_normal;
			}
		}

		for(int i = 0; i < vertex_normals.Length; i++) {
			vertex_normals[i].Normalize();
		}
		return vertex_normals;
	}

	Vector3 SurfaceNormalFromIndices(int id_A, int id_B, int id_C) {
		
		Vector3 point_A = (id_A < 0) ? border_vertices[-id_A-1] : vertices[id_A];
		Vector3 point_B = (id_B < 0) ? border_vertices[-id_B-1] : vertices[id_B];
		Vector3 point_C = (id_C < 0) ? border_vertices[-id_C-1] : vertices[id_C];
		
		Vector3 side_AB = point_B - point_A;
		Vector3 side_AC = point_C - point_A;

		return Vector3.Cross(side_AB, side_AC);
	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		//mesh.RecalculateNormals();
		mesh.normals = CalculateNormals();;
		return mesh;
	}

}
