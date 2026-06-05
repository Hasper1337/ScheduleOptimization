namespace UniversityScheduleOptimization;

public sealed class ScheduleSlot
{
    public int Day { get; set; }
    public int Pair { get; set; }
    public Lesson Lesson { get; set; } = new();

    public string DayName => Day switch
    {
        1 => "ПН",
        2 => "ВТ",
        3 => "СР",
        4 => "ЧТ",
        5 => "ПТ",
        6 => "СБ",
        _ => Day.ToString()
    };
}
