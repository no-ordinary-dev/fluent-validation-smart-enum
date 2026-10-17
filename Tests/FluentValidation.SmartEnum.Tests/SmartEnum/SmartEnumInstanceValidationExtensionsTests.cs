namespace FluentValidation.SmartEnum.Tests;

using Ardalis.SmartEnum;
using FluentAssertions;
using FluentValidation;
using Xunit;

public class SmartEnumInstanceValidationExtensionsTests
{
    private sealed class Priority(string name, int value) : SmartEnum<Priority, int>(name, value)
    {
        public static readonly Priority Low = new(nameof(Low), 1);
        public static readonly Priority High = new(nameof(High), 2);
    }

    private sealed class State(string name, string value) : SmartEnum<State, string>(name, value)
    {
        public static readonly State Open = new(nameof(Open), "open");
    }

    private sealed class DefaultPriority : SmartEnum<DefaultPriority>
    {
        public static readonly DefaultPriority Low = new(nameof(Low), 1);

        private DefaultPriority(string name, int value) : base(name, value) { }
    }

    private sealed class Model
    {
        public Priority? PriorityId { get; set; }
        public Priority PriorityIdNonNullable { get; set; } = Priority.Low;
        public State? State { get; set; }
        public State StateNonNullable { get; set; } = State.Open;
        public DefaultPriority DefaultPriority { get; set; } = DefaultPriority.Low;
    }

    private sealed class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator()
        {
            this.RuleFor(x => x.PriorityId).IsInSmartEnum();
            this.RuleFor(x => x.PriorityIdNonNullable).IsInSmartEnum();
            this.RuleFor(x => x.State).IsInSmartEnum();
            this.RuleFor(x => x.StateNonNullable).IsInSmartEnum();
            this.RuleFor(x => x.DefaultPriority).IsInSmartEnum();
        }
    }

    [Fact]
    public void Instance_properties_accept_declared_members()
    {
        var result = new ModelValidator().Validate(new Model
        {
            PriorityId = Priority.Low,
            PriorityIdNonNullable = Priority.High
        });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Instance_properties_accept_known_values_with_different_names()
    {
        var result = new ModelValidator().Validate(new Model
        {
            PriorityId = new Priority("Low alias", Priority.Low.Value),
            PriorityIdNonNullable = new Priority("High alias", Priority.High.Value)
        });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Instance_properties_reject_unknown_values(bool nullableProperty)
    {
        var unknown = new Priority(nameof(Priority.Low), 99);
        var model = new Model();
        if (nullableProperty)
        {
            model.PriorityId = unknown;
        }
        else
        {
            model.PriorityIdNonNullable = unknown;
        }

        var result = new ModelValidator().Validate(model);

        result.IsValid.Should().BeFalse();
        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be(nullableProperty
            ? nameof(Model.PriorityId)
            : nameof(Model.PriorityIdNonNullable));
        failure.ErrorCode.Should().Be("EnumValidator");
        failure.AttemptedValue.Should().BeSameAs(unknown);
    }

    [Fact]
    public void Instance_properties_allow_null()
    {
        var result = new ModelValidator().Validate(new Model
        {
            PriorityId = null,
            PriorityIdNonNullable = null!
        });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Instance_properties_can_require_non_null_values(bool nullableProperty)
    {
        var validator = new InlineValidator<Model>();
        var model = new Model();
        if (nullableProperty)
        {
            validator.RuleFor(x => x.PriorityId).IsInSmartEnum().NotNull();
        }
        else
        {
            validator.RuleFor(x => x.PriorityIdNonNullable).NotNull().IsInSmartEnum();
            model.PriorityIdNonNullable = null!;
        }

        var result = validator.Validate(model);

        result.IsValid.Should().BeFalse();
        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be(nullableProperty
            ? nameof(Model.PriorityId)
            : nameof(Model.PriorityIdNonNullable));
        failure.ErrorCode.Should().Be("NotNullValidator");
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("open", true)]
    [InlineData("unknown", false)]
    public void String_backed_instance_properties_validate_membership(string? value, bool expectedValid)
    {
        var instance = value is null ? null : new State("Alias", value);
        var result = new ModelValidator().Validate(new Model
        {
            State = instance,
            StateNonNullable = instance!
        });

        result.IsValid.Should().Be(expectedValid);
        if (expectedValid)
        {
            result.Errors.Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().HaveCount(2);
            result.Errors.Select(error => error.PropertyName).Should()
                .BeEquivalentTo(nameof(Model.State), nameof(Model.StateNonNullable));
            result.Errors.Should().OnlyContain(error => error.ErrorCode == "EnumValidator");
        }
    }
}
