#!/usr/bin/env python
# -*- coding: utf-8 -*-

import sys
import time
import threading

def th_func():
    while True:
        print("thread", flush=True)
        time.sleep(1)
def th_func2():
    while True:
        print("thread2", flush=True)
        time.sleep(1)
def main():
    try:
        th = threading.Thread(target=th_func)
        th.daemon= True
        th.start()
        th2 = threading.Thread(target=th_func2)
        th2.daemon= True
        th2.start()
        while th.is_alive() and th2.is_alive() :
            print("main", flush=True)
            time.sleep(1)
    except KeyboardInterrupt:
        print("except KeyboardInterrupt")
        sys.exit()

if __name__ == "__main__":
    main()