using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public enum TargetingMode
    {
        MostProgress = 0,
        Closest = 1,
        Farthest = 2,
        Weakest = 3,
        Strongest = 4,
    }

    private enum TowerArchetype
    {
        Unknown,
        Archer,
        Cannon,
        Freezer,
        Mage,
    }

    [SerializeField] private SpriteRenderer _towerPlace;
    [SerializeField] private SpriteRenderer _towerHead;

    [SerializeField] private int _goldCost = 100;
    [SerializeField] private int _shootPower = 1;
    [SerializeField] private float _shootDistance = 1f;
    [SerializeField] private float _shootDelay = 5f;
    [SerializeField] private float _bulletSpeed = 1f;
    [SerializeField] private float _bulletSplashRadius = 0f;
    [SerializeField] private TargetingMode _targetingMode = TargetingMode.MostProgress;
    [SerializeField] private bool _canApplySlow = false;
    [SerializeField] private float _slowMultiplier = 0.6f;
    [SerializeField] private float _slowDuration = 1.5f;

    [SerializeField] private Bullet _bulletPrefab;
    
    private float _runningShootDelay;
    private Enemy _targetEnemy;
    private Quaternion _targetRotation;

    public Vector2? PlacePosition { get; private set; }
    public int GoldCost => _goldCost;

    public static int GetBlueprintGoldCost (Tower prefab)
    {
        if (prefab == null)
        {
            return int.MaxValue;
        }

        return GetGoldCostForArchetype (ResolveArchetype (prefab.gameObject.name));
    }

    private void Awake ()
    {
        ApplyTowerArchetype ();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnValidate ()
    {
        ApplyTowerArchetype ();
    }

    private void ApplyTowerArchetype ()
    {
        TowerArchetype archetype = ResolveArchetype (gameObject.name);
        switch (archetype)
        {
            case TowerArchetype.Archer:
                _goldCost = 100;
                _shootPower = 2;
                _shootDistance = 5.5f;
                _shootDelay = 1f;
                _bulletSpeed = 5.8f;
                _bulletSplashRadius = 0f;
                _canApplySlow = false;
                _slowMultiplier = 0.55f;
                _slowDuration = 2f;
                _targetingMode = TargetingMode.MostProgress;
                break;

            case TowerArchetype.Cannon:
                _goldCost = 200;
                _shootPower = 15;
                _shootDistance = 9f;
                _shootDelay = 4.25f;
                _bulletSpeed = 3.4f;
                _bulletSplashRadius = 0f;
                _canApplySlow = false;
                _targetingMode = TargetingMode.Strongest;
                break;

            case TowerArchetype.Freezer:
                _goldCost = 120;
                _shootPower = 1;
                _shootDistance = 5.25f;
                _shootDelay = 1.05f;
                _bulletSpeed = 5.5f;
                _bulletSplashRadius = 0f;
                _canApplySlow = true;
                _slowMultiplier = 0.48f;
                _slowDuration = 2.6f;
                _targetingMode = TargetingMode.MostProgress;
                break;

            case TowerArchetype.Mage:
                _goldCost = 150;
                _shootPower = 5;
                _shootDistance = 3.65f;
                _shootDelay = 2.15f;
                _bulletSpeed = 4.8f;
                _bulletSplashRadius = 2.15f;
                _canApplySlow = false;
                _targetingMode = TargetingMode.MostProgress;
                break;

            default:
                break;
        }
    }

    private static TowerArchetype ResolveArchetype (string rawName)
    {
        string towerName = rawName.ToLowerInvariant ();
        if (towerName.Contains ("tower variant 1") || towerName.Contains ("variant 1") || towerName.Contains ("archer"))
        {
            return TowerArchetype.Archer;
        }

        if (towerName.Contains ("tower variant 2") || towerName.Contains ("variant 2") || towerName.Contains ("cannon"))
        {
            return TowerArchetype.Cannon;
        }

        if (towerName.Contains ("tower variant 3") || towerName.Contains ("variant 3") || towerName.Contains ("freezer"))
        {
            return TowerArchetype.Freezer;
        }

        if (towerName.Contains ("tower variant 4") || towerName.Contains ("variant 4") || towerName.Contains ("mage"))
        {
            return TowerArchetype.Mage;
        }

        return TowerArchetype.Unknown;
    }

    private static int GetGoldCostForArchetype (TowerArchetype archetype)
    {
        switch (archetype)
        {
            case TowerArchetype.Archer:
                return 100;
            case TowerArchetype.Cannon:
                return 200;
            case TowerArchetype.Freezer:
                return 120;
            case TowerArchetype.Mage:
                return 150;
            default:
                return 100;
        }
    }

    public Sprite GetTowerHeadIcon ()
    {
        SpriteRenderer iconRenderer = GetAimRenderer ();
        return iconRenderer != null ? iconRenderer.sprite : null;
    }

    public void SetPlacePosition(Vector2? newPosition)
    {
        PlacePosition = newPosition;
    }

    public void LockPlacement ()
    {
        transform.position = (Vector2) PlacePosition;
        TowerPlacement.ReleaseTowerFromAllSlots (this);
    }

    private void OnDestroy ()
    {
        TowerPlacement.ReleaseTowerFromAllSlots (this);
    }

    public void ToggleOrderInLayer (bool toFront)
    {
        int orderInLayer = toFront ? 2 : 0;
        if (_towerPlace != null)
        {
            _towerPlace.sortingOrder = orderInLayer;
        }
        if (_towerHead != null)
        {
            _towerHead.sortingOrder = orderInLayer;
        }
    }

    public void CheckNearestEnemy (List<Enemy> enemies)
    {
        if (_targetEnemy != null)
        {
            if (!_targetEnemy.gameObject.activeSelf || Vector3.Distance (transform.position, _targetEnemy.transform.position) > _shootDistance)
            {
                _targetEnemy = null;
            }
            else
            {
                return;
            }
        }

        float bestScore = float.NegativeInfinity;
        Enemy bestEnemy = null;
        
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
            {
                continue;
            }

            float distance = Vector3.Distance (transform.position, enemy.transform.position);
            if (distance > _shootDistance)
            {
                continue;
            }

            float score = GetTargetPriorityScore (enemy, distance);
            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }
        _targetEnemy = bestEnemy;
    }

    public void ShootTarget ()
    {
        if (_targetEnemy == null)
        {
            return;
        }

        _runningShootDelay -= Time.deltaTime;
        if (_runningShootDelay <= 0f)
        {
            Bullet bullet = LevelManager.Instance.GetBulletFromPool (_bulletPrefab);
            bullet.transform.position = transform.position;
            bullet.SetProperties (_shootPower, _bulletSpeed, _bulletSplashRadius, _canApplySlow, _slowMultiplier, _slowDuration);
            bullet.SetTargetEnemy (_targetEnemy);
            bullet.gameObject.SetActive (true);
            _runningShootDelay = _shootDelay;
        }
    }

    public void SeekTarget ()
    {
    }

    private SpriteRenderer GetAimRenderer ()
    {
        if (_towerHead != null)
        {
            return _towerHead;
        }

        return _towerPlace;
    }

    private float GetTargetPriorityScore (Enemy enemy, float distance)
    {
        switch (_targetingMode)
        {
            case TargetingMode.Closest:
                return -distance;
            case TargetingMode.Farthest:
                return distance;
            case TargetingMode.Weakest:
                return -enemy.GetCurrentHealth ();
            case TargetingMode.Strongest:
                return enemy.GetCurrentHealth ();
            case TargetingMode.MostProgress:
            default:
                return enemy.GetPathProgressScore ();
        }
    }
}
