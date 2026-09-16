using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ResistorPlacer : MonoBehaviour
{
    [SerializeField] private GameObject resistorPrefab;
    [SerializeField] private GameObject resistorParent;

    private GridManager gm;
    private ToolManager tm;
    private HashSet<(Vector2Int, Vector2Int)> resistors = new();
    private Dictionary<(Vector2Int, Vector2Int), ResistorInstance> resistorInstances = new();

    void Start()
    {
        gm = GameManager.Instance.Grid;
        tm = GameManager.Instance.Tool;
        tm.OnToolEdgePlaced += ToolEdgePlacedHandler;
    }
    void OnDestroy()
    {
        tm.OnToolEdgePlaced -= ToolEdgePlacedHandler;
    }
    private void ToolEdgePlacedHandler(
        Vector2Int startGrid,
        List<Vector2Int?> nearGrids
    )
    {
        if (tm.SelectedTool != ToolType.Resistor) return;

        for (int i = 0; i < nearGrids.Count; i++)
        {
            try
            {
                Vector2Int? endGrid = nearGrids[i];
                if (endGrid == null) break;

                (Vector2Int A, Vector2Int B) = Utils.SortPositions(startGrid, (Vector2Int)endGrid);
                if (resistors.Contains((A, B)))
                {
                    if (resistorInstances[(A, B)].IsInteractable())
                        RemoveResistor(gm, A, B);
                }
                else if (PlaceResistor(gm, A, B))
                {
                    break;
                }
                else continue;
            }
            catch (ArgumentException)
            {
                Debug.Log("Invalid Resistor");
                continue;
            }
        }
    }

    private bool CanPlaceResistor(
        GridManager gm,
        Vector2Int a,
        Vector2Int b
    )
    {
        if (!gm.IsValidPos(a, b))
        {
            return false;
        }
        return true;
    }
    public bool PlaceResistor(
        GridManager gm,
        Vector2Int a,
        Vector2Int b
    )
    {
        if (!tm.UseTool()) return false;
        if (!CanPlaceResistor(gm, a, b)) return false;

        resistors.Add((a, b));
        Resistor resistor = new(a, b);
        if (!gm.PlaceResistor(resistor))
        {
            resistor.SetValid(false);
            gm.AddInvalid(resistor);
        }

        GameObject go = Instantiate(resistorPrefab, resistorParent.transform);
        ResistorInstance instance = go.GetComponent<ResistorInstance>();
        instance.Initialize(resistor);
        resistorInstances.Add((resistor.A, resistor.B), instance);

        instance.ResistorAppear();
        
        return true;
    }
    public void RemoveResistor(
        GridManager gm,
        Vector2Int a,
        Vector2Int b
    )
    {
        resistors.Remove((a, b));
        gm.RemoveResistor(resistorInstances[(a, b)].ResistorData);
        Destroy(resistorInstances[(a, b)].gameObject);
        resistorInstances.Remove((a, b));
        tm.CancelTool(ToolType.Resistor);
    }
    public void RemoveResistors(
        GridManager gm
    )
    {
        foreach ((Vector2Int a, Vector2Int b) in resistors)
        {
            RemoveResistor(gm, a, b);
        }
        resistors.Clear();
        resistorInstances.Clear();
    }
    public bool Check(GridManager gm, Resistor resistor)
    {
        if (!gm.PlaceResistor(resistor)) return false;

        resistor.SetValid(true);
        // TODO: 여러가지 연출 처리 (색상 등)

        return true;
    }
}