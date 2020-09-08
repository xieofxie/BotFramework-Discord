using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Middlewares
{
    // save reference for different targets
    public class ReferenceMiddleware : IMiddleware
    {
        private ConcurrentDictionary<string, ConversationReference> byUser = new ConcurrentDictionary<string, ConversationReference>();
        private ConcurrentDictionary<string, ConversationReference> byConversation = new ConcurrentDictionary<string, ConversationReference>();

        public ReferenceMiddleware()
        {
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            byUser[GetUserKey(activity)] = activity.GetConversationReference();
            byConversation[GetConversationKey(activity)] = activity.GetConversationReference();

            turnContext.TurnState.Add(this);
            await next(cancellationToken);
        }

        // thread-safe
        public ConversationReference GetUserReference(Activity activity)
        {
            return byUser[GetUserKey(activity)];
        }

        // thread-safe
        public ConversationReference GetConversationReference(Activity activity)
        {
            return byConversation[GetConversationKey(activity)];
        }

        public static string GetUserKey(Activity activity)
        {
            return $"{activity.ChannelId}/{activity.From.Id}";
        }

        public static string GetConversationKey(Activity activity)
        {
            return $"{activity.ChannelId}/{activity.Conversation.Id}";
        }
    }
}
