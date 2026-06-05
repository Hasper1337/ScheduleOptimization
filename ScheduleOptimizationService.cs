using System.Text;

namespace UniversityScheduleOptimization;


public static class ScheduleOptimizationService
{
    private const int DayCount = 6;
    private const int PairCount = 4;

    private readonly record struct DayPattern(int Lectures, int Practices, int Labs)
    {
        public int Total => Lectures + Practices + Labs;
        public bool IsUsed => Total > 0;
    }

    public static OptimizationResult Optimize(IReadOnlyList<Lesson> lessons)
    {
        if (lessons.Count == 0)
            throw new InvalidOperationException("Список занятий пуст.");

        int lecturesNeed = lessons.Count(l => l.Type == LessonType.Lecture);
        int practicesNeed = lessons.Count(l => l.Type == LessonType.Practice);
        int labsNeed = lessons.Count(l => l.Type == LessonType.Lab);

        if (lessons.Count > DayCount * PairCount)
            throw new InvalidOperationException("Занятий больше, чем доступных слотов: 6 дней * 4 пары = 24.");

        var allPatterns = BuildDayPatterns();
        List<DayPattern>? best = null;
        int bestUsedDays = int.MaxValue;

        void Search(int day, int lecturesLeft, int practicesLeft, int labsLeft, List<DayPattern> current)
        {
            if (day == DayCount)
            {
                if (lecturesLeft == 0 && practicesLeft == 0 && labsLeft == 0)
                {
                    int usedDays = current.Count(p => p.IsUsed);
                    if (usedDays < bestUsedDays)
                    {
                        bestUsedDays = usedDays;
                        best = current.ToList();
                    }
                }
                return;
            }

            int currentUsed = current.Count(p => p.IsUsed);
            if (currentUsed >= bestUsedDays)
                return;

            int remainingCapacity = (DayCount - day) * PairCount;
            if (lecturesLeft + practicesLeft + labsLeft > remainingCapacity)
                return;

            // Нижняя оценка: меньше ceil(оставшиеся занятия / 4) дней уже не получится.
            int lowerBound = currentUsed + (int)Math.Ceiling((lecturesLeft + practicesLeft + labsLeft) / (double)PairCount);
            if (lowerBound >= bestUsedDays)
                return;

            // Сначала пробуем более заполненные дни, т.к. цель — минимум дней.
            foreach (var pattern in allPatterns)
            {
                if (pattern.Lectures > lecturesLeft || pattern.Practices > practicesLeft || pattern.Labs > labsLeft)
                    continue;

                current.Add(pattern);
                Search(day + 1,
                       lecturesLeft - pattern.Lectures,
                       practicesLeft - pattern.Practices,
                       labsLeft - pattern.Labs,
                       current);
                current.RemoveAt(current.Count - 1);
            }
        }

        Search(0, lecturesNeed, practicesNeed, labsNeed, new List<DayPattern>());

        if (best is null)
            throw new InvalidOperationException("Не найдено допустимое расписание под заданные ограничения.");

        var schedule = BuildSchedule(lessons, best);
        return new OptimizationResult
        {
            ObjectiveValue = bestUsedDays,
            Slots = schedule,
            Explanation = BuildExplanation(lessons, best, bestUsedDays)
        };
    }

    private static List<DayPattern> BuildDayPatterns()
    {
        var result = new List<DayPattern>();

        for (int l = 0; l <= 1; l++)       // не более 1 лекций в день
        for (int p = 0; p <= PairCount; p++)
        for (int lab = 0; lab <= PairCount; lab++)
        {
            int total = l + p + lab;
            if (total <= PairCount)
                result.Add(new DayPattern(l, p, lab));
        }

        return result
            .OrderByDescending(x => x.Total)       // быстрее найти минимум дней
            .ThenByDescending(x => x.Lectures)     // лекции раньше в дне
            .ThenByDescending(x => x.Practices)
            .ThenByDescending(x => x.Labs)
            .ToList();
    }

    private static List<ScheduleSlot> BuildSchedule(IReadOnlyList<Lesson> lessons, List<DayPattern> patterns)
    {
        var lectures = new Queue<Lesson>(lessons.Where(l => l.Type == LessonType.Lecture));
        var practices = new Queue<Lesson>(lessons.Where(l => l.Type == LessonType.Practice));
        var labs = new Queue<Lesson>(lessons.Where(l => l.Type == LessonType.Lab));

        var slots = new List<ScheduleSlot>();

        for (int day = 1; day <= patterns.Count; day++)
        {
            var pattern = patterns[day - 1];
            int pair = 1;

            for (int i = 0; i < pattern.Lectures; i++)
                slots.Add(new ScheduleSlot { Day = day, Pair = pair++, Lesson = lectures.Dequeue() });

            for (int i = 0; i < pattern.Practices; i++)
                slots.Add(new ScheduleSlot { Day = day, Pair = pair++, Lesson = practices.Dequeue() });

            for (int i = 0; i < pattern.Labs; i++)
                slots.Add(new ScheduleSlot { Day = day, Pair = pair++, Lesson = labs.Dequeue() });
        }

        return slots.OrderBy(s => s.Day).ThenBy(s => s.Pair).ToList();
    }

    private static string BuildExplanation(IReadOnlyList<Lesson> lessons, List<DayPattern> patterns, int bestUsedDays)
    {
        int l = lessons.Count(x => x.Type == LessonType.Lecture);
        int p = lessons.Count(x => x.Type == LessonType.Practice);
        int lab = lessons.Count(x => x.Type == LessonType.Lab);

        var sb = new StringBuilder();
        sb.AppendLine("ОПТИМИЗАЦИОННАЯ МОДЕЛЬ");
        sb.AppendLine();
        sb.AppendLine("Множества:");
        sb.AppendLine("Y = {1,...,6} — дни недели; X = {1,...,4} — номера пар; Z — множество занятий.");
        sb.AppendLine("Z = L ∪ Pr ∪ Lab, где |L| = " + l + ", |Pr| = " + p + ", |Lab| = " + lab + ".");
        sb.AppendLine();
        sb.AppendLine("Переменные:");
        sb.AppendLine("ω[z,x,y] ∈ {0,1}: занятие z поставлено в день y на пару x.");
        sb.AppendLine("u[y] ∈ {0,1}: в день y есть хотя бы одна пара.");
        sb.AppendLine();
        sb.AppendLine("Целевая функция:");
        sb.AppendLine("F = Σ u[y] → min, y=1..6.");
        sb.AppendLine();
        sb.AppendLine("Ограничения:");
        sb.AppendLine("1) Каждое занятие ставится ровно один раз: Σ_y Σ_x ω[z,x,y] = 1 для всех z∈Z.");
        sb.AppendLine("2) В одном слоте не более одного занятия: Σ_z ω[z,x,y] ≤ 1 для всех x,y.");
        sb.AppendLine("3) Максимум 4 пары в день задаётся X={1..4}; минимум 0 допускается.");
        sb.AppendLine("4) Без окон: если пара x занята, то все предыдущие пары в этот день заняты.");
        sb.AppendLine("5) Порядок в дне: лекции → практики → лабы; лекций в день не больше 2.");
        sb.AppendLine();
        sb.AppendLine("Найденный минимум: F* = " + bestUsedDays + " учебных дня.");
        sb.AppendLine("Нижняя оценка: ceil(" + lessons.Count + "/4) = " + (int)Math.Ceiling(lessons.Count / 4.0) + ", поэтому меньше получить нельзя.");
        sb.AppendLine();
        sb.AppendLine("Суточные шаблоны (Л, Пр, Лаб):");
        for (int day = 1; day <= patterns.Count; day++)
        {
            var pt = patterns[day - 1];
            sb.AppendLine($"День {day}: ({pt.Lectures}, {pt.Practices}, {pt.Labs}), всего {pt.Total}");
        }

        return sb.ToString();
    }
}
