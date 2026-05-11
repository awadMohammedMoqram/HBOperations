using HBMail.Domain.Entities;

namespace HBMail.Application.Common.Interfaces;

public interface ISystemSettingService
{
    Task<string> GetValueAsync(string key, string defaultValue = "");
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task SetValueAsync(string key, string value);
    void InvalidateCache();
}
