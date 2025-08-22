errorsfile = r'D:\Projects\engineeringonquack\EngineeringOnQuack\Assets\Scripts\Language\Errors.cs'
output = r'errors.md'

with open(errorsfile, 'r') as file:
    lines = file.readlines()

newlines = []

for line in lines:
    line = line.strip()

    if line.startswith("public static Output"):
        newline = ""

        description = line[21:]
        argstart = description.find("(")
        description = description[:argstart]
        print(description)

        newline += f'`{description}`'

        argsstring = line[(22 + argstart) : (line.find("Interpreter") - 2) ]

        if len(argsstring) > 0:
            args = argsstring.split(' ')
            
            for arg in args[1::2]:
                newline += f' `({arg})`'
        
        newline += '\\'

        newlines.append(newline)
    if line.startswith("Error"):
        newline = ""

        description = line[line.find('"') + 1:line.find('", i')]

        temp = ""
        for letter in description:
            if letter == '{':
                temp += '`{'
            elif letter == '}':
                temp += '}`'
            else:
                temp += letter        
        description = temp

        print(description)
        
        newlines.append(description)
        newlines.append('')

print(newlines)

with open(output, 'w') as newfile:
    for line in newlines:
        newfile.write(line+'\n')