/// <summary>
/// Киностудия (справочная таблица, сторона «один»)
/// </summary>
class Studio
{
    /// <summary>Идентификатор киностудии</summary>
    public int Id { get; set; }

    /// <summary>Название киностудии</summary>
    public string Name { get; set; }

    /// <summary>Конструктор с параметрами</summary>
    public Studio(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Конструктор по умолчанию</summary>
    public Studio() : this(0, "") { }

    public override string ToString() => $"[{Id}] {Name}";
}