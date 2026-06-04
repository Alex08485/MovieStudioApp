using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Управление базой данных SQLite.
/// Инкапсулирует все операции с БД: создание таблиц,
/// импорт CSV, CRUD-операции, выполнение запросов для отчётов.
/// </summary>
class DatabaseManager
{
    private string _connectionString;

    /// <summary>
    /// Конструктор. Принимает путь к файлу БД.
    /// </summary>
    public DatabaseManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    // ============== Инициализация ==============

    /// <summary>
    /// Создаёт таблицы (если не существуют) и загружает CSV при первом запуске
    /// </summary>
    public void InitializeDatabase(string studiosCsvPath, string moviesCsvPath)
    {
        CreateTables();

        // СНАЧАЛА загружаем студии
        if (GetAllStudios().Count == 0 && File.Exists(studiosCsvPath))
        {
            ImportStudiosFromCsv(studiosCsvPath);
            Console.WriteLine($"[OK] Загружены киностудии из {studiosCsvPath}");
        }

        // ПОТОМ загружаем фильмы
        if (GetAllMovies().Count == 0 && File.Exists(moviesCsvPath))
        {
            ImportMoviesFromCsv(moviesCsvPath);
            Console.WriteLine($"[OK] Загружены фильмы из {moviesCsvPath}");
        }
    }

    /// <summary>Создание таблиц</summary>
    private void CreateTables()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Временно отключаем проверку внешних ключей
        var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = OFF;";
        pragmaCmd.ExecuteNonQuery();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS studio (
                studio_id INTEGER PRIMARY KEY AUTOINCREMENT,
                studio_name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS movie (
                movie_id INTEGER PRIMARY KEY AUTOINCREMENT,
                studio_id INTEGER NOT NULL,
                movie_name TEXT NOT NULL,
                budget_mln REAL NOT NULL,
                FOREIGN KEY (studio_id) REFERENCES studio(studio_id)
            );";
        cmd.ExecuteNonQuery();

        // Включаем обратно проверку внешних ключей
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCmd.ExecuteNonQuery();
    }

    /// <summary>Импорт киностудий из CSV</summary>
    private void ImportStudiosFromCsv(string path)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string[] lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(';');
            if (parts.Length < 2) continue;

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO studio (studio_id, studio_name) VALUES (@id, @name)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@name", parts[1]);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Импорт фильмов из CSV (с проверкой существования студии)</summary>
    private void ImportMoviesFromCsv(string path)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string[] lines = File.ReadAllLines(path);

        // Получаем список существующих студий
        var existingStudios = new HashSet<int>();
        var studioCmd = conn.CreateCommand();
        studioCmd.CommandText = "SELECT studio_id FROM studio";
        using var reader = studioCmd.ExecuteReader();
        while (reader.Read())
        {
            existingStudios.Add(reader.GetInt32(0));
        }

        Console.WriteLine($"[INFO] Найдено студий: {existingStudios.Count}");

        int importedCount = 0;
        int skippedCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(';');
            if (parts.Length < 4) continue;

            int movieId = int.Parse(parts[0]);
            int studioId = int.Parse(parts[1]);
            string movieName = parts[2];
            decimal budget = decimal.Parse(parts[3]);

            // Проверяем, существует ли студия
            if (!existingStudios.Contains(studioId))
            {
                Console.WriteLine($"[WARN] Студия с ID={studioId} не найдена, фильм '{movieName}' пропущен");
                skippedCount++;
                continue;
            }

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO movie (movie_id, studio_id, movie_name, budget_mln)
                VALUES (@id, @studioId, @name, @budget)";
            cmd.Parameters.AddWithValue("@id", movieId);
            cmd.Parameters.AddWithValue("@studioId", studioId);
            cmd.Parameters.AddWithValue("@name", movieName);
            cmd.Parameters.AddWithValue("@budget", budget);
            cmd.ExecuteNonQuery();
            importedCount++;
        }

        Console.WriteLine($"[INFO] Загружено фильмов: {importedCount}, пропущено: {skippedCount}");
    }

    // ============== Чтение данных ==============

    /// <summary>Получить все киностудии</summary>
    public List<Studio> GetAllStudios()
    {
        var result = new List<Studio>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT studio_id, studio_name FROM studio ORDER BY studio_id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Studio(reader.GetInt32(0), reader.GetString(1)));
        }
        return result;
    }

    /// <summary>Получить все фильмы</summary>
    public List<Movie> GetAllMovies()
    {
        var result = new List<Movie>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT movie_id, studio_id, movie_name, budget_mln FROM movie ORDER BY movie_id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Movie(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDecimal(3)));
        }
        return result;
    }

    /// <summary>Получить фильм по Id</summary>
    public Movie? GetMovieById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT movie_id, studio_id, movie_name, budget_mln FROM movie WHERE movie_id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Movie(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDecimal(3));
        }
        return null;
    }

    // ============== Изменение данных ==============

    /// <summary>Добавить фильм</summary>
    public void AddMovie(Movie movie)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO movie (studio_id, movie_name, budget_mln)
            VALUES (@studioId, @name, @budget)";
        cmd.Parameters.AddWithValue("@studioId", movie.StudioId);
        cmd.Parameters.AddWithValue("@name", movie.Name);
        cmd.Parameters.AddWithValue("@budget", movie.BudgetMln);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Обновить данные фильма</summary>
    public void UpdateMovie(Movie movie)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE movie 
            SET studio_id = @studioId, movie_name = @name, budget_mln = @budget
            WHERE movie_id = @id";
        cmd.Parameters.AddWithValue("@id", movie.Id);
        cmd.Parameters.AddWithValue("@studioId", movie.StudioId);
        cmd.Parameters.AddWithValue("@name", movie.Name);
        cmd.Parameters.AddWithValue("@budget", movie.BudgetMln);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Удалить фильм по Id</summary>
    public void DeleteMovie(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM movie WHERE movie_id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ============== Выполнение произвольного запроса (для отчётов) ==============

    /// <summary>
    /// Выполняет SQL-запрос и возвращает имена столбцов и строки результата.
    /// Используется классом ReportBuilder.
    /// </summary>
    public (string[] columns, List<string[]> rows) ExecuteQuery(string sql)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();

        string[] columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            columns[i] = reader.GetName(i);

        var rows = new List<string[]>();
        while (reader.Read())
        {
            string[] row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i)?.ToString() ?? "";
            rows.Add(row);
        }

        return (columns, rows);
    }

    // ============== Дополнительные методы ==============

    /// <summary>Экспорт обеих таблиц в CSV-файлы</summary>
    public void ExportToCsv(string studiosPath, string moviesPath)
    {
        var studioLines = new List<string>();
        studioLines.Add("studio_id;studio_name");
        foreach (var studio in GetAllStudios())
            studioLines.Add($"{studio.Id};{studio.Name}");
        File.WriteAllLines(studiosPath, studioLines.ToArray());

        var movieLines = new List<string>();
        movieLines.Add("movie_id;studio_id;movie_name;budget_mln");
        foreach (var movie in GetAllMovies())
            movieLines.Add($"{movie.Id};{movie.StudioId};{movie.Name};{movie.BudgetMln}");
        File.WriteAllLines(moviesPath, movieLines.ToArray());

        Console.WriteLine($"Киностудии экспортированы в: {studiosPath}");
        Console.WriteLine($"Фильмы экспортированы в: {moviesPath}");
    }

    /// <summary>Получить фильмы конкретной киностудии</summary>
    public List<Movie> GetMoviesByStudio(int studioId)
    {
        var result = new List<Movie>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT movie_id, studio_id, movie_name, budget_mln
            FROM movie WHERE studio_id = @studioId ORDER BY movie_name";
        cmd.Parameters.AddWithValue("@studioId", studioId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Movie(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDecimal(3)));
        }
        return result;
    }
}