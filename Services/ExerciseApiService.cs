using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Api;

namespace WorkoutTrackerWeb.Services
{
    public class ExerciseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExerciseApiService> _logger;
        private readonly string _apiKey;

        public ExerciseApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ExerciseApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["APIKeys:NinjaAPI"];

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        }

        /// <summary>
        /// Searches for exercises in the API Ninjas database
        /// </summary>
        /// <param name="searchParams">Search parameters</param>
        /// <returns>A list of matching exercises</returns>
        public async Task<List<ExerciseApiResponse>> SearchExercisesAsync(ExerciseSearchParams searchParams)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(searchParams.Name))
                    queryParams.Add($"name={Uri.EscapeDataString(searchParams.Name)}");
                
                if (!string.IsNullOrEmpty(searchParams.Type))
                    queryParams.Add($"type={Uri.EscapeDataString(searchParams.Type)}");
                
                if (!string.IsNullOrEmpty(searchParams.Muscle))
                    queryParams.Add($"muscle={Uri.EscapeDataString(searchParams.Muscle)}");
                
                if (!string.IsNullOrEmpty(searchParams.Difficulty))
                    queryParams.Add($"difficulty={Uri.EscapeDataString(searchParams.Difficulty)}");
                
                if (!string.IsNullOrEmpty(searchParams.Equipment))
                    queryParams.Add($"equipment={Uri.EscapeDataString(searchParams.Equipment)}");

                string queryString = string.Join("&", queryParams);
                string requestUri = $"https://api.api-ninjas.com/v1/exercises?{queryString}";

                _logger.LogInformation("Sending request to API Ninjas: {RequestUri}", requestUri);
                
                // Send the request
                var response = await _httpClient.GetAsync(requestUri);
                
                // Check for success
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API Ninjas request failed: {StatusCode} - {Error}", 
                        response.StatusCode, error);
                    throw new Exception($"API request failed: {response.StatusCode} - {error}");
                }
                
                // Parse the response
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var exercises = JsonSerializer.Deserialize<List<ExerciseApiResponse>>(content, options);
                _logger.LogInformation("Received {Count} exercises from API Ninjas", 
                    exercises?.Count ?? 0);
                
                return exercises ?? new List<ExerciseApiResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching exercises from API Ninjas");
                throw;
            }
        }
    }
}