using System;
using System.Collections.Generic;
using System.Text;
using Secs4Net;

namespace FakeHost;

internal sealed class ConsoleSecsLogger : ISecsGemLogger
{
    public void MessageIn(SecsMessage msg, int id) => Console.WriteLine($"<-- [0x{id:X8}] {msg}");
    public void MessageOut(SecsMessage msg, int id) => Console.WriteLine($"--> [0x{id:X8}] {msg}");
    public void Debug(string msg) => Console.WriteLine($"[DEBUG] {msg}");
    public void Info(string msg) => Console.WriteLine($"[INFO] {msg}");
    public void Warning(string msg) => Console.WriteLine($"[WARN] {msg}");
    public void Error(string msg, SecsMessage? message, Exception? ex) => Console.WriteLine($"[ERROR] {msg} {message}\n{ex}");
}

