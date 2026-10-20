using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;
using MachinePanel;
using System.Diagnostics;

public partial class MainViewModel : ObservableObject
{
    private readonly Machine _machine = new();
    private readonly DispatcherTimer _timer;
    private readonly EventStore _eventStore = new();
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

    public ObservableCollection<string> Log { get; } = new();
    public ObservableCollection<FaultSummary> FaultSummaries { get; } = new();

    public MainViewModel()
    {
        _machine.StateChanged += OnMachineStateChanged;
        _machine.PartCountChanged += OnMachinePartCountChanged;
        _machine.FaultOccurred += OnMachineFaultOccurred;
        _machine.CycleCompleted += OnMachineCycleCompleted;
        _machine.ElapsedDurationChanged += OnMachineTotalElapsedDurationChanged;

        State = _machine.State;
        PartCount = _machine.PartCount;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        RefreshFaultSummary();
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

    private void OnMachinePartCountChanged() => PartCount = _machine.PartCount;

    private void OnMachineFaultOccurred(string reason)
    {
        AddLog(EventTypes.Fault, reason);
        RefreshFaultSummary();
    }

    private void OnMachineCycleCompleted(TimeSpan cycleTime) => LastCycleSeconds = cycleTime.TotalSeconds;

    private void OnMachineTotalElapsedDurationChanged()
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
}
