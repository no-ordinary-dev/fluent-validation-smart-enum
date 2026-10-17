namespace FluentValidation.SmartEnum;

using Ardalis.SmartEnum;

/// <summary>
/// Provides FluentValidation extension methods for validating values against <see cref="Ardalis.SmartEnum.SmartEnum{TEnum, TValue}"/> definitions.
/// </summary>
public static class SmartEnumValidationExtensions
{
    /// <summary>
    /// Validates that a smart enum instance's underlying value exists in the target smart enum.
    /// Null instances are allowed; use NotNull to require a value.
    /// </summary>
    /// <typeparam name="T">The model type being validated.</typeparam>
    /// <typeparam name="TEnum">The smart enum type.</typeparam>
    /// <typeparam name="TValue">The smart enum underlying value type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The configured rule builder options.</returns>
    public static IRuleBuilderOptions<T, SmartEnum<TEnum, TValue>?>
        IsSmartEnum<T, TEnum, TValue>(
            this IRuleBuilder<T, SmartEnum<TEnum, TValue>?> ruleBuilder)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue> =>
        ruleBuilder
            .Must(instance =>
                instance is null ||
                SmartEnum<TEnum, TValue>.TryFromValue(instance.Value, out _))
            .WithErrorCode("EnumValidator");

    /// <summary>
    /// Validates that a value exists in the target smart enum.
    /// </summary>
    /// <typeparam name="T">The model type being validated.</typeparam>
    /// <typeparam name="TEnum">The smart enum type.</typeparam>
    /// <typeparam name="TValue">The smart enum underlying value type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The configured rule builder options.</returns>
    public static IRuleBuilderOptions<T, TValue>
        IsInSmartEnum<T, TEnum, TValue>(
            this IRuleBuilder<T, TValue> ruleBuilder)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue> =>
        ruleBuilder
            .Must(value =>
                value is null ||
                SmartEnum<TEnum, TValue>.TryFromValue(value, out _))
            .WithErrorCode("EnumValidator");

    /// <summary>
    /// Validates that a nullable value exists in the target smart enum.
    /// </summary>
    /// <typeparam name="T">The model type being validated.</typeparam>
    /// <typeparam name="TEnum">The smart enum type.</typeparam>
    /// <typeparam name="TValue">The smart enum underlying value type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The configured rule builder options.</returns>
    public static IRuleBuilderOptions<T, TValue?>
        IsInSmartEnum<T, TEnum, TValue>(
            this IRuleBuilder<T, TValue?> ruleBuilder)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : struct, IEquatable<TValue>, IComparable<TValue> =>
        ruleBuilder
            .Must(value =>
                !value.HasValue ||
                SmartEnum<TEnum, TValue>.TryFromValue(value.Value, out _))
            .WithErrorCode("EnumValidator");
}
