# documentation for the language
## contents
- [expressions](#expressions)
- [errors](#errors)
- [variables](#variables)
- [comments](#comments)
- [classes](#classes)

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

# variables
all variables are stored as `dynamic`s

naming convention:
 - starts either with letter or _
 - following characters can be letter, number or _
 - variable names are case sensitive

---
# comments
**single line comments** can be toggled with `--` sort of like an ambiguation for both `/*` and `*/` in c# or simply `"""` in python

**multi line comments** span across multiple lines, and can be toggled with `---`

**examples:**

single line comments:
- `log("hello world") --comment`
- they can also be used in the middle of code (if you wanted to for whatever reason)
    - `if --interrupting comment who?-- true: log("test")`

multiline comments:
```
---comment start
blah blah
more words---

log("hello world!")
```

# classes
**to define a new class, just write `class`, followed by the name, and a colon**
- classes work by having their own interpreter, which everything related to it runs on
- add a constructor to the class to allow new instances, in the format of a normal function except with the name of the class
- to create a new instance of the class, call the class name, with the appropriate arguments

example:
```
class foo:
    -- this is a variable local to the class itself
    A = 0
    
    -- this is a constructor
    def foo(argument): 
        A = argument

-- creating a new instance of the class
newinstance = foo(5)

log()