
namespace MachinePanel;

public record CycleSummary(long Count, TimeSpan AverageCycleTime, TimeSpan MinCycleTime, TimeSpan MaxCycleTime);