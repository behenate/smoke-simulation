using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class SmokeSimulation : MonoBehaviour
{
    public float timeStep = 0.5f;
    public float cellScale;
    public Vector3Int cellCount;

    private Vector3 _simulationDimensions;

    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;

    public Vector3 windDirection;
    public Vector3 windSource;
    public float windRange;
    private int _cachedInstanceCount = -1;
    private int _cachedSubMeshIndex = -1;
    private ComputeBuffer _positionBuffer;
    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _argsBuffer;
    private readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    private SmokeCell[,,] _smokeCells;
    private readonly List<SmokeCell> _renderedSmokeCells = new List<SmokeCell>();

    private List<SmokeCell> toAddAfterIteration = new List<SmokeCell>();

    void Start()
    {
        _simulationDimensions = cellScale * (Vector3)cellCount;
        _smokeCells = new SmokeCell[cellCount.x, cellCount.y, cellCount.z];

        for (int i = 0; i < cellCount.x; i++)
        {
            for (int j = 0; j < cellCount.y; j++)
            {
                for (int k = 0; k < cellCount.z; k++)
                {
                    _smokeCells[i, j, k] = null;
                }
            }
        }

        for (int x = cellCount.x / 2 - 10; x < cellCount.x / 2 + 10; x++)
        {
            for (int z = cellCount.z / 2 - 10; z < cellCount.z / 2 + 10; z++)
            {
                Vector3Int cellIndex = new Vector3Int(x, 0, z);
                SmokeCell newCell = new SmokeCell(cellIndex, GetCellPosition(cellIndex))
                {
                    temperature = 1000,
                    smokeAmount = 1
                };
                newCell.airMovement = CalculateWind(newCell.position);
                _smokeCells[x, cellCount.y / 2, z] = newCell;
                _renderedSmokeCells.Add(newCell);
            }
        }

        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        UpdateBuffers();
        StartCoroutine(CellStateUpdateCoroutine());
    }

    IEnumerator CellStateUpdateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeStep);
            SimulationIteration();
            UpdateBuffers();
        }
    }

    void SimulationIteration()
    {
        foreach (var smokeCell in _renderedSmokeCells)
        {
            List<SmokeCell> neighbors = findNeighbors(smokeCell.index);
            foreach (var neighbor in neighbors)
            {
                float d = 0.5f;
                if (smokeCell.smokeAmount > 0.1)
                {
                    if (neighbor.index.y > smokeCell.index.y)
                    {
                        d = 1;
                    }
                    else if (neighbor.index.y < smokeCell.index.y)
                    {
                        d = 0.25f;
                    }
                    float dSmoke = d * Math.Min(0.1666f * smokeCell.smokeAmount, 0.1666f * (1 - neighbor.smokeAmount));
                    smokeCell.dSmokeAmount -= dSmoke;
                    neighbor.dSmokeAmount += dSmoke;
                }
                // Update the neighbours
                if (!(neighbor.temperature <= smokeCell.temperature)) continue;
                float dt = smokeCell.getThermalConductivity() *
                           (smokeCell.temperature - neighbor.temperature);
                neighbor.dTemperature += dt;
                smokeCell.dTemperature -= dt;
            }
        }

        foreach (var toAddCell in toAddAfterIteration)
        {
            _renderedSmokeCells.Add(toAddCell);
        }

        toAddAfterIteration = new List<SmokeCell>();

        foreach (var smokeCell in _renderedSmokeCells)
        {
            smokeCell.updateValues();
        }
    }


    List<SmokeCell> findNeighbors(Vector3Int index)
    {
        int x = index.x;
        int y = index.y;
        int z = index.z;

        List<SmokeCell> neighbors = new List<SmokeCell>();
        if (x + 1 < cellCount.x)
        {
            if (_smokeCells[x + 1, y, z] == null)
            {
                Vector3Int cellIndex = new Vector3Int(x + 1, y, z);
                _smokeCells[x + 1, y, z] = new SmokeCell(cellIndex, GetCellPosition(cellIndex));
                _smokeCells[x + 1, y, z].airMovement = CalculateWind(_smokeCells[x + 1, y, z].position);
                toAddAfterIteration.Add(_smokeCells[x + 1, y, z]);
            }

            neighbors.Add(_smokeCells[x + 1, y, z]);
        }

        if (y + 1 < cellCount.y)
        {
            if (_smokeCells[x, y + 1, z] == null)
            {
                Vector3Int cellIndex = new Vector3Int(x, y + 1, z);
                _smokeCells[x, y + 1, z] = new SmokeCell(cellIndex, GetCellPosition(cellIndex));
                _smokeCells[x, y + 1, z].airMovement = CalculateWind(_smokeCells[x, y + 1, z].position);
                toAddAfterIteration.Add(_smokeCells[x, y + 1, z]);
            }

            neighbors.Add(_smokeCells[x, y + 1, z]);
        }

        if (z + 1 < cellCount.z)
        {
            Vector3Int cellIndex = new Vector3Int(x, y, z + 1);
            if (_smokeCells[x, y, z + 1] == null)
            {
                _smokeCells[x, y, z + 1] = new SmokeCell(cellIndex, GetCellPosition(cellIndex));
                _smokeCells[x, y, z + 1].airMovement = CalculateWind(_smokeCells[x, y, z + 1].position);
                toAddAfterIteration.Add(_smokeCells[x, y, z + 1]);
            }

            // _smokeCells[x, y, z + 1] ??= new SmokeCell(cellIndex, GetCellPosition(cellIndex));
            neighbors.Add(_smokeCells[x, y, z + 1]);
        }

        if (x - 1 >= 0)
        {
            if (_smokeCells[x - 1, y, z] == null)
            {
                Vector3Int cellIndex = new Vector3Int(x - 1, y, z);
                _smokeCells[x - 1, y, z] = new SmokeCell(cellIndex, GetCellPosition(cellIndex));
                _smokeCells[x - 1, y, z].airMovement = CalculateWind(_smokeCells[x - 1, y, z].position);
                toAddAfterIteration.Add(_smokeCells[x - 1, y, z]);
            }

            neighbors.Add(_smokeCells[x - 1, y, z]);
        }

        if (y - 1 >= 0)
        {
            if (_smokeCells[x, y - 1, z] == null)
            {
                Vector3Int cellIndex = new Vector3Int(x, y - 1, z);
                _smokeCells[x, y - 1, z] = new SmokeCell(cellIndex, GetCellPosition(cellIndex));
                _smokeCells[x, y - 1, z].airMovement = CalculateWind(_smokeCells[x, y - 1, z].position);
                toAddAfterIteration.Add(_smokeCells[x, y - 1, z]);
            }

            neighbors.Add(_smokeCells[x, y - 1, z]);
        }

        if (z - 1 >= 0)
        {
            Vector3Int cellIndex = new Vector3Int(x, y, z - 1);
            if (_smokeCells[x, y, z - 1] == null)
            {
                _smokeCells[x, y, z - 1] = new SmokeCell(cellIndex, GetCellPosition(cellIndex));
                _smokeCells[x, y, z - 1].airMovement = CalculateWind(_smokeCells[x, y, z - 1].position);
                toAddAfterIteration.Add(_smokeCells[x, y, z - 1]);
            }

            // _smokeCells[x, y, z-1] ??= new SmokeCell(cellIndex, GetCellPosition(cellIndex));
            neighbors.Add(_smokeCells[x, y, z - 1]);
        }

        return neighbors;
    }

    void Update()
    {
        // // Update starting position buffer
        // if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
        //     UpdateBuffers();


        // Render
        // UpdateBuffers();
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial,
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), _argsBuffer);
    }

    Vector3 GetCellPosition(Vector3Int cellIndex)
    {
        Vector3 cellIndexV3 = (Vector3)cellIndex;
        Vector3 position = transform.position + (cellIndexV3 * cellScale) - _simulationDimensions / 2;
        return position;
    }

    Vector3 CalculateWind(Vector3 cellPosition)
    {
        Vector3 worldPositionWindSource = transform.position + windSource - _simulationDimensions / 2;
        float distance = Vector3.Distance(cellPosition, worldPositionWindSource);
        if (distance > windRange)
        {
            return new Vector3(0, 0, 0);
        }

        float x = distance;
        float intensity = -(1 / windRange) * x + 1;
        Vector3 windInCell = windDirection * intensity;
        return windInCell;
    }

    void UpdateBuffers()
    {
        int instanceCount = _renderedSmokeCells.Count;
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);


        _colorBuffer?.Release();
        _positionBuffer?.Release();

        _colorBuffer = new ComputeBuffer(instanceCount, 16);
        _positionBuffer = new ComputeBuffer(instanceCount, 16);


        Color[] colors = new Color[instanceCount];
        Vector4[] positions = new Vector4[instanceCount];
        int cnt = 0;
        foreach (var smokeCell in _renderedSmokeCells)
        {
            float scale = cellScale;
            if (smokeCell.smokeAmount < 0.1 && smokeCell.airMovement.x < 0.01 && smokeCell.temperature < 22)
            {
                scale = 0;
            }

            colors[cnt] = new Color(0, 0, 0, smokeCell.smokeAmount);
            positions[cnt] = new Vector4(
                transform.position.x + smokeCell.index.x * cellScale - _simulationDimensions.x / 2,
                transform.position.y + smokeCell.index.y * cellScale - _simulationDimensions.y / 2,
                transform.position.z + smokeCell.index.z * cellScale - _simulationDimensions.z / 2,
                cellScale);
            cnt++;
        }

        _colorBuffer.SetData(colors);
        _positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("colorBuffer", _colorBuffer);
        instanceMaterial.SetBuffer("positionBuffer", _positionBuffer);


        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }

        _argsBuffer.SetData(args);

        _cachedInstanceCount = instanceCount;
        _cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable()
    {
        if (_positionBuffer != null)
            _positionBuffer.Release();
        _positionBuffer = null;

        if (_argsBuffer != null)
            _argsBuffer.Release();
        _argsBuffer = null;
        if (_colorBuffer != null)
            _colorBuffer.Release();
    }


    void OnDrawGizmos()
    {
        _simulationDimensions = cellScale * (Vector3)cellCount;
        Gizmos.color = new Color(1, 0, 0, 1);
        Gizmos.DrawWireCube(transform.position, cellScale * (Vector3)cellCount);
        Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
        Gizmos.DrawWireSphere(transform.position + windSource - _simulationDimensions / 2, cellScale);

        // Gizmos.DrawCube(transform.position, size);
    }
}