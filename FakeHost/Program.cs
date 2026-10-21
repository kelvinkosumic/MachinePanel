using FakeHost;
using Microsoft.Extensions.Options;
using Secs4Net;

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

Console.WriteLine("Press Enter to send S1F1 (Are You There). Type q to quit.");
while(Console.ReadLine() != "q")
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