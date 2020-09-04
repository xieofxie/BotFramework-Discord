using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.CustomAction.Middlewares;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    // Invoke a proactive activity (usually event) to bot which allows you to send, execute logic etc.
    public class InvokeProactiveActivity : Dialog
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenManager> userProactives = new ConcurrentDictionary<string, CancellationTokenManager>();

        [JsonProperty("$kind")]
        public const string Kind = "CustomAction.InvokeProactiveActivity";

        [JsonConstructor]
        public InvokeProactiveActivity([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        // If exists, will cancel the last one
        [JsonProperty("identifier")]
        public StringExpression Identifier { get; set; }

        // If null, stop the Identifier
        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        // Allow invoke on behalf of other one
        [DefaultValue("turn.activity.from")]
        [JsonProperty("invokeFrom")]
        public StringExpression InvokeFrom { get; set; } = "turn.activity.from";

        // Delay in milliseconds
        [JsonProperty("delay")]
        public IntExpression Delay { get; set; }

        // Result of starting
        [JsonProperty("resultProperty")]
        public string resultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var identifier = this.Identifier.GetValue(dcState);
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(identifier);
            }

            var referenceMiddleware = dc.Context.TurnState.Get<ReferenceMiddleware>();
            var userKey = referenceMiddleware.GetUserKey(dc.Context.Activity);
            var result = false;
            if (Activity == null)
            {
                if (userProactives.TryGetValue(userKey, out CancellationTokenManager manager))
                {
                    result = manager.Stop(identifier);
                }
            }
            else
            {
                var bot = dc.Context.TurnState.Get<IBot>();
                var adapter = dc.Context.TurnState.Get<BotAdapter>();
                var microsoftAppId = dc.Context.TurnState.Get<IConfiguration>().GetValue<string>("MicrosoftAppId");
                var activity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);
                var delay = this.Delay?.GetValue(dcState) ?? 0;
                var manager = userProactives.GetOrAdd(userKey, new CancellationTokenManager());
                var invokeFrom = InvokeFrom.GetValue(dc.State);
                var from = dc.State.GetValue<JObject>(invokeFrom).ToObject<ChannelAccount>();

                result = manager.Start(identifier, async (CancellationToken cancel) =>
                {
                    // Get the lastest reference
                    var reference = referenceMiddleware.GetConversationReference(dc.Context.Activity);
                    reference.User = from;
                    await Task.Delay(delay, cancel);
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

            if (this.resultProperty != null)
            {
                dcState.SetValue(resultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        private class CancellationTokenManager
        {
            private ConcurrentDictionary<string, CancellationTokenSource> CTSs { get; set; } = new ConcurrentDictionary<string, CancellationTokenSource>();

            public bool Stop(string identifier)
            {
                if (CTSs.TryRemove(identifier, out CancellationTokenSource cancellationTokenSource))
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        return true;
                    }
                }
                return false;
            }

            public bool Start(string identifier, Action<CancellationToken> action)
            {
                Stop(identifier);
                var cts = new CancellationTokenSource();
                var task = Task.Run(() => action(cts.Token));
                return CTSs.TryAdd(identifier, cts);
            }
        }
    }
}
