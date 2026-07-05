using FluentValidation;

namespace CarPlates.Application.Scanner.Validation;

public class ScanVehicleCommandValidator : AbstractValidator<Scanner.Commands.ScanVehicleCommand>
{
    public ScanVehicleCommandValidator()
    {
        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("Plate number is required")
            .MinimumLength(3).WithMessage("Plate number must be at least 3 characters");

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0f, 1f).WithMessage("Confidence must be between 0 and 1");
    }
}
