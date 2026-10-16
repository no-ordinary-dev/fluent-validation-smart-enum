namespace FluentValidation.SmartEnum.Tests;

using Ardalis.SmartEnum;
using FluentAssertions;
using FluentValidation;
using Xunit;

public class SmartEnumValidationExtensionsTests
{
    private sealed class Priority : SmartEnum<Priority, int>
    {
        public static readonly Priority Low = new(nameof(Low), 1);
        public static readonly Priority High = new(nameof(High), 2);

        private Priority(string name, int value) : base(name, value) { }
    }

    private sealed class Model
    {
        public int? PriorityId { get; set; }
        public int PriorityIdNonNullable { get; set; }
    }

    private sealed class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator()
        {
            this.RuleFor(x => x.PriorityId)
                .IsInSmartEnum<Model, Priority, int>();

            this.RuleFor(x => x.PriorityIdNonNullable)
                .IsInSmartEnum<Model, Priority, int>();
        }
    }

    [Fact]
    public void Nullable_property_allows_null()
    {
        var validator = new ModelValidator();

        var result = validator.Validate(new Model
        {
            PriorityId = null,
            PriorityIdNonNullable = 1
        });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_value_passes()
    {
        var validator = new ModelValidator();

        var result = validator.Validate(new Model
        {
            PriorityId = 1,
            PriorityIdNonNullable = 2
        });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_nullable_value_fails()
    {
        var validator = new ModelValidator();

        var result = validator.Validate(new Model
        {
            PriorityId = 99,
            PriorityIdNonNullable = 1
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(Model.PriorityId));
    }

    [Fact]
    public void Invalid_non_nullable_value_fails()
    {
        var validator = new ModelValidator();

        var result = validator.Validate(new Model { PriorityIdNonNullable = 99 });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(Model.PriorityIdNonNullable));
    }

    [Fact]
    public void Error_code_matches_enum_validator()
    {
        var validator = new ModelValidator();

        var result = validator.Validate(new Model { PriorityIdNonNullable = 99 });

        result.Errors.Should().ContainSingle()
            .Which.ErrorCode.Should().Be("EnumValidator");
    }
}
