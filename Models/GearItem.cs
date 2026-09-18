namespace CampGearApp.Models;

public class GearItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int WeightGram { get; set; }
    public bool IsPacked { get; set; }
    public string Memo { get; set; } = string.Empty;

    public string PhotoDataUrl { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    public string Maker { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public string PurchaseStore { get; set; } = string.Empty;
    public int? PurchasePrice { get; set; }

    public int SortOrder { get; set; }

    // 論理削除フラグ（紛失・廃棄したが記録として残したい場合にtrue）
    public bool IsDeleted { get; set; }
}