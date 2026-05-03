using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _spriteRotationOffset = 0f;

    private int _bulletPower;
    private float _bulletSpeed;
    private float _bulletSplashRadius;
    private bool _canApplySlow;
    private float _slowMultiplier;
    private float _slowDuration;

    private Enemy _targetEnemy;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void FixedUpdate ()
    {
        if (LevelManager.Instance.IsOver)
        {
            return;
        }

        if (_targetEnemy != null)
        {
            if (!_targetEnemy.gameObject.activeSelf)
            {
                gameObject.SetActive (false);
                _targetEnemy = null;
                return;
            }
            transform.position = Vector3.MoveTowards (transform.position, _targetEnemy.transform.position, _bulletSpeed * Time.fixedDeltaTime);
            Vector3 direction = _targetEnemy.transform.position - transform.position;
            float targetAngle = Mathf.Atan2 (direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, targetAngle + _spriteRotationOffset));
        }
    }

    private void OnTriggerEnter2D (Collider2D collision)
    {
        if (_targetEnemy == null)
        {
            return;
        }
        
        if (collision.gameObject.Equals (_targetEnemy.gameObject))
        {
            gameObject.SetActive (false);

            if (_bulletSplashRadius > 0f)
            {
                LevelManager.Instance.ExplodeAt (transform.position, _bulletSplashRadius, _bulletPower);
            }

            else
            {
                ExplosionFx.PlayImpactBurst (transform.position);
                _targetEnemy.ReduceEnemyHealth (_bulletPower);
                if (_canApplySlow && _targetEnemy.gameObject.activeSelf)
                {
                    _targetEnemy.ApplySlow (_slowMultiplier, _slowDuration);
                }
            }
            _targetEnemy = null;
        }
    }

    public void SetProperties (int bulletPower, float bulletSpeed, float bulletSplashRadius, bool canApplySlow, float slowMultiplier, float slowDuration)
    {
        _bulletPower = bulletPower;
        _bulletSpeed = bulletSpeed;
        _bulletSplashRadius = bulletSplashRadius;
        _canApplySlow = canApplySlow;
        _slowMultiplier = slowMultiplier;
        _slowDuration = slowDuration;
    }

    public void SetTargetEnemy (Enemy enemy)
    {
        _targetEnemy = enemy;
    }
}
