namespace FluentValidation.SmartEnum.Tests;

using System.Globalization;
using Ardalis.SmartEnum;
using FluentAssertions;
using FluentValidation;
using Xunit;

public class SmartEnumExclusionValidationExtensionsTests
{
    private sealed class Status(string name, int value) : SmartEnum<Status, int>(name, value)
    {
        public static readonly Status A = new(nameof(A), 1);
        public static readonly Status B = new(nameof(B), 2);
        public static readonly Status C = new(nameof(C), 3);
        public static readonly Status D = new(nameof(D), 4);
        public static readonly Status E = new(nameof(E), 5);
    }

    private sealed class State(string name, string value) : SmartEnum<State, string>(name, value)
    {
        public static readonly State Open = new(nameof(Open), "open");
        public static readonly State Closed = new(nameof(Closed), "closed");
    }

    [SmartEnumStringComparer(StringComparison.OrdinalIgnoreCase)]
    private sealed class CaseInsensitiveState(string name, string value)
        : SmartEnum<CaseInsensitiveState, string>(name, value)
    {
        public static readonly CaseInsensitiveState Open = new(nameof(Open), "open");
        public static readonly CaseInsensitiveState Closed = new(nameof(Closed), "closed");
    }

    private sealed class DefaultStatus : SmartEnum<DefaultStatus>
    {
        public static readonly DefaultStatus A = new(nameof(A), 1);
        public static readonly DefaultStatus B = new(nameof(B), 2);

        private DefaultStatus(string name, int value) : base(name, value) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class ReferenceValueComparerAttribute()
        : SmartEnumComparerAttribute<string>(ReferenceEqualityComparer.Instance);

    [ReferenceValueComparer]
    private sealed class ReferenceState(string name, string value) : SmartEnum<ReferenceState, string>(name, value)
    {
        public static readonly ReferenceState First = new(nameof(First), new string('x', 1));
        public static readonly ReferenceState Second = new(nameof(Second), new string('x', 1));
    }

    private sealed class Model
    {
        public Status? Instance { get; set; }
        public Status NonNullableInstance { get; set; } = Status.A;
        public int Value { get; set; }
        public int? NullableValue { get; set; }
        public string Text { get; set; } = State.Open.Value;
        public State? StringInstance { get; set; }
        public CaseInsensitiveState? CaseInsensitiveInstance { get; set; }
        public DefaultStatus DefaultInstance { get; set; } = DefaultStatus.A;
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(99, false)]
    public void Instance_exclusions_narrow_membership_by_underlying_value(int value, bool expectedValid)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(
            new Status("B alias", 2), Status.D);
        var instance = new Status("Property alias", value);

        var result = validator.Validate(new Model { Instance = instance });

        result.IsValid.Should().Be(expectedValid);
        if (expectedValid)
        {
            result.Errors.Should().BeEmpty();
        }
        else
        {
            var failure = result.Errors.Should().ContainSingle().Which;
            failure.PropertyName.Should().Be(nameof(Model.Instance));
            failure.ErrorCode.Should().Be("EnumValidator");
            failure.AttemptedValue.Should().BeSameAs(instance);
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(99, false)]
    public void Value_exclusions_narrow_membership_for_both_value_overloads(int value, bool expectedValid)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(2, 4);
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(2, 4);

        var result = validator.Validate(new Model { Value = value, NullableValue = value });

        result.IsValid.Should().Be(expectedValid);
        if (expectedValid)
        {
            result.Errors.Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().HaveCount(2);
            result.Errors.Select(error => error.PropertyName).Should()
                .BeEquivalentTo(nameof(Model.Value), nameof(Model.NullableValue));
            result.Errors.Should().OnlyContain(error => error.ErrorCode == "EnumValidator");
            result.Errors.Should().OnlyContain(error => Equals(error.AttemptedValue, value));
        }
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("value")]
    [InlineData("nullable value")]
    [InlineData("text")]
    public void Null_exclusion_arrays_are_rejected_when_constructing_rules(string property)
    {
        var validator = new InlineValidator<Model>();
        Action configure = property switch
        {
            "instance" => () => validator.RuleFor(x => x.Instance).IsInSmartEnum(excludedValues: null!),
            "value" => () => validator.RuleFor(x => x.Value)
                .IsInSmartEnum<Model, Status, int>(excludedValues: null!),
            "nullable value" => () => validator.RuleFor(x => x.NullableValue)
                .IsInSmartEnum<Model, Status, int>(excludedValues: null!),
            "text" => () => validator.RuleFor(x => x.Text)
                .IsInSmartEnum<Model, State, string>(excludedValues: null!),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };

        configure.Should().Throw<ArgumentNullException>().WithParameterName("excludedValues");
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("value")]
    [InlineData("nullable value")]
    [InlineData("text")]
    public void Undeclared_exclusions_are_rejected_when_constructing_rules(string property)
    {
        var validator = new InlineValidator<Model>();
        Action configure = property switch
        {
            "instance" => () => validator.RuleFor(x => x.Instance)
                .IsInSmartEnum(new Status("Unknown", 99)),
            "value" => () => validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(99),
            "nullable value" => () => validator.RuleFor(x => x.NullableValue)
                .IsInSmartEnum<Model, Status, int>(99),
            "text" => () => validator.RuleFor(x => x.Text)
                .IsInSmartEnum<Model, State, string>("unknown"),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };

        configure.Should().Throw<ArgumentException>().WithParameterName("excludedValues");
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("text")]
    public void Null_exclusion_elements_are_rejected_when_constructing_rules(string property)
    {
        var validator = new InlineValidator<Model>();
        Action configure = property switch
        {
            "instance" => () => validator.RuleFor(x => x.Instance).IsInSmartEnum(Status.A, null!),
            "text" => () => validator.RuleFor(x => x.Text)
                .IsInSmartEnum<Model, State, string>("open", null!),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };

        configure.Should().Throw<ArgumentException>().WithParameterName("excludedValues");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Null_properties_pass_even_when_all_members_are_excluded(bool excludeAll)
    {
        Status[] exclusions = excludeAll
            ? [Status.A, Status.B, Status.C, Status.D, Status.E]
            : [Status.B];
        var values = exclusions.Select(member => member.Value).ToArray();
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(exclusions);
        validator.RuleFor(x => x.NonNullableInstance).IsInSmartEnum(exclusions);
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(values);
        validator.RuleFor(x => x.Text).IsInSmartEnum<Model, State, string>("open", "closed");
        validator.RuleFor(x => x.StringInstance).IsInSmartEnum(State.Open, State.Closed);

        var result = validator.Validate(new Model { NonNullableInstance = null!, Text = null! });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NotNull_composes_with_exclusions_without_duplicate_membership_failures()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(Status.B).NotNull();
        validator.RuleFor(x => x.NullableValue).NotNull().IsInSmartEnum<Model, Status, int>(2);
        validator.RuleFor(x => x.Text).IsInSmartEnum<Model, State, string>("closed").NotNull();

        var result = validator.Validate(new Model { Text = null! });

        result.Errors.Should().HaveCount(3);
        result.Errors.Select(error => error.PropertyName).Should()
            .BeEquivalentTo(nameof(Model.Instance), nameof(Model.NullableValue), nameof(Model.Text));
        result.Errors.Should().OnlyContain(error => error.ErrorCode == "NotNullValidator");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Empty_exclusions_preserve_membership_for_all_overloads(int value)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum([]);
        validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>([]);
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>([]);
        validator.RuleFor(x => x.Text).IsInSmartEnum<Model, State, string>([]);

        var result = validator.Validate(new Model
        {
            Instance = new Status("Alias", value),
            Value = value,
            NullableValue = value
        });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void Duplicate_exclusions_behave_like_a_single_exclusion(int value, bool expectedValid)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(Status.B, Status.B);
        validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(2, 2);
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(2, 2);

        var result = validator.Validate(new Model
        {
            Instance = new Status("Alias", value),
            Value = value,
            NullableValue = value
        });

        result.IsValid.Should().Be(expectedValid);
        result.Errors.Should().HaveCount(expectedValid ? 0 : 3);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(99)]
    public void Excluding_every_member_rejects_every_non_null_value(int value)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(Status.A, Status.B, Status.C, Status.D, Status.E);
        validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(1, 2, 3, 4, 5);
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(1, 2, 3, 4, 5);

        var result = validator.Validate(new Model
        {
            Instance = new Status("Alias", value),
            Value = value,
            NullableValue = value
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().OnlyContain(error => error.ErrorCode == "EnumValidator");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Caller_array_mutation_does_not_change_captured_exclusions(int value, bool expectedValid)
    {
        var instanceExclusions = new[] { Status.B };
        var valueExclusions = new[] { 2 };
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(instanceExclusions);
        validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(valueExclusions);
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(valueExclusions);
        instanceExclusions[0] = Status.C;
        valueExclusions[0] = 3;

        var result = validator.Validate(new Model
        {
            Instance = new Status("Alias", value),
            Value = value,
            NullableValue = value
        });

        result.IsValid.Should().Be(expectedValid);
        result.Errors.Should().HaveCount(expectedValid ? 0 : 3);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("open", true)]
    [InlineData("closed", false)]
    [InlineData("Open", false)]
    [InlineData("CLOSED", false)]
    [InlineData("unknown", false)]
    public void String_exclusions_preserve_value_matching_without_name_or_case_normalization(
        string? value, bool expectedValid)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Text).IsInSmartEnum<Model, State, string>("closed");
        validator.RuleFor(x => x.StringInstance).IsInSmartEnum(new State("Excluded alias", "closed"));
        var instance = value is null ? null : new State("Property alias", value);

        var result = validator.Validate(new Model { Text = value!, StringInstance = instance });

        result.IsValid.Should().Be(expectedValid);
        if (expectedValid)
        {
            result.Errors.Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().HaveCount(2);
            result.Errors.Select(error => error.PropertyName).Should()
                .BeEquivalentTo(nameof(Model.Text), nameof(Model.StringInstance));
            result.Errors.Should().OnlyContain(error => error.ErrorCode == "EnumValidator");
            result.Errors.Single(error => error.PropertyName == nameof(Model.Text))
                .AttemptedValue.Should().Be(value);
            result.Errors.Single(error => error.PropertyName == nameof(Model.StringInstance))
                .AttemptedValue.Should().BeSameAs(instance);
        }
    }

    [Theory]
    [InlineData("open", true)]
    [InlineData("OPEN", true)]
    [InlineData("closed", false)]
    [InlineData("CLOSED", false)]
    public void Exclusions_respect_the_SmartEnum_configured_value_comparer(string value, bool expectedValid)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Text).IsInSmartEnum<Model, CaseInsensitiveState, string>("ClOsEd");
        validator.RuleFor(x => x.CaseInsensitiveInstance)
            .IsInSmartEnum(new CaseInsensitiveState("Excluded alias", "ClOsEd"));

        var result = validator.Validate(new Model
        {
            Text = value,
            CaseInsensitiveInstance = new CaseInsensitiveState("Property alias", value)
        });

        result.IsValid.Should().Be(expectedValid);
        result.Errors.Should().HaveCount(expectedValid ? 0 : 2);
    }

    [Fact]
    public void Exclusions_do_not_conflate_values_distinguished_by_the_SmartEnum_comparer()
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Text)
            .IsInSmartEnum<Model, ReferenceState, string>(ReferenceState.First.Value);

        var excluded = validator.Validate(new Model { Text = ReferenceState.First.Value });
        var allowed = validator.Validate(new Model { Text = ReferenceState.Second.Value });

        excluded.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be("EnumValidator");
        allowed.IsValid.Should().BeTrue();
        allowed.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void Non_nullable_and_default_integer_backed_instances_support_single_exclusions(
        int value, bool expectedValid)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.NonNullableInstance).IsInSmartEnum(Status.B);
        validator.RuleFor(x => x.DefaultInstance).IsInSmartEnum(DefaultStatus.B);

        var result = validator.Validate(new Model
        {
            NonNullableInstance = new Status("Alias", value),
            DefaultInstance = value == 1 ? DefaultStatus.A : DefaultStatus.B
        });

        result.IsValid.Should().Be(expectedValid);
        result.Errors.Should().HaveCount(expectedValid ? 0 : 2);
    }

    [Fact]
    public void Excluded_members_use_the_default_EnumValidator_message()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var validator = new InlineValidator<Model>();
            validator.RuleFor(x => x.Instance).IsInSmartEnum(Status.B).WithName("Status");
            validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(2).WithName("Status");
            validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(2).WithName("Status");

            var result = validator.Validate(new Model { Instance = Status.B, Value = 2, NullableValue = 2 });

            result.Errors.Should().HaveCount(3);
            result.Errors.Single(error => error.PropertyName == nameof(Model.Instance)).ErrorMessage
                .Should().Be("'Status' has a range of values which does not include 'B'.");
            result.Errors.Where(error => error.PropertyName != nameof(Model.Instance))
                .Should().OnlyContain(error =>
                    error.ErrorMessage == "'Status' has a range of values which does not include '2'.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(99)]
    public void Custom_messages_preserve_error_codes_and_attempted_values(int value)
    {
        var validator = new InlineValidator<Model>();
        validator.RuleFor(x => x.Instance).IsInSmartEnum(Status.B).WithMessage("Status is not acceptable.");
        validator.RuleFor(x => x.Value).IsInSmartEnum<Model, Status, int>(2)
            .WithMessage("Status is not acceptable.");
        validator.RuleFor(x => x.NullableValue).IsInSmartEnum<Model, Status, int>(2)
            .WithMessage("Status is not acceptable.");
        var instance = new Status("Alias", value);

        var result = validator.Validate(new Model
        {
            Instance = instance,
            Value = value,
            NullableValue = value
        });

        result.Errors.Should().HaveCount(3);
        result.Errors.Select(error => error.PropertyName).Should()
            .BeEquivalentTo(nameof(Model.Instance), nameof(Model.Value), nameof(Model.NullableValue));
        result.Errors.Should().OnlyContain(error =>
            error.ErrorCode == "EnumValidator" && error.ErrorMessage == "Status is not acceptable.");
        result.Errors.Single(error => error.PropertyName == nameof(Model.Instance))
            .AttemptedValue.Should().BeSameAs(instance);
        result.Errors.Where(error => error.PropertyName != nameof(Model.Instance))
            .Should().OnlyContain(error => Equals(error.AttemptedValue, value));
    }
}
