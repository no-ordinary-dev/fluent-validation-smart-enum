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

The `IsSmartEnum()` extension validates properties typed as SmartEnum
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
        RuleFor(x => x.PriorityId).IsSmartEnum();
        RuleFor(x => x.PriorityIdNonNullable).IsSmartEnum();
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
- Existing `IsInSmartEnum` underlying-value overloads and behavior remain
  unchanged.