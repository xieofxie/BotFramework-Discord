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
using Microsoft.BotFramework.Composer.CustomAction.Utils;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    // Invoke a proactive activity (usually event) to bot which allows you to send, execute logic etc.
    public class InvokeProactiveActivity : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "CustomAction.InvokeProactiveActivity";

        [JsonConstructor]
        public InvokeProactiveActivity([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        // If empty, a random one. If already exists, will cancel the last one
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
            var identifier = this.Identifier?.GetValue(dcState) ?? Guid.NewGuid().ToString();
            var taskManager = dc.Context.TurnState.Get<TaskManager>();
            var result = false;
            if (Activity == null)
            {
                result = taskManager.Stop(dc.Context, identifier);
            }
            else
            {
                var activity = await Activity.BindAsync(dc, dcState).ConfigureAwait(false);
                var delay = this.Delay?.GetValue(dcState) ?? 0;
                var invokeFrom = InvokeFrom.GetValue(dcState);
                var from = dcState.GetValue<JObject>(invokeFrom).ToObject<ChannelAccount>();
                result = taskManager.SendActivity(dc.Context, identifier, activity, delay, from);
            }

            if (this.resultProperty != null)
            {
                dcState.SetValue(resultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
