using UnityEngine;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// This class handles mesh generation for skidmarks, for all vehicles in the scene.
	/// You just have to put an instance of it in your scene.
	/// Note : this class is inspired by the Unity Car Tutorial skidmarks script, and optimized to improve performance.
	/// </summary>
	public class SkidmarksManager : MonoBehaviour
	{
		/// The maximum amount of skidmarks stored. Above this number, old skidmarks are destroyed. This number is shared between all vehicles.
		public int MaxMarksNumber = 1024;
		[Range(0.1f,1f)]
		/// the width of the skidmarks at ground level
		public float MarkWidth = 0.2f;
		[Range(0.01f,0.1f)]
		/// the offset between the mark and the ground
		public float GroundDistance = 0.02f;
		/// the minimal distance between each point of the mark
		[Range(0.1f, 10f)]
 		public const float DistanceBetweenMarks = 0.5f;

		/// a mark's section, used to store the mesh data
		protected class Section
		{
			public Vector3 Position; 
			public Vector3 Normal;
			public Vector4 Tangent;
			public Vector3 PositionLeft;
			public Vector3 PositionRight;
			public byte Intensity; 
			public int LastIndex; 
		};
		protected Section[] _skidmarksArray;
		protected Vector3[] _verticesArray;
		protected Vector3[] _normalsArray;
		protected Vector4[] _tangentsArray;
		protected Color32[] _colorsArray;
		protected Vector2[] _uvsArray;
		protected int[] _trianglesArray;
		protected bool _needNewSkidmark;
		protected MeshFilter _meshFilter;
		protected Mesh _mesh;

		protected int markIndex;
		protected bool _firstInitializationOfBoundsDone;

		/// <summary>
		/// Initalizes the array storing skidmarks and elements used for marks
		/// </summary>
		protected virtual void Start()
		{
			_skidmarksArray = new Section[MaxMarksNumber];
			for (int i = 0; i < MaxMarksNumber; i++)
			{
				_skidmarksArray[i] = new Section();
			}
				
			_verticesArray = new Vector3[MaxMarksNumber * 4];
			_normalsArray = new Vector3[MaxMarksNumber * 4];
			_tangentsArray = new Vector4[MaxMarksNumber * 4];
			_colorsArray = new Color32[MaxMarksNumber * 4];
			_uvsArray = new Vector2[MaxMarksNumber * 4];
			_trianglesArray = new int[MaxMarksNumber * 6];

			_meshFilter = GetComponent<MeshFilter>();

			// dynamic mesh creation
			_mesh = new Mesh();
			_mesh.MarkDynamic();
			_meshFilter.sharedMesh = _mesh;
		}

		/// <summary>
		/// Checks wether a mark should be added and updates the mesh if needed
		/// </summary>
		protected virtual void LateUpdate()
		{
			if (!_needNewSkidmark)
			{
				return;
			}
				
			_needNewSkidmark = false;

			_mesh.vertices = _verticesArray;
			_mesh.normals = _normalsArray;
			_mesh.tangents = _tangentsArray;
			_mesh.triangles = _trianglesArray;
			_mesh.colors32 = _colorsArray;
			_mesh.uv = _uvsArray;

			if (!_firstInitializationOfBoundsDone)
			{
				// Bounds are initialized with a large value so that the object is forced drawn on screen
				_mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10000, 10000, 10000));
				_firstInitializationOfBoundsDone = true;
			}

			_meshFilter.sharedMesh = _mesh;
		}

		/// <summary>
		/// Adds a new trace
		/// This method must be called from the wheel's code to add a mark at the desired moment
		/// </summary>
		/// <returns>The skid mark index.</returns>
		/// <param name="position">The mark's position.</param>
		/// <param name="normal">Ground normal.</param>
		/// <param name="intensity">Intensity of the mark (from 0 to 1).</param>
		/// <param name="lastIndex">Previous mark index for that wheel.</param>
		public virtual int AddSkidMark(Vector3 position, Vector3 normal, float intensity, int lastIndex)
		{
			if (lastIndex > 0)
			{
				// a mark is already in progress for this wheel, we compute the distance to the previous mark
				// if it's too close, we don't add a mark yet
				Vector3 diffPosition = position - _skidmarksArray[lastIndex].Position;
				if (diffPosition.sqrMagnitude < (DistanceBetweenMarks * DistanceBetweenMarks)) 
				{
					// and keep the same index
					return lastIndex;
				}
			}

			// we store the section 
			Section section = _skidmarksArray[markIndex];

			// section initialization
			section.Position = position + normal * GroundDistance;
			section.Normal = normal;
			section.Intensity = (byte)(Mathf.Clamp01(intensity) * 255f);
			section.LastIndex = lastIndex;

			if (lastIndex != -1)
			{
				// if the mark is attached to the previous one, we compute the deformation
				Section formerSection = _skidmarksArray[lastIndex];
				Vector3 diffPosition = (section.Position - formerSection.Position);
				Vector3 markDirection = Vector3.Cross(diffPosition, normal).normalized;

				section.PositionLeft = section.Position + markDirection * MarkWidth;
				section.PositionRight = section.Position - markDirection * MarkWidth;
				section.Tangent = new Vector4(markDirection.x, markDirection.y, markDirection.z, 1);

				if (formerSection.LastIndex == -1)
				{
					// if the previous mark was the first, we align it to the new one
					formerSection.Tangent = section.Tangent;
					formerSection.PositionLeft = section.Position + markDirection * MarkWidth;
					formerSection.PositionRight = section.Position - markDirection * MarkWidth;
				}
			}

			UpdateMesh();
			int newIndex = markIndex;
			markIndex = (markIndex + 1) % MaxMarksNumber;
			return newIndex;
		}
			
		/// <summary>
		/// Mesh update
		/// </summary>
		protected virtual void UpdateMesh()
		{
			Section mark = _skidmarksArray[markIndex];

			if (mark.LastIndex == -1)
			{
				// if it's the first mark we do nothing and exit
				return;
			}

			Section formerMark = _skidmarksArray[mark.LastIndex];

			_verticesArray[markIndex * 4 + 0] = formerMark.PositionLeft;
			_verticesArray[markIndex * 4 + 1] = formerMark.PositionRight;
			_verticesArray[markIndex * 4 + 2] = mark.PositionLeft;
			_verticesArray[markIndex * 4 + 3] = mark.PositionRight;

			_normalsArray[markIndex * 4 + 0] = formerMark.Normal;
			_normalsArray[markIndex * 4 + 1] = formerMark.Normal;
			_normalsArray[markIndex * 4 + 2] = mark.Normal;
			_normalsArray[markIndex * 4 + 3] = mark.Normal;

			_tangentsArray[markIndex * 4 + 0] = formerMark.Tangent;
			_tangentsArray[markIndex * 4 + 1] = formerMark.Tangent;
			_tangentsArray[markIndex * 4 + 2] = mark.Tangent;
			_tangentsArray[markIndex * 4 + 3] = mark.Tangent;

			_colorsArray[markIndex * 4 + 0] = new Color32(0, 0, 0, formerMark.Intensity);
			_colorsArray[markIndex * 4 + 1] = new Color32(0, 0, 0, formerMark.Intensity);
			_colorsArray[markIndex * 4 + 2] = new Color32(0, 0, 0, mark.Intensity);
			_colorsArray[markIndex * 4 + 3] = new Color32(0, 0, 0, mark.Intensity);

			_uvsArray[markIndex * 4 + 0] = new Vector2(0, 0);
			_uvsArray[markIndex * 4 + 1] = new Vector2(1, 0);
			_uvsArray[markIndex * 4 + 2] = new Vector2(0, 1);
			_uvsArray[markIndex * 4 + 3] = new Vector2(1, 1);

			_trianglesArray[markIndex * 6 + 0] = markIndex * 4 + 0;
			_trianglesArray[markIndex * 6 + 2] = markIndex * 4 + 1;
			_trianglesArray[markIndex * 6 + 1] = markIndex * 4 + 2;

			_trianglesArray[markIndex * 6 + 3] = markIndex * 4 + 2;
			_trianglesArray[markIndex * 6 + 5] = markIndex * 4 + 1;
			_trianglesArray[markIndex * 6 + 4] = markIndex * 4 + 3;

			_needNewSkidmark = true;
		}
	}
}