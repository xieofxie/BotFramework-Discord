using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    // Write to storage directly
    public class WriteStorage : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "CustomAction.WriteStorage";

        [JsonProperty("property")]
        public StringExpression Property { get; set; }

        [JsonProperty("value")]
        public ValueExpression Value { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            JToken value = null;
            if (this.Value != null)
            {
                var (val, valueError) = this.Value.TryGetValue(dc.State);
                if (valueError != null)
                {
                    throw new Exception($"Expression evaluation resulted in an error. Expression: {this.Value.ToString()}. Error: {valueError}");
                }

                if (val != null)
                {
                    value = JToken.FromObject(val).DeepClone();
                }
            }

            value = value?.ReplaceJTokenRecursively(dc.State);

            var changes = new Dictionary<string, object>();
            changes.Add(this.Property.GetValue(dc.State), value);

            var storage = dc.Context.TurnState.Get<IStorage>();
            await storage.WriteAsync(changes, cancellationToken).ConfigureAwait(false);

            return await dc.EndDialogAsync(null, cancellationToken).ConfigureAwait(false);
        }
    }
}
