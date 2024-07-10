from watchdog.events import PatternMatchingEventHandler
from watchdog.observers import Observer
import threading
import os
import time
import json
import testPLC
import KVKLE02mcp
import queue
import motionHandler
from collections import deque
import sys
import signal
#スレッド停止イベント管理
stop_event = threading.Event()
operatingMode = 1   #0:PLC使用　1:テスト用ダミーPLCプログラム使用
# ミューテックスの作成
lock = threading.Lock()
# キューの作成（情報を送信するため）
info_queue = queue.Queue()
# 共有リソース# 保持するデータの最大長を設定
shared_data = deque(maxlen=10)

###使用するステータスの登録###
def resetInState():
    return{
        "waitingUpdate":{},
        "waittingFinishFlag":False,
        "icelimitcount":0,
        "dummyLaneCount":0
}
def resetExState():
    return {
        "machineEmergency":False, #非常停止ボタン押下
        "runMode":"",  #現在実行中のモード
        "orderReady":False,
        "drinkResetRequest":False,    #ドリンクリセット要求
        "glassRemoveRequest":False,  #グラス取り出し要求
        "glassNone":False,  #使用するグラスがグラスラックにない
        "iceNone":False,    #氷がない
        "plcConnectError":False,    #PLCとの接続エラー
        "plcReceiveError":False,    #PLCのデータ受け取りエラー
        "orderError":False, #注文内容が規定外
        "orderErrorNum":0,  #規定外だった注文内容の注文番号
    }

def convertPlcState(errorcode,adr):
    if errorcode == 0:
        providedLanesSensor = [False] * 3
        glasslist = [False] * 15
        externalPLCdata = {
            "autoMode": adr[4] == 1,    #自動動作中
            "operating": adr[5] == 1,   #何かしら動作しているか
            "errorNum": adr[6], #エラー番号
            "orderCompleteNum": adr[15], #注文受付済み番号
            "drinkCompletionNum": adr[16],   #ドリンク作製済み番号
            "drinkReseted": adr[17] == 1,    #ドリンクリセット完了
            "iceRequest": adr[22] == 1, #氷補充要求
            "providedLanesFull": [adr[i + 25] == 1 for i in range(len(providedLanesSensor))],    #提供レーンの満杯状況
            "plcConnectError":False,
            "plcReceiveError":0
        }
        internalPLCdata = {
            "machineReady": adr[0] == 1,    #機械動作可否
            "makingDrinks": adr[3] == 1,    #機械がドリンク製作中
            "plccontinueError": adr[7] == 1,    #自動動作継続不可エラー
            "plcOrderReady": adr[10] == 1,  #注文受付可能
            "conveyourDrinkSensor": adr[20] == 1,   #搬送機上のセンサー値
            "glassManualRemovalCompleted": adr[18] == 1,     #搬送機グラス取り出し完了
            "glassElevatorSensor": adr[21] == 1,    #搬送機昇降部センサー値
            "glassSensing": [adr[i + 30] == 1 for i in range(len(glasslist))]   #各グラスラックのセンサー状況
        }
    elif errorcode == 408:
        externalPLCdata = {
            "plcConnectError":True
        }
        internalPLCdata= {

        }
    else:
        externalPLCdata = {
            "plcReceiveError":errorcode
        }
        internalPLCdata= {

        }
    return externalPLCdata, internalPLCdata

class MyFileWatchHandler(PatternMatchingEventHandler):
    ###jsonファイルの更新により実行されるハンドラー###
    controleid = 0
    nextorderid = 0
    def on_modified(self, event):
        senddata = []
        with lock:
            filepath = event.src_path
            #jsonファイルの存在確認
            if self.checkjsonfile(filepath):
                filename = os.path.basename(filepath)
                #jsonデータの中身取得
                rdata, mode = self.jsonData(filename)
                if rdata is not None:
                    mode.append(rdata)
                    info_queue.put(mode)

    def checkjsonfile(self, path):
        try:
            with open(path, 'r') as c:
                return bool(json.load(c))
        except json.JSONDecodeError:
            return False
    def jsonData(self,path):
        mode=[0]
        if path == "controle.json":
            with open(path, 'r') as f:
                data = json.load(f)
                if data["controleID"] != self.controleid:
                    self.controleid = data["controleID"]
                    mode[0]=2
                    return data, mode
        elif path == "nextOrder.json":
            with open(path, 'r') as f:
                data = json.load(f)
                if data["nextOrderID"] != self.nextorderid:
                    self.nextorderid = data["nextOrderID"]
                    mode[0]=3
                    return data, mode
        return None, None
def updateJson(name,stop_event):
    #jsonファイルの更新を監視する関数
    # 対象ディレクトリ
    DIR_WATCH = '.'
    PATTERNS = ['*.json']
    # 対象ファイルパスのパターン
    event_handler = MyFileWatchHandler(PATTERNS)
    observer = Observer()
    observer.schedule(event_handler, DIR_WATCH, recursive=True)
    observer.start()
    try:
        while not stop_event.is_set():
            time.sleep(1)
    except Exception as e:
        # stop_event.set()
        # observer.stop()
        print(f"Thread {name} stopped with error: {e}")
        # sys.exit()
    finally:
        stop_event.set()
        observer.stop()
        observer.join()
    # observer.join()

def checkPLC(name,stop_event):
    try:
        while not stop_event.is_set():
            time.sleep(1)   # 定期的な間隔で実行（例：5秒ごと）
            with lock:
                info_queue.put([1])
    except Exception as e:
        print(f"Thread {name} stopped with error: {e}")
        stop_event.set()


def updateSender(plcdata,jsondata,plcUpdateflag,jsonUpdateflag):
    newjsondata={}
    if jsonUpdateflag:
       
        with open("state.json", 'r') as f:
            jdata = json.load(f)
            stateid=jdata.get("stateID", 0)
            stateid+=1 if stateid < 100000 else 0
            newjsondata.update(stateID = stateid)
            newjsondata={**newjsondata,**jsondata}
        with open('state.json', 'w') as f:
            json.dump(newjsondata, f, indent=2, ensure_ascii=False)
    if plcUpdateflag:    #機械に操作指示があるなら送信
        if operatingMode == 0:
            KVKLE02mcp.toPLC(plcdata)
        elif operatingMode == 1:
            testPLC.test(plcdata)
def allStateDebug(bistate,bestate,uistate,uestate):
    newjsondata={}
    beforeState={**bistate,**bestate}
    updateState={**uistate,**uestate}
    if beforeState!=updateState:
        with open("allStatus.json", 'r') as f:
            jdata = json.load(f)
            stateid=jdata.get("stateID", 0)
            stateid+=1 if stateid < 100000 else 0
            newjsondata.update(stateID = stateid)
            newjsondata={**newjsondata,**updateState}
        with open('allStatus.json', 'w') as f:
            json.dump(newjsondata, f, indent=2, ensure_ascii=False)

beforeInState={}
beforeExState={}
def info_sender(name,stop_event):
    try:
        while not stop_event.is_set():
            # キューから情報を取得して送信
            info = info_queue.get()
            s=motionHandler.DrinkBotMotionHandler()
            if info:
                # ここで情報を送信する
                if info[0] == 1:    #PLCから定期ステータス取得・json更新
                    if operatingMode == 0:
                        rdata = KVKLE02mcp.toPLC(info)  #PLCからステータス取得
                    elif operatingMode == 1:
                        rdata = testPLC.test(info)  #PLCからステータス取得
                    edata, idata = convertPlcState(rdata[0],rdata[1:])   #PLCから取得したデータをシステム内で使用する辞書型にコンバート
                    senddata, updateInState, updateExState = s.updateState("plc",beforeInState,beforeExState,idata,edata)   #ステータスの更新により動作を行うアドレスを取得 
                    judflag = True if updateExState != beforeExState else False
                    pudflag = True if senddata[0] == 2 else False
                    ###Log出し###
                    allStateDebug(beforeInState,beforeExState,updateInState,updateExState)
                    if judflag:
                        for key in beforeExState:
                            if key in updateExState and beforeExState[key] != updateExState[key]:
                                print("differentis",key)
                        print("stateChange!!")
                        print("runMode",updateExState["runMode"])
                elif info[0] == 2:
                    senddata, updateInState, updateExState = s.updateState("controle",beforeInState,beforeExState,info[1])
                    judflag = False
                elif info[0] == 3:
                    senddata, updateInState, updateExState = s.updateState("order",beforeInState,beforeExState,info[1])
                    judflag = False
                else:
                    judflag = False
                    senddata=[0]
                    updateInState = beforeInState
                    updateExState = beforeExState
                pudflag = True if senddata[0] == 2 else False
                updateSender(senddata ,updateExState ,pudflag ,judflag)
                beforeInState.update(updateInState)
                beforeExState.update(updateExState)
            info_queue.task_done()
    except Exception as e:
        print(f"Thread {name} stopped with error: {e}")
        stop_event.set()

if __name__ == "__main__":
    data0=[0]*50
    edata, idata = convertPlcState(data0[0],data0[1:])
    #起動時のjsonファイル読み込み
    with open("controle.json", 'r') as f:
        data1 = json.load(f)
    with open("nextOrder.json", 'r') as f:
        data2 = json.load(f)
    resIndata = resetInState()
    resExdata = resetExState()
    internalData = {**idata, **data1, **data2,**resIndata}
    externalData={**resExdata, **edata}
    beforeInState.update(internalData)
    beforeExState.update(externalData)
    
    # DrinkBotMotionHandler.ex_bstate = stateData
    try:
        t1 = threading.Thread(target=updateJson,args=("Thread-1",stop_event),daemon=True)
        t2 = threading.Thread(target=checkPLC,args=("Thread-2",stop_event),daemon=True)
        t3 = threading.Thread(target=info_sender, args=("Thread-3",stop_event),daemon=True)
        t1.start()
        t2.start()
        t3.start()
        while t1.is_alive() and t2.is_alive() and t3.is_alive():
            # print("main", flush=True)
            # flush=True
            time.sleep(1)
    except KeyboardInterrupt:
        print("except KeyboardInterrupt")
        sys.exit()

    # t1.join()
    # t2.join()
    # t3.join()
