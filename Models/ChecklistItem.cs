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

    // Gearの場合はGearId、Setの場合はSetIdを使う
    public Guid? GearId { get; set; }
    public Guid? SetId { get; set; }

    public bool IsChecked { get; set; }

    // 画面表示用：セットの内訳を開いているかどうか（表示状態の保持のみ）
    public bool IsExpanded { get; set; }

    // 元のギア・セットが物理削除された場合の表示名・写真のフォールバック
    public string SnapshotName { get; set; } = string.Empty;
    public string SnapshotPhoto { get; set; } = string.Empty;

    // セットの場合のみ使用：登録時点の子ギアの一覧
    public List<ChecklistChildSnapshot> ChildrenSnapshot { get; set; } = new();
}

public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;

    // 使用日（例：キャンプに行く日）
    public DateTime EventDate { get; set; }

    // 作成日（参考情報として保持）
    public DateTime CreatedDate { get; set; } = DateTime.Today;

    public List<ChecklistEntry> Entries { get; set; } = new();
}