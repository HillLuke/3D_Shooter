using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTester : MonoBehaviour
{
    public Transform SpawnArea;
    public GameObject Prefab;
    public float SpawnTime;

    private IEnumerator coroutine;

    void Start()
    {
        coroutine = WaitAndPrint(SpawnTime);
        StartCoroutine(coroutine);
    }

    private IEnumerator WaitAndPrint(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            Vector3 origin = SpawnArea.position;
            Vector3 range = SpawnArea.localScale / 2.0f;
            Vector3 randomRange = new Vector3(Random.Range(-range.x, range.x),
                                              Random.Range(-range.y, range.y),
                                              Random.Range(-range.z, range.z));
            Vector3 randomCoordinate = origin + randomRange;

            Instantiate(Prefab, randomCoordinate, Quaternion.identity);
        }
    }
}
