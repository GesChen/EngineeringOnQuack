import os 
import re
import shutil

start = r'C:\CProjects\engineeringonquack\EngineeringOnQuack\Assets\Scripts'
mdfolder = r'C:\CProjects\engineeringonquack\EngineeringOnQuack\Assets\Scripts\fun\graphvisualization\mds'

def findreferences(file_path):
	refs = []

	# Open and read the file
	with open(file_path, 'r') as file:
		lines = file.readlines()

	# Iterate through each line
	for line in lines:
		# Check if the line has one tab indentation
		if (line.startswith('\t') and not line.startswith('\t\t') 
	  		and 'void' not in line
			and '[' not in line):

			# Remove the leading tab for easier processing
			line = line.lstrip('\t')
			# Use regex to find words starting with a capital letter not followed by parentheses
			match = re.search(r'\b([A-Z][a-zA-Z]*)\b(?!\()', line)
			if match:
				refs.append(match.group(1))

	refs = list(set(refs)) # unique

	return refs

def extract_classes_from_csharp_file(file_path):
    """
    Extracts all referenced class names from a C# file.

    :param file_path: Path to the C# source file.
    :return: A set of unique class names referenced in the file.
    """
    with open(file_path, 'r') as file:
        file_content = file.read()

    # Define regex patterns
    class_declaration_pattern = r'\bclass\s+(\w+)'  # Matches class declarations
    type_reference_pattern = r'\b(\w+)\s*::'        # Matches class references (like Type::Method)
    variable_declaration_pattern = r'\b(\w+)\s+\w+\s*='  # Matches variable types (like MyClass myVar = ...)

    # Extract class names
    classes = set(re.findall(class_declaration_pattern, file_content))
    class_references = set(re.findall(type_reference_pattern, file_content))
    variable_declarations = set(re.findall(variable_declaration_pattern, file_content))

    # Combine all sets and return the unique class references
    all_referenced_classes = classes.union(class_references).union(variable_declarations)

    return all_referenced_classes

def clear_folder(folder_path):
    # Verify the folder exists
    if not os.path.exists(folder_path):
        print(f"Folder {folder_path} does not exist.")
        return

    # Iterate through all contents and remove them
    for filename in os.listdir(folder_path):
        file_path = os.path.join(folder_path, filename)
        try:
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)
            elif os.path.isdir(file_path):
                shutil.rmtree(file_path)
        except Exception as e:
            print(f'Failed to delete {file_path}. Reason: {e}')

clear_folder(mdfolder)

for root, _, files in os.walk(start):
	for filename in files:
		if '.cs' in filename and not '.meta' in filename:
			refs = extract_classes_from_csharp_file(os.path.join(root, filename))

			name = filename[:filename.index('.')]

			with open(os.path.join(mdfolder, name + '.md'), 'w') as file:
				print(f'writing to {name}')
				for r in refs:
					line = f'[[{r}]]\n'
					file.write(line)