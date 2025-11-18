namespace ConsoleApp.Models;

public class RickAndMortyApiResponse
{
    public Info? Info { get; set; }
    public List<ApiCharacter>? Results { get; set; }
}

public class Info
{
    public int Count { get; set; }
    public int Pages { get; set; }
    public string? Next { get; set; }
    public string? Prev { get; set; }
}

public class ApiCharacter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public LocationInfo? Location { get; set; }
}

public class LocationInfo
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
