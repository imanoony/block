using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITool : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public ToolType Type { get; private set; }
    [SerializeField] private Image hover;
    [SerializeField] private Image select;
    [SerializeField] private Image count;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Resources")]
    [SerializeField] private Sprite[] toolSprites;
    private bool selected = false;
    private ToolManager tm = null;

    public void Init(ToolManager tm, ToolType type, int count)
    {
        this.tm = tm;
        Type = type;
        gameObject.GetComponent<Image>().sprite = toolSprites[(int)type];

        hover.gameObject.SetActive(false);
        select.gameObject.SetActive(false);
        countText.text = count.ToString();

        this.tm.OnToolCountChanged += ToolCountChangedHandler;
        this.tm.OnSelectedToolChanged += SelectedToolChangedHandler;
    }

    void OnDisable()
    {
        tm.OnToolCountChanged -= ToolCountChangedHandler;
        tm.OnSelectedToolChanged -= SelectedToolChangedHandler;
    }

    private void ToolCountChangedHandler(ToolType type, int max, int curr)
    {
        if (type != Type) return;
        countText.text = curr.ToString();
    }

    private void SelectedToolChangedHandler(ToolType type)
    {
        if (type != Type && selected)
        {
            select.gameObject.SetActive(false);
            selected = false;
        }
        else if (type == Type)
        {
            select.gameObject.SetActive(true);
            hover.gameObject.SetActive(false);
            selected = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selected)
        {
            tm.SelectTool(ToolType.None);
        }
        else
        {
            tm.SelectTool(Type);
        }   
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selected) return;

        hover.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selected) return;

        hover.gameObject.SetActive(false);
    }
}
