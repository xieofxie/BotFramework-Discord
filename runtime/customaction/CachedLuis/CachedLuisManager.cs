using Microsoft.Bot.Builder.AI.Luis;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;

namespace Microsoft.BotFramework.Composer.CustomAction.CachedLuis
{
    public class CachedLuisManager
    {
        private CachedLuisOptions _options;
        private Dictionary<string, CachedLuisData> _datas = new Dictionary<string, CachedLuisData>();

        public CachedLuisManager(CachedLuisOptions options)
        {
            _options = options;

            if (_options.CachePolicy == CachedLuisOptions.CachePolicyType.FixForEach)
            {
            }
            else
            {
                throw new InvalidEnumArgumentException($"{_options.CachePolicy} is not supported!");
            }
        }

        public CachedLuisData Add(LuisAdaptiveRecognizer recognizer)
        {
            var key = ComputeDataKey(recognizer);
            if (_datas.TryGetValue(key, out CachedLuisData data))
            {
                return data;
            }
            data = new CachedLuisData(recognizer);
            _datas.Add(key, data);
            return data;
        }

        // Thread safe method
        public HttpResponseMessage Get(CachedLuisData data, string utterance)
        {
            lock (data)
            {
                return data.Get(utterance);
            }
        }

        // Thread safe method
        public void Set(CachedLuisData data, string utterance, HttpResponseMessage message)
        {
            lock(data)
            {
                var bytes = data.Set(utterance, message);
                if (_options.CachePolicy == CachedLuisOptions.CachePolicyType.FixForEach)
                {
                    long target = _options.MaxBytes / _datas.Count;
                    while (bytes > target)
                    {
                        bytes = data.Remove();
                    }
                }
            }
        }

        private string ComputeDataKey(LuisAdaptiveRecognizer recognizer)
        {
            return recognizer.Endpoint + ":" + recognizer.ApplicationId;
        }
    }
}
