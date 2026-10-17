# FluentValidation.SmartEnum

[![NoOrdinaryDev.FluentValidation.SmartEnum NuGet Package](https://img.shields.io/nuget/v/NoOrdinaryDev.FluentValidation.SmartEnum.svg)](https://www.nuget.org/packages/NoOrdinaryDev.FluentValidation.SmartEnum/) [![NoOrdinaryDev.FluentValidation.SmartEnum NuGet Package Downloads](https://img.shields.io/nuget/dt/NoOrdinaryDev.FluentValidation.SmartEnum)](https://www.nuget.org/packages/NoOrdinaryDev.FluentValidation.SmartEnum) [![GitHub Actions Status](https://github.com/no-ordinary-dev/fluent-validation-smart-enum/workflows/Build/badge.svg?branch=main)](https://github.com/no-ordinary-dev/fluent-validation-smart-enum/actions)

[![GitHub Actions Build History](https://buildstats.info/github/chart/no-ordinary-dev/fluent-validation-smart-enum?branch=main&includeBuildsFromPullRequest=false)](https://github.com/no-ordinary-dev/fluent-validation-smart-enum/actions)

FluentValidation extensions for validating [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) values and instances.

## Why?

`FluentValidation` has `IsInEnum()` for regular C# enums, but SmartEnum values are not regular enums. This package adds reusable validators that check whether an underlying value or instance matches a SmartEnum member.

## Installation

```bash
dotnet add package FluentValidation.SmartEnum
```

## SmartEnum instance validation

The `IsInSmartEnum()` extension validates properties typed as SmartEnum
instances, with or without nullable annotations:

```csharp
private sealed class Model
{
    public Priority? PriorityId { get; set; }
    public Priority PriorityIdNonNullable { get; set; } = Priority.Low;
}

private sealed class ModelValidator : AbstractValidator<Model>
{
    public ModelValidator()
    {
        RuleFor(x => x.PriorityId).IsInSmartEnum();
        RuleFor(x => x.PriorityIdNonNullable).IsInSmartEnum();
    }
}
```

- Membership is determined by the underlying value. A separate instance with a
  known value passes even if its name differs; an unknown value fails.
- Null passes for both property declarations. Use `.NotNull()` when a property
  is required.
- The method infers the model, SmartEnum, and underlying value types. Support is
  not limited to integer-backed SmartEnums.
- Membership failures use the `EnumValidator` error code.

## Underlying-value validation

Use explicit model, SmartEnum, and underlying value type arguments when the
property holds an underlying value rather than a SmartEnum instance:

```csharp
private sealed class ValueModel
{
    public int PriorityId { get; set; }
    public int? OptionalPriorityId { get; set; }
}

private sealed class ValueModelValidator : AbstractValidator<ValueModel>
{
    public ValueModelValidator()
    {
        RuleFor(x => x.PriorityId).IsInSmartEnum<ValueModel, Priority, int>();
        RuleFor(x => x.OptionalPriorityId).IsInSmartEnum<ValueModel, Priority, int>();
    }
}
```

Reference underlying values, such as strings, are supported as well. Null
properties pass; use `.NotNull()` to require a value.

## Excluding members

Supply zero or more exclusions to narrow the members accepted by a particular
rule. Arguments are excluded members, **not** an allow-list. Instance rules
accept SmartEnum instances:

```csharp
// Inside ModelValidator: accept Low, but reject High.
RuleFor(x => x.PriorityId).IsInSmartEnum(Priority.High);
```

Underlying-value rules accept underlying values, including for nullable
properties:

```csharp
// Inside ValueModelValidator.
RuleFor(x => x.PriorityId)
    .IsInSmartEnum<ValueModel, Priority, int>(Priority.High.Value);
RuleFor(x => x.OptionalPriorityId)
    .IsInSmartEnum<ValueModel, Priority, int>(Priority.High.Value);
```

- Pass multiple exclusions as separate arguments or an array. With five
  declared members A-E, excluding B and D accepts A, C, and E; undeclared
  values still fail.
- Instance exclusions match by underlying value, not name or object identity.
  Matching follows the SmartEnum type's existing value comparer; this package
  adds no name matching or case normalization.
- Omitting exclusions or supplying an empty array performs ordinary membership
  validation. Duplicate exclusions are harmless.
- Excluding every declared member is permitted and rejects every non-null
  property. Null properties still pass, even with all members excluded.
- Exclusions are captured when constructing the rule. Later changes to the
  caller's array do not change validation.
- A null exclusions array throws `ArgumentNullException`. Null exclusion
  elements and undeclared exclusions throw `ArgumentException`. These errors
  occur when constructing the rule, not when validating a model.
- Excluded and undeclared property values both fail with `EnumValidator`, the
  default FluentValidation message, and the original attempted value. Use
  `.WithMessage(...)` to customize the message.

## Publishing releases

NuGet.org publishing uses [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
with GitHub OIDC instead of a stored API key.
