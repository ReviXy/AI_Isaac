using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock_Tile : MonoBehaviour
{
    float penaltyDelay = 0.35f;
    float lastPenalty = 0f;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Character"))
        {
            if (Time.time > lastPenalty + penaltyDelay)
            {
                collision.gameObject.GetComponent<Character>().AddReward(-0.05f);
                lastPenalty = Time.time;
            }
        }
    }
}
