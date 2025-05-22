namespace WorkoutTrackerWeb.Services.Session
{
    /// <summary>
    /// Interface for session serialization implementations
    /// </summary>
    public interface ISessionSerializer
    {
        /// <summary>
        /// Serializes a value to a byte array for storage in a session
        /// </summary>
        /// <typeparam name="T">The type of value to serialize</typeparam>
        /// <param name="value">The value to serialize</param>
        /// <returns>A byte array representation of the value</returns>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserializes a byte array back to the original value
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <returns>The deserialized value</returns>
        T Deserialize<T>(byte[] data);
    }
}