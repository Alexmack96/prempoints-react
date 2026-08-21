using Api.Features.Prices.CreatePrice;
using FluentValidation.TestHelper;

namespace UnitTests.Validators;

public class CreatePriceValidatorTests
{
    private readonly CreatePrice.Validator _validator = new();

    [Fact]
    public void CreateTeam_ShouldHaveNoError_WhenValid()
    {
        var request = new CreatePrice.Request("Arsenal", Bid: 68m, Ask: 70m, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TeamName_ShouldHaveError_WhenEmpty(string? value)
    {
        var request = new CreatePrice.Request(value!, Bid: 68m, Ask: 70m, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TeamName);
    }

    [Fact]
    public void Bid_ShouldHaveError_WhenNegative()
    {
        var request = new CreatePrice.Request("Arsenal", Bid: -1m, Ask: 70m, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Bid);
    }

    [Fact]
    public void Ask_ShouldHaveError_WhenBelowBid()
    {
        // An inverted spread would put the mid somewhere neither side agreed to
        // and let a player buy below the sell price.
        var request = new CreatePrice.Request("Arsenal", Bid: 70m, Ask: 68m, new DateOnly(2025, 08, 15));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Ask);
    }
}
