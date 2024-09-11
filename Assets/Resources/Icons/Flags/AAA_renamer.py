import os

def trim_s_from_filenames(directory):
    for dirpath, dirnames, filenames in os.walk(directory):
        for filename in filenames:
            # Split the filename into the name and the extension
            base_name, extension = os.path.splitext(filename)
            # Check if the base name ends with 's'
            if base_name.endswith('ists'):
                new_base_name = base_name[:-1]  # Remove the last 's'
                new_filename = new_base_name + extension  # Reattach the extension
                old_filepath = os.path.join(dirpath, filename)
                new_filepath = os.path.join(dirpath, new_filename)
                os.rename(old_filepath, new_filepath)
                print(f"Renamed: {old_filepath} -> {new_filepath}")

if __name__ == "__main__":
    # Get the directory where the script is located
    script_directory = os.path.dirname(os.path.abspath(__file__))
    
    # Start the renaming process
    trim_s_from_filenames(script_directory)
