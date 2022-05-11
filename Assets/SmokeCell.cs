using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeCell
{
    public int type = 0;
    public float temperature = 21;
    public float dTemperature = 0;
    
    public Vector3 airMovement = new Vector3(0,0,0);
    public Vector3 dAirMovement = new Vector3(0,0,0);
    public Vector3 position;
    public float smokeAmount = 0;
    public float dSmokeAmount = 0;

    public Vector3Int index;
    
    public SmokeCell(Vector3Int arrayIndex, Vector3 position)
    {
        index = arrayIndex;
        this.position = position;
    }
    
    public void updateValues()
    {
        airMovement += dAirMovement;
        smokeAmount += dSmokeAmount;
        temperature += dTemperature;
        dSmokeAmount = 0;
        dAirMovement = new Vector3(0, 0, 0);
        dTemperature = 0;
    }

    public float getThermalConductivity()
    {
        // return 20 + temperature * 0.075f;
        return 0.01f;
    }
    
}
