using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.CustomAction.Utils;
using Newtonsoft.Json;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    public class TextInputPlus : TextInput
    {
        protected const string TIMEOUT_ID_PROPERTY = "this.timeoutId";

        [JsonProperty("$kind")]
        public new const string Kind = "CustomAction.TextInputPlus";

        // Timeout in milliseconds
        [JsonProperty("timeout")]
        public IntExpression Timeout { get; set; }

        // Will be sent when timeout. Should be a valid message.
        // One could use a special value to know it.
        [JsonProperty("timeoutActivity")]
        public ITemplate<Activity> TimeoutActivity { get; set; }

        public TextInputPlus([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var dcState = dc.State;
            var timeout = this.Timeout?.GetValue(dcState) ?? 0;
            if (timeout != 0)
            {
                // TODO it is not good as luis will take up many time
                var identifier = $"{Kind}/{Guid.NewGuid()}";
                var activity = await TimeoutActivity.BindAsync(dc, dcState).ConfigureAwait(false);
                var taskManager = dc.Context.TurnState.Get<TaskManager>();
                var result = taskManager.SendActivity(dc.Context, identifier, activity, timeout);
                dcState.SetValue(TIMEOUT_ID_PROPERTY, identifier);
            }

            return await base.BeginDialogAsync(dc, options, cancellationToken);
        }

        // Copy with updates
        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            if (activity.Type != ActivityTypes.Message)
            {
                return Dialog.EndOfTurn;
            }

            var interrupted = dc.State.GetValue<bool>(TurnPath.Interrupted, () => false);
            var turnCount = dc.State.GetValue<int>(TURN_COUNT_PROPERTY, () => 0);

            // Perform base recognition
            var state = await this.RecognizeInputAsync(dc, interrupted ? 0 : turnCount, cancellationToken).ConfigureAwait(false);

            if (state == InputState.Valid)
            {
                var input = dc.State.GetValue<object>(VALUE_PROPERTY);

                // set output property
                if (this.Property != null)
                {
                    dc.State.SetValue(this.Property.GetValue(dc.State), input);
                }

                StopTimeout(dc);
                return await dc.EndDialogAsync(input, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else if (this.MaxTurnCount == null || turnCount < this.MaxTurnCount.GetValue(dc.State))
            {
                // increase the turnCount as last step
                dc.State.SetValue(TURN_COUNT_PROPERTY, turnCount + 1);
                return await this.PromptUserAsync(dc, state, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (this.DefaultValue != null)
                {
                    var (value, error) = this.DefaultValue.TryGetValue(dc.State);
                    if (this.DefaultValueResponse != null)
                    {
                        var response = await this.DefaultValueResponse.BindAsync(dc, cancellationToken).ConfigureAwait(false);

                        var properties = new Dictionary<string, string>()
                        {
                            { "template", JsonConvert.SerializeObject(DefaultValueResponse) },
                            { "result", response == null ? string.Empty : JsonConvert.SerializeObject(response, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }) },
                        };
                        TelemetryClient.TrackEvent("GeneratorResult", properties);

                        await dc.Context.SendActivityAsync(response, cancellationToken).ConfigureAwait(false);
                    }

                    // set output property
                    dc.State.SetValue(this.Property.GetValue(dc.State), value);

                    StopTimeout(dc);
                    return await dc.EndDialogAsync(value, cancellationToken).ConfigureAwait(false);
                }
            }

            return await dc.EndDialogAsync().ConfigureAwait(false);
        }

        protected void StopTimeout(DialogContext dc)
        {
            var id = dc.State.GetStringValue(TIMEOUT_ID_PROPERTY);
            if (!string.IsNullOrEmpty(id))
            {
                var taskManager = dc.Context.TurnState.Get<TaskManager>();
                taskManager.Stop(dc.Context, id);
                dc.State.RemoveValue(TIMEOUT_ID_PROPERTY);
            }
        }
    }
}
