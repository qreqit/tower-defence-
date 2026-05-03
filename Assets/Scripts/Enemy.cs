using UnityEngine;

public class Enemy : MonoBehaviour
{
    private enum EnemyArchetype
    {
        Unknown,
        Goblin,
        Orc,
        Ghost,
    }

    [SerializeField] private int _attackCost = 10;
    [SerializeField] private int _goldReward = 10;
    [SerializeField] private int _maxHealth = 1;
    [SerializeField] private float _moveSpeed = 1f;
    [SerializeField] private bool _ignoreSlowEffects = false;
    [SerializeField] private SpriteRenderer _healthBar;
    [SerializeField] private SpriteRenderer _healthFill;

    private int _currentHealth;
    private float _slowMultiplier = 1f;
    private float _slowDurationRemaining = 0f;
    private bool _isKilledByDamage = false;

    public Vector3 TargetPosition { get; private set; }
    public int CurrentPathIndex { get; private set; }
    public int AttackCost => _attackCost;
    public int GoldReward => _goldReward;

    private void Awake ()
    {
        ApplyEnemyArchetype ();
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (_slowDurationRemaining > 0f)
        {
            _slowDurationRemaining -= Time.deltaTime;
            if (_slowDurationRemaining <= 0f)
            {
                _slowDurationRemaining = 0f;
                _slowMultiplier = 1f;
            }
        }
    }

    private void OnValidate ()
    {
        ApplyEnemyArchetype ();
    }

    private void ApplyEnemyArchetype ()
    {
        EnemyArchetype archetype = ResolveArchetype (gameObject.name);
        switch (archetype)
        {
            case EnemyArchetype.Goblin:
                _attackCost = 10;
                _goldReward = 10;
                _maxHealth = 4;
                _moveSpeed = 2.35f;
                _ignoreSlowEffects = false;
                break;

            case EnemyArchetype.Orc:
                _attackCost = 25;
                _goldReward = 25;
                _maxHealth = 38;
                _moveSpeed = 0.82f;
                _ignoreSlowEffects = false;
                break;

            case EnemyArchetype.Ghost:
                _attackCost = 20;
                _goldReward = 20;
                _maxHealth = 12;
                _moveSpeed = 1.48f;
                _ignoreSlowEffects = true;
                break;

            default:
                if (_goldReward <= 0)
                {
                    _goldReward = Mathf.Max (1, _attackCost);
                }

                break;
        }
    }

    private static EnemyArchetype ResolveArchetype (string rawName)
    {
        string enemyName = rawName.ToLowerInvariant ();
        if (enemyName.Contains ("enemy variant 1") || enemyName.Contains ("variant 1") || enemyName.Contains ("goblin"))
        {
            return EnemyArchetype.Goblin;
        }

        if (enemyName.Contains ("enemy variant 2") || enemyName.Contains ("variant 2") || enemyName.Contains ("orc"))
        {
            return EnemyArchetype.Orc;
        }

        if (enemyName.Contains ("enemy variant 3") || enemyName.Contains ("variant 3") || enemyName.Contains ("ghost"))
        {
            return EnemyArchetype.Ghost;
        }

        return EnemyArchetype.Unknown;
    }

    private void OnEnable ()
    {
        _currentHealth = _maxHealth;
        _healthFill.size = _healthBar.size;
        _slowMultiplier = 1f;
        _slowDurationRemaining = 0f;
        _isKilledByDamage = false;
    }

    public void MoveToTarget ()
    {
        float effectiveSpeed = _moveSpeed * _slowMultiplier;
        transform.position = Vector3.MoveTowards (transform.position, TargetPosition, effectiveSpeed * Time.deltaTime);
    }

    public void SetTargetPosition (Vector3 targetPosition)
    {
        TargetPosition = targetPosition;
        _healthBar.transform.parent = null;

        Vector3 distance = TargetPosition - transform.position;
        if (Mathf.Abs (distance.y) > Mathf.Abs (distance.x))
        {
            if (distance.y > 0)
            {
                transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, 90f));
            }

            else
            {
                transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, -90f));
            }
        }
        else
        {
            if (distance.x > 0)
            {
                transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, 0f));
            }

            else
            {
                transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, 180f));
            }
        }
        _healthBar.transform.parent = transform;
    }

    public void SetCurrentPathIndex (int currentIndex)
    {
        CurrentPathIndex = currentIndex;
    }

    public void ReduceEnemyHealth (int damage)
    {
        _currentHealth -= damage;
        AudioPlayer.Instance.PlaySFX ("hit-enemy");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _isKilledByDamage = true;
            ExplosionFx.PlayEnemyDeath (transform.position);
            gameObject.SetActive (false);
            AudioPlayer.Instance.PlaySFX ("enemy-die");
        }

        float healthPercentage = (float) _currentHealth / _maxHealth;
        _healthFill.size = new Vector2 (healthPercentage * _healthBar.size.x, _healthBar.size.y);
    }

    private void OnDisable ()
    {
        if (_isKilledByDamage && LevelManager.Instance != null && !LevelManager.Instance.IsOver)
        {
            LevelManager.Instance.AddGold (_goldReward);
        }
    }

    public int GetCurrentHealth ()
    {
        return _currentHealth;
    }

    public float GetPathProgressScore ()
    {
        float distanceToTarget = Vector2.Distance (transform.position, TargetPosition);
        return CurrentPathIndex - (distanceToTarget * 0.01f);
    }

    public void ApplySlow (float speedMultiplier, float duration)
    {
        if (_ignoreSlowEffects)
        {
            return;
        }

        _slowMultiplier = Mathf.Min (_slowMultiplier, Mathf.Clamp (speedMultiplier, 0.1f, 1f));
        _slowDurationRemaining = Mathf.Max (_slowDurationRemaining, duration);
    }
}
