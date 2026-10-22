

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

    public static readonly MachineAlarm ManualFaultAlarm = new(99, "Manual fault (test)");

    private static readonly MachineAlarm[] s_randomFaultAlarms =
    {
        new(1, "Conveyer jam"),
        new(2, "Sensor timeout"),
        new(3, "Vacuum pressure"),
    };

    private TimeSpan _cycleElapsed = TimeSpan.Zero;
    private TimeSpan _cycleTarget = NextCycleTarget();

    public MachineState State { get; private set; } = MachineState.Idle;
    public int PartCount { get; private set; }
    public TimeSpan TotalElapsedDuration { get; private set; }
    public MachineAlarm? ActiveAlarm { get; private set; }
    

    public event Action? StateChanged;
    public event Action? PartCountChanged;
    public event Action? TickOccurred;
    public event Action<TimeSpan>? CycleCompleted;
    public event Action<MachineAlarm>? FaultOccurred;
    public event Action<MachineAlarm>? AlarmCleared;

    public void Start()
    {
        if (!CanStartMachine()) return;

        SetState(MachineState.Running);
    }

    public void Stop()
    {
        if (!CanStopMachine()) return;

        SetState(MachineState.Stopped);
    }

    public void ResetAlarm()
    {
        if(State != MachineState.Alarm) return;

        var clearedAlarm = ActiveAlarm;
        ActiveAlarm = null;
        _cycleElapsed = TimeSpan.Zero;

        if (clearedAlarm is not null) AlarmCleared?.Invoke(clearedAlarm);
        SetState(MachineState.Idle);
    }

    public void TriggerAlarm(MachineAlarm alarm)
    {
        if (State != MachineState.Running) return;

        ActiveAlarm = alarm;
        FaultOccurred?.Invoke(alarm);
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
            var reason = s_randomFaultAlarms[Random.Shared.Next(s_randomFaultAlarms.Length)];
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

    public bool CanStartMachine()
    {
        if (State != MachineState.Idle && State != MachineState.Stopped) return false;
        return true;
    }

    public bool CanStopMachine()
    {
        if (State != MachineState.Running) return false;
        return true;
    }
}
