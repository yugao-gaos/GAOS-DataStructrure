namespace GAOS.DataStructure.Interfaces
{
    /// <summary>
    /// Interface for objects that observe changes in a data container.
    /// </summary>
    public interface IDataContainerObserver
    {
        /// <summary>
        /// Called when a value in the data container changes.
        /// </summary>
        /// <param name="key">The key that changed.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        void OnValueChanged(string key, object oldValue, object newValue);
    }
} 