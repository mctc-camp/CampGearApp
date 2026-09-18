namespace CampGearApp.Models;

public class CategoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    // 将来のドラッグ＆ドロップ並び替え用（ステップ5で使用）
    public int SortOrder { get; set; }
}