using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using System.Threading;
using System;

namespace Microsoft.BotFramework.Composer.CustomAction.Recognizers
{
    public class RecognizerSetWithPriority : Recognizer
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.RecognizerSetWithPriority";

        [JsonConstructor]
        public RecognizerSetWithPriority([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the input recognizers.
        /// </summary>
        /// <value>
        /// The input recognizers.
        /// </value>
        [JsonProperty("recognizers")]
#pragma warning disable CA2227 // Collection properties should be read only (we can't change this without breaking binary compat)
        public List<Recognizer> Recognizers { get; set; } = new List<Recognizer>();
#pragma warning restore CA2227 // Collection properties should be read only

        public override async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken = default, Dictionary<string, string> telemetryProperties = null, Dictionary<string, double> telemetryMetrics = null)
        {
            if (dialogContext == null)
            {
                throw new ArgumentNullException(nameof(dialogContext));
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            RecognizerResult result = null;
            // Get recognizer result one after another
            foreach (var r in Recognizers)
            {
                result = await r.RecognizeAsync(dialogContext, activity, cancellationToken, telemetryProperties, telemetryMetrics).ConfigureAwait(false);

                if (result != null)
                {
                    var (intent, score) = result.GetTopScoringIntent();
                    if (!intent.Equals("None"))
                    {
                        break;
                    }
                }
            }

            this.TrackRecognizerResult(dialogContext, "RecognizerSetWithPriority", this.FillRecognizerResultTelemetryProperties(result, telemetryProperties), telemetryMetrics);

            return result;
        }
    }
}
