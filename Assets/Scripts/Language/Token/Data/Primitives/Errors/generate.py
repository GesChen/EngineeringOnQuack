with open("Errors.txt", 'r') as source:
	with open("Errors.cs", 'w') as file:
		file.writelines(
'''public static class Errors
{
''')
		
		state = 0
		for line in source.readlines():
			line = line.strip()
			if line == "":
				state = 0

				args = line2 if line3 != "" else ""
				message = line3 if line3 != "" else line2
				file.write(f'	public static Primitive.Error {line1}({args})\n')
				file.write(f'		=> new($"{message}");\n\n')
				
				line1 = ""
				line2 = ""
				line3 = ""
			else:
				state += 1

				if state == 1:
					line1 = line.strip()
				elif state == 2:
					line2 = line.strip()
				elif state == 3:
					line3 = line.strip()

		file.write('}')