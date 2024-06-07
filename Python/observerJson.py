import json
import time
from abc import ABCMeta, abstractmethod
import os


class FileObserver(metaclass=ABCMeta):
    @abstractmethod
    def update(self, new_data):
        pass


class JSONFileWatcher:
    def __init__(self, file_path):
        self.file_path = file_path
        self.last_data = self.read_file()
        self.observers = []

    def read_file(self):
        if not os.path.exists(self.file_path):
            return {}
        with open(self.file_path, 'r') as file:
            return json.load(file)

    def addObserver(self, observer):
        self.observers.append(observer)

    def deleteObserver(self, observer):
        self.observers.remove(observer)

    def notifyObservers(self, new_data):
        for observer in self.observers:
            observer.update(new_data)

    def watch(self):
        while True:
            time.sleep(1)
            current_data = self.read_file()
            if current_data != self.last_data:
                self.notifyObservers(current_data)
                self.last_data = current_data


class JSONFileWriter(FileObserver):
    def __init__(self, output_file_path):
        self.output_file_path = output_file_path

    def update(self, new_data):
        with open(self.output_file_path, 'w') as file:
            json.dump(new_data, file, indent=4)
        print(f"Changes detected and written to {self.output_file_path}")


if __name__ == '__main__':
    input_file_path = 'file1.json'
    output_file_path = 'file2.json'

    watcher = JSONFileWatcher(input_file_path)
    writer = JSONFileWriter(output_file_path)

    watcher.addObserver(writer)
    watcher.watch()
