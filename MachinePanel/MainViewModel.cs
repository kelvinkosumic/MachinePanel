using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;

namespace MachinePanel;

public partial class MainViewModel : ObservableObject
{
    private readonly Machine _machine = new();
    private readonly DispatcherTimer _timer;
    private readonly EventStore _eventStore;
    private readonly CycleStore _cycleStore;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(TriggerAlarmCommand), nameof(ResetAlarmCommand))]
    private MachineState _state;

    [ObservableProperty]
    private int _partCount;

    [ObservableProperty]
    private double _lastCycleSeconds;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunTimeText))]
    private TimeSpan _totalElapsedDuration = TimeSpan.Zero;
    public string RunTimeText => TotalElapsedDuration.ToString(@"hh\:mm\:ss");

    [ObservableProperty]
    private CycleSummary _cycleSummaries = new CycleSummary(0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);

    public ObservableCollection<string> Log { get; } = new();
    public ObservableCollection<FaultSummary> FaultSummaries { get; } = new();

    public MainViewModel()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "machine.db");
        var connectionString = $"Data Source={dbPath}";
        _eventStore = new(connectionString);
        _cycleStore = new(connectionString);

        _machine.StateChanged += OnMachineStateChanged;
        _machine.PartCountChanged += OnMachinePartCountChanged;
        _machine.FaultOccurred += OnMachineFaultOccurred;
        _machine.CycleCompleted += OnMachineCycleCompleted;
        _machine.TickOccurred += OnMachineTickOccurred;

        State = _machine.State;
        PartCount = _machine.PartCount;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        RefreshFaultSummary();
        RefreshCycleSummary();
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start() => _machine.Start();
    private bool CanStart() => State == MachineState.Idle || State == MachineState.Stopped;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _machine.Stop();
    private bool CanStop() => State == MachineState.Running;

    [RelayCommand(CanExecute = nameof(CanTriggerAlarm))]
    private void TriggerAlarm() => _machine.TriggerAlarm("Manual Fault (Test)");
    private bool CanTriggerAlarm() => State == MachineState.Running;

    [RelayCommand(CanExecute = nameof(CanResetAlarm))]
    private void ResetAlarm() => _machine.ResetAlarm();
    private bool CanResetAlarm() => State == MachineState.Alarm;

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;
        _stopwatch.Restart();
        _machine.Tick(elapsed);
    }

    private void OnMachineStateChanged()
    {
        State = _machine.State;
        AddLog(EventTypes.State, State.ToString());
    }

    private void OnMachinePartCountChanged()
    {
        PartCount = _machine.PartCount;
    }

    private void OnMachineFaultOccurred(string reason)
    {
        AddLog(EventTypes.Fault, reason);
        RefreshFaultSummary();
    }

    private void OnMachineCycleCompleted(TimeSpan cycleTime)
    {
        LastCycleSeconds = cycleTime.TotalSeconds;

        _cycleStore.Add(DateTime.Now, _machine.PartCount, (int)cycleTime.TotalMilliseconds);
        RefreshCycleSummary();
    }

    private void OnMachineTickOccurred()
    {
        TotalElapsedDuration = _machine.TotalElapsedDuration;
    }

    private void AddLog(string eventType, string message)
    {
        var currentTime = DateTime.Now;
        _eventStore.Add(currentTime, eventType, message);
        Log.Add($"{currentTime:HH:mm:ss} - {eventType}: {message}");
    }

    private void RefreshFaultSummary()
    {
        FaultSummaries.Clear();
        foreach(FaultSummary summary in _eventStore.GetFaultSummary())
        {
            FaultSummaries.Add(summary);
        }
    }

    private void RefreshCycleSummary()
    {
        CycleSummary summary = _cycleStore.GetCycleSummary();
        CycleSummaries = summary;
    }
}
