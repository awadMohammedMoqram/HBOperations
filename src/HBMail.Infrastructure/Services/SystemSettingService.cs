using HBMail.Application.Common.Interfaces;
using HBMail.Domain.Entities;
using HBMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HBMail.Infrastructure.Services;

public class SystemSettingService(AppDbContext context, IMemoryCache cache) : ISystemSettingService
{
    private const string CacheKey = "SystemSettings_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<string> GetValueAsync(string key, string defaultValue = "")
    {
        var settings = await GetAllCachedAsync();
        return settings.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var val = await GetValueAsync(key);
        return int.TryParse(val, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var val = await GetValueAsync(key);
        return string.IsNullOrEmpty(val) ? defaultValue : val.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetValueAsync(string key, string value)
    {
        var setting = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is not null)
        {
            setting.Value = value;
            await context.SaveChangesAsync();
            InvalidateCache();
        }
    }

    public void InvalidateCache()
    {
        cache.Remove(CacheKey);
    }

    private async Task<Dictionary<string, string>> GetAllCachedAsync()
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, string>? cached) && cached is not null)
            return cached;

        var settings = await context.SystemSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        cache.Set(CacheKey, settings, CacheDuration);
        return settings;
    }
}
