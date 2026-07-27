namespace Home.Api.Models;

public class Folder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridW { get; set; } = 2;
    public int GridH { get; set; } = 2;
    public int SortOrder { get; set; }

    public ICollection<ServiceLink> Services { get; set; } = new List<ServiceLink>();
}
