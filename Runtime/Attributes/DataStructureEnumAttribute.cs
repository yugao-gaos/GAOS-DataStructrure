using System;

namespace GAOS.DataStructure
{
    /// <summary>
    /// Attribute to mark enums that should be available for use in DataStructure.
    /// Any enum marked with this attribute will be automatically registered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class DataStructureEnumAttribute : Attribute
    {
        // No additional properties needed for basic registration
    }
} 