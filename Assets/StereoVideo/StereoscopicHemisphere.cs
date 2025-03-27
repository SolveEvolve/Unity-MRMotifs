using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class StereoscopicHemisphere : MonoBehaviour
{
    [Header("Hemisphere Settings")]
    public float HemisphereSize = 10f;
    public enum Eye { Left, Right }
    public Eye LeftOrRightEye = Eye.Left;

    [Header("Video Settings")]
    // If not assigned, the script will try to pull the texture from a VideoPlayer component on this GameObject.
    public Texture VideoTexture;

    [Header("Mesh Resolution")]
    [Range(4, 128)]
    public int segmentsHorizontal = 32;
    [Range(4, 128)]
    public int segmentsVertical = 32;

    [Header("Shader Settings")]
    // Drag your updated shader (e.g. Custom/EyeMask_Discard_Fixed) here.
    public Shader hemisphereShader;

    // Internal reference to the generated material.
    private Material _material;

    void Start()
    {
        GenerateHemisphere();
    }

    void GenerateHemisphere()
    {
        // Generate lists for vertices, UVs, and triangles.
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // Create the hemisphere using spherical coordinates.
        // Horizontal angle (phi) ranges from -90° to +90° (180° total).
        // Vertical angle (theta) ranges from 0° (top) to 180° (bottom).
        // UV mapping: u = (phi + PI/2) / PI, v = theta / PI.
        for (int i = 0; i <= segmentsVertical; i++)
        {
            float theta = Mathf.PI * i / segmentsVertical;
            float v = theta / Mathf.PI;

            for (int j = 0; j <= segmentsHorizontal; j++)
            {
                float phi = -Mathf.PI / 2f + Mathf.PI * j / segmentsHorizontal;
                float u = (phi + Mathf.PI / 2f) / Mathf.PI;

                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                float x = HemisphereSize * sinTheta * cosPhi;
                float y = HemisphereSize * cosTheta;
                float z = HemisphereSize * sinTheta * sinPhi;

                vertices.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2(u, v));
            }
        }

        // Build triangles from the vertex grid.
        for (int i = 0; i < segmentsVertical; i++)
        {
            for (int j = 0; j < segmentsHorizontal; j++)
            {
                int first = i * (segmentsHorizontal + 1) + j;
                int second = first + segmentsHorizontal + 1;

                // First triangle.
                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);

                // Second triangle.
                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }

        // Create and assign the mesh.
        Mesh mesh = new Mesh();
        mesh.name = "HemisphereMesh";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
            mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
            mr = gameObject.AddComponent<MeshRenderer>();

        // Verify that a shader has been assigned.
        if (hemisphereShader == null)
        {
            Debug.LogError("Please assign a shader in the Inspector (Hemisphere Shader field).");
            return;
        }
        _material = new Material(hemisphereShader);

        // Set the video texture.
        if (VideoTexture != null)
        {
            _material.mainTexture = VideoTexture;
        }
        else
        {
            VideoPlayer vp = GetComponent<VideoPlayer>();
            if (vp != null)
            {
                if (vp.targetTexture != null)
                {
                    VideoTexture = vp.targetTexture;
                    _material.mainTexture = VideoTexture;
                }
                else
                {
                    StartCoroutine(WaitForVideoTexture(vp));
                }
            }
            else
            {
                Debug.LogWarning("No VideoTexture assigned and no VideoPlayer component found on this GameObject!");
            }
        }

        // Set the shader's _TargetEye property (0 for left, 1 for right).
        _material.SetFloat("_TargetEye", (LeftOrRightEye == Eye.Left) ? 0f : 1f);

        // Adjust the texture scale/offset so that each hemisphere samples the correct half of the stereoscopic video.
        // Left hemisphere: sample u = 0–0.5; right hemisphere: sample u = 0.5–1.
        _material.mainTextureScale = new Vector2(0.5f, 1f);
        _material.mainTextureOffset = (LeftOrRightEye == Eye.Left) ? Vector2.zero : new Vector2(0.5f, 0f);

        // Force the render queue to the Background value (around 1000) so this renders behind everything else.
        _material.renderQueue = 1000;

        mr.material = _material;
    }

    // Coroutine to wait until the VideoPlayer's texture becomes available.
    private IEnumerator WaitForVideoTexture(VideoPlayer vp)
    {
        while (vp.texture == null)
        {
            yield return null;
        }
        VideoTexture = vp.texture;
        if (_material != null)
        {
            _material.mainTexture = VideoTexture;
        }
    }
}