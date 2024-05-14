using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tear : MonoBehaviour
{
    Vector2 startTransform;

    [HideInInspector]
    public float range;
    public float damage;

    void Start()
    {
        startTransform = transform.position;
    }
    void FixedUpdate()
    {
        if (Vector2.Distance(startTransform, transform.position) > range)
            Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Rock"))
        {
            FindObjectOfType<Character>().AddReward(-0.05f);
            FindObjectOfType<Character>().lastHitEnemyID = -1;
            Destroy(gameObject);
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            FindObjectOfType<Character>().AddReward(0.05f);
            collision.GetComponent<Enemy>().Damaged(damage);
            FindObjectOfType<Character>().lastHitEnemyID = collision.GetComponent<Enemy>().ID;
            Destroy(gameObject);
        }

        if (collision.gameObject.CompareTag("Fire_Place"))
        {
            FindObjectOfType<Character>().lastHitEnemyID = -1;
            collision.gameObject.GetComponent<DamagingTile>().Hit();
            Destroy(gameObject);
        }
        
    }

    private void OnDestroy()
    {
        
    }

}
