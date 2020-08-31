using System;
using System.Net.Http;

namespace Microsoft.BotFramework.Composer.CustomAction.CachedLuis
{
    public class CachedLuisCache
    {
        private string _utterance;

        private HttpResponseMessage _message;

        public CachedLuisCache(string utterance, HttpResponseMessage message)
        {
            _utterance = utterance;
            _message = message;

            Size = GetSize();
        }

        public int Size { get; }

        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is CachedLuisCache))
                return false;
            else
                return _utterance == ((CachedLuisCache)obj)._utterance;
        }

        public override int GetHashCode()
        {
            return _utterance.GetHashCode();
        }

        public HttpResponseMessage GetMessage()
        {
            return _message;
        }

        private int GetSize()
        {
            return GetSize(_utterance) + (_message == null ? 0 : (GetSize(_message.ReasonPhrase) + _message.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Length));
        }

        private int GetSize(string str)
        {
            return str.Length * sizeof(char);
        }
    }
}
