using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class InstancingTst : MonoBehaviour {
    public int instanceCount = 100000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start() {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        UpdateBuffers();
    }

    void Update() {
        // Update starting position buffer
        if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
            UpdateBuffers();

        // Pad input
        // if (Input.GetAxisRaw("Horizontal") != 0.0f)
        //     instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);

        // Render
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    // private void LateUpdate()
    // {
    //     UpdateBuffers();
    // }
    

    void UpdateBuffers()
    {
        print("Hi!");
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();
        
        if (colorBuffer != null)
            colorBuffer.Release();
        
        positionBuffer = new ComputeBuffer(instanceCount, 16);
        colorBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];
        Color[] colors = new Color[instanceCount];
        float time = Time.time;
        for (int i = 0; i < instanceCount; i++)
        {
            int x = i % (int)Math.Sqrt(instanceCount);
            int y = i / (int)Math.Sqrt(instanceCount);
            positions[i] = new Vector4(x, y, Mathf.Cos((x+y)*time/100)*10, 0.1f);
            colors[i] = new Color(Mathf.Cos(time/10*x+10), Mathf.Cos(time/10*x), Mathf.Sin(time/10*x), 0.5f);
        }
        positionBuffer.SetData(positions);
        colorBuffer.SetData(colors);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
        instanceMaterial.SetBuffer("colorBuffer", colorBuffer);
        // Indirect args
        if (instanceMesh != null) {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable() {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
        if(colorBuffer != null)
            colorBuffer.Release();
    }
}