from datetime import datetime
startTime = datetime.now()

a = 0
for i in range(1000):
    a *= 2
print(a)

#Python 3: 
print(datetime.now() - startTime)