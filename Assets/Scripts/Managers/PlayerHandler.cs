using System.Collections.Generic;
using UnityEngine;

public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private GameObject player_prefab;
    [SerializeField] public List<int> players;
    GameObject spawn_point;
    
    void Awake()
    {
        spawn_point = GameObject.Find("SpawnPoint");

        if (spawn_point == null)
        {
            return;
        }

        int player_id = CreatePlayer(spawn_point);
        if (player_id > 0)
        {
            players.Add(player_id);
        }
    }

    public int CreatePlayer(GameObject spawn_point)
    {
        GameObject player = Instantiate(player_prefab, spawn_point.transform.position, Quaternion.identity);
        int player_id = player.GetInstanceID();
        // Fetch spawn point position
        
        
        return player_id;
    }
    
    public bool RemovePlayer(int player_id)
    {
        return players.Remove(player_id);
    }
}
