using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;
using MachinePanel;

public partial class MainViewModel : ObservableObject
{
    private readonly Machine _machine = new();
    private readonly DispatcherTimer _timer;
    private readonly EventStore _eventStore = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(TriggerAlarmCommand), nameof(ResetAlarmCommand))]
    private MachineState _state;

    [ObservableProperty]
    private int _partCount;

    public ObservableCollection<string> Log { get; } = new();

    public MainViewModel()
    {
        _machine.StateChanged += OnMachineStateChanged;
        _machine.PartCountChanged += OnMachinePartCountChanged;
        _machine.FaultOccurred += OnMachineFaultOccurred;

        State = _machine.State;
        PartCount = _machine.PartCount;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
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
        _machine.Tick();
    }

    private void OnMachineStateChanged()
    {
        State = _machine.State;

        AddLog($"State", State.ToString());
    }

    private void OnMachinePartCountChanged() => PartCount = _machine.PartCount;

    private void OnMachineFaultOccurred(string reason) => AddLog($"Fault", reason);

    private void AddLog(string eventType, string message)
    {
        var currentTime = DateTime.Now;
        _eventStore.Add(currentTime, eventType, message);
        Log.Add($"{currentTime:HH:mm:ss} - {eventType}: {message}");
    }
}
