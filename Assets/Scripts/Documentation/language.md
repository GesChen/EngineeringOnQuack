# documentation for the language
## contents
- [expressions](#expressions)
- [errors](errors.md)
- [variables](#variables)
- [comments](#comments)
- [functions](#functions)
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

<details><summary><b>all supported operations:</b></summary>

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
</details>

</br>

<details><summary><b>supported operations for ints:</b></summary>
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
</details>

</br>

<details><summary><b>supported operations for bools:</b></summary>
 - `==`
 - `!=`
 - `&&`
 - `||`
 - `!` (modifier)
 - `!&`
 - `!|`
 - `!!` 
</details>

</br>

strings and lists only support `+`

<details><summary><b>supported operation type pairs:</b></summary>
 - `number` - `number`
 - `number` - `string` (if string is able to be parsed as number)
 - `number` - `bool` (true is 1, false is 0)
 - `string` - `any` (right side will be converted to string)
 - `bool` - `string` (string has to either be `"true"` or `"false"`, otherwise will throw error)
 - `bool` - `number` (number has to either be `1` or `0`, otherwise will throw error)
 - `list` - `any` (single item will be added to end of list)
 - `list` - `list`
</details>

---
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

---
# functions
functions can be defined with `def`, followed by the function name, and in parentheses, the argument names, separated by commas

arguments act as variables, and store whatever was passed into them in the local scope. if a global variable is overriden by an argument, it will not be changed after the function finishes

global variables modified in functions however, will keep their new value after finishing

functions can call themselves, this is called "recursion", however this is limited to a depth of 128 recursions, as memory issues arise after more than that

call a function by typing its name, followed with the correct number of arguments in parentheses separated by commas

use `return` to "return" information out of functions

functions with no `return` will return an empty string to avoid nulls

**example:**
```
def examplefunction(x, y):
    log(x)
    return x + y

foo = examplefunction(1, 2)

-- expected output: 
-- logs "1"
-- foo is assigned a new value of 3 (1 + 2) 

```

---
# classes
**to define a new class, just write `class`, followed by the name, and a colon**
- classes work by having their own interpreter, which everything related to it runs on
- to create a new instance of the class, call the class name, with the appropriate arguments

**constructors:**
- add a constructor to the class to allow new instances
- constructors are functions with the name of the class
- when called, they will return a new instance of the class, along with running whatever is inside of the function

**other special functions:**
- `string()`
    - define a `string()` function to describe how a class should be represented as a string
    - it will automatically be used whenever an instance is referenced
    - if no return is defined, or the function terminates without returning anything, or an error occurs within the function, the default class instance return value will be returned ("`class instance of {}`")

**example:**
```
class foo:
    -- this is a variable local to the class itself
    A = 0
    
    -- this is a constructor
    def foo(argument): 
        A = argument
    
    -- this is a method
    def examplemethod(x):
        log(A + x)

-- creating a new instance of the class
newinstance = foo(5)

-- retrieve class variables (attributes) with .
-- this should log 5
log(newinstance.A) 

-- you can also call methods with .
-- this should log 7 (5+2)
newinstance.examplemethod(2) 
```
