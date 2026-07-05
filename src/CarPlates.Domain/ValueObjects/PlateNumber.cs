using CarPlates.Domain.Enums;

namespace CarPlates.Domain.ValueObjects;

public sealed record PlateNumber
{
    public string Value { get; }
    public PlateType Type { get; }
    public float Confidence { get; }

    private PlateNumber(string value, PlateType type, float confidence)
    {
        Value = value;
        Type = type;
        Confidence = confidence;
    }

    public static PlateNumber Create(string value, PlateType type, float confidence)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Plate number cannot be empty", nameof(value));

        if (confidence < 0 || confidence > 1)
            throw new ArgumentException("Confidence must be between 0 and 1", nameof(confidence));

        return new PlateNumber(value.Trim().ToUpperInvariant(), type, confidence);
    }

    public string ToNormalizedString() => Value;

    public override string ToString() => Value;
}
