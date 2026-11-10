using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField, Min(0.1f)] 
    private float tickIntervalSeconds = 1f;
    private float elapsedSeconds = 0f;

    public int LoopLength { get; private set; } = 0;
    public int CurrentTick { get; private set; } = 0;
    
    public event Action<int> OnTick;
    public bool IsRunning { get; private set; } = false;

    public void SetLoopLength(int loopLength)
    {
        LoopLength = loopLength;
    }

    void Update()
    {
        if (LoopLength == 0) return;

        elapsedSeconds += Time.deltaTime;
        while (elapsedSeconds >= tickIntervalSeconds)
        {
            elapsedSeconds -= tickIntervalSeconds;
            AdvanceTick();
        }
    }

    public void StartTick()
    {
        IsRunning = true;
    }

    public void StopTick()
    {
        IsRunning = false;
    }

    private void AdvanceTick()
    {
        CurrentTick = (CurrentTick + 1) % LoopLength;
        OnTick?.Invoke(CurrentTick);
    }
}
