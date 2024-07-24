import os

def delete_tga_files(directory):
    # Counter for deleted files
    count = 0

    # Walk through all directories and files in the specified directory
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.lower().endswith('.tga'):
                file_path = os.path.join(root, file)
                try:
                    os.remove(file_path)
                    print(f"Deleted: {file_path}")
                    count += 1
                except Exception as e:
                    print(f"Failed to delete {file_path}: {e}")

    # Feedback on completion
    print(f"Total files deleted: {count}")

if __name__ == '__main__':
    current_directory = os.path.dirname(os.path.realpath(__file__))
    delete_tga_files(current_directory)
