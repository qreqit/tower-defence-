using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TowerUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _towerIcon;
    [SerializeField] private Text _priceText;
    
    private Tower _towerPrefab;
    private Tower _currentSpawnedTower;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetTowerPrefab (Tower tower)
    {
        _towerPrefab = tower;
        _towerIcon.sprite = tower.GetTowerHeadIcon ();
        _towerIcon.preserveAspect = true;
        EnsurePriceText ();
        _priceText.text = $"{Tower.GetBlueprintGoldCost (tower)}";
    }

    public void OnBeginDrag (PointerEventData eventData)
    {
        if (LevelManager.Instance.IsOver || !LevelManager.Instance.IsPreparationPhase)
        {
            return;
        }

        if (!LevelManager.Instance.CanAffordTower (_towerPrefab))
        {
            return;
        }

        GameObject newTowerObj = Instantiate (_towerPrefab.gameObject);
        _currentSpawnedTower = newTowerObj.GetComponent<Tower> ();
        _currentSpawnedTower.ToggleOrderInLayer (true);
    }

    public void OnDrag (PointerEventData eventData)
    {
        if (_currentSpawnedTower == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint (mousePosition);
        _currentSpawnedTower.transform.position = targetPosition;
    }

    public void OnEndDrag (PointerEventData eventData)
    {
        if (_currentSpawnedTower == null)
        {
            return;
        }

        if (_currentSpawnedTower.PlacePosition == null)
        {
            Destroy (_currentSpawnedTower.gameObject);
            _currentSpawnedTower = null;
        }
        else
        {
            if (!LevelManager.Instance.TryBuyTower (_currentSpawnedTower.GoldCost))
            {
                Destroy (_currentSpawnedTower.gameObject);
                _currentSpawnedTower = null;
                return;
            }

            _currentSpawnedTower.LockPlacement ();
            _currentSpawnedTower.ToggleOrderInLayer (false);
            LevelManager.Instance.RegisterSpawnedTower (_currentSpawnedTower);
            _currentSpawnedTower = null;
        }
    }

    private void EnsurePriceText ()
    {
        if (_priceText != null)
        {
            return;
        }

        GameObject priceObj = new GameObject ("Price", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform priceRect = priceObj.GetComponent<RectTransform> ();
        priceRect.SetParent (transform, false);
        priceRect.anchorMin = new Vector2 (0.5f, 0f);
        priceRect.anchorMax = new Vector2 (0.5f, 0f);
        priceRect.pivot = new Vector2 (0.5f, 1f);
        priceRect.anchoredPosition = new Vector2 (0f, 8f);
        priceRect.sizeDelta = new Vector2 (110f, 28f);
        priceRect.localScale = Vector3.one;

        _priceText = priceObj.GetComponent<Text> ();
        Font hudFont = ResolveUiFont ();
        if (hudFont != null)
        {
            _priceText.font = hudFont;
        }

        _priceText.fontSize = 20;
        _priceText.fontStyle = FontStyle.Bold;
        _priceText.alignment = TextAnchor.MiddleCenter;
        _priceText.color = new Color (1f, 0.95f, 0.45f, 1f);
        _priceText.raycastTarget = false;
        _priceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _priceText.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = priceObj.AddComponent<Outline> ();
        outline.effectColor = new Color (0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2 (1.25f, -1.25f);
    }

    private Font ResolveUiFont ()
    {
        Font builtin = Resources.GetBuiltinResource<Font> ("LegacyRuntime.ttf");
        if (builtin != null)
        {
            return builtin;
        }

        builtin = Resources.GetBuiltinResource<Font> ("Arial.ttf");
        if (builtin != null)
        {
            return builtin;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas> ();
        if (rootCanvas != null)
        {
            Text[] texts = rootCanvas.GetComponentsInChildren<Text> (true);
            foreach (Text t in texts)
            {
                if (t != null && t.font != null)
                {
                    return t.font;
                }
            }
        }

        try
        {
            return Font.CreateDynamicFontFromOSFont (new[] { "Liberation Sans", "Arial", "DejaVu Sans" }, 16);
        }
        catch
        {
            return null;
        }
    }
}
