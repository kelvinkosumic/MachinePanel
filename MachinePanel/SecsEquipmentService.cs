using Secs4Net;
using static Secs4Net.Item;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace MachinePanel;

public enum RemoteCommandResult
{
    Performed,
    UnknownCommand,
    CannotPerformNow,
    AlreadyInState,
}

public sealed class SecsEquipmentService : IAsyncDisposable
{
    private const string MODEL_NAME = "MachinePanel";
    private const string LISTEN_IP = "127.0.0.1";
    private const int HSMS_PORT = 5000;

    private const uint DATA_ID = 0;
    private const uint CEID_MACHINE_STATE_CHANGED = 1000;
    private const uint RPTID_MACHINE_STATUS = 1;

    private const byte ALCD_SET_BIT = 0x80;
    private const byte ALARM_CATEGORY_EQUIPMENT_STATUS = 6;

    private const byte HCACK_PERFORMED = 0;
    private const byte HCACK_UNKNOWN_COMMAND = 1;
    private const byte HCACK_CANNOT_PERFORM_NOW = 2;
    private const byte HCACK_ALREADY_IN_STATE = 5;

    private readonly ISecsGemLogger _logger;

    private static readonly string s_softwareRevision = typeof(SecsEquipmentService).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    private readonly HsmsConnection _connection;
    private readonly SecsGem _secsGem;
    private readonly CancellationTokenSource _cts = new();

    public Func<string, RemoteCommandResult>? RemoteCommandHandler { get; set; }

    public SecsEquipmentService(ISecsGemLogger logger)
    {
        _logger = logger;

        var options = Options.Create(new SecsGemOptions
        {
            IsActive = false,
            IpAddress = LISTEN_IP,
            Port = HSMS_PORT,
            DeviceId = 0,
        });

        _connection = new HsmsConnection(options, _logger);
        _secsGem = new SecsGem(options, _connection, _logger);
    }

    public void Start()
    {
        _connection.Start(_cts.Token);
        _ = HandlePrimaryMessagesAsync(_cts.Token);
    }

    private async Task HandlePrimaryMessagesAsync(CancellationToken token)
    {
        try
        {
            await foreach(var e in _secsGem.GetPrimaryMessageAsync(token))
            {
                using var primary = e.PrimaryMessage;

                if(primary.S == 1 && primary.F == 1)// S1F1 means Are You There.
                {
                    await SendAreYouThereReplyAsync(e, token);
                }
                else if(primary.S == 2 && primary.F == 41) // S2F41 means command (START or STOP)
                {
                    await HandleCommandAsync(e, token);
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private async Task HandleCommandAsync(PrimaryMessageWrapper e, CancellationToken token)
    {
        var primary = e.PrimaryMessage;

        string command = primary.SecsItem?[0].GetString() ?? "";
        RemoteCommandResult result = RemoteCommandHandler?.Invoke(command) ?? RemoteCommandResult.UnknownCommand;

        var hcack = ToHcack(result);

        using var s2f42 = new SecsMessage(2, 42)
        {
            Name = "HostCommandResult",
            SecsItem = L(
                    B(hcack),
                    L()
                )
        };

        await e.TryReplyAsync(s2f42, token);
    }

    public async Task SendStateChangedAsync(MachineState state, int partCount)
    {
        using var s6f11 = new SecsMessage(6, 11)
        {
            Name = "MachineStateChanged",
            SecsItem = L(
                U4(DATA_ID),
                U4(CEID_MACHINE_STATE_CHANGED),
                L(
                    L(
                        U4(RPTID_MACHINE_STATUS),
                        L(
                            A(state.ToString()),
                            U4((uint)partCount)
                        )
                    )
                )
            )
        };

        await SendIfSelectedAsync(s6f11);
    }

    private async Task SendAreYouThereReplyAsync(PrimaryMessageWrapper e, CancellationToken token)
    {
        using var s1f2 = new SecsMessage(1, 2)
        {
            Name = "OnLineData",
            SecsItem = L(A(MODEL_NAME), A(s_softwareRevision)),
        };
        await e.TryReplyAsync(s1f2, token);
    }

    public async Task SendAlarmAsync(MachineAlarm alarm, bool isSet)
    {
        byte alarmCode = isSet ? (byte)(ALCD_SET_BIT | ALARM_CATEGORY_EQUIPMENT_STATUS) : ALARM_CATEGORY_EQUIPMENT_STATUS;

        using var s5f1 = new SecsMessage(5, 1)
        {
            Name = isSet ? "AlarmSet" : "AlarmCleared",
            SecsItem = L(
                B(alarmCode),
                U4(alarm.Id),
                A(alarm.Text)
                )
        };

        await SendIfSelectedAsync(s5f1);
    }

    private async Task SendIfSelectedAsync(SecsMessage message)
    {
        if (_connection.State != ConnectionState.Selected) return;

        try
        {
            using var reply = await _secsGem.SendAsync(message, _cts.Token);
        }
        catch(SecsException ex)
        {
            _logger.Warning($"{message.Name} failed: {ex.Message}");
        }
    }

    private static byte ToHcack(RemoteCommandResult result) => result switch
    {
        RemoteCommandResult.Performed => HCACK_PERFORMED,
        RemoteCommandResult.UnknownCommand => HCACK_UNKNOWN_COMMAND,
        RemoteCommandResult.CannotPerformNow => HCACK_CANNOT_PERFORM_NOW,
        RemoteCommandResult.AlreadyInState => HCACK_ALREADY_IN_STATE,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
    };

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _secsGem.Dispose();
        await _connection.DisposeAsync();
        _cts.Dispose();
    }
}
