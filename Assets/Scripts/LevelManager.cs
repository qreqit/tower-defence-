using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    private enum BattlePhase
    {
        Preparation,
        Combat,
    }

    private static LevelManager _instance = null;

    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelManager> ();
            }
            return _instance;
        }
    }

    [SerializeField] private Transform _towerUIParent;
    [SerializeField] private GameObject _towerUIPrefab;
    [SerializeField] private Tower[] _towerPrefabs;

    private List<Tower> _spawnedTowers = new List<Tower> ();

    [SerializeField] private Enemy[] _enemyPrefabs;
    [SerializeField] private Transform[] _enemyPaths;
    [SerializeField] private float _spawnDelay = 1.15f;
    [SerializeField] private bool _autoBuildEnemyPaths = true;
    [SerializeField] private float _preparationDuration = 15f;
    [SerializeField] private int _startingAttackerBudget = 200;
    [SerializeField] private int _budgetGrowthPerRound = 30;
    [SerializeField] private int _totalRounds = 10;
    [SerializeField] private int _firstWaveEnemyCount = 10;
    [SerializeField] private int _waveEnemyCountStep = 10;
    [SerializeField] private int _waveClearGoldBonus = 40;

    private List<Enemy> _spawnedEnemies = new List<Enemy> ();
    private readonly Queue<Enemy> _pendingWaveEnemies = new Queue<Enemy> ();
    private float _runningSpawnDelay;
    private float _preparationTimer;

    private readonly Dictionary<int, List<Bullet>> _bulletPoolByPrefab = new Dictionary<int, List<Bullet>> ();

    public bool IsOver { get; private set; }
    public bool IsPaused => _isPaused;
    public bool IsPreparationPhase => _currentPhase == BattlePhase.Preparation;

    [SerializeField] private int _maxLives = 20;
    [SerializeField] private int _startingGold = 380;

    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _statusInfo;
    [SerializeField] private Text _livesInfo;
    [SerializeField] private Text _goldInfo;
    [SerializeField] private Text _phaseInfo;
    [SerializeField] private Text _totalEnemyInfo;

    private int _currentLives;
    private int _currentGold;
    private int _currentRound;
    private BattlePhase _currentPhase;
    private bool _isPaused;

    private void Start()
    {
        InitializeEnemyPaths ();
        EnsureGoldInfoText ();
        EnsurePhaseInfoText ();
        ApplyHudVisibility ();
        LayoutHudElements ();
        SetCurrentLives (_maxLives);
        SetGold (_startingGold);
        InstantiateAllTowerUI ();
        LayoutTowerShop ();
        StartPreparationPhase ();
        RefreshHud ();
    }

    private void Update()
    {
        if (Input.GetKeyDown (KeyCode.Escape))
        {
            SetPaused (!_isPaused);
        }

        if (Input.GetKeyDown (KeyCode.R))
        {
            SetPaused (false);
            SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
        }

        if (IsOver)
        {
            RefreshHud ();
            return;
        }

        if (!_isPaused && HasUsableEnemyPath ())
        {
            UpdatePhase ();
            UpdateTowerActions ();
            UpdateEnemyMovement ();
        }

        RefreshHud ();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Time.timeScale = 1f;
        }
    }

    private void SetPaused (bool paused)
    {
        if (_isPaused == paused)
        {
            return;
        }

        _isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private void UpdatePhase ()
    {
        if (_currentPhase == BattlePhase.Preparation)
        {
            _preparationTimer -= Time.unscaledDeltaTime;
            if (_preparationTimer <= 0f)
            {
                StartCombatPhase ();
            }
            RefreshHud ();
            return;
        }

        _runningSpawnDelay -= Time.unscaledDeltaTime;
        if (_runningSpawnDelay <= 0f && _pendingWaveEnemies.Count > 0)
        {
            SpawnEnemyFromQueue ();
            _runningSpawnDelay = _spawnDelay;
        }

        if (_pendingWaveEnemies.Count == 0 && _spawnedEnemies.Find (e => e != null && e.gameObject.activeSelf) == null)
        {
            GrantWaveClearGoldBonus ();
            if (_currentRound >= _totalRounds)
            {
                SetGameOver (true);
            }
            else
            {
                StartPreparationPhase ();
            }
        }

        RefreshHud ();
    }

    private void UpdateTowerActions ()
    {
        foreach (Tower tower in _spawnedTowers)
        {
            if (tower == null)
            {
                continue;
            }
            tower.CheckNearestEnemy (_spawnedEnemies);
            tower.SeekTarget ();
            tower.ShootTarget ();
        }
    }

    private void UpdateEnemyMovement ()
    {
        foreach (Enemy enemy in _spawnedEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
            {
                continue;
            }

            if (Vector2.Distance (enemy.transform.position, enemy.TargetPosition) < 0.1f)
            {
                enemy.SetCurrentPathIndex (enemy.CurrentPathIndex + 1);
                if (enemy.CurrentPathIndex < _enemyPaths.Length)
                {
                    enemy.SetTargetPosition (_enemyPaths[enemy.CurrentPathIndex].position);
                }
                else
                {
                    ReduceLives (1);
                    enemy.gameObject.SetActive (false);
                }
            }

            else
            {
                enemy.MoveToTarget ();
            }
        }
    }

    private void InstantiateAllTowerUI ()
    {
        foreach (Tower tower in _towerPrefabs)
        {
            GameObject newTowerUIObj = Instantiate (_towerUIPrefab.gameObject, _towerUIParent);
            TowerUI newTowerUI = newTowerUIObj.GetComponent<TowerUI> ();
            newTowerUI.SetTowerPrefab (tower);
            newTowerUI.transform.name = tower.name;
        }
    }

    public void RegisterSpawnedTower (Tower tower)
    {
        ReplaceTowerAtSamePosition (tower);
        _spawnedTowers.Add (tower);
    }

    private void ReplaceTowerAtSamePosition (Tower newTower)
    {
        Vector2 newPosition = newTower.transform.position;
        for (int i = _spawnedTowers.Count - 1; i >= 0; i--)
        {
            Tower existingTower = _spawnedTowers[i];
            if (existingTower == null || existingTower == newTower)
            {
                _spawnedTowers.RemoveAt (i);
                continue;
            }

            if (Vector2.Distance (existingTower.transform.position, newPosition) <= 0.2f)
            {
                _spawnedTowers.RemoveAt (i);
                Destroy (existingTower.gameObject);
            }
        }
    }

    private void StartPreparationPhase ()
    {
        _currentPhase = BattlePhase.Preparation;
        _preparationTimer = _preparationDuration;
    }

    private void GrantWaveClearGoldBonus ()
    {
        if (_currentRound <= 0)
        {
            return;
        }

        int waveBonus = Mathf.RoundToInt (_waveClearGoldBonus * (1f + 0.12f * (_currentRound - 1)));
        AddGold (waveBonus);
    }

    private void StartCombatPhase ()
    {
        _currentRound++;
        _currentPhase = BattlePhase.Combat;
        _runningSpawnDelay = 0f;
        BuildWaveForCurrentRound ();
    }

    private void BuildWaveForCurrentRound ()
    {
        if (_enemyPrefabs == null || _enemyPrefabs.Length == 0 || !HasUsableEnemyPath ())
        {
            return;
        }

        List<Enemy> pool = _enemyPrefabs.Where (prefab => prefab != null).ToList ();
        if (pool.Count == 0)
        {
            return;
        }

        int waveSize = Mathf.Max (1, _firstWaveEnemyCount + ((_currentRound - 1) * _waveEnemyCountStep));
        float roundProgress = _totalRounds <= 1 ? 1f : (_currentRound - 1f) / (_totalRounds - 1f);

        for (int i = 0; i < waveSize; i++)
        {
            Enemy picked = PickEnemyPrefabForWave (pool, roundProgress);
            _pendingWaveEnemies.Enqueue (picked);
        }
    }

    private static Enemy PickEnemyPrefabForWave (List<Enemy> pool, float roundProgress01)
    {
        float goblinWeight = Mathf.Lerp (0.62f, 0.28f, roundProgress01);
        float orcWeight = Mathf.Lerp (0.18f, 0.42f, roundProgress01);
        float ghostWeight = Mathf.Max (0.08f, 1f - goblinWeight - orcWeight);
        float sum = goblinWeight + orcWeight + ghostWeight;
        goblinWeight /= sum;
        orcWeight /= sum;
        ghostWeight /= sum;

        Enemy PickByHint (System.Func<string, bool> hint)
        {
            List<Enemy> matches = pool.FindAll (p => hint (p.gameObject.name.ToLowerInvariant ()));
            if (matches.Count > 0)
            {
                return matches[Random.Range (0, matches.Count)];
            }

            return pool[Random.Range (0, pool.Count)];
        }

        float roll = Random.value;
        if (roll < goblinWeight)
        {
            return PickByHint (n => n.Contains ("variant 1") || n.Contains ("goblin"));
        }

        if (roll < goblinWeight + orcWeight)
        {
            return PickByHint (n => n.Contains ("variant 2") || n.Contains ("orc"));
        }

        return PickByHint (n => n.Contains ("variant 3") || n.Contains ("ghost"));
    }

    private void SpawnEnemyFromQueue ()
    {
        if (_pendingWaveEnemies.Count == 0)
        {
            return;
        }

        Enemy enemyPrefab = _pendingWaveEnemies.Dequeue ();
        if (enemyPrefab == null)
        {
            return;
        }

        Enemy newEnemy = GetPooledEnemy (enemyPrefab);
        newEnemy.transform.position = _enemyPaths[0].position;
        newEnemy.SetTargetPosition (_enemyPaths[1].position);
        newEnemy.SetCurrentPathIndex (1);
        newEnemy.gameObject.SetActive (true);
    }

    private Enemy GetPooledEnemy (Enemy enemyPrefab)
    {
        Enemy pooledEnemy = _spawnedEnemies.Find (enemy =>
            enemy != null
            && !enemy.gameObject.activeSelf
            && enemy.name.StartsWith (enemyPrefab.name));

        if (pooledEnemy != null)
        {
            return pooledEnemy;
        }

        Enemy newEnemy = Instantiate (enemyPrefab);
        _spawnedEnemies.Add (newEnemy);
        return newEnemy;
    }

    private void OnDrawGizmos ()
    {
        if (_enemyPaths == null || _enemyPaths.Length < 2)
        {
            return;
        }

        for (int i = 0; i < _enemyPaths.Length - 1; i++)
        {
            if (_enemyPaths[i] == null || _enemyPaths[i + 1] == null)
            {
                continue;
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine (_enemyPaths[i].position, _enemyPaths[i + 1].position);
        }
    }

    private void InitializeEnemyPaths ()
    {
        if (!_autoBuildEnemyPaths)
        {
            return;
        }

        Transform[] autoBuiltPaths = BuildPathsFromWayObjects ();
        bool hasAutoBuiltPath = autoBuiltPaths != null
            && autoBuiltPaths.Length >= 2
            && autoBuiltPaths.All (way => way != null);

        if (hasAutoBuiltPath)
        {
            _enemyPaths = autoBuiltPaths;
            return;
        }

        if (!HasUsableEnemyPath ())
        {
            Debug.LogWarning ("Enemy path is invalid. Add waypoint objects named 'Way n (0)', 'Way n (1)', etc.");
        }
    }

    private bool HasUsableEnemyPath ()
    {
        return _enemyPaths != null
            && _enemyPaths.Length >= 2
            && _enemyPaths.All (way => way != null);
    }

    private Transform[] BuildPathsFromWayObjects ()
    {
        List<(int index, Transform point)> orderedPoints = new List<(int index, Transform point)> ();
        Transform[] allTransforms = FindObjectsOfType<Transform> (true);

        foreach (Transform tr in allTransforms)
        {
            string trName = tr.name;
            if (!trName.StartsWith ("Way n (") || !trName.EndsWith (")"))
            {
                continue;
            }

            string indexPart = trName.Substring (7, trName.Length - 8);
            if (!int.TryParse (indexPart, out int parsedIndex))
            {
                continue;
            }

            orderedPoints.Add ((parsedIndex, tr));
        }

        return orderedPoints
            .OrderBy (entry => entry.index)
            .Select (entry => entry.point)
            .ToArray ();
    }

    public Bullet GetBulletFromPool (Bullet prefab)
    {
        int prefabKey = prefab.GetInstanceID ();
        if (!_bulletPoolByPrefab.TryGetValue (prefabKey, out List<Bullet> pool))
        {
            pool = new List<Bullet> ();
            _bulletPoolByPrefab[prefabKey] = pool;
        }

        Bullet pooledBullet = pool.Find (b => b != null && !b.gameObject.activeSelf);
        if (pooledBullet != null)
        {
            return pooledBullet;
        }

        Bullet newBullet = Instantiate (prefab);
        pool.Add (newBullet);
        return newBullet;
    }

    public void ExplodeAt (Vector2 point, float radius, int damage)
    {
        ExplosionFx.PlayMagicExplosion (point, radius);

        foreach (Enemy enemy in _spawnedEnemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                if (Vector2.Distance (enemy.transform.position, point) <= radius)
                {
                    enemy.ReduceEnemyHealth (damage);
                }
            }
        }
    }

    public void ReduceLives (int value)
    {
        SetCurrentLives (_currentLives - value);
        if (_currentLives <= 0)
        {
            SetGameOver (false);
        }
    }

    public void SetCurrentLives (int currentLives)
    {
        _currentLives = Mathf.Max (currentLives, 0);
        if (_livesInfo != null)
        {
            _livesInfo.text = $"Життя: {_currentLives}";
        }
    }

    public bool CanAffordTower (Tower tower)
    {
        return tower != null && _currentGold >= Tower.GetBlueprintGoldCost (tower);
    }

    public bool TryBuyTower (int towerCost)
    {
        if (_isPaused || !IsPreparationPhase || towerCost <= 0 || _currentGold < towerCost)
        {
            return false;
        }

        SetGold (_currentGold - towerCost);
        return true;
    }

    public void AddGold (int value)
    {
        if (value <= 0)
        {
            return;
        }

        SetGold (_currentGold + value);
    }

    private void SetGold (int gold)
    {
        _currentGold = Mathf.Max (gold, 0);
        RefreshHud ();
    }

    private int GetWavesRemainingIncludingCurrent ()
    {
        if (_totalRounds <= 0)
        {
            return 0;
        }

        if (_currentPhase == BattlePhase.Preparation)
        {
            return Mathf.Max (0, _totalRounds - _currentRound);
        }

        return Mathf.Max (0, _totalRounds - _currentRound + 1);
    }

    private void RefreshHud ()
    {
        int aliveEnemies = _spawnedEnemies.Count (enemy => enemy != null && enemy.gameObject.activeSelf);
        int pendingEnemies = _pendingWaveEnemies.Count;
        int totalEnemiesInRound = aliveEnemies + pendingEnemies;
        int wavesLeft = GetWavesRemainingIncludingCurrent ();
        if (_totalEnemyInfo != null)
        {
            _totalEnemyInfo.text = $"Вороги в хвилі: {totalEnemiesInRound} · Хвиль залишилось: {wavesLeft}";
        }

        if (_goldInfo != null)
        {
            _goldInfo.text = $"Золото: {_currentGold}";
        }

        if (_livesInfo != null)
        {
            _livesInfo.text = $"Життя: {_currentLives}";
        }

        if (_phaseInfo != null)
        {
            if (_isPaused)
            {
                _phaseInfo.text = "Пауза · натисніть ESC, щоб продовжити";
                _phaseInfo.color = new Color (0.75f, 0.85f, 1f, 1f);
            }
            else if (_currentPhase == BattlePhase.Preparation)
            {
                int secLeft = Mathf.Max (0, Mathf.CeilToInt (_preparationTimer));
                _phaseInfo.text = $"Підготовка · можна ставити вежі · {secLeft} с";
                _phaseInfo.color = new Color (0.55f, 1f, 0.65f, 1f);
            }
            else
            {
                _phaseInfo.text = $"Бій · хвиля {_currentRound}/{_totalRounds} · розміщення веж вимкнено";
                _phaseInfo.color = new Color (1f, 0.62f, 0.45f, 1f);
            }
        }
    }

    private void EnsureGoldInfoText ()
    {
        if (_goldInfo != null || _livesInfo == null)
        {
            return;
        }

        GameObject goldTextObj = Instantiate (_livesInfo.gameObject, _livesInfo.transform.parent);
        goldTextObj.name = "Gold";

        RectTransform livesRect = _livesInfo.rectTransform;
        RectTransform goldRect = goldTextObj.GetComponent<RectTransform> ();
        goldRect.anchorMin = livesRect.anchorMin;
        goldRect.anchorMax = livesRect.anchorMax;
        goldRect.pivot = livesRect.pivot;
        goldRect.anchoredPosition = livesRect.anchoredPosition + new Vector2 (0f, -40f);
        goldRect.sizeDelta = new Vector2 (Mathf.Max (livesRect.sizeDelta.x, 260f), Mathf.Max (livesRect.sizeDelta.y, 36f));

        _goldInfo = goldTextObj.GetComponent<Text> ();
    }

    private void EnsurePhaseInfoText ()
    {
        if (_phaseInfo != null || _livesInfo == null)
        {
            return;
        }

        GameObject phaseObj = Instantiate (_livesInfo.gameObject, _livesInfo.transform.parent);
        phaseObj.name = "Phase";
        _phaseInfo = phaseObj.GetComponent<Text> ();
    }

    private void ApplyHudVisibility ()
    {
        StyleHudText (_livesInfo);
        StyleHudText (_totalEnemyInfo);
        StyleHudText (_goldInfo);
        StyleHudText (_phaseInfo);
        if (_goldInfo != null)
        {
            _goldInfo.color = new Color (1f, 0.92f, 0.35f, 1f);
        }

        if (_totalEnemyInfo != null)
        {
            _totalEnemyInfo.transform.SetAsLastSibling ();
        }

        if (_livesInfo != null)
        {
            _livesInfo.transform.SetAsLastSibling ();
        }

        if (_goldInfo != null)
        {
            _goldInfo.transform.SetAsLastSibling ();
        }

        if (_phaseInfo != null)
        {
            _phaseInfo.transform.SetAsLastSibling ();
        }
    }

    private void LayoutHudElements ()
    {
        const float leftInset = 16f;
        const float topInset = 14f;
        const float rowHeight = 40f;

        if (_goldInfo != null)
        {
            ApplyHudRowLayout (_goldInfo.rectTransform, leftInset, topInset, 300f, rowHeight);
        }

        if (_livesInfo != null)
        {
            ApplyHudRowLayout (_livesInfo.rectTransform, leftInset, topInset + rowHeight, 220f, rowHeight);
        }

        if (_totalEnemyInfo != null)
        {
            ApplyHudRowLayout (_totalEnemyInfo.rectTransform, leftInset, topInset + (rowHeight * 2f), 520f, rowHeight);
        }

        if (_phaseInfo != null)
        {
            RectTransform r = _phaseInfo.rectTransform;
            r.anchorMin = new Vector2 (0.5f, 1f);
            r.anchorMax = new Vector2 (0.5f, 1f);
            r.pivot = new Vector2 (0.5f, 1f);
            r.anchoredPosition = new Vector2 (0f, -8f);
            r.sizeDelta = new Vector2 (860f, 44f);
        }
    }

    private static void ApplyHudRowLayout (RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = new Vector2 (0f, 1f);
        rect.anchorMax = new Vector2 (0f, 1f);
        rect.pivot = new Vector2 (0f, 1f);
        rect.anchoredPosition = new Vector2 (left, -top);
        rect.sizeDelta = new Vector2 (width, height);
    }

    private void LayoutTowerShop ()
    {
        if (_towerUIParent == null)
        {
            return;
        }

        HorizontalLayoutGroup layout = _towerUIParent.GetComponent<HorizontalLayoutGroup> ();
        if (layout != null)
        {
            layout.padding.left = 20;
            layout.padding.right = 12;
            layout.padding.top = 12;
            layout.padding.bottom = 8;
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
        }

        const float slotSize = 108f;
        for (int i = 0; i < _towerUIParent.childCount; i++)
        {
            RectTransform child = _towerUIParent.GetChild (i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            child.sizeDelta = new Vector2 (slotSize, slotSize);
        }

        RectTransform shopRoot = _towerUIParent.parent as RectTransform;
        if (shopRoot != null)
        {
            shopRoot.anchoredPosition = new Vector2 (-24f, shopRoot.anchoredPosition.y);
        }
    }

    private static void StyleHudText (Text text)
    {
        if (text == null)
        {
            return;
        }

        UiFont.ApplyTo (text);

        RectTransform rect = text.rectTransform;
        Vector3 lp = rect.localPosition;
        rect.localPosition = new Vector3 (lp.x, lp.y, 0f);

        if (rect.sizeDelta.x < 80f || rect.sizeDelta.y < 24f)
        {
            rect.sizeDelta = new Vector2 (Mathf.Max (rect.sizeDelta.x, 280f), Mathf.Max (rect.sizeDelta.y, 36f));
        }

        text.raycastTarget = false;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        if (text.gameObject.name == "Lives")
        {
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
        }
        else if (text.gameObject.name == "Gold")
        {
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
        }
        else if (text.gameObject.name == "Total Enemy")
        {
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
        }
        else if (text.gameObject.name == "Phase")
        {
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
        }
        else
        {
            text.color = Color.white;
        }

        Outline outline = text.GetComponent<Outline> ();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline> ();
        }

        outline.effectColor = new Color (0f, 0f, 0f, 0.92f);
        outline.effectDistance = new Vector2 (2f, -2f);
    }

    public void SetGameOver (bool isWin)
    {
        SetPaused (false);
        IsOver = true;
        _statusInfo.text = isWin ? "You Win!" : "You Lose!";
        _panel.gameObject.SetActive (true);
    }
}
