from datetime import datetime
startTime = datetime.now()

a = 0
for i in range(100000):
    a += 1
print(a)

#Python 3: 
print(datetime.now() - startTime)