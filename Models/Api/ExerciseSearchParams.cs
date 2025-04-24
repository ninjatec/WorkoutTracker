namespace WorkoutTrackerWeb.Models.Api
{
    /// <summary>
    /// Represents search parameters for the API Ninjas exercise API
    /// </summary>
    public class ExerciseSearchParams
    {
        /// <summary>
        /// Gets or sets the name to search for
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the type of exercise to search for
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the muscle group to search for
        /// </summary>
        public string Muscle { get; set; }
        
        /// <summary>
        /// Gets or sets the difficulty level to search for
        /// </summary>
        public string Difficulty { get; set; }
        
        /// <summary>
        /// Gets or sets the equipment to search for
        /// </summary>
        public string Equipment { get; set; }
    }
}