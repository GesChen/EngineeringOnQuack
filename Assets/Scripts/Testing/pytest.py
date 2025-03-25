from datetime import datetime
startTime = datetime.now()

x = 0
for i in range(10000):
    x += i

#Python 3: 
print((datetime.now() - startTime).microseconds / 1000)