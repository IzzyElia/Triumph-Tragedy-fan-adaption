import os
from PIL import Image

def combine_images(images):
    """Combine multiple images horizontally."""
    total_width = sum(img.size[0] for img in images)
    max_height = max(img.size[1] for img in images)
    combined_image = Image.new('RGBA', (total_width, max_height))
    x_offset = 0
    for img in images:
        combined_image.paste(img, (x_offset, 0))
        x_offset += img.size[0]
    return combined_image

def convert_and_combine_tga(directory):
    # Dictionary to hold image data
    image_dict = {}
    # Walk through all files and group by base name
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.tga'):
                full_path = os.path.join(root, file)
                # Identify the base name
                base_name = file.split('_')[1].split('.')[0]
                # Append to the list of paths for this base name
                if (root, base_name) not in image_dict:
                    image_dict[(root, base_name)] = []
                image_dict[(root, base_name)].append(full_path)

    # Process each group of images
    for (root, base_name), paths in image_dict.items():
        images = [Image.open(p) for p in paths]
        if len(images) > 1:
            combined_image = combine_images(images)
            file_name_map = {
                "communism": "Flag_Communists",
                "democratic": "Flag_Capitalists",
                "fascism": "Flag_Fascists",
                "neutrality": "Flag_Default"
            }
            new_file_name = file_name_map.get(base_name, f"Flag_{base_name.capitalize()}")
            combined_image.save(os.path.join(root, new_file_name + ".png"))
            print(f"Combined and saved images to {os.path.join(root, new_file_name + '.png')}")
        else:
            # Only one image, save it directly under the new naming scheme
            image = images[0]
            new_file_name = {
                "communism": "Flag_Communists.png",
                "democratic": "Flag_Capitalists.png",
                "fascism": "Flag_Fascists.png",
                "neutrality": "Flag_Default.png"
            }.get(base_name, f"Flag_{base_name.capitalize()}.png")
            image.save(os.path.join(root, new_file_name))
            os.remove(paths[0])  # Remove the original TGA file only if not combined
            print(f"Converted and removed {paths[0]}, saved new file to {os.path.join(root, new_file_name)}")

# Get the directory where this script is located
start_directory = os.path.dirname(os.path.realpath(__file__))
convert_and_combine_tga(start_directory)
