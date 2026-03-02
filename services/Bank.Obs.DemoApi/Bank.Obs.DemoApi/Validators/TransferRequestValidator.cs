using FluentValidation;

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.FromAccount).NotEmpty().Length(8, 64);
        RuleFor(x => x.ToAccount).NotEmpty().Length(8, 64);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(50000);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}