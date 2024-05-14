using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DamagingTile : MonoBehaviour
{
    public GameObject sprite;

    int maxHP = 3;
    int curHP;

    private void Start()
    {
        curHP = maxHP;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Character"))
        {
            collision.gameObject.GetComponent<Character>().Damaged(1);
        }
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        /*
        if (gameObject.CompareTag("Fire_Place"))
            if (collision.gameObject.CompareTag("Tear"))
            {
                Destroy(collision.gameObject);
                Destroy(gameObject);
            }
        */
    }

    public void Hit()
    {
        curHP--;
        if (curHP <= 0) {

            Tilemap tm = FindFirstObjectByType<Tilemap>();
            tm.SetTile(tm.WorldToCell(transform.position), null);
            AstarPath.active.Scan();
        }
        else
            sprite.transform.localScale = new Vector3(1 - 0.25f * (maxHP - curHP), 1 - 0.25f * (maxHP - curHP), 1);
    }

}
