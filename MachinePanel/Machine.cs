

namespace MachinePanel;

public class Machine
{
    private const double FaultChancePerPart = 0.05;

    private static readonly TimeSpan s_minCycleTime = TimeSpan.FromSeconds(0.8);
    private static readonly TimeSpan s_maxCycleTime = TimeSpan.FromSeconds(1.5);

    private static readonly string[] s_faultReasons =
    {
        "Conveyor jam",
        "Sensor timeout",
        "Vacuum pressure low"
    };

    private TimeSpan _cycleElapsed = TimeSpan.Zero;
    private TimeSpan _cycleTarget = NextCycleTarget();

    public MachineState State { get; private set; } = MachineState.Idle;
    public int PartCount { get; private set; }
    public TimeSpan TotalElapsedDuration { get; private set; }
    

    public event Action? StateChanged;
    public event Action? PartCountChanged;
    public event Action? TickOccurred;
    public event Action<TimeSpan>? CycleCompleted;
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

        _cycleElapsed = TimeSpan.Zero;
        SetState(MachineState.Idle);
    }

    public void TriggerAlarm(string reason)
    {
        if (State != MachineState.Running) return;

        FaultOccurred?.Invoke(reason);
        SetState(MachineState.Alarm);
    }

    public void Tick(TimeSpan elapsed)
    {
        if (State != MachineState.Running) return;

        _cycleElapsed += elapsed;
        TotalElapsedDuration += elapsed;

        TickOccurred?.Invoke();

        if (_cycleElapsed < _cycleTarget) return;

        var cycleTime = _cycleElapsed;
        _cycleElapsed = TimeSpan.Zero;
        _cycleTarget = NextCycleTarget();

        PartCount++;
        PartCountChanged?.Invoke();
        CycleCompleted?.Invoke(cycleTime);

        if(Random.Shared.NextDouble() < FaultChancePerPart)
        {
            var reason = s_faultReasons[Random.Shared.Next(s_faultReasons.Length)];
            TriggerAlarm(reason);
        }
    }

    private static TimeSpan NextCycleTarget()
    {
        var range = s_maxCycleTime - s_minCycleTime;
        var randomTime = s_minCycleTime + (range * Random.Shared.NextDouble());

        return randomTime;
    }

    private void SetState(MachineState newState)
    {
        State = newState;
        StateChanged?.Invoke();
    }
}
