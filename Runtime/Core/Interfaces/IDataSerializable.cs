namespace GAOS.DataStructure.Interfaces
{
    /// <summary>
    /// Interface for objects that can be serialized to and deserialized from JSON.
    /// </summary>
    public interface IDataSerializable
    {
        /// <summary>
        /// Converts the object to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the object.</returns>
        string ToJson();

        /// <summary>
        /// Populates the object from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        void FromJson(string json);
    }
} 