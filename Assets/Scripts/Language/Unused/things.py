import math
import asyncio

# Basic data types and operations
x = 10
y = 3.14
z = 1 + 2j
result = x + y * z

# String manipulation
greeting = "Hello, World!"
formatted = f"{greeting.upper()} The answer is {result:.2f}"

# Lists and list comprehension
numbers = [1, 2, 3, 4, 5]
squares = [n**2 for n in numbers if n % 2 == 0]

# Dictionary and set
person = {"name": "Alice", "age": 30}
unique_numbers = set(numbers)

# Control flow
if x > 5:
    print("x is greater than 5")
elif x < 5:
    print("x is less than 5")
else:
    print("x is equal to 5")

# Loops
for num in numbers:
    print(num)

while x > 0:
    x -= 1

# Functions and lambda
def greet(name):
    return f"Hello, {name}!"

square = lambda x: x**2

# Classes and inheritance
class Animal:
    def speak(self):
        pass

class Dog(Animal):
    def speak(self):
        print("Woof!")

# Exception handling
try:
    result = 10 / 0
except ZeroDivisionError as e:
    print(f"Error: {e}")
finally:
    print("This always executes")

# Context manager
with open("example.txt", "w") as file:
    file.write("Hello, File!")

# Generators
def countdown(n):
    while n > 0:
        yield n
        n -= 1

# Decorators
def timer(func):
    def wrapper(*args, **kwargs):
        import time
        start = time.time()
        result = func(*args, **kwargs)
        end = time.time()
        print(f"{func.__name__} took {end - start:.2f} seconds")
        return result
    return wrapper

@timer
def slow_function():
    import time
    time.sleep(1)

# Asynchronous programming
async def fetch_data(url):
    await asyncio.sleep(1)  # Simulating network delay
    return f"Data from {url}"

async def main():
    urls = ["http://example.com", "http://example.org"]
    tasks = [fetch_data(url) for url in urls]
    results = await asyncio.gather(*tasks)
    print(results)

# Walrus operator (Python 3.8+)
if (n := len(numbers)) > 3:
    print(f"List has {n} items")

# Match statement (Python 3.10+)
status = 404
match status:
    case 200:
        print("OK")
    case 404:
        print("Not Found")
    case _:
        print("Unknown status")

# Unpacking
a, *b, c = [1, 2, 3, 4, 5]

# Dictionary comprehension
squared_dict = {n: n**2 for n in range(5)}

# Set operations
set1 = {1, 2, 3}
set2 = {3, 4, 5}
union = set1 | set2
intersection = set1 & set2

# Ternary operator
is_adult = True
status = "Adult" if is_adult else "Minor"

if __name__ == "__main__":
    dog = Dog()
    dog.speak()
    slow_function()
    asyncio.run(main())
    print(f"Unpacking: a={a}, b={b}, c={c}")
    print(f"Squared dict: {squared_dict}")
    print(f"Set union: {union}, intersection: {intersection}")
    print(f"Status: {status}")
