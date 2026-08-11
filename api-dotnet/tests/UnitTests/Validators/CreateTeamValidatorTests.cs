using Api.Features.Teams.CreateTeam;
using FluentValidation.TestHelper;

namespace UnitTests.Validators;

public class CreateTeamValidatorTests
{
    private readonly CreateTeam.Validator _validator = new();

    [Fact]
    public void CreateTeam_ShouldHaveNoError_WhenValid()
    {
        var request = new CreateTeam.Request("Arsenal");

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TeamName_ShouldHaveError_WhenEmpty(string? value)
    {
        var request = new CreateTeam.Request(value!);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TeamName);
    }

    [Fact]
    public void TeamName_Should_Have_Error_When_TooLong()
    {
        var longName = new string('X', 51);
        var request = new CreateTeam.Request(longName);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TeamName);
    }
}
