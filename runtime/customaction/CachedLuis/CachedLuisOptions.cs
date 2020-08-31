namespace Microsoft.BotFramework.Composer.CustomAction.CachedLuis
{
    public class CachedLuisOptions
    {
        public enum CachePolicyType
        {
            FixForEach,
            BalanceByUsed,
        }

        public CachePolicyType CachePolicy { get; set; } = CachePolicyType.FixForEach;

        // Allows bytes for utterance + content of all luis
        public long MaxBytes { get; set; } = 128 * 1024 * 1024;
    }
}
