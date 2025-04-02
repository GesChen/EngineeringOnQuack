from datetime import datetime
startTime = datetime.now()

import re

# Regular expressions for syntax highlighting
KEYWORDS = r"\b(if|else|for|while|def|class|return|try|except|finally|import|from|as|lambda|with)\b"
STRINGS = r"\".*?\"|'.*?'"
NUMBERS = r"\b\d+\b|\b\d+\.\d+\b"
COMMENTS = r"#.*?$"
PUNCTUATION = r"[\(\)\[\]\{\};,\.]"
OPERATORS = r"(\+|-|\*|\/|%|\+=|-=|\*=|/=|==|!=|<|>|<=|>=|=|and|or|not)"
VARIABLES_FUNCTIONS = r"\b[a-zA-Z_][a-zA-Z0-9_]*\b"

# ANSI color codes for syntax highlighting
RESET = '\033[0m'
KEYWORD_COLOR = '\033[34m'
STRING_COLOR = '\033[32m'
NUMBER_COLOR = '\033[36m'
COMMENT_COLOR = '\033[90m'
PUNCTUATION_COLOR = '\033[33m'
OPERATOR_COLOR = '\033[31m'
VARIABLE_COLOR = '\033[35m'

def highlight_syntax(code):
    """
    Highlights Python code with colors for syntax elements.
    """
    # Replace keywords, strings, numbers, comments, etc.
    code = re.sub(KEYWORDS, lambda match: f"{KEYWORD_COLOR}{match.group(0)}{RESET}", code)
    code = re.sub(STRINGS, lambda match: f"{STRING_COLOR}{match.group(0)}{RESET}", code)
    code = re.sub(NUMBERS, lambda match: f"{NUMBER_COLOR}{match.group(0)}{RESET}", code)
    code = re.sub(COMMENTS, lambda match: f"{COMMENT_COLOR}{match.group(0)}{RESET}", code)
    code = re.sub(PUNCTUATION, lambda match: f"{PUNCTUATION_COLOR}{match.group(0)}{RESET}", code)
    code = re.sub(OPERATORS, lambda match: f"{OPERATOR_COLOR}{match.group(0)}{RESET}", code)
    code = re.sub(VARIABLES_FUNCTIONS, lambda match: f"{VARIABLE_COLOR}{match.group(0)}{RESET}", code)
    return code

def test_highlighting():
    """
    Runs a test code through the highlighter to check all syntax features.
    """
    test_code = """
# This is a comment
def my_function(arg1, arg2):
    if arg1 > arg2:
        result = arg1 + arg2
        return result
    else:
        return arg2 * 2

x = 10
y = 3.14
message = "Hello, world!"
my_function(x, y)
    """
    highlighted_code = highlight_syntax(test_code)
    print(highlighted_code)

if __name__ == "__main__":
    test_highlighting()


#Python 3: 
print((datetime.now() - startTime).microseconds / 1000)