using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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
    }

}
