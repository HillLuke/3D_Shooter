using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform SpawnArea;
    public GameObject PlayerPrefab;
    public GameObject Player;

    // Start is called before the first frame update
    void Start()
    {
        Respawn();
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
    }

    void OnDie()
    {
        Debug.Log("Dead!");
    }
}
