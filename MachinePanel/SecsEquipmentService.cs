using Secs4Net;
using static Secs4Net.Item;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace MachinePanel;

public sealed class SecsEquipmentService : IAsyncDisposable
{
    private const string MODEL_NAME = "MachinePanel";
    private const string LISTEN_IP = "127.0.0.1";
    private const int HSMS_PORT = 5000;

    private static readonly string s_softwareRevision = typeof(SecsEquipmentService).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    private readonly HsmsConnection _connection;
    private readonly SecsGem _secsGem;
    private readonly CancellationTokenSource _cts = new();

    public SecsEquipmentService(ISecsGemLogger logger)
    {
        var options = Options.Create(new SecsGemOptions
        {
            IsActive = false,
            IpAddress = LISTEN_IP,
            Port = HSMS_PORT,
            DeviceId = 0,
        });

        _connection = new HsmsConnection(options, logger);
        _secsGem = new SecsGem(options, _connection, logger);
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
                    using var s1f2 = new SecsMessage(1, 2)
                    {
                        Name = "OnLineData",
                        SecsItem = L(A(MODEL_NAME), A(s_softwareRevision)),
                    };
                    await e.TryReplyAsync(s1f2, token);
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _secsGem.Dispose();
        await _connection.DisposeAsync();
        _cts.Dispose();
    }
}
