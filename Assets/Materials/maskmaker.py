import os
from PIL import Image
import numpy as np

directory = r"Wood"

METALLIC_SUFFIX = "_metal"
AO_SUFFIX = "_ao_4k.jpg"
ROUGHNESS_SUFFIX = "_rough_4k.png"

# METALLIC_SUFFIX = "_Metalness.png"
# AO_SUFFIX = "_ao.png"
# ROUGHNESS_SUFFIX = "_Roughness.png"

MASK_MAP_SUFFIX = "_mask_map.png"

DEFAULT_METALLIC = 128 
DEFAULT_AO = 128       
DEFAULT_ROUGHNESS = 128

def find_mask_images(directory):
    directory = os.path.join(os.getcwd(), directory)

    metallic_path = None
    ao_path = None
    roughness_path = None
    base_name = None

    for filename in os.listdir(directory):
        lower_filename = filename.lower()
        if lower_filename.endswith(METALLIC_SUFFIX.lower()):
            metallic_path = os.path.join(directory, filename)
            base_name = filename[:-len(METALLIC_SUFFIX)]
        elif lower_filename.endswith(AO_SUFFIX.lower()):
            ao_path = os.path.join(directory, filename)
            base_name = filename[:-len(AO_SUFFIX)]
        elif lower_filename.endswith(ROUGHNESS_SUFFIX.lower()):
            roughness_path = os.path.join(directory, filename)
            base_name = filename[:-len(ROUGHNESS_SUFFIX)]

    return metallic_path, ao_path, roughness_path, base_name

def create_default_image(size, default_value):
    return Image.new('L', size, color=default_value)

def create_mask_map(metallic_path, ao_path, roughness_path, output_path):
    # Determine the size from the first available image
    size = None
    for path in [metallic_path, ao_path, roughness_path]:
        if path:
            with Image.open(path) as img:
                size = img.size
            break
    
    if size is None:
        raise ValueError("No valid input images found")

    # Load or create default images
    metallic = Image.open(metallic_path).convert('L') if metallic_path else create_default_image(size, DEFAULT_METALLIC)
    ao = Image.open(ao_path).convert('L') if ao_path else create_default_image(size, DEFAULT_AO)
    roughness = Image.open(roughness_path).convert('L') if roughness_path else create_default_image(size, DEFAULT_ROUGHNESS)

    # Ensure all images have the same size
    metallic = metallic.resize(size)
    ao = ao.resize(size)
    roughness = roughness.resize(size)

    # Convert images to numpy arrays
    metallic_array = np.array(metallic)
    ao_array = np.array(ao)
    roughness_array = np.array(roughness)

    # Convert roughness to smoothness
    smoothness_array = 255 - roughness_array

    # Create the mask map
    mask_map = np.zeros((size[1], size[0], 4), dtype=np.uint8)
    mask_map[:,:,0] = metallic_array  # Red channel: Metallic
    mask_map[:,:,1] = ao_array        # Green channel: Ambient Occlusion
    mask_map[:,:,3] = smoothness_array  # Alpha channel: Smoothness (converted from Roughness)

    # Create and save the final image
    result = Image.fromarray(mask_map, mode='RGBA')
    result.save(output_path)

    print(f"Mask map saved to {output_path}")

def main():
    metallic_path, ao_path, roughness_path, base_name = find_mask_images(directory)

    if base_name:
        output_filename = base_name + MASK_MAP_SUFFIX
        output_path = os.path.join(directory, output_filename)
        create_mask_map(metallic_path, ao_path, roughness_path, output_path)
        
        # Print information about which masks were used
        print("Masks used:")
        print(f"Metallic: {'Found' if metallic_path else 'Default'}")
        print(f"Ambient Occlusion: {'Found' if ao_path else 'Default'}")
        print(f"Roughness: {'Found' if roughness_path else 'Default'}")
    else:
        print("Error: Could not determine base name. No compatible mask images found.")

if __name__ == "__main__":
    main()
