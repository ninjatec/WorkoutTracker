using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Options;

namespace WorkoutTrackerWeb.Services.Session
{
    /// <summary>
    /// Options for configuring JSON serialization of session data
    /// </summary>
    public class JsonSessionSerializerOptions
    {
        public JsonNamingPolicy? PropertyNamingPolicy { get; set; }
        public bool WriteIndented { get; set; }
        public System.Text.Json.Serialization.JsonIgnoreCondition DefaultIgnoreCondition { get; set; }
    }

    /// <summary>
    /// Session serializer that uses System.Text.Json instead of BinaryFormatter
    /// for more efficient and secure session serialization
    /// </summary>
    public class JsonSessionSerializer : ISessionSerializer
    {
        private readonly JsonSerializerOptions _options;

        public JsonSessionSerializer(IOptions<JsonSessionSerializerOptions> options)
        {
            var serializerOptions = options.Value;
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = serializerOptions.PropertyNamingPolicy,
                WriteIndented = serializerOptions.WriteIndented,
                DefaultIgnoreCondition = serializerOptions.DefaultIgnoreCondition
            };
        }

        public byte[] Serialize<T>(T value)
        {
            // For null values, return empty byte array
            if (value == null)
            {
                return Array.Empty<byte>();
            }

            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(value, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize session data: {ex.Message}", ex);
            }
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(data, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize session data: {ex.Message}", ex);
            }
        }
    }
}