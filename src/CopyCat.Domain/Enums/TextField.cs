namespace CopyCat.Domain.Enums;

/// <summary>
/// Selects which text field of a message a condition is evaluated against.
/// </summary>
public enum TextField
{
    /// <summary>The main message text.</summary>
    Text,

    /// <summary>The media caption.</summary>
    Caption,

    /// <summary>The pre-processed normalised text.</summary>
    NormalizedText,

    /// <summary>Text and caption combined.</summary>
    Combined,
}
