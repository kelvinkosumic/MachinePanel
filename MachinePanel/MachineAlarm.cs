using System;
using System.Collections.Generic;
using System.Text;

namespace MachinePanel;

public sealed record MachineAlarm(uint Id, string Text);
