using System.Text.Json;
using System.Text.RegularExpressions;
using CampGearApp.Models;

namespace CampGearApp.Services;

public class GearDataService
{
    private readonly HttpClient _httpClient;
    private readonly string _appsScriptUrl;

    public List<CategoryItem> Categories { get; private set; } = new();
    public List<GearItem> Gears { get; private set; } = new();
    public List<SetItem> Sets { get; private set; } = new();
    public List<ChecklistItem> Checklists { get; private set; } = new();

    public event Action? OnChange;

    public bool IsLoading { get; private set; } = true;
    public bool IsSaving { get; private set; }
    public bool IsUploadingPhoto { get; private set; }
    public string LastErrorMessage { get; private set; } = string.Empty;

    public GearDataService(HttpClient httpClient, string appsScriptUrl)
    {
        _httpClient = httpClient;
        _appsScriptUrl = appsScriptUrl;
    }

    public void NotifyChange()
    {
        OnChange?.Invoke();
    }

    // ---- スプレッドシートからの読み込み ----
    public async Task LoadFromServerAsync()
    {
        IsLoading = true;
        LastErrorMessage = string.Empty;
        NotifyChange();

        const int maxRetryCount = 3;

        for (int attempt = 1; attempt <= maxRetryCount; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(_appsScriptUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}" || json.Trim() == "[]")
                {
                    Categories = new();
                    Gears = new();
                    Sets = new();
                    Checklists = new();
                }
                else
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshot = JsonSerializer.Deserialize<AppDataSnapshot>(json, options);

                    Categories = snapshot?.Categories ?? new();
                    Gears = snapshot?.Gears ?? new();
                    Sets = snapshot?.Sets ?? new();
                    Checklists = snapshot?.Checklists ?? new();
                }

                // 成功したのでリトライループを抜ける
                LastErrorMessage = string.Empty;
                break;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetryCount)
                {
                    LastErrorMessage = $"データの読み込みに失敗しました（{maxRetryCount}回試行）：{ex.Message}";
                }
                else
                {
                    // 少し待ってから再試行する
                    await Task.Delay(1000 * attempt);
                }
            }
        }

        IsLoading = false;
        NotifyChange();
    }

    // ---- スプレッドシートへの保存 ----
    private async Task SaveToServerAsync()
    {
        IsSaving = true;
        LastErrorMessage = string.Empty;
        NotifyChange();

        try
        {
            var snapshot = new AppDataSnapshot
            {
                Categories = Categories,
                Gears = Gears,
                Sets = Sets,
                Checklists = Checklists
            };

            var json = JsonSerializer.Serialize(snapshot);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");

            var response = await _httpClient.PostAsync(_appsScriptUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"データの保存に失敗しました：{ex.Message}";
        }
        finally
        {
            IsSaving = false;
            NotifyChange();
        }
    }

    private void NotifyChangeAndSave()
    {
        NotifyChange();
        _ = SaveToServerAsync();
    }

    // ---- 写真アップロード（Googleドライブへ保存し、URLを受け取る） ----
    public async Task<string?> UploadPhotoAsync(byte[] fileBytes, string fileName, string mimeType)
    {
        IsUploadingPhoto = true;
        LastErrorMessage = string.Empty;
        NotifyChange();

        try
        {
            var base64 = Convert.ToBase64String(fileBytes);
            var payload = new
            {
                action = "uploadPhoto",
                fileName,
                mimeType,
                base64Data = base64
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");

            var response = await _httpClient.PostAsync(_appsScriptUrl, content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PhotoUploadResult>(resultJson, options);

            if (result?.Result == "success")
            {
                return result.Url;
            }

            LastErrorMessage = $"写真のアップロードに失敗しました：{result?.Message}";
            return null;
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"写真のアップロードに失敗しました：{ex.Message}";
            return null;
        }
        finally
        {
            IsUploadingPhoto = false;
            NotifyChange();
        }
    }

    // ---- 写真削除（不要になった写真をドライブから削除。失敗しても致命的ではないため無視） ----
    public async Task DeletePhotoAsync(string? photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl))
        {
            return;
        }

        var match = Regex.Match(photoUrl, "id=([a-zA-Z0-9_-]+)");
        if (!match.Success)
        {
            return;
        }

        try
        {
            var payload = new
            {
                action = "deletePhoto",
                fileId = match.Groups[1].Value
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");

            await _httpClient.PostAsync(_appsScriptUrl, content);
        }
        catch
        {
            // 写真削除の失敗はアプリの動作に致命的ではないため無視する
        }
    }

    // ---- カテゴリ操作 ----
    public bool AddCategory(string name, out string errorMessage)
    {
        errorMessage = string.Empty;
        name = name.Trim();

        if (string.IsNullOrEmpty(name))
        {
            errorMessage = "カテゴリ名を入力してください。";
            return false;
        }

        if (Categories.Any(c => c.Name == name))
        {
            errorMessage = "同じ名前のカテゴリが既に存在します。";
            return false;
        }

        Categories.Add(new CategoryItem
        {
            Name = name,
            SortOrder = Categories.Count
        });

        NotifyChangeAndSave();
        return true;
    }

    public void RemoveCategory(CategoryItem category)
    {
        Categories.Remove(category);
        NotifyChangeAndSave();
    }

    // カテゴリを、ドラッグ後の並び順(idの一覧)通りに更新する
    public void ReorderCategoriesByIds(List<Guid> orderedIds)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var cat = Categories.FirstOrDefault(c => c.Id == orderedIds[i]);
            if (cat != null)
            {
                cat.SortOrder = i;
            }
        }

        NotifyChangeAndSave();
    }

    // ---- ギア操作 ----
    public bool AddGear(GearItem gear, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(gear.Category))
        {
            errorMessage = "カテゴリを選択してください。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(gear.Name))
        {
            errorMessage = "ギア名を入力してください。";
            return false;
        }

        gear.SortOrder = Gears.Count;
        Gears.Add(gear);

        NotifyChangeAndSave();
        return true;
    }

    public GearItem? FindGearById(Guid id) => Gears.FirstOrDefault(g => g.Id == id);

    public bool UpdateGear(Guid id, GearItem editedGear, out string errorMessage)
    {
        errorMessage = string.Empty;

        var target = FindGearById(id);
        if (target == null)
        {
            errorMessage = "対象のギアが見つかりませんでした。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(editedGear.Category))
        {
            errorMessage = "カテゴリを選択してください。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(editedGear.Name))
        {
            errorMessage = "ギア名を入力してください。";
            return false;
        }

        target.Category = editedGear.Category;
        target.Name = editedGear.Name;
        target.WeightGram = editedGear.WeightGram;
        target.IsPacked = editedGear.IsPacked;
        target.Memo = editedGear.Memo;
        target.PhotoDataUrl = editedGear.PhotoDataUrl;
        target.Url = editedGear.Url;
        target.Maker = editedGear.Maker;
        target.PurchaseDate = editedGear.PurchaseDate;
        target.PurchaseStore = editedGear.PurchaseStore;
        target.PurchasePrice = editedGear.PurchasePrice;
        target.IsDeleted = editedGear.IsDeleted;

        NotifyChangeAndSave();
        return true;
    }

    public void SaveGearQuickChange()
    {
        NotifyChangeAndSave();
    }

    public void RemoveGear(GearItem gear)
    {
        Gears.Remove(gear);

        foreach (var set in Sets)
        {
            if (set.LinkedGearId == gear.Id)
            {
                set.LinkedGearId = null;
                if (string.IsNullOrEmpty(set.ManualName))
                {
                    set.ManualName = "(元ギア削除済み)";
                }
            }
            set.ChildGearIds.Remove(gear.Id);
        }

        NotifyChangeAndSave();
    }

    // 指定カテゴリ内のギアを、ドラッグ後の並び順(idの一覧)通りに更新する
    public void ReorderGearsByIds(string category, List<Guid> orderedIds)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var gear = Gears.FirstOrDefault(g => g.Id == orderedIds[i] && g.Category == category);
            if (gear != null)
            {
                gear.SortOrder = i;
            }
        }

        NotifyChangeAndSave();
    }

    // ---- 合計重量 ----
    public int TotalPackedWeightGram => Gears.Where(g => !g.IsDeleted && g.IsPacked).Sum(g => g.WeightGram);
    public double TotalPackedWeightKg => TotalPackedWeightGram / 1000.0;

    public int TotalAllWeightGram => Gears.Where(g => !g.IsDeleted).Sum(g => g.WeightGram);
    public double TotalAllWeightKg => TotalAllWeightGram / 1000.0;

    // ---- セット操作 ----
    public bool AddSet(Guid? linkedGearId, string manualName, out string errorMessage)
    {
        errorMessage = string.Empty;
        manualName = manualName.Trim();

        if (linkedGearId == null && string.IsNullOrEmpty(manualName))
        {
            errorMessage = "親をギアから選ぶか、セット名を入力してください。";
            return false;
        }

        Sets.Add(new SetItem
        {
            LinkedGearId = linkedGearId,
            ManualName = linkedGearId == null ? manualName : string.Empty,
            SortOrder = Sets.Count
        });

        NotifyChangeAndSave();
        return true;
    }

    public SetItem? FindSetById(Guid id) => Sets.FirstOrDefault(s => s.Id == id);

    public void RemoveSet(SetItem set)
    {
        Sets.Remove(set);
        NotifyChangeAndSave();
    }

    public bool AddChildToSet(SetItem set, Guid gearId, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (set.LinkedGearId == gearId)
        {
            errorMessage = "親と同じギアは子に登録できません。";
            return false;
        }

        if (set.ChildGearIds.Contains(gearId))
        {
            errorMessage = "既に子として登録されています。";
            return false;
        }

        set.ChildGearIds.Add(gearId);
        NotifyChangeAndSave();
        return true;
    }

    public void RemoveChildFromSet(SetItem set, Guid gearId)
    {
        set.ChildGearIds.Remove(gearId);
        NotifyChangeAndSave();
    }

    public string GetSetDisplayName(SetItem set)
    {
        if (set.LinkedGearId.HasValue)
        {
            var gear = FindGearById(set.LinkedGearId.Value);
            if (gear != null)
            {
                return gear.Name;
            }
        }
        return set.ManualName;
    }

    public string GetSetPhoto(SetItem set)
    {
        if (set.LinkedGearId.HasValue)
        {
            var gear = FindGearById(set.LinkedGearId.Value);
            if (gear != null)
            {
                return gear.PhotoDataUrl;
            }
        }
        return string.Empty;
    }

    // ---- チェックリスト操作 ----
    public bool CreateChecklist(string title, DateTime eventDate, IEnumerable<Guid> gearIds, IEnumerable<Guid> setIds, out string errorMessage)
    {
        errorMessage = string.Empty;
        title = title.Trim();

        if (string.IsNullOrEmpty(title))
        {
            errorMessage = "チェックリスト名を入力してください。";
            return false;
        }

        var checklist = new ChecklistItem
        {
            Title = title,
            EventDate = eventDate.Date,
            CreatedDate = DateTime.Today
        };

        foreach (var gearId in gearIds)
        {
            var gear = FindGearById(gearId);
            if (gear == null) continue;

            checklist.Entries.Add(new ChecklistEntry
            {
                EntryType = ChecklistEntryType.Gear,
                GearId = gear.Id,
                SnapshotName = gear.Name,
                SnapshotPhoto = gear.PhotoDataUrl
            });
        }

        foreach (var setId in setIds)
        {
            var set = FindSetById(setId);
            if (set == null) continue;

            var entry = new ChecklistEntry
            {
                EntryType = ChecklistEntryType.Set,
                SetId = set.Id,
                SnapshotName = GetSetDisplayName(set),
                SnapshotPhoto = GetSetPhoto(set)
            };

            foreach (var childId in set.ChildGearIds)
            {
                var childGear = FindGearById(childId);
                if (childGear == null) continue;

                entry.ChildrenSnapshot.Add(new ChecklistChildSnapshot
                {
                    GearId = childGear.Id,
                    Name = childGear.Name,
                    PhotoDataUrl = childGear.PhotoDataUrl
                });
            }

            checklist.Entries.Add(entry);
        }

        Checklists.Add(checklist);
        NotifyChangeAndSave();
        return true;
    }

    public ChecklistItem? FindChecklistById(Guid id) => Checklists.FirstOrDefault(c => c.Id == id);

    public void RemoveChecklist(ChecklistItem checklist)
    {
        Checklists.Remove(checklist);
        NotifyChangeAndSave();
    }

    public void SaveChecklistChange()
    {
        NotifyChangeAndSave();
    }

    public (string Name, string Photo) GetDisplayParentInfo(ChecklistEntry entry)
    {
        if (entry.EntryType == ChecklistEntryType.Gear)
        {
            var gear = FindGearById(entry.GearId ?? Guid.Empty);
            if (gear != null)
            {
                return (gear.Name, gear.PhotoDataUrl);
            }
            return ($"{entry.SnapshotName}（削除済み）", entry.SnapshotPhoto);
        }
        else
        {
            var set = FindSetById(entry.SetId ?? Guid.Empty);
            if (set != null)
            {
                return (GetSetDisplayName(set), GetSetPhoto(set));
            }
            return ($"{entry.SnapshotName}（削除済み）", entry.SnapshotPhoto);
        }
    }

    public List<ChecklistChildSnapshot> GetDisplayChildren(ChecklistItem checklist, ChecklistEntry entry)
    {
        if (entry.EntryType != ChecklistEntryType.Set)
        {
            return new List<ChecklistChildSnapshot>();
        }

        bool useLive = checklist.EventDate >= DateTime.Today;

        if (useLive)
        {
            var set = FindSetById(entry.SetId ?? Guid.Empty);
            if (set != null)
            {
                var liveChildren = new List<ChecklistChildSnapshot>();
                foreach (var childId in set.ChildGearIds)
                {
                    var childGear = FindGearById(childId);
                    if (childGear != null)
                    {
                        liveChildren.Add(new ChecklistChildSnapshot
                        {
                            GearId = childGear.Id,
                            Name = childGear.Name,
                            PhotoDataUrl = childGear.PhotoDataUrl
                        });
                    }
                }
                return liveChildren;
            }
        }

        return entry.ChildrenSnapshot;
    }
}