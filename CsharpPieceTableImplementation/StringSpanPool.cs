namespace CsharpPieceTableImplementation
{
    public sealed class StringSpanPool
    {
        private readonly Dictionary<Span, string> _cache = new();

        public string? GetStringFromCache(Span span)
        {
            if (_cache.TryGetValue(span, out string? result))
            {
                return result;
            }

            return null;
        }

        public void Cache(Span span, string entry)
        {
            _cache.TryAdd(span, entry);
        }

        public void Reset()
        {
            _cache.Clear();
        }
    }
}
