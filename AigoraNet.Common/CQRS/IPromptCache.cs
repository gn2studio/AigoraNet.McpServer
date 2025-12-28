using AigoraNet.Common.CQRS.Prompts;

namespace AigoraNet.Common.CQRS;

public interface IPromptCache
{
    bool TryGet(string key, out PromptMatchResult? value);
    void Set(string key, PromptMatchResult value, TimeSpan ttl);
}
