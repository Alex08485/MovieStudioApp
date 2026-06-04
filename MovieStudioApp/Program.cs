using System.Text;
using Microsoft.Data.Sqlite;

namespace MovieStudioApp;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Пути к файлам
        string dbPath = "movies.db";
        string studiosCsv = Path.Combine(AppContext.BaseDirectory, "studios.csv");
        string moviesCsv = Path.Combine(AppContext.BaseDirectory, "movies.csv");

        // Создаём менеджер БД и инициализируем данные
        var db = new DatabaseManager(dbPath);
        db.InitializeDatabase(studiosCsv, moviesCsv);

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   КИНОСТУДИИ И ФИЛЬМЫ");
        Console.WriteLine("   Бюджеты фильмов в млн $");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Главный цикл меню
        string choice;
        do
        {
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║            ГЛАВНОЕ МЕНЮ                   ║");
            Console.WriteLine("╠══════════════════════════════════════════╣");
            Console.WriteLine("║  1 — Показать все киностудии              ║");
            Console.WriteLine("║  2 — Показать все фильмы                  ║");
            Console.WriteLine("║  3 — Добавить фильм                       ║");
            Console.WriteLine("║  4 — Редактировать фильм                  ║");
            Console.WriteLine("║  5 — Удалить фильм                        ║");
            Console.WriteLine("║  6 — Отчёты                               ║");
            Console.WriteLine("║  7 — Фильтр по киностудии                 ║");
            Console.WriteLine("║  8 — Экспорт в CSV                        ║");
            Console.WriteLine("║  0 — Выход                                ║");
            Console.WriteLine("╚══════════════════════════════════════════╝");
            Console.Write("Ваш выбор: ");

            choice = Console.ReadLine()?.Trim() ?? "";
            Console.WriteLine();

            switch (choice)
            {
                case "1": ShowStudios(db); break;
                case "2": ShowMovies(db); break;
                case "3": AddMovie(db); break;
                case "4": EditMovie(db); break;
                case "5": DeleteMovie(db); break;
                case "6": ReportsMenu(db); break;
                case "7": FilterByStudio(db); break;
                case "8": ExportCsv(db); break;
                case "0": Console.WriteLine("До свидания!"); break;
                default: Console.WriteLine("Неверный пункт меню."); break;
            }
            Console.WriteLine();
        } while (choice != "0");
    }

    // ============== Функции пунктов меню ==============

    static void ShowStudios(DatabaseManager db)
    {
        Console.WriteLine("---- Все киностудии ----");
        var studios = db.GetAllStudios();
        foreach (var studio in studios)
            Console.WriteLine($"  {studio}");
        Console.WriteLine($"Итого: {studios.Count} киностудий");
    }

    static void ShowMovies(DatabaseManager db)
    {
        Console.WriteLine("---- Все фильмы ----");
        var movies = db.GetAllMovies();
        foreach (var movie in movies)
            Console.WriteLine($"  {movie}");
        Console.WriteLine($"Итого: {movies.Count} фильмов");
    }

    static void AddMovie(DatabaseManager db)
    {
        Console.WriteLine("---- Добавление фильма ----");

        // Показываем студии, чтобы пользователь мог выбрать
        Console.WriteLine("Доступные киностудии:");
        var studios = db.GetAllStudios();
        foreach (var studio in studios)
            Console.WriteLine($"  {studio}");

        Console.Write("ID киностудии: ");
        if (!int.TryParse(Console.ReadLine(), out int studioId))
        {
            Console.WriteLine("Ошибка: введите целое число.");
            return;
        }

        Console.Write("Название фильма: ");
        string name = Console.ReadLine()?.Trim() ?? "";
        if (name.Length == 0)
        {
            Console.WriteLine("Ошибка: название не может быть пустым.");
            return;
        }

        Console.Write("Бюджет (млн $): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal budget))
        {
            Console.WriteLine("Ошибка: введите число.");
            return;
        }

        try
        {
            var movie = new Movie(0, studioId, name, budget);
            db.AddMovie(movie);
            Console.WriteLine("Фильм добавлен.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static void EditMovie(DatabaseManager db)
    {
        Console.WriteLine("---- Редактирование фильма ----");
        Console.Write("Введите ID фильма: ");

        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Ошибка: введите целое число.");
            return;
        }

        var movie = db.GetMovieById(id);
        if (movie == null)
        {
            Console.WriteLine($"Фильм с ID={id} не найден.");
            return;
        }

        Console.WriteLine($"Текущие данные: {movie}");
        Console.WriteLine("(Нажмите Enter, чтобы оставить значение без изменений)");

        // Название
        Console.Write($"Название [{movie.Name}]: ");
        string input = Console.ReadLine()?.Trim() ?? "";
        if (input.Length > 0)
            movie.Name = input;

        // Студия
        Console.Write($"ID киностудии [{movie.StudioId}]: ");
        input = Console.ReadLine()?.Trim() ?? "";
        if (input.Length > 0 && int.TryParse(input, out int newStudioId))
            movie.StudioId = newStudioId;

        // Бюджет
        Console.Write($"Бюджет [{movie.BudgetMln}]: ");
        input = Console.ReadLine()?.Trim() ?? "";
        if (input.Length > 0 && decimal.TryParse(input, out decimal newBudget))
        {
            try
            {
                movie.BudgetMln = newBudget;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return;
            }
        }

        db.UpdateMovie(movie);
        Console.WriteLine("Данные обновлены.");
    }

    static void DeleteMovie(DatabaseManager db)
    {
        Console.WriteLine("---- Удаление фильма ----");
        Console.Write("Введите ID фильма: ");

        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Ошибка: введите целое число.");
            return;
        }

        var movie = db.GetMovieById(id);
        if (movie == null)
        {
            Console.WriteLine($"Фильм с ID={id} не найден.");
            return;
        }

        Console.Write($"Удалить «{movie.Name}»? (да/нет): ");
        string confirm = Console.ReadLine()?.Trim().ToLower() ?? "";
        if (confirm == "да")
        {
            db.DeleteMovie(id);
            Console.WriteLine("Фильм удалён.");
        }
        else
        {
            Console.WriteLine("Удаление отменено.");
        }
    }

    // ============== Подменю отчётов ==============

    static void ReportsMenu(DatabaseManager db)
    {
        string choice;
        do
        {
            Console.WriteLine("--- ОТЧЁТЫ ---");
            Console.WriteLine(" 1 - Список фильмов с названиями киностудий");
            Console.WriteLine(" 2 - Количество фильмов по киностудиям");
            Console.WriteLine(" 3 - Средний бюджет фильмов по киностудиям");
            Console.WriteLine(" 0 - Назад");
            Console.Write("Ваш выбор: ");

            choice = Console.ReadLine()?.Trim() ?? "";

            switch (choice)
            {
                case "1": Report1_MoviesWithStudios(db); break;
                case "2": Report2_CountByStudio(db); break;
                case "3": Report3_AvgBudgetByStudio(db); break;
                case "0": break;
                default: Console.WriteLine("Неверный пункт."); break;
            }
            Console.WriteLine();
        } while (choice != "0");
    }

    // Отчёт 1: Фильмы с названиями киностудий (JOIN)
    static void Report1_MoviesWithStudios(DatabaseManager db)
    {
        new ReportBuilder(db)
            .Query(@"SELECT m.movie_name, s.studio_name, m.budget_mln 
                     FROM movie m
                     JOIN studio s ON m.studio_id = s.studio_id 
                     ORDER BY m.movie_name")
            .Title("Фильмы по киностудиям")
            .Header("Название фильма", "Киностудия", "Бюджет (млн $)")
            .ColumnWidths(30, 20, 15)
            .Numbered()
            .Footer("Всего фильмов")
            .Print();
    }

    // Отчёт 2: Количество фильмов по киностудиям (GROUP BY + COUNT)
    static void Report2_CountByStudio(DatabaseManager db)
    {
        new ReportBuilder(db)
            .Query(@"SELECT s.studio_name, COUNT(*) AS cnt 
                     FROM movie m
                     JOIN studio s ON m.studio_id = s.studio_id 
                     GROUP BY s.studio_name 
                     ORDER BY s.studio_name")
            .Title("Количество фильмов по киностудиям")
            .Header("Киностудия", "Кол-во фильмов")
            .ColumnWidths(25, 12)
            .Print();
    }

    // Отчёт 3: Средний бюджет по киностудиям (GROUP BY + AVG)
    static void Report3_AvgBudgetByStudio(DatabaseManager db)
    {
        new ReportBuilder(db)
            .Query(@"SELECT s.studio_name, ROUND(AVG(m.budget_mln), 1) AS avg_budget 
                     FROM movie m
                     JOIN studio s ON m.studio_id = s.studio_id 
                     GROUP BY s.studio_name 
                     ORDER BY avg_budget DESC")
            .Title("Средний бюджет фильмов по киностудиям")
            .Header("Киностудия", "Средний бюджет (млн $)")
            .ColumnWidths(25, 20)
            .Print();
    }

    // Фильтр по киностудии
    static void FilterByStudio(DatabaseManager db)
    {
        Console.WriteLine("---- Фильтр по киностудии ----");
        Console.WriteLine("Доступные киностудии:");
        var studios = db.GetAllStudios();
        foreach (var st in studios)
            Console.WriteLine($"  {st}");

        Console.Write("Введите ID киностудии: ");
        if (!int.TryParse(Console.ReadLine(), out int studioId))
        {
            Console.WriteLine("Ошибка: введите целое число.");
            return;
        }

        var movies = db.GetMoviesByStudio(studioId);
        if (movies.Count == 0)
        {
            Console.WriteLine("На этой киностудии нет фильмов.");
            return;
        }

        var targetStudio = studios.FirstOrDefault(s => s.Id == studioId);
        Console.WriteLine($"\nФильмы киностудии \"{targetStudio?.Name ?? studioId.ToString()}\":");
        foreach (var movie in movies)
            Console.WriteLine($"  {movie}");
        Console.WriteLine($"Итого: {movies.Count} фильмов");
    }

    // Экспорт в CSV
    static void ExportCsv(DatabaseManager db)
    {
        string studiosPath = Path.Combine(AppContext.BaseDirectory, "studios_export.csv");
        string moviesPath = Path.Combine(AppContext.BaseDirectory, "movies_export.csv");
        db.ExportToCsv(studiosPath, moviesPath);
    }
}
