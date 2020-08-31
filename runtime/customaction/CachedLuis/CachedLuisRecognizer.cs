using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using RichardSzalay.MockHttp;

namespace Microsoft.BotFramework.Composer.CustomAction.CachedLuis
{
    public class CachedLuisRecognizer : Recognizer
    {
        private LuisAdaptiveRecognizer _recognizer;

        private CachedLuisManager _cachedLuisManager;

        private CachedLuisData _cachedLuisData;

        public CachedLuisRecognizer(
            LuisAdaptiveRecognizer recognizer,
            CachedLuisManager manager)
        {
            _recognizer = recognizer;
            _cachedLuisManager = manager;
            _cachedLuisData = _cachedLuisManager.Add(_recognizer);
        }

        /// <inheritdoc/>
        public override async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken = default, Dictionary<string, string> telemetryProperties = null, Dictionary<string, double> telemetryMetrics = null)
        {
            var oldHandler = _recognizer.HttpClient;

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.Fallback.Respond(async (request) =>
            {
                var utterance = activity.Text;
                var message = _cachedLuisManager.Get(_cachedLuisData, utterance);
                if (message == null)
                {
                    using (var client = new HttpClient())
                    using (var clonedRequest = await MockedHttpClientHandler.CloneHttpRequestMessageAsync(request).ConfigureAwait(false))
                    {
                        message = await client.SendAsync(clonedRequest, cancellationToken).ConfigureAwait(false);
                    }
                    _cachedLuisManager.Set(_cachedLuisData, utterance, message);
                }
                return message;
            });
            var newHandler = new MockedHttpClientHandler(mockHandler);

            _recognizer.HttpClient = newHandler;
            var result = await _recognizer.RecognizeAsync(dialogContext, activity, cancellationToken, telemetryProperties, telemetryMetrics).ConfigureAwait(false);
            _recognizer.HttpClient = oldHandler;
            return result;
        }
    }
}
