import subprocess
import os
import sys
from tkinter import messagebox

def decompress_cpk(file_path, cpk_name):
    try:
        # 不再需要指定 criPakTools_path
        command = ["CriPakTools.exe", file_path, 'all']

        # 使用 Popen 而不是 run 来实时捕获输出
        process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1)
        
        # 实时读取输出并打印到控制台
        for line in iter(process.stdout.readline, ''):
            print(line, end='')  # 使用 end='' 避免打印额外的换行符

        process.stdout.close()
        return_code = process.wait()
        if return_code:
            raise subprocess.CalledProcessError(return_code, command)

        print(f"Successfully decompressed {cpk_name}")

    except subprocess.CalledProcessError as e:
        error_message = e.output.strip() if e.output else str(e)
        print(f"Error decompressing {cpk_name}: {error_message}", file=sys.stderr)
        messagebox.showerror("Error", error_message)
    except Exception as e:
        print(f"An unexpected error occurred: {e}", file=sys.stderr)
        messagebox.showerror("Error", str(e))

def select_folder_and_decompress():
    directory_path = input("请输入cpk文件所在的路径: ")
    if os.path.isdir(directory_path):
        for root, dirs, files in os.walk(directory_path):
            for file in files:
                if file.lower().endswith('.cpk'):
                    file_path = os.path.join(root, file)
                    cpk_name = os.path.splitext(os.path.basename(file_path))[0]
                    decompress_cpk(file_path, cpk_name)
    else:
        print(f"输入的不是一个有效的路径.")

if __name__ == "__main__":
    select_folder_and_decompress()