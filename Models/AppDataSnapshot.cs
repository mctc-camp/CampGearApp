namespace CampGearApp.Models;

public class AppDataSnapshot
{
    public List<CategoryItem> Categories { get; set; } = new();
    public List<GearItem> Gears { get; set; } = new();
    public List<SetItem> Sets { get; set; } = new();
    public List<ChecklistItem> Checklists { get; set; } = new();
}