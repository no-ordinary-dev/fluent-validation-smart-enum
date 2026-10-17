# SmartEnum Validation

Validation of SmartEnum membership, whether supplied as a value or an instance.

## Language

**SmartEnum value**:
The underlying value identifying a member of a SmartEnum type.

**SmartEnum instance**:
An object representing a SmartEnum member through its name and underlying value.

**Valid member**:
A value or instance whose underlying value matches a declared member of the
corresponding SmartEnum type. A matching name or object identity is not required.

**Excluded member**:
A declared SmartEnum member that is not acceptable for a particular validation
rule, even though it belongs to the corresponding SmartEnum type.
_Avoid_: Allowed value, invalid member
