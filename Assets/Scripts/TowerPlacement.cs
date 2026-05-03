using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlacement : MonoBehaviour
{
    private static readonly List<TowerPlacement> Instances = new List<TowerPlacement> ();

    private Tower _placedTower;

    private void OnEnable ()
    {
        if (!Instances.Contains (this))
        {
            Instances.Add (this);
        }
    }

    private void OnDisable ()
    {
        Instances.Remove (this);
    }

    public static void ReleaseTowerFromAllSlots (Tower tower)
    {
        if (tower == null)
        {
            return;
        }

        for (int i = Instances.Count - 1; i >= 0; i--)
        {
            TowerPlacement zone = Instances[i];
            if (zone != null && zone._placedTower == tower)
            {
                zone._placedTower = null;
            }
        }
    }

    private static Tower ResolveTowerFromCollider (Collider2D collision)
    {
        if (collision == null)
        {
            return null;
        }

        Tower tower = collision.GetComponentInParent<Tower> ();
        if (tower != null)
        {
            return tower;
        }

        if (collision.attachedRigidbody != null)
        {
            return collision.attachedRigidbody.GetComponent<Tower> ();
        }

        return null;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter2D (Collider2D collision)
    {
        Tower tower = ResolveTowerFromCollider (collision);
        if (tower == null)
        {
            return;
        }

        if (_placedTower != null && _placedTower != tower)
        {
            _placedTower = null;
        }

        if (_placedTower == tower)
        {
            return;
        }

        tower.SetPlacePosition (transform.position);
        _placedTower = tower;
    }

    private void OnTriggerExit2D (Collider2D collision)
    {
        Tower tower = ResolveTowerFromCollider (collision);
        if (tower == null || _placedTower != tower)
        {
            return;
        }

        _placedTower.SetPlacePosition (null);
        _placedTower = null;
    }
}
