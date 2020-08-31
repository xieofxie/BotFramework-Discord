using C5;
using Microsoft.Bot.Builder.AI.Luis;
using System.Net.Http;

namespace Microsoft.BotFramework.Composer.CustomAction.CachedLuis
{
    public class CachedLuisData
    {
        public CachedLuisData(LuisAdaptiveRecognizer recognizer)
        {
            Recognizer = recognizer;
        }

        public long TotalUsed { get; private set; }

        public long Used { get; private set; }

        public long TotalBytes { get; private set; }

        public long Bytes { get; private set; }

        public long TotalCaches { get; private set; }

        public HashedLinkedList<CachedLuisCache> Caches { get; } = new HashedLinkedList<CachedLuisCache>();

        public LuisAdaptiveRecognizer Recognizer { get; }

        public HttpResponseMessage Get(string utterance)
        {
            // We always add used
            TotalUsed++;
            Used++;

            var input = new CachedLuisCache(utterance, null);
            // TODO: make it first
            if (Caches.Remove(input, out CachedLuisCache output))
            {
                Caches.Add(output);
                return output.GetMessage();
            }
            else
            {
                return null;
            }
        }

        public long Set(string utterance, HttpResponseMessage message)
        {
            var cache = new CachedLuisCache(utterance, message);
            Caches.Add(cache);
            var size = cache.Size;
            TotalBytes += size;
            Bytes += size;
            TotalCaches += 1;
            return Bytes;
        }

        public long Remove()
        {
            var output = Caches.Remove();
            Bytes -= output.Size;
            return Bytes;
        }

        public void Clear()
        {
            Used = 0;
            Bytes = 0;
            Caches.Clear();
        }
    }
}
