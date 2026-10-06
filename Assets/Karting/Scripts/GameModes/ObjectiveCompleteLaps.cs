﻿using System.Collections;
using KartGame.Track;
using UnityEngine;

public class ObjectiveCompleteLaps : Objective
{
    
    [Tooltip("How many laps should the player complete before the game is over?")]
    public int lapsToComplete;

    [Header("Notification")]
    [Tooltip("Start sending notification about remaining laps when this amount of laps is left")]
    public int notificationLapsRemainingThreshold = 1;


    
    // currentLap is the number of COMPLETED laps, as used by the existing HUD.
    public int currentLap { get; private set; }
    public int CurrentRaceLap => Mathf.Clamp(currentLap + 1, 1, Mathf.Max(1, lapsToComplete));
    public event System.Action<int> LapStarted;

    void Awake()
    {
        currentLap = 0;
        
        // set a title and description specific for this type of objective, if it hasn't one
        if (string.IsNullOrEmpty(title))
            title = $"Complete {lapsToComplete} {targetName}s";
        
    }

    IEnumerator Start()
    {
        TimeManager.OnSetTime(totalTimeInSecs, isTimed, gameMode);
        TimeDisplay.OnSetLaps(lapsToComplete);
        yield return new WaitForEndOfFrame();
        Register();
    }

    protected override void ReachCheckpoint(int remaining)
    {

        if (isCompleted)
            return;

        currentLap++;

        // The first crossing starts the existing lap timer without reaching this method.
        // Only a completed lap starts a new trap configuration; finishing starts no lap 4.
        if (currentLap < lapsToComplete)
            LapStarted?.Invoke(CurrentRaceLap);

        int targetRemaining = lapsToComplete - currentLap;

        // update the objective text according to how many enemies remain to kill
        if (targetRemaining == 0)
        {
            CompleteObjective(string.Empty, GetUpdatedCounterAmount(),
                "Objective complete: " + title);
        }
        else if (targetRemaining == 1)
        {
            string notificationText = notificationLapsRemainingThreshold >= targetRemaining
                ? "One " + targetName + " left"
                : string.Empty;
            UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
        }
        else if (targetRemaining > 1)
        {
            // create a notification text if needed, if it stays empty, the notification will not be created
            string notificationText = notificationLapsRemainingThreshold >= targetRemaining
                ? targetRemaining + " " + targetName + "s to collect left"
                : string.Empty;

            UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
        }

    }
    
    public override string GetUpdatedCounterAmount()
    {
        return currentLap + " / " + lapsToComplete;
    }
  
   
  
  

}
