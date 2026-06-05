namespace UniversityScheduleOptimization;

public sealed class OptimizationResult
{
    public int ObjectiveValue { get; set; }
    public List<ScheduleSlot> Slots { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
}
