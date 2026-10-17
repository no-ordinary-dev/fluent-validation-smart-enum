# SmartEnum exclusions

Status: implemented.

## API and compatibility

Rename the SmartEnum-instance extension method from `IsSmartEnum` to
`IsInSmartEnum`. Remove the old method rather than retaining an obsolete alias.
This is a breaking API change: instance-validation callers must update their
method name.

Support exclusions on all `IsInSmartEnum` variants: SmartEnum instances,
underlying values, and nullable underlying values. Calls without exclusions
retain their existing membership-validation behavior.

Accept multiple exclusions through `params` arguments. For instance properties,
exclusions are SmartEnum instances; for underlying-value properties, including
nullable properties, exclusions are underlying values. Do not introduce an
allow-list or predicate filter.

For example, an instance rule excludes `Status.Closed`, while a value rule
excludes `Status.Closed.Value`.

## Membership and exclusions

An exclusion removes a declared member from the acceptable members for one rule;
it does not change the SmartEnum type's declared members.

For declared members A, B, C, D, and E, excluding B and D accepts A, C, and E.
Undeclared values remain invalid regardless of the exclusions.

Match instance properties and instance exclusions by their underlying values,
not their names or object identities. A separate instance with the underlying
value of a declared member identifies that member even if its name differs.
Use the existing underlying-value equality semantics; do not introduce name
matching, case normalization, or a case-insensitive option.

A null property continues to pass, with or without exclusions. Callers must use
`NotNull()` separately to require a property value.

## Rule construction

Capture exclusions as a fixed set when constructing the rule. Later mutations
of the caller's exclusions array must not change validation behavior.

Reject invalid exclusion configuration immediately with explicit argument
exceptions:

- A null exclusions array.
- A null exclusion element.
- An exclusion whose underlying value does not identify a declared member.

These are configuration errors, not validation failures for a model.

An omitted or empty exclusions array excludes nothing. Duplicate exclusions are
harmless. Excluding every declared member is permitted and makes every non-null
property fail; null properties still pass.

## Validation failures

Both excluded members and undeclared values fail with the existing
`EnumValidator` error code and default FluentValidation message. Callers can
customize the message with `WithMessage()`.

Preserve the original property value as the failure's `AttemptedValue`,
including when that value is a SmartEnum instance.

## Acceptance scenarios

Apply these scenarios to instance, underlying-value, and nullable-value rules
where applicable:

| Scenario | Expected outcome |
| --- | --- |
| Declared member, no exclusions | Pass |
| Declared member, empty exclusions | Pass |
| Declared member not in the exclusion set | Pass |
| Declared member in the exclusion set | Fail with `EnumValidator` |
| Undeclared property value | Fail with `EnumValidator` |
| Null property, with or without exclusions | Pass |
| Null exclusions array | Argument exception during rule construction |
| Null exclusion element | Argument exception during rule construction |
| Undeclared exclusion | Argument exception during rule construction |
| Duplicate exclusions | Same result as one exclusion |
| Every declared member excluded, non-null property | Fail |
| Every declared member excluded, null property | Pass |
| Caller changes exclusions array after rule construction | Original exclusion set remains effective |
| Separate instance with a declared underlying value | Same membership and exclusion result as the declared instance |

## Implementation

The public membership-validation extensions implement this contract, with tests
at the public rule-builder boundary. See the [README](../README.md) for usage and
migration examples, and the [glossary](../GLOSSARY.md) for the domain term
**Excluded member**.
