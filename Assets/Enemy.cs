using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Enemy : MonoBehaviour
{
    Rigidbody2D rb;
    Character character;

    public float maxHP;
    public float curHP;
    public float speed;

    public static int ID_setter = 0;
    [HideInInspector]
    public int ID;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ID = ID_setter++;
        curHP = maxHP;
        character = FindObjectOfType<Character>();
        GetComponent<AIDestinationSetter>().target = character.transform;
        GetComponent<AIPath>().maxSpeed = speed;
    }

    public void Damaged(float damage)
    {
            curHP = curHP - damage;
            if (curHP <= 0)
            {
                FindObjectOfType<Character>().AddReward(1.5f);
                Destroy(gameObject);
            }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Character"))
        {
            collision.gameObject.GetComponent<Character>().Damaged(1);
        }
    }


}
