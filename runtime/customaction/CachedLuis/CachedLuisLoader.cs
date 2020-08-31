using System;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs.Declarative.Loaders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFramework.Composer.CustomAction.CachedLuis
{
    public class CachedLuisLoader : ICustomDeserializer
    {
        private CachedLuisManager _cachedLuisManager;

        public CachedLuisLoader(CachedLuisManager manager)
        {
            _cachedLuisManager = manager;
        }

        /// <inheritdoc/>
        public object Load(JToken obj, JsonSerializer serializer, Type type)
        {
            var recognizer = obj.ToObject<LuisAdaptiveRecognizer>(serializer);
            return new CachedLuisRecognizer(recognizer, _cachedLuisManager);
        }
    }
}
