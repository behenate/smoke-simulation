using System;
using UnityEngine;


public class BackupSmokeSimulation : MonoBehaviour
{
    public float cellScale;
    public Vector3Int cellCount;

    private Vector3 simulationDiemensions;
    
    
    private int instanceCount;
    public  Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    public GameObject smokeCellPrefab;

    private SmokeCell[,,] _smokeCells;
    void Start()
    {   
        
        instanceCount = cellCount.x * cellCount.y * cellCount.z;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        simulationDiemensions = cellScale * (Vector3)cellCount;
        
        Vector4[] positions = new Vector4[instanceCount];
        int cnt = 0;
        for (int i = 0; i < cellCount.x; i++)
        {
            for (int j = 0; j < cellCount.y; j++)
            {
                for (int k = 0; k < cellCount.z; k++)
                {
                    positions[cnt] = new Vector4(transform.position.x + i * cellScale - simulationDiemensions.x/2, transform.position.y + j * cellScale - simulationDiemensions.y/2, transform.position.z + k * cellScale - simulationDiemensions.z/2, cellScale);
                    cnt++;
                }
            }
        }
        positionBuffer = new ComputeBuffer(instanceCount, 16);
        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
        UpdateBuffers();
        
    }

    void Update() {
        // // Update starting position buffer
        // if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
        //     UpdateBuffers();

        // Pad input
        // if (Input.GetAxisRaw("Horizontal") != 0.0f)
        //     instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);

        // Render
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    private void LateUpdate()
    {
        UpdateBuffers();
    }
    

    void UpdateBuffers()
    {
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);
        
        
        if (colorBuffer != null)
            colorBuffer.Release();
        
        colorBuffer = new ComputeBuffer(instanceCount, 16);
        
        Color[] colors = new Color[instanceCount];
        float time = Time.time;
        for (int i = 0; i < instanceCount; i++)
        {
            int x = i % (int)Math.Sqrt(instanceCount);
            colors[i] = new Color(Mathf.Cos(time/100*x+10), Mathf.Cos(time/100*x), Mathf.Sin(time/100*x), 0.1f);
        }
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
    


    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 1);
        Gizmos.DrawWireCube(transform.position, cellScale * (Vector3)cellCount);
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.1f);
        // Gizmos.DrawCube(transform.position, size);
    }
}