using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class Character : Agent {

    Rigidbody2D rb;
    public Animator bodyAnimator;
    public Animator headAnimator;

    const float moveSpeed = 8f;

    Vector2 movement;
    Vector2 last_movement;

    float shootVert;
    float shootHor;

    float tearDamage = 3.5f;
    float tearRange = 12f;
    float fireDelay = 0.6f;
    float lastFire = 0f;
    float shotSpeed = 10f;
    public GameObject tearPrefab;

    float lastHit = 0f;
    float immunityFrames = 1f;

    int maxHP = 4;
    int curHP;

    [HideInInspector]
    public Tilemap tm;
    [HideInInspector]
    public int lastHitEnemyID;

    public List<GameObject> tilemapList;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    int sign(float a)
    {
        if (a > 0) return 1;
        else if (a < 0) return -1;
        else return 0;
    }

    
    private void FixedUpdate()
    {
        bodyAnimator.SetFloat("Horizontal", movement.x);
        bodyAnimator.SetFloat("Vertical", movement.y);
        bodyAnimator.SetFloat("Speed", movement.sqrMagnitude);

        headAnimator.SetFloat("Horizontal", movement.x);
        headAnimator.SetFloat("Vertical", movement.y);
        headAnimator.SetFloat("Speed", movement.sqrMagnitude);
        headAnimator.SetFloat("ShootHorizontal", shootHor);
        headAnimator.SetFloat("ShootVertical", shootVert);
        headAnimator.SetBool("Shooting", (shootHor != 0 || shootVert != 0));

        if (movement.sqrMagnitude > 0)
        {
            rb.velocity = (movement.normalized + last_movement.normalized * 5f).normalized * moveSpeed;
            last_movement = movement;
        }
        else { 
            if (rb.velocity.magnitude < 0.05f) 
            { 
                rb.velocity = Vector2.zero; 
                last_movement = Vector2.zero; 
            } else 
                rb.velocity *= 0.775f; };
    }
    

    private void Shoot(float shootHor, float shootVert)
    {
        headAnimator.SetTrigger("Shot");
        GameObject tear = Instantiate(tearPrefab, new Vector3(transform.position.x, transform.position.y - 0.25f, transform.position.z), transform.rotation) as GameObject;
        tear.GetComponent<Tear>().range = tearRange;
        tear.GetComponent<Tear>().damage = tearDamage;
        if (shootHor != 0 && shootVert != 0) shootHor = 0;
        Vector2 temp = movement.normalized * moveSpeed / 2f;
        float addSpeed = (shootHor != 0 ? (sign(temp.x) == sign(shootHor) ? Mathf.Abs(temp.x) : 0) : (sign(temp.y) == sign(shootVert) ? Mathf.Abs(temp.y) : 0));
        tear.GetComponent<Rigidbody2D>().velocity = (new Vector2(shootHor, shootVert) + (movement.normalized) / 3f).normalized * (shotSpeed + addSpeed);
    }

    public void Damaged(int damage)
    {
        AddReward(-0.1f);
        if (Time.time > lastHit + immunityFrames)
        {
            curHP = curHP - damage;
            if (curHP <= 0)
            {
                AddReward(-1f);
                EndEpisode();
            }
            else
            {
                bodyAnimator.SetTrigger("Damaged");
                headAnimator.SetTrigger("Damaged");
            }
            lastHit = Time.time;
        }
    }

    public int GetEnemyType(string name)
    {
        switch (name)
        {
            case "Gaper": return 1;
            case "Fatty": return 2;
            case "AttackFly": return 3;
            default: return -1;
        }
    }
    public static float Dist(float ax, float ay, float bx, float by)
    {
        return Vector2.Distance(new Vector2(ax, ay), new Vector2(bx, by));
    }

    //_____________________ LEARNING ________________________

    public override void OnEpisodeBegin()
    {
        Enemy.ID_setter = 0;
        Destroy(FindObjectOfType<Tilemap>().gameObject);
        Instantiate(tilemapList[(new System.Random()).Next(0, tilemapList.Count)], FindObjectOfType<Grid>().transform);

        curHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        tm = FindFirstObjectByType<Tilemap>();
        lastHitEnemyID = -1;
        AstarPath.active.Scan();

        posReroll:
        float x, y;
        switch((new System.Random()).Next(0, 4))
        {
            case 0: x = 9.6f; y = 0f; break;
            case 1: x = -9.6f; y = 0f; break;
            case 2: x = 0f; y = 4.8f; break;
            case 3: x = 0f; y = -4.8f; break;
            default: x = 0f; y = 0f; break;
        }
        if (tm.GetTile(tm.WorldToCell(new Vector3(x, y, 0))) != null) goto posReroll; // Если комната не допускает вход через полученную позицию - меняем.

        transform.position = new Vector3(x, y, 0);
        rb.velocity = Vector2.zero;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        movement.x = actions.ContinuousActions[0];
        movement.y = actions.ContinuousActions[1];
        shootHor = actions.ContinuousActions[2];
        shootVert = actions.ContinuousActions[3];

        if ((shootHor != 0 || shootVert != 0) && Time.time > lastFire + fireDelay)
        {
            Shoot(shootHor, shootVert);
            lastFire = Time.time;
        }

        if (FindObjectsOfType<Enemy>().Length == 0)
        {
            AddReward(1f);
            EndEpisode();
        }

        AddReward(-0.0001f);
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // GET CHARACTER INFO
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.y);

        // GET TEAR INFO
        sensor.AddObservation(lastHitEnemyID);

        // GET TILE INFO
        int r = 3;
        for (int i = 0; i < 2 * r + 1; i++)
            for (int j = 0; j < 2 * r + 1; j++)
            {
                Vector3Int coor = tm.WorldToCell(transform.position) + new Vector3Int(i - r, j - r, 0);
                if (coor.x >= tm.size.x || coor.y >= tm.size.y || coor.x < 0 || coor.y < 0) sensor.AddObservation(-1); // out of bounds
                else
                {
                    TileBase tile = tm.GetTile(coor);
                    if (tile == null) sensor.AddObservation(0); // air
                    else switch (tile.name)
                        {
                            case "Rock": sensor.AddObservation(1); break;
                            case "Fire_Place": sensor.AddObservation(2); break;
                            default: break;
                        }
                }
            }

        // GET ENEMY INFO
        int amount = 5;
        Enemy[] lst = FindObjectsOfType<Enemy>();
        int len = lst.Length;

        var e = new List<(int type, int ID, float x, float y, float dist)>();

        for (int i = 0; i < len; i++)
            e.Add((GetEnemyType(lst[i].name), lst[i].ID, lst[i].transform.position.x, lst[i].transform.position.y,
                Dist(transform.position.x, transform.position.y, lst[i].transform.position.x, lst[i].transform.position.y)));

        e.Sort((a, b) => a.dist.CompareTo(b.dist));

        if (len >= amount)
        {
            for (int i = 0; i < amount; i++)
            {
                sensor.AddObservation(e[i].type);
                sensor.AddObservation(e[i].ID);
                sensor.AddObservation(e[i].x);
                sensor.AddObservation(e[i].y);
            }
        }
        else
        {
            for (int i = 0; i < len; i++)
            {
                sensor.AddObservation(e[i].type);
                sensor.AddObservation(e[i].ID);
                sensor.AddObservation(e[i].x);
                sensor.AddObservation(e[i].y);
            }
            for (int i = 0; i < amount - len; i++)
            {
                sensor.AddObservation(-1);
                sensor.AddObservation(-1);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
            }
        }
    }
    

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("ShootHorizontal");
        continuousActionsOut[3] = Input.GetAxis("ShootVertical");
    }

    /*
    public int[,] GetTileInfo(int r)
    {
        int[,] map = new int[2*r+1, 2*r+1];

        for (int i = 0; i < 2*r+1; i++)
            for(int j = 0; j < 2*r+1; j++)
            {
                Vector3Int coor = tm.WorldToCell(transform.position) + new Vector3Int(i-r, j-r, 0);
                if (coor.x >= tm.size.x || coor.y >= tm.size.y || coor.x < 0 || coor.y < 0) map[i, j] = -1; // out of bounds
                else
                {
                    var a = tm.GetTile(coor);
                    if (a == null) map[i, j] = 0; // air
                    else switch (a.name)
                        {
                            case "Rock": map[i, j] = 1; break;
                            case "Fire_Place": map[i, j] = 2; break;
                            default: break;
                        }
                }
            }

        // DEBUG
        for (int j = 2 * r; j >= 0; j--) {
            string str = "";
            for (int i = 0; i < 2*r+1; i++)
            {
                str += $"{map[i, j]} ";
            }
            Debug.Log(str);
        }
        Debug.Log("__________");

        return map;
    }
    */


}
