namespace CampGearApp.Models;

public class SetItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ギアから親を登録した場合はこちらにギアのIdが入る
    public Guid? LinkedGearId { get; set; }

    // 名前だけで親を登録した場合の名前（LinkedGearIdがある場合は使わない）
    public string ManualName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    // 子は必ずギアのIdの一覧（自由入力なし）
    public List<Guid> ChildGearIds { get; set; } = new();
}