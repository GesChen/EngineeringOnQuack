import os 

def get_character_count(filename):
  try:
    with open(filename, 'r') as file:
      content = file.read()
      return len(content)
  except FileNotFoundError:
    print(f"Error: File not found - {filename}")
    return 0

start = r'C:\CTools\Unity\PRP_HDRP\Assets\Scripts'

counts = {}
for root, _, files in os.walk(start):
    for filename in files:
        if '.cs' in filename:
            counts[filename] = get_character_count(os.path.join(root,filename))

#print(counts)

total = 0
for key in counts.keys():
   total += counts[key]

print(f"total: {total}")
print(f"average words: {total / 4.7}")
print(f"1000 word essays: {total / 4.7 / 1000}")