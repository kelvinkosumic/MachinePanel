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

var selected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

connection.ConnectionChanged += delegate
{
    Console.WriteLine($"[Host] Connection: {connection.State}");
    if (connection.State == ConnectionState.Selected) selected.TrySetResult();
};

connection.Start(cts.Token);

_ = HandlePrimaryMessagesAsync(secsGem, cts.Token);


Console.WriteLine("[Host] Waiting for equipment...");
await selected.Task;

Console.WriteLine("\nWelcome to a simulated host, written by Kelvin Goh Chor Cuang!");
Console.WriteLine("\ncheck - send S1F1 (AreYouThere) \nstart - to send START command. \nstop - to send STOP command. \nq | quit - quit. \n");

while (true)
{
    Console.Write("\nInput: ");

    string? input = await Task.Run(() => Console.ReadLine());

    if (input is null) break;

    string cleanedInput = input.Trim().ToUpperInvariant();

    if (cleanedInput == "Q" || cleanedInput == "QUIT") break;

    switch (cleanedInput)
    {
        case "": case "CHECK": await SendAreYouOnlineCommand(secsGem); break;
        case "START": await SendStartCommand(secsGem); break;
        case "STOP": await SendStopCommand(secsGem); break;
        default: Console.WriteLine("[Host] Unknown input"); break;
    }


    
}

cts.Cancel();

static async Task SendAreYouOnlineCommand(SecsGem secsGem)
{
    try
    {
        using var s1f1 = new SecsMessage(1, 1) { Name = "AreYouThere" };
        using var s1f2 = await secsGem.SendAsync(s1f1);

        var modelName = s1f2.SecsItem?[0].GetString();
        var softwareRevision = s1f2.SecsItem?[1].GetString();
        Console.WriteLine($"[Host] S1F2 reply: {modelName} v{softwareRevision}");
    }
    catch (SecsException ex)
    {
        Console.WriteLine($"[Host] Failed: {ex.Message}");
    }
}
static async Task SendStartCommand(SecsGem secsGem)
{
    try
    {
        using var s2f41 = new SecsMessage(2, 41) 
        { 
            Name = "HostCommand", 
            SecsItem = L(
                A("START"),
                L()
                )
        };
        using var s2f42 = await secsGem.SendAsync(s2f41);

        var item = s2f42.SecsItem;
        if (item is null)
        {
            Console.WriteLine("[Host] S2F42 reply had no data");
            return;
        }

        byte hcack = item[0].FirstValue<byte>();

        Console.WriteLine($"[Host] S2F42 reply: HCACK Code {hcack} || Description {DescribeHcack(hcack)}");
    }
    catch (SecsException ex)
    {
        Console.WriteLine($"[Host] Failed: {ex.Message}");
    }
}

static async Task SendStopCommand(SecsGem secsGem)
{
    try
    {
        using var s2f41 = new SecsMessage(2, 41)
        {
            Name = "HostCommand",
            SecsItem = L(
                A("STOP"),
                L()
                )
        };

        using var s2f42 = await secsGem.SendAsync(s2f41);

        var item = s2f42.SecsItem;
        if (item is null)
        {
            Console.WriteLine("[Host] S2F42 reply had no data");
            return;
        }

        byte hcack = item[0].FirstValue<byte>();

        Console.WriteLine($"[Host] S2F42 reply: HCACK Code {hcack} || Description {DescribeHcack(hcack)}");
    }
    catch (SecsException ex)
    {
        Console.WriteLine($"[Host] Failed: {ex.Message}");
    }
}

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
            else if(primary.S == 5 && primary.F == 1)
            {
                PrintAlarmReport(primary);

                using var s5f2 = new SecsMessage(5, 2)
                {
                    Name = "AlarmAck",
                    SecsItem = B(0),
                };

                await e.TryReplyAsync(s5f2, token);
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

static void PrintAlarmReport(SecsMessage s5f1)
{
    const byte ALCD_SET_BIT = 0x80;
    const byte ALCD_CATEGORY_MASK = 0x7F;

    var item = s5f1.SecsItem;
    if (item is null) return;

    byte alarmCode = item[0].FirstValue<byte>();
    uint alarmId = item[1].FirstValue<uint>();
    string alarmText = item[2].GetString();

    bool isSet = (alarmCode & ALCD_SET_BIT) != 0;
    int category = alarmCode & ALCD_CATEGORY_MASK;

    Console.WriteLine($"[Host] Alarm {(isSet ? "SET" : "CLEARED")}: ALID={alarmId} \"{alarmText}\" (category {category})");
}
static string DescribeHcack(byte hcack) => hcack switch
{
    0 => "Performed",
    1 => "Command does not exist",
    2 => "Cannot perform now",
    3 => "Parameter invalid",
    4 => "Accepted, will finish later",
    5 => "Already in desired condition",
    6 => "No such object",
    _ => $"Unknown HCACK {hcack}",
};