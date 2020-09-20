using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform SpawnArea;
    public GameObject PlayerPrefab;
    public GameObject Player;
    public GameObject HUD;
    public float RespawnCountDown = 5f;

    private float m_deathTime = 0.0f;
    private bool isDead;    


    // Start is called before the first frame update
    void Start()
    {
        Respawn();
    }

    void Update()
    {
        if (isDead)
        {
            m_deathTime += Time.deltaTime;

            if (m_deathTime >= RespawnCountDown)
            {
                Respawn();
            }
        }
    }

    public void Respawn()
    {
        Vector3 origin = SpawnArea.position;
        Vector3 range = SpawnArea.localScale / 2.0f;
        Vector3 randomRange = new Vector3(Random.Range(-range.x, range.x),
                                          Random.Range(-range.y, range.y),
                                          Random.Range(-range.z, range.z));
        Vector3 randomCoordinate = origin + randomRange;

        Player = Instantiate(PlayerPrefab, randomCoordinate, Quaternion.identity);
        var health = Player.GetComponent<Health>();
        health.onDie += OnDie;
        isDead = false;
        HUD.SetActive(true);
    }

    void OnDie()
    {
        HUD.SetActive(false);
        isDead = true;
        m_deathTime = Time.deltaTime;
    }
}
