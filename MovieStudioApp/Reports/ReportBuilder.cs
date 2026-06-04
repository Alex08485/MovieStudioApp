using System.Text;

/// <summary>
/// Построитель отчётов с использованием паттерна Fluent Interface.
/// Промежуточные методы возвращают this (цепочка вызовов).
/// Терминальные методы выполняют запрос и формируют результат.
/// </summary>
class ReportBuilder
{
    private DatabaseManager _db;

    // Настройки отчёта
    private string _sql = "";
    private string _title = "";
    private string[] _headers = Array.Empty<string>();
    private int[] _widths = Array.Empty<int>();
    private bool _numbered = false;     // [ГРУППА А]
    private string _footer = "";         // [ГРУППА В]

    /// <summary>Конструктор. Принимает DatabaseManager для выполнения запросов.</summary>
    public ReportBuilder(DatabaseManager db)
    {
        _db = db;
    }

    // ============== Промежуточные методы (возвращают this) ==============

    /// <summary>SQL-запрос для отчёта</summary>
    public ReportBuilder Query(string sql)
    {
        _sql = sql;
        return this;
    }

    /// <summary>Заголовок отчёта</summary>
    public ReportBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Названия колонок для отображения</summary>
    public ReportBuilder Header(params string[] columns)
    {
        _headers = columns;
        return this;
    }

    /// <summary>Ширина каждой колонки в символах</summary>
    public ReportBuilder ColumnWidths(params int[] widths)
    {
        _widths = widths;
        return this;
    }

    /// <summary>[ГРУППА А] Включить нумерацию строк</summary>
    public ReportBuilder Numbered()
    {
        _numbered = true;
        return this;
    }

    /// <summary>[ГРУППА В] Добавить итоговую строку</summary>
    public ReportBuilder Footer(string label)
    {
        _footer = label;
        return this;
    }

    // ============== Терминальные методы ==============

    /// <summary>
    /// Выполняет запрос, форматирует результат через StringBuilder,
    /// возвращает готовую строку отчёта
    /// </summary>
    public string Build()
    {
        var (columns, rows) = _db.ExecuteQuery(_sql);
        var sb = new StringBuilder();

        // Заголовок отчёта
        if (_title.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"=== {_title} ===");
        }

        // Определяем названия колонок
        string[] displayHeaders = _headers.Length > 0 ? _headers : columns;

        // Определяем ширину колонок
        int colCount = displayHeaders.Length;
        int[] widths;
        if (_widths.Length >= colCount)
        {
            widths = _widths;
        }
        else
        {
            widths = new int[colCount];
            for (int i = 0; i < colCount; i++)
                widths[i] = 20;
        }

        // Ширина столбца нумерации
        int numWidth = _numbered ? 6 : 0;

        // Шапка таблицы
        if (_numbered)
            sb.Append("№".PadRight(numWidth));

        for (int i = 0; i < colCount; i++)
            sb.Append(displayHeaders[i].PadRight(widths[i]));
        sb.AppendLine();

        // Разделитель
        int totalWidth = numWidth;
        for (int i = 0; i < colCount; i++)
            totalWidth += widths[i];
        sb.AppendLine(new string('-', totalWidth));

        // Строки данных
        for (int r = 0; r < rows.Count; r++)
        {
            if (_numbered)
                sb.Append((r + 1).ToString().PadRight(numWidth));

            for (int c = 0; c < rows[r].Length && c < colCount; c++)
                sb.Append(rows[r][c].PadRight(widths[c]));
            sb.AppendLine();
        }

        // Итоговая строка [ГРУППА В]
        if (_footer.Length > 0)
        {
            sb.AppendLine(new string('-', totalWidth));
            sb.AppendLine($"{_footer}: {rows.Count}");
        }

        return sb.ToString();
    }

    /// <summary>Выполняет Build() и выводит результат в консоль</summary>
    public void Print()
    {
        Console.WriteLine(Build());
    }

    /// <summary>[ГРУППА Б] Сохраняет отчёт в текстовый файл</summary>
    public void SaveToFile(string path)
    {
        File.WriteAllText(path, Build());
        Console.WriteLine($"Отчёт сохранён в файл: {path}");
    }
}