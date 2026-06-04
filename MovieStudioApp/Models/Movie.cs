/// <summary>
/// Фильм (основная таблица, сторона «много»)
/// </summary>
class Movie
{
    /// <summary>Идентификатор фильма</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор киностудии (внешний ключ)</summary>
    public int StudioId { get; set; }

    /// <summary>Название фильма</summary>
    public string Name { get; set; }

    private decimal _budgetMln;

    /// <summary>
    /// Бюджет фильма в миллионах долларов (не может быть отрицательным)
    /// </summary>
    public decimal BudgetMln
    {
        get => _budgetMln;
        set
        {
            if (value < 0)
                throw new ArgumentException("Бюджет фильма не может быть отрицательным");
            _budgetMln = value;
        }
    }

    /// <summary>Конструктор с параметрами</summary>
    public Movie(int id, int studioId, string name, decimal budgetMln)
    {
        Id = id;
        StudioId = studioId;
        Name = name;
        BudgetMln = budgetMln; // валидация сработает здесь
    }

    /// <summary>Конструктор по умолчанию</summary>
    public Movie() : this(0, 0, "", 0) { }

    public override string ToString() => $"[{Id}] {Name}, студия #{StudioId}, бюджет: {BudgetMln} млн $";
}