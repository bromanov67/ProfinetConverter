public class GsdmlModuleDto
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Info { get; set; }
    public List<int> AllowedInSlots { get; set; } = new();
}