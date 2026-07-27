namespace Home.Api.Models;

public class Widget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = "clock"; // clock | weather
    public string ConfigJson { get; set; } = "{}";
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridW { get; set; } = 2;
    public int GridH { get; set; } = 2;
}
