using Microsoft.Bot.Builder;
using System;

namespace Microsoft.BotFramework.Composer.CustomAction.Utils
{
    public class ConversationUserState : ConversationState
    {
        public ConversationUserState(IStorage storage)
            : base(storage)
        {
        }

        protected override string GetStorageKey(ITurnContext turnContext)
        {
            var channelId = turnContext.Activity.ChannelId ?? throw new InvalidOperationException("invalid activity-missing channelId");
            var conversationId = turnContext.Activity.Conversation?.Id ?? throw new InvalidOperationException("invalid activity-missing Conversation.Id");
            var userId = turnContext.Activity.From?.Id ?? throw new InvalidOperationException("invalid activity-missing From.Id");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            return $"{channelId}/conversationusers/{conversationId}/{userId}";
        }
    }
}
