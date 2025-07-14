using UnityEngine;
using Unity.Netcode;

public class DebrisSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private int debrisCount = 10;
    [SerializeField] private float spawnRadius = 20f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Only the server (or host) spawns debris

        SpawnDebris();
    }

    private void SpawnDebris()
    {
        for (int i = 0; i < debrisCount; i++)
        {
            Vector3 position = transform.position + Random.insideUnitSphere * spawnRadius;
            position.y = Mathf.Max(1f, position.y); // Optional: keep debris above ground

            GameObject debris = Instantiate(debrisPrefab, position, Quaternion.identity);
            debris.GetComponent<NetworkObject>().Spawn();
        }
    }
}
