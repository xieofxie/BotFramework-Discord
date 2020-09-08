using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.CustomAction.Middlewares;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Utils
{
    public class TaskManager
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenManager> userProactives = new ConcurrentDictionary<string, CancellationTokenManager>();

        public TaskManager()
        {
        }

        // Thread-safe
        public bool Stop(string key, string identifier, bool doCancel = true)
        {
            if (userProactives.TryGetValue(key, out CancellationTokenManager manager))
            {
                return manager.Stop(identifier, doCancel);
            }

            return false;
        }

        // Thread-safe. Will Stop same identifier.
        public bool Start(string key, string identifier, Action<CancellationToken> action)
        {
            var manager = userProactives.GetOrAdd(key, new CancellationTokenManager());
            return manager.Start(identifier, action);
        }

        private class CancellationTokenManager
        {
            private ConcurrentDictionary<string, CancellationTokenSource> CTSs { get; set; } = new ConcurrentDictionary<string, CancellationTokenSource>();

            // Thread-safe
            public bool Stop(string identifier, bool doCancel = true)
            {
                if (CTSs.TryRemove(identifier, out CancellationTokenSource cancellationTokenSource))
                {
                    if (doCancel && !cancellationTokenSource.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        return true;
                    }
                }
                return false;
            }

            // Thread-safe. Will Stop same identifier.
            public bool Start(string identifier, Action<CancellationToken> action)
            {
                Stop(identifier);
                var cts = new CancellationTokenSource();
                var task = Task.Run(() => action(cts.Token));
                return CTSs.TryAdd(identifier, cts);
            }
        }
    }

    public static class TaskManagerExtension
    {
        public static bool Stop(this TaskManager taskManager, ITurnContext context, string identifier)
        {
            var userKey = ReferenceMiddleware.GetUserKey(context.Activity);
            return taskManager.Stop(userKey, identifier);
        }

        public static bool SendActivity(this TaskManager taskManager, ITurnContext context, string identifier, Activity activity, int millisecondsDelay, ChannelAccount from = null)
        {
            var bot = context.TurnState.Get<IBot>();
            var adapter = context.TurnState.Get<BotAdapter>();
            var microsoftAppId = context.TurnState.Get<IConfiguration>().GetValue<string>("MicrosoftAppId");
            var referenceMiddleware = context.TurnState.Get<ReferenceMiddleware>();
            var userKey = ReferenceMiddleware.GetUserKey(context.Activity);
            return taskManager.Start(userKey, identifier, async (CancellationToken cancel) =>
            {
                await Task.Delay(millisecondsDelay, cancel);
                taskManager.Stop(userKey, identifier, false);
                // Get the lastest reference
                var reference = referenceMiddleware.GetConversationReference(context.Activity);
                reference = new ConversationReference
                {
                    ActivityId = reference.ActivityId,
                    User = from ?? reference.User,
                    Bot = reference.Bot,
                    Conversation = reference.Conversation,
                    ChannelId = reference.ChannelId,
                    Locale = string.IsNullOrEmpty(activity.Locale) ? reference.Locale : activity.Locale,
                    ServiceUrl = reference.ServiceUrl
                };
                await ((BotAdapter)adapter).ContinueConversationAsync(microsoftAppId, reference, async (tc, ct) =>
                {
                    // TODO middlewares still use ContinueConversation activity
                    tc.Activity.Type = activity.Type;
                    tc.Activity.Name = activity.Name;
                    tc.Activity.Text = activity.Text;
                    tc.Activity.Speak = activity.Speak;
                    tc.Activity.Value = activity.Value;
                    tc.Activity.Attachments = activity.Attachments;
                    tc.Activity.AttachmentLayout = activity.AttachmentLayout;
                    await bot.OnTurnAsync(tc, ct);
                }, cancel);
            });
        }
    }
}
