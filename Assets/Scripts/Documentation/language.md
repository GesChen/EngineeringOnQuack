# documentation for the language
---

# expressions
**types:** 
 - numbers (always(?) stored as `floats`)
 - strings
 - lists (stored as `List<dynamic>`)
 - bools

operations are semi-soft-typed\
the value on the left side of an operation determines the output type

**all supported operations:**
 - `+` add
 - `-` subtract
 - `*` multiply
 - `/` divide
 - `^` power
 - `%` modulo
 - `==` equals
 - `!=` does not equal
 - `<` is less than
 - `>` is greater than
 - `<=` less than or equal to
 - `>=` greather than or equal to
 - `&&` and
 - `||` or
 - `!` not (modifier)
 - `!&` nand (not and )
 - `!|` nor (not or)
 - `!!` xor (exclusive or)

**supported operations for ints:**
 - `+`
 - `-`
 - `-` (negative modifier)
 - `*`
 - `/`
 - `^`
 - `%`
 - `==`
 - `!=`
 - `<`
 - `>`
 - `<=`
 - `>=`

**supported operations for bools:**
 - `==`
 - `!=`
 - `&&`
 - `||`
 - `!` (modifier)
 - `!&`
 - `!|`
 - `!!` 

strings and lists only support `+`

**supported operation type pairs:**
 - `number` - `number`
 - `number` - `string` (if string is able to be parsed as number)
 - `number` - `bool` (true is 1, false is 0)
 - `string` - `any` (right side will be converted to string)
 - `bool` - `string` (string has to either be `"true"` or `"false"`, otherwise will throw error)
 - `bool` - `number` (number has to either be `1` or `0`, otherwise will throw error)
 - `list` - `any` (single item will be added to end of list)
 - `list` - `list`



---
# errors
`Error` `(error)`\
`TestError`\
`MismatchedParentheses`\
Mismatched parentheses

`MismatchedBrackets`\
Mismatched brackets

`AttemptedEvalStringAsExpr`\
Attempted to evaluate a string as an expression

`OperatorInBadPosition` `(op)`\
Operator `{op}` in bad position

`OperatorInBadPosition`\
An operator is in a bad position

`OperatorDoesntExist` `(op)`\
Operator `{op}` doesn't exist

`OperatorMissingSide` `(op)`\
One side of operator `{op}` is missing a number

`UnableToParseStrAsNum` `(str)`\
Unable to parse \"`{str}`\" as number

`OperationFailed` `(op)`\
Operation `{op}` failed unexpectedly

`InvaidString` `(str)`\
String \"`{str}`\" is not a valid string

`UnsupportedOperation` `(op,)` `(type1,)` `(type2)`\
Unsupported operation: `{op}` between `{type1}` and `{type2}`

`DivisionByZero`\
Attempted to divide by zero

`MalformedString` `(s)`\
String `{s}` was malformed, could not parse

`MalformedList` `(s)`\
Could not parse malformed list: `{s}`

`UnknownVariable` `(name)`\
Unknown variable \"`{name}`\"

`UnableToParseBool`\
Unable to parse bool

`IndexListWithType` `(type)`\
Attempted to index a list with a `{type}` (only whole numbers are allowed)

`IndexOutOfRange` `(index)`\
List index `{index}` was out of range

`EvaluatedNothing`\
Attempted to evaluate nothing

