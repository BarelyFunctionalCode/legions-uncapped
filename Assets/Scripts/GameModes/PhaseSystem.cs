using UnityEngine;
using System;
using System.Collections.Generic;


public enum Phase
{
    WARMUP,
    ACTIVE,
    ENDGAME
}


public class PhaseSystem : MonoBehaviour
{
    // Delegate
    public event EventHandler<Phase> PhaseChanged;

    // List of phases
    private List<Phase> Phases = new List<Phase> { Phase.WARMUP, Phase.ACTIVE, Phase.ENDGAME };

    // Current phase
    public Phase CurrentPhase;


    // Step phase forward
    public bool Step()
    {
        bool Success = true;
        int phase_count = Enum.GetNames(typeof(Phase)).Length;
        int index = Phases.IndexOf(CurrentPhase);

        Phase next_phase = Phases[phase_count % index];
        CurrentPhase = next_phase;

        OnPhaseChanged(next_phase);

        return Success;
    }

    // Bypass intended stepping and set Phase directly.
    // Used for restarting a match, or early ending a match.
    public bool HardSet()
    {
        bool Success = true;


        return Success;
    }

    public virtual void OnPhaseChanged(Phase phase)
    {
        PhaseChanged?.Invoke(this, phase);
    }
}
