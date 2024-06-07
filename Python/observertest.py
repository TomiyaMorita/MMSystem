import random
import json
import time
from abc import ABCMeta, abstractmethod


class NumberGenerator(metaclass=ABCMeta):   #観察される側
    def __init__(self):
        self.__observers = []

    def addObserver(self, observer):
        self.__observers.append(observer)

    def deleteObserver(self, observer):
        self.__observers.remove(observer)

    def notifyObserver(self):
        for o in self.__observers:
            o.update(self)

    @abstractmethod
    def getNumber(self):
        pass

    @abstractmethod
    def execute(self):
        pass


class RandomNumberGenerator(NumberGenerator):   #状態が変化したら、登録されているObserver役に伝える
    def __init__(self):
        self.__number = 0
        super(RandomNumberGenerator, self).__init__()

    def getNumber(self):
        return self.__number

    def execute(self):  #ランダムに20個の数字生成
        for _ in range(20):
            self.__number = random.randint(0, 49)
            self.notifyObserver()



class Observer(metaclass=ABCMeta):  #状態変化の監視
    @abstractmethod
    def update(self, ganerator):
        pass


class DigitObserver(Observer):
    def update(self, generator):
        print("DigitObservser: {0}".format(generator.getNumber()))
        time.sleep(0.1)


class GraphObserver(Observer):
    def update(self, generator):
        print("GraphicObserver:", end='')
        count = generator.getNumber()
        for _ in range(count):
            print('*', end='')
        print("")
        time.sleep(0.1)

def startMain():
    generator = RandomNumberGenerator()
    observer1 = DigitObserver()
    observer2 = GraphObserver()
    generator.addObserver(observer1)
    generator.addObserver(observer2)
    generator.execute()


if __name__ == '__main__':
    while(True):
        time.sleep(1)
        startMain()