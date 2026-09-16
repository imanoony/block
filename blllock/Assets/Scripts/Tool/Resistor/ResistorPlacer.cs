using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ResistorPlacer : MonoBehaviour
{
    private ToolManager tm;
    private List<(Vector2Int, Vector2Int)> resistorPos= new();
    private void ToolEdgePlacedHandler(
        Vector2Int startGrid,
        List<Vector2Int?> nearGrids
    )
    {
        for (int i = 0; i < nearGrids.Count; i++)
        {
            // TODO
        }
    }

    private bool CanPlaceResistor(
        GridManager gm,
        Resistor resistor
    )
    {
        if (!gm.IsValidPos(resistor))
        {
            return false;
        }
        return true;
    }
    public bool PlaceResistor(
        GridManager gm,
        Resistor resistor
    )
    {
        if (!tm.UseTool()) return false;
        if (!CanPlaceResistor(gm, resistor)) return false;

        resistorPos.Add((resistor.A, resistor.B));
        if (!gm.PlaceResistor(resistor))
        {
            resistor.SetValid(false);
            gm.AddInvalid(resistor);
        }

        // TODO: resistor instance anim 재생
        
        return true;
    }
    public void RemoveResistor()
    {
        // TODO
    }
    public void RemoveResistors()
    {
        // TODO
    }
    public bool Check(GridManager gm, Resistor resistor)
    {
        // TODO
        return false;
    }
}