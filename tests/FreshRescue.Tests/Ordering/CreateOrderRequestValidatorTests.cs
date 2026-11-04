using FluentAssertions;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Business.Features.Ordering.Validators;

namespace FreshRescue.Tests.Ordering;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    [Theory]
    [InlineData(CreateOrderRequestValidator.MaxItems, true)]
    [InlineData(CreateOrderRequestValidator.MaxItems + 1, false)]
    public void Validate_LimitsItemCount(int itemCount, bool expectedValid)
    {
        var items = Enumerable.Range(0, itemCount)
            .Select(_ => new CreateOrderItemRequest(Guid.NewGuid(), 1))
            .ToList();

        var result = _validator.Validate(new CreateOrderRequest(Guid.NewGuid(), items));

        result.IsValid.Should().Be(expectedValid);
    }
}
