using UnityEngine;
using System.Collections.Generic;


/*
 *  - Manages game session -
 *
 *  TeamStructure handles teams of players
 *      - Maintains list of teams and each players' assigned team
 *      - Checks for friendly fire, which can be a toggled option for server hosts
 *      - Free-for-all games (no teams) = Each player is on its own team, where the team name = player name
 *
 *  PhaseSystem handles state of the game mode
 *      - States:
 *        - Warmup pre-game
 *        - Starting a game
 *        - Cleanup post-game
 *      - Receives signal to change state from server host (force game start/game end),
 *        or the states' Step condition
 *        - Pregame warmup has an idle timer of 30 seconds to allow everyone to load into the session,
 *          and select a team, after which it changes state to "In Session"
 *        - In-session phase step condition is # of points
 *        - Cleanup post game phase has a timer of 30 seconds, allows for voting on the next map
 *
 *  GameStats handles all StatEvents
 *      - All events in game will be tracked as a "StatEvent"
 *        - Bullets fired count toward a players' hitrate/missrate
 *        - Flag captures, drops, recoveries
 *        - Kills, deaths, assists
 *      - All game modes are point based, where points are given based on a certain type of StatEvent
 *        - Kills award points for Death match
 *        - Flag captures award points for CTF
 *        - Flag hold duration award points for rabbit
 *      - Points are awarded to players
 *        - Points are also awarded to teams based on which team the player is on
 *        - Player points do nothing but track stats
 *        - Team points trigger state change/win condition
 *          - This is to prevent players switching teams and triggering win condition with player points
 *      - Points accumulate to Team the player is on, if team based
 *      - On point change, check win condition
 *      - GameStat
 *
 */


public class GameModeBase : MonoBehaviour
{
    #region State
    private bool isInSession = false;
    private bool isComplete = false;
    #endregion

    #region Componens
    [SerializeField] public TeamStructure team_structure;
    [SerializeField] public PhaseSystem phase_system;
    #endregion

    private virtual void LaunchSession()
    {
        phase_system.Step();
    }



    public virtual void EndSession()
    {
    
    }

    public bool GetIsInSession() { return isInSession; }
    public bool GetIsComplete() { return isComplete; }
}
