/// <summary>
/// Discrete emotional state zones mapped from a 0-1 intensity scalar.
/// Each level covers a 0.25 band of the intensity range.
/// </summary>
public enum IntensityLevel
{
    /// <summary>0.00 - 0.25 (anxiety)</summary>
    Calm = 0,
    /// <summary>0.25 - 0.50 (fear)</summary>
    Elevated = 1,
    /// <summary>0.50 - 0.75 (terror)</summary>
    Intense = 2,
    /// <summary>0.75 - 1.00 (panic)</summary>
    Overload = 3,
}
