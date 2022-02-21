import os
import shutil
from pathlib import Path
import re

def CreateDirectory(path):
    Path(path).mkdir(parents=True, exist_ok=True)

def DeleteDirectory(path):
    shutil.rmtree(path)

def CopyDirectory(source, destination):
    shutil.copytree(source, destination, dirs_exist_ok=True)

def CopyFile(source, destination):
    shutil.copyfile(source, destination)

def GetAllFolderFiles(folderpath):
    returnedfiles = []
    for file in os.listdir(folderpath):
        returnedfiles.append(file)
    return returnedfiles

def GetFolderFiles(folderpath, suffixes):
    returnedfiles = []
    for file in os.listdir(folderpath):
        suffix = Path(file).suffix
        if suffix in suffixes:
            returnedfiles.append(file)
    return returnedfiles

def ReadAllText(filepath):
    file = open(filepath, mode='r')
    text = file.read()
    file.close()
    return text

def WriteAllText(filepath, text):
    file = open(filepath, mode='w')
    file.write(text)
    file.close()

def AppendAllText(filepath, text):
    file = open(filepath, mode='a')
    file.write(text)
    file.close()

def FileExists(path):
    if os.path.exists(path):
        return True
    else:
        return False

def DeleteFile(path):
    os.remove(path)
	
def ParentPath(path):
	path = Path(path)
	return str(path.parent)

def CurrentPath():
    return str(Path(__file__).parent.absolute())

def RelativePath(original):
    originalreplaced = original.replace("../", "*")
    parentscount = len(originalreplaced.split("*"))-1
    if(parentscount <= 0):
        return original
    parentpath = Path(CurrentPath())
    for i in range(parentscount):
        parentpath = parentpath.parent
    returnedpath = str(parentpath) + "/" + originalreplaced.split("*")[-1]
    return returnedpath.replace("\\", "/")