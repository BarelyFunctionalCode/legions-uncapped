using UnityEngine;
using System.Collections.Generic;

public class TeamStructure : MonoBehaviour
{
    // Team names, "Red" vs "Blue" or empty for a free-for-all
    [SerializeField]
    public List<string> teams = new List<string> {};

    // Players are assigned here by PlayerHandler, referenced by their instance ID
    // Format: Dictionary[Player, TeamName]
    public Dictionary<int, string> team_assignment = new Dictionary<int, string>();

    // Check if the acting player is on the enemy team of receiving player.
    // By default, a FFA match has no teams and therefore will always return true.
    public bool IsEnemies (int acting_player_id, int receiving_player_id)
    {
        bool result = true;

        if (teams.Count > 0)
        {
            string ActingTeam = GetTeam(acting_player_id);
            string RcvingTeam = GetTeam(receiving_player_id);

            // Test if they are enemies - Not(Same team)
            result = !(ActingTeam == RcvingTeam);
        }

        return result;

    }

    private string GetTeam (int player_id)
    {
        string result;
        team_assignment.TryGetValue(player_id, out result);

        return result;
    }

}
