namespace CampGearApp.Models;

public enum ChecklistEntryType
{
    Gear,
    Set
}

// セットの子ギアを、登録時点の内容として保存しておくためのスナップショット
public class ChecklistChildSnapshot
{
    public Guid GearId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhotoDataUrl { get; set; } = string.Empty;
}

public class ChecklistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ChecklistEntryType EntryType { get; set; }

    public Guid? GearId { get; set; }
    public Guid? SetId { get; set; }

    public bool IsChecked { get; set; }
    public bool IsExpanded { get; set; }

    public string SnapshotName { get; set; } = string.Empty;
    public string SnapshotPhoto { get; set; } = string.Empty;

    public List<ChecklistChildSnapshot> ChildrenSnapshot { get; set; } = new();
}

// キャンプの記録に載せる写真（Googleドライブ上の写真のURLを持つ）
public class ChecklistPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
}

public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Today;

    public List<ChecklistEntry> Entries { get; set; } = new();

    // ---- 記録ページ用 ----
    // チェックイン・チェックアウトは、キャンプ場の時刻（日付なし）
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
    public string SiteName { get; set; } = string.Empty;
    // キャンプ場のHPや予約サイトのURL
    public string SiteUrl { get; set; } = string.Empty;
    public int? Fee { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<ChecklistPhoto> Photos { get; set; } = new();
}