`Error` `(error)`\
`TestError`\
`MismatchedParentheses`\
Mismatched parentheses

`MismatchedBrackets`\
Mismatched brackets

`MismatchedQuotes`\
Mismatched string quotes

`AttemptedEvalStringAsExpr`\
Cannot evaluate a string as an expression

`OperatorInInvalidPosition` `(op)`\
Operator `{op}` in invalid position

`OperatorInInvalidPosition`\
An operator is in an invalid position

`OperatorDoesntExist` `(op)`\
Operator `{op}` doesn't exist

`OperatorMissingSide` `(op)`\
One side of operator `{op}` is missing a number

`UnableToParseStrAsNum` `(str)`\
Unable to parse \"`{str}`\" as number

`OperationFailed` `(op)`\
Operation `{op}` failed unexpectedly

`InvalidString` `(str)`\
String \"`{str}`\" is not a valid string

`UnsupportedOperation` `(op,)` `(type1,)` `(type2)`\
Unsupported operation: `{op}` between `{type1}` and `{type2}`

`DivisionByZero`\
Division by zero

`MalformedString` `(s)`\
Cannot parse malformed string: \"`{s}`\"

`MalformedList` `(s)`\
Cannot parse malformed list: `{s}`

`UnknownVariable` `(name)`\
Unknown variable \"`{name}`\"

`UnknownFunction` `(name)`\
Unknown function \"`{name}`\"

`UnknownSymbol` `(symbol)`\
Unknown symbol \"`{symbol}`\"

`UnknownType`\
Unknown type

`UnableToParseAsBool` `(s)`\
Unable to parse `{s}` as bool

`IndexListWithType` `(type)`\
Cannot index a list with a `{type}` (only whole numbers are allowed)

`IndexOutOfRange` `(index)`\
List index `{index}` was out of range

`EvaluatedNothing`\
Cannot evaluate nothing

`ClassAlreadyExists` `(name)`\
A class called \"`{name}`\" already exists

`FunctionAlreadyExists` `(name,)` `(numArgs)`\
Function \"`{name}`\" with `{numArgs}` args already exists

`NoFunctionExists` `(name,)` `(numargs)`\
No function \"`{name}`\" exists that takes `{numargs}` arguments

`VariableIsNotFunction` `(name)`\
Variable \"`{name}`\" is not a function

`UnexpectedNumberofArgs` `(name,)` `(expected,)` `(got)`\
Unexpected number of args for method \"`{name}`\": got `{got}`, expected `{expected}`

`CannotSetKeyword` `(name)`\
Cannot set keyword \"`{name}`\"

`UnexpectedIndent`\
Unexpected indent

`ExpectedColon`\
Expected colon

`ExpectedBoolInIf` `(gottype)`\
Expected a boolean expression in if statement, got `{gottype}`

`UnexpectedStatementAfterParentheses` `(statement)`\
Unexpected statement \"`{statement}`\" after parentheses

`ExpectedCustom` `(expected)`\
Expected `{expected}`

`ExpectedParentheses`\
Expected parentheses

`UnexpectedElse`\
Unexpected else statement

`InvalidVariableName` `(invalidname)`\
Invalid variable name: `{invalidname}`

`InvalidFunctionName` `(invalidname)`\
Invalid function name: `{invalidname}`

`InvalidClassName` `(invalidname)`\
Invalid class name: `{invalidname}`

`UnexpectedCatch`\
Unexpected catch statement

`EmptyFunction`\
Function definition is empty

`DuplicateArguments` `(duplicatename)`\
Duplicate arguments: `{duplicatename}`

`MaxRecursion` `(maxdepth)`\
Max recursion depth reached (`{maxdepth}`)

`ExpectedClassDef`\
Expected class definition

`AlreadyIsClass` `(name)`\
Cannot set variable \"`{name}`\" as it is a class

`AlreadyIsFunction` `(name)`\
Cannot set variable \"`{name}`\" as it is a function

`InterpreterDoesntHaveEval`\
Interpreter doesn't have an evaluator. How did you manage to do this?

`TypeHasNoAttributes` `(typeName)`\
A `{typeName}` has no attributes

`InvalidUseOfPeriod`\
Invalid use of period

`TypeHasNoMethod` `(methodname,)` `(type)`\
Type `{type}` has no method \"`{methodname}`\"

`TypeHasNoMethod` `(methodname,)` `(type,)` `(numArgs)`\
Type `{type}` has no method \"`{methodname}`\" that takes `{numArgs}` arguments

`TypeHasNoAttribute` `(attributename,)` `(type)`\
Type `{type}` has no attribute \"`{attributename}`\"

`MethodExpectedType` `(methodname,)` `(expected,)` `(given)`\
Method `{methodname}` expected an argument of type `{expected}`, given `{given}`

`NoOperator`\
Expression contains no operators (???)

`UnknownError`\
Unknown error

`ExpectedExpression`\
Expected expression

