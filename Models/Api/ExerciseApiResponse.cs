using System.Text.Json.Serialization;

namespace WorkoutTrackerWeb.Models.Api
{
    /// <summary>
    /// Represents a response from the API Ninjas exercise API
    /// </summary>
    public class ExerciseApiResponse
    {
        /// <summary>
        /// Gets or sets the name of the exercise
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the type of exercise (cardio, strength, etc.)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the primary muscle group targeted
        /// </summary>
        [JsonPropertyName("muscle")]
        public string Muscle { get; set; }
        
        /// <summary>
        /// Gets or sets the equipment needed for the exercise
        /// </summary>
        [JsonPropertyName("equipment")]
        public string Equipment { get; set; }
        
        /// <summary>
        /// Gets or sets the difficulty level of the exercise
        /// </summary>
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }
        
        /// <summary>
        /// Gets or sets the detailed instructions for performing the exercise
        /// </summary>
        [JsonPropertyName("instructions")]
        public string Instructions { get; set; }
        
        /// <summary>
        /// Gets or sets information about how this exercise was found (e.g. word permutation details)
        /// This property is not part of the API response but used internally for tracking search information.
        /// </summary>
        [JsonIgnore]
        public string SearchInfo { get; set; }
    }
}