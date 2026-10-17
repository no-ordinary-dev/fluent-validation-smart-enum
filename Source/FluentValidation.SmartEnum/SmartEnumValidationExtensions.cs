namespace FluentValidation.SmartEnum;

using Ardalis.SmartEnum;

/// <summary>
/// Provides FluentValidation extension methods for validating values against <see cref="Ardalis.SmartEnum.SmartEnum{TEnum, TValue}"/> definitions.
/// </summary>
public static class SmartEnumValidationExtensions
{
    /// <summary>
    /// Validates that a smart enum instance's underlying value exists in the target smart enum
    /// and is not excluded from this rule.
    /// Null instances are allowed; use NotNull to require a value.
    /// </summary>
    /// <typeparam name="T">The model type being validated.</typeparam>
    /// <typeparam name="TEnum">The smart enum type.</typeparam>
    /// <typeparam name="TValue">The smart enum underlying value type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <param name="excludedValues">Members that are not acceptable for this rule.</param>
    /// <returns>The configured rule builder options.</returns>
    /// <exception cref="ArgumentNullException">The exclusions array is null.</exception>
    /// <exception cref="ArgumentException">An exclusion is null or does not identify a declared member.</exception>
    public static IRuleBuilderOptions<T, SmartEnum<TEnum, TValue>?>
        IsInSmartEnum<T, TEnum, TValue>(
            this IRuleBuilder<T, SmartEnum<TEnum, TValue>?> ruleBuilder,
            params SmartEnum<TEnum, TValue>[] excludedValues)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(excludedValues);
        var values = new TValue[excludedValues.Length];
        for (var index = 0; index < excludedValues.Length; index++)
        {
            var instance = excludedValues[index] ??
                throw new ArgumentException("Excluded members cannot be null.", nameof(excludedValues));
            values[index] = instance.Value;
        }

        var exclusions = GetExcludedMembers<TEnum, TValue>(values);
        return ruleBuilder
            .Must(instance =>
                instance is null ||
                (SmartEnum<TEnum, TValue>.TryFromValue(instance.Value, out var member) &&
                 !exclusions.Contains(member)))
            .WithErrorCode("EnumValidator");
    }

    /// <summary>
    /// Validates that a value exists in the target smart enum and is not excluded from this rule.
    /// Null values are allowed; use NotNull to require a value.
    /// </summary>
    /// <typeparam name="T">The model type being validated.</typeparam>
    /// <typeparam name="TEnum">The smart enum type.</typeparam>
    /// <typeparam name="TValue">The smart enum underlying value type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <param name="excludedValues">Underlying values that are not acceptable for this rule.</param>
    /// <returns>The configured rule builder options.</returns>
    /// <exception cref="ArgumentNullException">The exclusions array is null.</exception>
    /// <exception cref="ArgumentException">An exclusion is null or does not identify a declared member.</exception>
    public static IRuleBuilderOptions<T, TValue>
        IsInSmartEnum<T, TEnum, TValue>(
            this IRuleBuilder<T, TValue> ruleBuilder,
            params TValue[] excludedValues)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        var exclusions = GetExcludedMembers<TEnum, TValue>(excludedValues);
        return ruleBuilder
            .Must(value =>
                value is null ||
                (SmartEnum<TEnum, TValue>.TryFromValue(value, out var member) &&
                 !exclusions.Contains(member)))
            .WithErrorCode("EnumValidator");
    }

    /// <summary>
    /// Validates that a nullable value exists in the target smart enum and is not excluded from this rule.
    /// Null values are allowed; use NotNull to require a value.
    /// </summary>
    /// <typeparam name="T">The model type being validated.</typeparam>
    /// <typeparam name="TEnum">The smart enum type.</typeparam>
    /// <typeparam name="TValue">The smart enum underlying value type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <param name="excludedValues">Underlying values that are not acceptable for this rule.</param>
    /// <returns>The configured rule builder options.</returns>
    /// <exception cref="ArgumentNullException">The exclusions array is null.</exception>
    /// <exception cref="ArgumentException">An exclusion does not identify a declared member.</exception>
    public static IRuleBuilderOptions<T, TValue?>
        IsInSmartEnum<T, TEnum, TValue>(
            this IRuleBuilder<T, TValue?> ruleBuilder,
            params TValue[] excludedValues)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : struct, IEquatable<TValue>, IComparable<TValue>
    {
        var exclusions = GetExcludedMembers<TEnum, TValue>(excludedValues);
        return ruleBuilder
            .Must(value =>
                !value.HasValue ||
                (SmartEnum<TEnum, TValue>.TryFromValue(value.Value, out var member) &&
                 !exclusions.Contains(member)))
            .WithErrorCode("EnumValidator");
    }

    private static HashSet<TEnum> GetExcludedMembers<TEnum, TValue>(TValue[] excludedValues)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(excludedValues);
        var exclusions = new HashSet<TEnum>(ReferenceEqualityComparer.Instance);
        foreach (var value in excludedValues)
        {
            if (value is null || !SmartEnum<TEnum, TValue>.TryFromValue(value, out var member))
            {
                throw new ArgumentException(
                    "Each excluded value must identify a declared SmartEnum member.", nameof(excludedValues));
            }

            // Canonical members preserve SmartEnum's comparer without applying a second value comparer.
            exclusions.Add(member);
        }

        return exclusions;
    }
}
