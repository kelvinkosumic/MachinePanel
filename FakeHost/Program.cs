using FakeHost;
using Microsoft.Extensions.Options;
using Secs4Net;
using static Secs4Net.Item;

const string EQUIPMENT_IP = "127.0.0.1";
const int HSMS_PORT = 5000;

var options = Options.Create(new SecsGemOptions
{
    IsActive = true,
    IpAddress = EQUIPMENT_IP,
    Port = HSMS_PORT,
    DeviceId = 0,
});

var logger = new ConsoleSecsLogger();
await using var connection = new HsmsConnection(options, logger);
using var secsGem = new SecsGem(options, connection, logger);
using var cts = new CancellationTokenSource();

connection.ConnectionChanged += delegate
{
    Console.WriteLine($"[Host] Connection: {connection.State}");
};

connection.Start(cts.Token);

_ = HandlePrimaryMessagesAsync(secsGem, cts.Token);

Console.WriteLine("Press Enter to send S1F1 (Are You There). Type q to quit.");
while(await Task.Run(() => Console.ReadLine()) != "q")
{
    try
    {
        using var s1f1 = new SecsMessage(1, 1) { Name = "AreYouThere" };
        using var s1f2 = await secsGem.SendAsync(s1f1);

        var modelName = s1f2.SecsItem?[0].GetString();
        var softwareRevision = s1f2.SecsItem?[1].GetString();
        Console.WriteLine($"[Host] S1F2 reply: {modelName} v{softwareRevision}");
    }
    catch(SecsException ex)
    {
        Console.WriteLine($"[Host] Failed: {ex.Message}");
    }
}

cts.Cancel();

static async Task HandlePrimaryMessagesAsync(SecsGem secsGem, CancellationToken token)
{
    try
    {
        await foreach(var e in secsGem.GetPrimaryMessageAsync(token))
        {
            using var primary = e.PrimaryMessage;

            if(primary.S == 6 && primary.F == 11)// S6F11 means MachineStateChanged.
            {
                PrintEventReport(primary);

                using var s6f12 = new SecsMessage(6, 12)
                {
                    Name = "EventReportAck",
                    SecsItem = B(0),
                };
                await e.TryReplyAsync(s6f12, token);
            }
        }
        
    }
    catch (OperationCanceledException)
    {

    }
}

static void PrintEventReport(SecsMessage s6f11)
{
    var item = s6f11.SecsItem;
    if (item is null) return;

    uint ceid = item[1].FirstValue<uint>();
    var values = item[2][0][1];
    string state = values[0].GetString();
    uint partCount = values[1].FirstValue<uint>();

    Console.WriteLine($"[Host] Event {ceid}: state={state}, parts={partCount}");
}