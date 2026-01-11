using UnityEngine;
using System.Collections.Generic;

public class GameModeBase : MonoBehaviour
{
    private bool isInSession = false;
    private bool isComplete = false;
    
    [SerializeField] protected TeamStructure team_structure;
    [SerializeField] protected PhaseSystem phase_system;
    
    
    public List<string> teams = new List<string> {"Red", "Blue"};
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public virtual void LaunchSession()
    {
    
    }
    public virtual void EndSession()
    {
    
    }

    public bool GetIsInSession() { return isInSession; }
    public bool GetIsComplete() { return isComplete; }
}
