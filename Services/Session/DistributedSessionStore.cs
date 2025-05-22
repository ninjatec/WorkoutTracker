using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services.Session
{
    /// <summary>
    /// Custom implementation of the distributed session store that uses
    /// a customizable serializer for better performance and security
    /// </summary>
    public class DistributedSessionStore : ISessionStore
    {
        private readonly IDistributedCache _cache;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISessionSerializer _serializer;

        public DistributedSessionStore(
            IDistributedCache cache,
            ILoggerFactory loggerFactory, 
            ISessionSerializer serializer)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                throw new ArgumentException("Session key cannot be null or empty", nameof(sessionKey));
            }

            return new DistributedSession(
                _cache,
                sessionKey,
                idleTimeout,
                ioTimeout,
                tryEstablishSession,
                _loggerFactory,
                isNewSessionKey,
                _serializer);
        }
    }

    /// <summary>
    /// Custom implementation of ISession that uses Redis as the backend
    /// with custom serialization
    /// </summary>
    internal class DistributedSession : ISession
    {
        private const string KeyPrefix = "Session:";
        private readonly IDistributedCache _cache;
        private readonly string _sessionKey;
        private readonly TimeSpan _idleTimeout;
        private readonly TimeSpan _ioTimeout;
        private readonly Func<bool> _tryEstablishSession;
        private readonly ILogger _logger;
        private readonly bool _isNewSessionKey;
        private readonly ISessionSerializer _serializer;
        private bool _isModified;
        private bool _loaded;
        private SessionData _sessionData;

        internal DistributedSession(
            IDistributedCache cache,
            string sessionKey,
            TimeSpan idleTimeout,
            TimeSpan ioTimeout,
            Func<bool> tryEstablishSession,
            ILoggerFactory loggerFactory,
            bool isNewSessionKey,
            ISessionSerializer serializer)
        {
            _cache = cache;
            _sessionKey = sessionKey;
            _idleTimeout = idleTimeout;
            _ioTimeout = ioTimeout;
            _tryEstablishSession = tryEstablishSession;
            _logger = loggerFactory.CreateLogger<DistributedSession>();
            _isNewSessionKey = isNewSessionKey;
            _serializer = serializer;
        }

        public string Id => _sessionKey;

        public bool IsAvailable => true;

        public IEnumerable<string> Keys
        {
            get
            {
                Load();
                return _sessionData.Data.Keys;
            }
        }

        public void Clear()
        {
            Load();
            _sessionData.Data.Clear();
            _isModified = true;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (!_isModified)
            {
                return Task.CompletedTask;
            }

            if (!_tryEstablishSession())
            {
                throw new InvalidOperationException("Failed to establish session");
            }

            var sessionData = _sessionData;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _idleTimeout
            };

            var key = KeyPrefix + _sessionKey;
            return _cache.SetAsync(key, _serializer.Serialize(sessionData), options, cancellationToken);
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (_loaded)
            {
                return Task.CompletedTask;
            }

            var key = KeyPrefix + _sessionKey;
            return LoadInternalAsync(key, cancellationToken);
        }

        public void Remove(string key)
        {
            Load();
            _isModified |= _sessionData.Data.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Load();
            _sessionData.Data[key] = value;
            _isModified = true;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Load();
            return _sessionData.Data.TryGetValue(key, out value);
        }

        private void Load()
        {
            if (!_loaded)
            {
                var cts = new CancellationTokenSource(_ioTimeout);
                var key = KeyPrefix + _sessionKey;
                LoadInternalAsync(key, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private async Task LoadInternalAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _cache.GetAsync(key, cancellationToken);
                if (data == null || data.Length == 0)
                {
                    _sessionData = new SessionData();
                    _isModified = _isNewSessionKey;
                }
                else
                {
                    _sessionData = _serializer.Deserialize<SessionData>(data) ?? new SessionData();
                }

                _loaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load session data from Redis");
                _sessionData = new SessionData();
                _loaded = true;
                _isModified = true;
            }
        }
    }

    /// <summary>
    /// Container for session data with serialization support
    /// </summary>
    internal class SessionData
    {
        public Dictionary<string, byte[]> Data { get; set; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    }
}