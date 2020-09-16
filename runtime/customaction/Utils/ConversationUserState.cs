using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.BotFramework.Composer.CustomAction.Utils
{
    public class ConversationUserState : ConversationState
    {
        protected readonly IStorage _storage;

        public ConversationUserState(IStorage storage)
            : base(storage)
        {
            _storage = storage;
        }

        protected override string GetStorageKey(ITurnContext turnContext)
        {
            var channelId = turnContext.Activity.ChannelId ?? throw new InvalidOperationException("invalid activity-missing channelId");
            var conversationId = turnContext.Activity.Conversation?.Id ?? throw new InvalidOperationException("invalid activity-missing Conversation.Id");
            var userId = turnContext.Activity.From?.Id ?? throw new InvalidOperationException("invalid activity-missing From.Id");

            var configKey = $"{channelId}/conversationusers-config/{conversationId}";
            var configRaw = _storage.ReadAsync(new string[] { configKey }).GetAwaiter().GetResult();
            if (configRaw != null && configRaw.TryGetValue(configKey, out object configValue))
            {
                Config config = null;
                if (configValue is JObject configJObject)
                {
                    config = configJObject.ToObject<Config>();
                }

                if (config?.Global == true)
                {
                    return $"{channelId}/conversationusers/{conversationId}";
                }
            }

#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            return $"{channelId}/conversationusers/{conversationId}/{userId}";
        }

        protected class Config
        {
            public bool Global { get; set; }
        }
    }
}
