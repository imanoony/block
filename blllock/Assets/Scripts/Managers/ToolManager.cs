using System;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    None = -1,
    Cable,
    Resistor,
}

public enum ToolPlaceType
{
    None = -1,
    Edge,
    InterEdge,
}

public class ToolManager : MonoBehaviour
{
    private GridManager gm;
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;
        edgeGhostSr = edgeGhost.GetComponent<SpriteRenderer>();
        edgeGhostMpb = new();

        gm = GameManager.Instance.Grid;

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        PlaceCheck();
    }
    public event Action<ToolType, int, int> OnToolCountChanged; // type, max, curr
    public event Action<ToolType> OnSelectedToolChanged;
    private Dictionary<ToolType, int> toolMaxCounts = new();
    private Dictionary<ToolType, int> toolCurrCounts = new();
    public void InitToolCounts(Dictionary<ToolType, int> toolMaxCounts)
    {
        this.toolMaxCounts = new Dictionary<ToolType, int>(toolMaxCounts);
        toolCurrCounts = new Dictionary<ToolType, int>(toolMaxCounts);
    }

    public ToolType SelectedTool { get; private set; } = ToolType.None;

    public bool SelectTool(ToolType type)
    {
        if (type == ToolType.None) 
        {
            SelectedTool = ToolType.None;
            OnSelectedToolChanged?.Invoke(ToolType.None);
            return true;
        }
        else
        {
            if (!toolMaxCounts.ContainsKey(type)) return false;
            SelectedTool = type;
            Debug.Log($"EDGE EVENT: {SelectedTool}");
            OnSelectedToolChanged?.Invoke(type);
            return true;
        }
    }

    public bool UseTool() // selected tool을 사용하므로 파라미터 없음
    {
        if (!toolMaxCounts.ContainsKey(SelectedTool)) return false;
        if (toolCurrCounts[SelectedTool] <= 0) return false;

        toolCurrCounts[SelectedTool]--;
        OnToolCountChanged?.Invoke(SelectedTool, toolMaxCounts[SelectedTool], toolCurrCounts[SelectedTool]);

        return true;
    }

    public bool CancelTool(ToolType type)
    {
        if (!toolMaxCounts.ContainsKey(type)) return false;

        toolCurrCounts[type]++;
        OnToolCountChanged?.Invoke(type, toolMaxCounts[type], toolCurrCounts[type]);
        
        return true;
    }

    #region Placement
    private ToolPlaceType Tool2PlaceType(ToolType type)
    {
        return type switch
        {
            ToolType.Cable => ToolPlaceType.Edge,
            ToolType.Resistor => ToolPlaceType.Edge,
            _ => ToolPlaceType.None,
        };
    }
    [SerializeField] private GameObject edgeGhost;
    private SpriteRenderer edgeGhostSr;
    private MaterialPropertyBlock edgeGhostMpb;
    public event Action<Vector2Int, List<Vector2Int?>> OnToolEdgePlaced;
    public event Action<Vector2Int, List<Vector2Int?>> OnToolInterEdgePlaced;
    private void PlaceCheck()
    {
        ToolPlaceType placeType = Tool2PlaceType(SelectedTool);

        if (placeType == ToolPlaceType.Edge) 
            PlaceCheckEdge();
        else if (placeType == ToolPlaceType.InterEdge) 
            PlaceCheckInterEdge();
    }

    private Vector2Int startGrid = new(-1, -1);
    private bool isEdgePlacing = false;
    private void PlaceCheckEdge()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new(mouseWorld.x, mouseWorld.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.TryGetComponent<GridInstance>(out var gi))
                {
                    startGrid = gi.GridData.Pos;
                    isEdgePlacing = true;

                    SetEdgeGhostMat(true, 0);
                    edgeGhost.transform.position = GetEdgeGhostWorld(startGrid);
                    edgeGhost.SetActive(true);
                }
            }
        }

        else if (Input.GetMouseButtonUp(0))
        {
            if (isEdgePlacing)
            {
                edgeGhost.SetActive(false);

                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                List<Vector2Int?> nearGrids = gm.GetNearestGrids(mouseWorld, 4);

                OnToolEdgePlaced?.Invoke(startGrid, nearGrids);
                isEdgePlacing = false;
                startGrid = new(-1, -1);
            }
        }

        if (isEdgePlacing)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            List<Vector2Int?> nearGrids = gm.GetNearestGrids(mouseWorld, 4, false);

            for (int i = 0; i < nearGrids.Count; i++)
            {
                Vector2Int? endGrid = nearGrids[i];
                if (endGrid == startGrid) continue;
                if (endGrid == null) return;
                if (!Utils.IsAdjacentGrids(startGrid, (Vector2Int)endGrid)) return;

                UpdateEdgeGhost(startGrid, (Vector2Int)endGrid, (Vector2)mouseWorld);
                break;
            }
        }
    }
    private void PlaceCheckInterEdge()
    {
        // TODO
    }
    #endregion

    #region Ghost
    // ------------------------------------------------
    // Edge Ghost
    // ------------------------------------------------
    private Vector3 GetEdgeGhostWorld(Vector2Int start)
    {
        return (Vector3)gm.GetTileTopLeftWorld(start.x, start.y);
    }

    private float GetEdgeGhostReveal(
        Vector2Int start, 
        Vector2Int end, 
        Vector2 curr
    )
    {
        Vector2 startWorld = (Vector2)(Vector3)gm.GetTileTopLeftWorld(start.x, start.y);
        Vector2 endWorld = (Vector2)(Vector3)gm.GetTileTopLeftWorld(end.x, end.y);

        Vector2 direction = endWorld - startWorld;
        if (direction.sqrMagnitude < Mathf.Epsilon) return 0f;
        return Mathf.Clamp01(Vector2.Dot(curr - startWorld, direction) / direction.sqrMagnitude);
    }

    private void UpdateEdgeGhost(
        Vector2Int start,
        Vector2Int end,
        Vector2 mouseWorld
    )
    {
        bool fromLeft = true;
        float reveal = GetEdgeGhostReveal(start, end, mouseWorld);

        if (start.x == end.x) // horizontal 
        {
            if (start.y > end.y) edgeGhost.transform.rotation = Quaternion.Euler(0, 0, 180);
            else edgeGhost.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else // vertical
        {
            if (start.x < end.x) edgeGhost.transform.rotation = Quaternion.Euler(0, 0, -90);
            else edgeGhost.transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        SetEdgeGhostMat(fromLeft, reveal);
    }

    private const string _fromLeft = "_FromLeft", _reveal = "_Reveal";
    private void SetEdgeGhostMat(bool fromLeft, float reveal)
    {
        edgeGhostSr.GetPropertyBlock(edgeGhostMpb);
        edgeGhostMpb.SetFloat(_fromLeft, fromLeft ? 1f : 0f);
        edgeGhostMpb.SetFloat(_reveal, reveal);
        edgeGhostSr.SetPropertyBlock(edgeGhostMpb);
    }
    // ------------------------------------------------
    #endregion
}