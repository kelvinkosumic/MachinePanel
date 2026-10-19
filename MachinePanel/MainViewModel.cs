using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;

namespace MachinePanel;

public enum MachineState
{
    Idle,
    Running,
    Stopped,
    Alarm
}


public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(TriggerAlarmCommand), nameof(ResetAlarmCommand))]
    private MachineState _state = MachineState.Idle;

    public ObservableCollection<string> Log { get; } = new();

    private readonly DispatcherTimer _timer;

    private const double FaultChance = 0.05;

    private static readonly string[] s_faultReasons =
    {
        "Conveyer jam",
        "Sensor timeout",
        "Vacuum pressure low"
    };

    [ObservableProperty]
    private int _partCount;

    public MainViewModel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        PartCount++;


        // Randomly trigger an alarm with a 5% chance
        if (new Random().NextDouble() < FaultChance)
        {
            var reason = s_faultReasons[Random.Shared.Next(s_faultReasons.Length)];
            Log.Add($"{DateTime.Now:HH:mm:ss} FAULT: {reason}");
            State = MachineState.Alarm;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start() => State = MachineState.Running;
    private bool CanStart() => State == MachineState.Idle || State == MachineState.Stopped;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => State = MachineState.Stopped;
    private bool CanStop() => State == MachineState.Running;

    [RelayCommand(CanExecute = nameof(CanTriggerAlarm))]
    private void TriggerAlarm() => State = MachineState.Alarm;
    private bool CanTriggerAlarm() => State == MachineState.Running;

    [RelayCommand(CanExecute = nameof(CanResetAlarm))]
    private void ResetAlarm() => State = MachineState.Idle;
    private bool CanResetAlarm() => State == MachineState.Alarm;

    partial void OnStateChanged(MachineState value)
    {
        Log.Add($"{DateTime.Now:HH:mm:ss} - State -> {value}");

        if (value == MachineState.Running)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }
}
