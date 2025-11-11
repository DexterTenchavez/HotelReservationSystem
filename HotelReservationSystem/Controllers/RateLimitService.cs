using Microsoft.Extensions.Caching.Memory;

namespace HotelReservationSystem.Services
{
    public interface IRateLimitService
    {
        bool IsRateLimited(string key, int maxAttempts, TimeSpan window);
        void RecordAttempt(string key, TimeSpan window);
        TimeSpan? GetTimeUntilReset(string key, TimeSpan window);
        int GetRemainingAttempts(string key, int maxAttempts, TimeSpan window);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(IMemoryCache cache, ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool IsRateLimited(string key, int maxAttempts, TimeSpan window)
        {
            var cacheKey = $"ratelimit:{key}";
            if (!_cache.TryGetValue(cacheKey, out List<DateTime> attempts))
            {
                return false;
            }

            var windowStart = DateTime.Now.Subtract(window);
            attempts = attempts.Where(a => a > windowStart).ToList();

            if (attempts.Count >= maxAttempts)
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}. Attempts: {Attempts}", key, attempts.Count);
                return true;
            }

            return false;
        }

        public void RecordAttempt(string key, TimeSpan window)
        {
            var cacheKey = $"ratelimit:{key}";
            var now = DateTime.Now;

            if (!_cache.TryGetValue(cacheKey, out List<DateTime> attempts))
            {
                attempts = new List<DateTime>();
            }

            attempts.Add(now);

            // Remove old attempts
            var windowStart = now.Subtract(window);
            attempts = attempts.Where(a => a > windowStart).ToList();

            _cache.Set(cacheKey, attempts, window);
        }

        public TimeSpan? GetTimeUntilReset(string key, TimeSpan window)
        {
            var cacheKey = $"ratelimit:{key}";
            if (!_cache.TryGetValue(cacheKey, out List<DateTime> attempts) || !attempts.Any())
            {
                return null;
            }

            var oldestAttempt = attempts.Min();
            var resetTime = oldestAttempt.Add(window);
            return resetTime - DateTime.Now;
        }

        public int GetRemainingAttempts(string key, int maxAttempts, TimeSpan window)
        {
            var cacheKey = $"ratelimit:{key}";
            if (!_cache.TryGetValue(cacheKey, out List<DateTime> attempts))
            {
                return maxAttempts;
            }

            var windowStart = DateTime.Now.Subtract(window);
            attempts = attempts.Where(a => a > windowStart).ToList();

            return Math.Max(0, maxAttempts - attempts.Count);
        }
    }
}