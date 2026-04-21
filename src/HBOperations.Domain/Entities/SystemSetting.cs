using HBOperations.Domain.Common;

namespace HBOperations.Domain.Entities;

public class SystemSetting : BaseEntity, IHasTimestamps
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? DescriptionAr { get; set; }
    public string Category { get; set; } = "General";
    public string ValueType { get; set; } = "string"; // string, int, bool, json
    public bool IsEditable { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
