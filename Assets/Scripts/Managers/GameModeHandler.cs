using System.Collections.Generic;
using UnityEngine;

public class GameModeHandler : MonoBehaviour
{	
    public GameModeBase current_game_mode;
    
    public void StartGame()
    {
        current_game_mode.LaunchSession();
    }
    
    public void StopGame()
    {
        current_game_mode.EndSession();
    }
    
    public void OnComplete()
    {
    
    }
    
    public void SelectNewMode(GameModeBase game_mode)
    {
    	current_game_mode = game_mode;
    	StartGame();
    }
}
