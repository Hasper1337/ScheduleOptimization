namespace UniversityScheduleOptimization;

public enum LessonType
{
    Lecture = 0,
    Practice = 1,
    Lab = 2
}

public sealed class Lesson
{
    public string Name { get; set; } = string.Empty;
    public LessonType Type { get; set; }

    public Lesson() { }

    public Lesson(string name, LessonType type)
    {
        Name = name;
        Type = type;
    }

    public string TypeRu => Type switch
    {
        LessonType.Lecture => "Лекция",
        LessonType.Practice => "Практика",
        LessonType.Lab => "Лаба",
        _ => Type.ToString()
    };
}
