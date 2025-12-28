using AigoraNet.Common.CQRS;
using AigoraNet.Common.CQRS.Prompts;
using Microsoft.Extensions.Caching.Memory;

namespace AigoraNet.WebApi.Services;

public class InMemoryPromptCache(IMemoryCache cache) : IPromptCache
{
    public bool TryGet(string key, out PromptMatchResult? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void Set(string key, PromptMatchResult value, TimeSpan ttl)
    {
        cache.Set(key, value, new MemoryCacheEntryOptions { SlidingExpiration = ttl });
    }
}
