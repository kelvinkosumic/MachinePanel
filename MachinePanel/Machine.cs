using System;
using System.Collections.Generic;
using System.Text;

namespace MachinePanel;

public class Machine
{
    private const double FaultChance = 0.05;

    private static readonly string[] s_faultReasons =
    {
        "Conveyor jam",
        "Sensor timeout",
        "Vacuum pressure low"
    };

    public MachineState State { get; private set; } = MachineState.Idle;
    public int PartCount { get; private set; }

    public event Action? StateChanged;
    public event Action? PartCountChanged;
    public event Action<string>? FaultOccurred;

    public void Start()
    {
        if (State != MachineState.Idle && State != MachineState.Stopped) return;

        SetState(MachineState.Running);
    }

    public void Stop()
    {
        if (State != MachineState.Running) return;

        SetState(MachineState.Stopped);
    }

    public void ResetAlarm()
    {
        if(State != MachineState.Alarm) return;

        SetState(MachineState.Idle);
    }

    public void TriggerAlarm(string reason)
    {
        if (State != MachineState.Running) return;

        FaultOccurred?.Invoke(reason);
        SetState(MachineState.Alarm);
    }

    public void Tick()
    {
        if (State != MachineState.Running) return;

        PartCount++;
        PartCountChanged?.Invoke();

        if(Random.Shared.NextDouble() < FaultChance)
        {
            var reason = s_faultReasons[Random.Shared.Next(s_faultReasons.Length)];
            TriggerAlarm(reason);
        }
    }

    private void SetState(MachineState newState)
    {
        State = newState;
        StateChanged?.Invoke();
    }
}
