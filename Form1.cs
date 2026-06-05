using System.ComponentModel;
using System.Text;

namespace UniversityScheduleOptimization;

public sealed class Form1 : Form
{
    private readonly BindingList<Lesson> _lessons = new();
    private readonly DataGridView _inputGrid = new();
    private readonly DataGridView _resultGrid = new();
    private readonly TextBox _modelBox = new();
    private readonly Label _statusLabel = new();

    public Form1()
    {
        Text = "Оптимизация расписания занятий";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(1000, 650);

        SeedDefaultLessons();
        BuildInterface();
    }

    private void BuildInterface()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10)
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        Controls.Add(main);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var optimizeButton = new Button { Text = "Оптимизировать", Width = 145, Height = 32 };
        var resetButton = new Button { Text = "Вернуть пример", Width = 125, Height = 32 };
        var closeButton = new Button { Text = "Закрыть", Width = 100, Height = 32 };
        optimizeButton.Click += (_, _) => Optimize();
        resetButton.Click += (_, _) => { SeedDefaultLessons(); _inputGrid.Refresh(); _resultGrid.DataSource = null; _modelBox.Clear(); _statusLabel.Text = "Пример восстановлен."; };
        closeButton.Click += (_, _) => Close();
        buttons.Controls.AddRange(new Control[] { optimizeButton, resetButton, closeButton });
        main.Controls.Add(buttons, 0, 0);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "Готово. Можно изменить список занятий и нажать «Оптимизировать».";
        main.Controls.Add(_statusLabel, 1, 0);

        ConfigureInputGrid();
        main.Controls.Add(Wrap("Исходные занятия", _inputGrid), 0, 1);

        ConfigureResultGrid();
        main.Controls.Add(Wrap("Оптимизированное расписание", _resultGrid), 1, 1);

        _modelBox.Dock = DockStyle.Fill;
        _modelBox.Multiline = true;
        _modelBox.ScrollBars = ScrollBars.Both;
        _modelBox.Font = new Font("Consolas", 10);
        _modelBox.ReadOnly = true;
        main.SetColumnSpan(_modelBox, 2);
        main.Controls.Add(Wrap("Математическая постановка и результат", _modelBox), 0, 2);
    }

    private static Control Wrap(string title, Control control)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(8) };
        control.Dock = DockStyle.Fill;
        group.Controls.Add(control);
        return group;
    }

    private void ConfigureInputGrid()
    {
        _inputGrid.Dock = DockStyle.Fill;
        _inputGrid.AutoGenerateColumns = false;
        _inputGrid.DataSource = _lessons;
        _inputGrid.AllowUserToAddRows = true;
        _inputGrid.AllowUserToDeleteRows = true;
        _inputGrid.RowHeadersWidth = 30;
        _inputGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _inputGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Занятие",
            DataPropertyName = nameof(Lesson.Name),
            FillWeight = 72
        });

        var typeColumn = new DataGridViewComboBoxColumn
        {
            HeaderText = "Тип",
            DataPropertyName = nameof(Lesson.Type),
            DataSource = Enum.GetValues(typeof(LessonType)),
            FillWeight = 28
        };
        _inputGrid.Columns.Add(typeColumn);
    }

    private void ConfigureResultGrid()
    {
        _resultGrid.Dock = DockStyle.Fill;
        _resultGrid.AutoGenerateColumns = false;
        _resultGrid.ReadOnly = true;
        _resultGrid.AllowUserToAddRows = false;
        _resultGrid.RowHeadersWidth = 30;
        _resultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "День", DataPropertyName = nameof(ScheduleSlot.DayName), FillWeight = 12 });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Пара", DataPropertyName = nameof(ScheduleSlot.Pair), FillWeight = 10 });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Тип", DataPropertyName = "Lesson.TypeRu", FillWeight = 18 });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Занятие", DataPropertyName = "Lesson.Name", FillWeight = 60 });
    }

    private void Optimize()
    {
        try
        {
            var lessons = _lessons
                .Where(l => !string.IsNullOrWhiteSpace(l.Name))
                .Select(l => new Lesson(l.Name.Trim(), l.Type))
                .ToList();

            var result = ScheduleOptimizationService.Optimize(lessons);

            _resultGrid.DataSource = result.Slots.Select(s => new
            {
                s.DayName,
                s.Pair,
                TypeRu = s.Lesson.TypeRu,
                LessonName = s.Lesson.Name
            }).ToList();

            _resultGrid.Columns.Clear();
            _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "День", DataPropertyName = "DayName", FillWeight = 12 });
            _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Пара", DataPropertyName = "Pair", FillWeight = 10 });
            _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Тип", DataPropertyName = "TypeRu", FillWeight = 18 });
            _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Занятие", DataPropertyName = "LessonName", FillWeight = 60 });

            _modelBox.Text = result.Explanation + Environment.NewLine + BuildScheduleText(result.Slots);
            _statusLabel.Text = $"Оптимум найден: {result.ObjectiveValue} учебных дня.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "Ошибка оптимизации.";
        }
    }

    private static string BuildScheduleText(IEnumerable<ScheduleSlot> slots)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("РАСПИСАНИЕ:");
        foreach (var group in slots.GroupBy(s => s.Day).OrderBy(g => g.Key))
        {
            sb.AppendLine($"День {group.Key}:");
            foreach (var slot in group.OrderBy(s => s.Pair))
                sb.AppendLine($"  {slot.Pair}) {slot.Lesson.TypeRu}: {slot.Lesson.Name}");
        }
        return sb.ToString();
    }

    private void SeedDefaultLessons()
    {
        _lessons.Clear();

        // 4 лекции
        _lessons.Add(new Lesson("Моделирование систем", LessonType.Lecture));
        _lessons.Add(new Lesson("Методы и технологии анализа данных временных рядов", LessonType.Lecture));
        _lessons.Add(new Lesson("Методы оптимизации", LessonType.Lecture));
        _lessons.Add(new Lesson("ООПиП", LessonType.Lecture));

        // 7 практик
        _lessons.Add(new Lesson("Учебная практика / НИР", LessonType.Practice));
        _lessons.Add(new Lesson("Проведение", LessonType.Practice));
        _lessons.Add(new Lesson("Методы оптимизации", LessonType.Practice));
        _lessons.Add(new Lesson("Проведение", LessonType.Practice));
        _lessons.Add(new Lesson("Физическая культура", LessonType.Practice));
        _lessons.Add(new Lesson("Основы экономической культуры", LessonType.Practice));
        _lessons.Add(new Lesson("Иностранный язык в профессиональной сфере", LessonType.Practice));

        // 2 лабы.
        _lessons.Add(new Lesson("Моделирование систем", LessonType.Lab));
        _lessons.Add(new Lesson("Методы и технологии анализа данных временных рядов", LessonType.Lab));
    }
}
