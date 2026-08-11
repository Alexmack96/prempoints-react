using Api.Features.Prices.CreatePrice;
using FluentValidation.TestHelper;

namespace UnitTests.Validators;

public class CreatePriceValidatorTests
{
    private readonly CreatePrice.Validator _validator = new();

    [Fact]
    public void CreateTeam_ShouldHaveNoError_WhenValid()
    {
        var request = new CreatePrice.Request("Arsenal", 69, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TeamName_ShouldHaveError_WhenEmpty(string? value)
    {
        var request = new CreatePrice.Request(value!, 69, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TeamName);
    }

    [Fact]
    public void Price_ShouldHaveError_WhenNegative()
    {
        var request = new CreatePrice.Request("Arsenal", -1, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Price);
    }
}
