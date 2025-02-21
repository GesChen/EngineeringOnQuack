class a:
    def y(self, x):
        self.x = x

    def __init__(self, x):
        self.x = x


b = a(1)

print(b.x)
b.x = 2
print(b.x)

print(b.y)
b.y = 5

print(b.y)
