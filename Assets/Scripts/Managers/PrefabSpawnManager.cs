using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawnManager : MonoBehaviour
{
    public Transform SpawnArea;
    public GameObject PrefabToSpawn;
    public float SpawnTime = 1f;
    public int maxPrefabs = 10;

    private List<GameObject> m_spawnedPrefabs = new List<GameObject>();

    void Start()
    {
        StartCoroutine(WaitAndPrint());
    }

    void Update()
    {
        
    }

    private IEnumerator WaitAndPrint()
    {
        while (true)
        {
            m_spawnedPrefabs.RemoveAll(item => item == null);

            if (m_spawnedPrefabs.Count != maxPrefabs)
            {
                Vector3 origin = SpawnArea.position;
                Vector3 range = SpawnArea.localScale / 2.0f;
                Vector3 randomRange = new Vector3(Random.Range(-range.x, range.x),
                                                  Random.Range(-range.y, range.y),
                                                  Random.Range(-range.z, range.z));
                Vector3 randomCoordinate = origin + randomRange;

                m_spawnedPrefabs.Add(Instantiate(PrefabToSpawn, randomCoordinate, Quaternion.identity));


            }
            yield return new WaitForSeconds(SpawnTime);
        }
    }
}
