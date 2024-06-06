from watchdog.events import PatternMatchingEventHandler
from watchdog.observers import Observer
import threading
import os
import time
import json
import testPLC
import KVKLE02mcp
import queue
import random
from collections import deque

operatingMode = 1   #0:PLC使用　1:テスト用ダミーPLCプログラム使用
# ミューテックスの作成
lock = threading.Lock()
# キューの作成（情報を送信するため）
info_queue = queue.Queue()
# 共有リソース# 保持するデータの最大長を設定
shared_data = deque(maxlen=10)
controleid = 0
nextorderid = 0

class DrinkBotMotionHandler:
    in_ustate = {}
    in_bstate = {}
    ex_bstate = {}
    ex_ustate = {}
    awaitingupdate = {}
    icelimitcount = 0
    @classmethod
    def update_instate(cls, in_state):
        #更新前のステータスbeforedictに保存
        cls.in_bstate = cls.in_ustate.copy()
        #全てのステータスをalldictに統合
        cls.in_ustate.update(in_state)

    @classmethod
    def checkUpdateState(cls, ex_state):
        cls.ex_bstate = cls.ex_ustate.copy()
        cls.ex_ustate.update(ex_state)

    @classmethod
    def motion_data(cls, mode):
        wadr = [0]
        adr = [0] * 250
        machineStopFlag = True
      
        if mode == 1:
            if not cls.in_ustate.get("machineReady", True):  #物理非常停止時
                machineStopFlag = False
                cls.ex_ustate.update(runMode="hardwareEmergency",machineEmergency=True)
            elif not cls.in_bstate.get("machineReady", True) and cls.in_ustate.get("machineReady", False):
                cls.ex_ustate.update(runMode="autoOperationStop",machineEmergency = False)
            if cls.ex_ustate.get("errorNum", 0) != 0 :    #エラー発生時
                machineStopFlag = False
                cls.ex_ustate["runMode"]="errorEmergency"
            if cls.in_ustate.get("plcConnectError", False):
                cls.ex_ustate["runMode"]="PLCConnectError!"
                machineStopFlag = False
        #ステータスの定期更新により実行されるモード
        if machineStopFlag and mode == 1:
            cls.ex_ustate.update(cls.awaitingupdate)   #更新待機中のステートをアップデート
            cls.awaitingupdate.clear()
             ###自動動作停止中###
            if cls.in_bstate.get("makingDrinks", False) and not cls.in_ustate.get("makingDrinks", True) and cls.ex_ustate.get("autoMode", False):    #搬送レーンにあるドリンクがなくなり次第自動動作停止
                adr[7] = 1
                wadr[0] = 2
                cls.ex_ustate.update(runMode="sequenceStopStanby")
            elif not cls.in_ustate.get("makingDrinks", True) and cls.ex_bstate.get("operating", False) and not cls.ex_ustate.get("operating", True):    #全動作終了後、自動動作シーケンス終了
                adr[7] = 2
                wadr[0] = 2
                cls.ex_ustate.update(runMode = "autoOperationStop")
            ###自動動作開始中###  
            if not cls.ex_ustate.get("autoMode", True):
                if cls.in_ustate.get("glassManualRemovalCompleted", False):   #搬送機から手動グラス取り出し時完了
                        cls.ex_ustate.update(glassManualRemoving = False,runMode = "waitingGlassRemoved")
                if cls.in_bstate.get("conveyourDrinkSensor", False) and not cls.in_ustate.get("conveyourDrinkSensor", True)  and not cls.ex_ustate.get("glassManualRemoving",False) and not cls.in_ustate.get("glassManualRemovalCompleted", True):
                        cls.ex_ustate.update(drinkRemovedError=False,runMode = "autoOperationStop")
            if cls.in_ustate.get("restartError", False):
                 cls.ex_ustate.update(runMode = "restartError",drinkRemovedError=True)
            ###注文受付可否###
            if cls.in_ustate.get("plcOrderReady", False) and cls.ex_ustate.get("autoMode", False):
                cls.ex_ustate.update(orderReady = True)
            elif not cls.in_ustate.get("plcOrderReady", True):
                cls.ex_ustate.update(orderReady = False)  
            ###ドリンクリセット時###
            if not cls.ex_bstate.get("drinkReseted", True) and cls.ex_ustate.get("drinkReseted", False):
                 cls.ex_ustate.update(runMode = "PLCdrinkReseted")         
            
            ###中間管理システム開始時動作判定###
            if cls.ex_ustate.get("autoMode", False) and cls.ex_ustate["runMode"]=='':
                cls.ex_ustate.update(runMode = "autoOperation")
            elif not cls.ex_ustate.get("autoMode", True) and cls.ex_ustate["runMode"]=='':
                cls.ex_ustate.update(runMode = "autoOperationStop")
            ###非常停止ボタン解除時動作判定###
            
            ###常時###
            if cls.ex_bstate.get("iceRequest", False) and not cls.ex_ustate.get("iceRequest", True):  #氷減少センサーがオンからオフになったとき、氷管理カウンターを0に戻す
                cls.icelimitcount = 0


        #モバイルオーダーシステムからのcontrole.json更新指示で変更になる動作モード
        if machineStopFlag and mode == 2:
            ###非常停止指示時###
            if cls.in_ustate.get("controleMode", "")=="softwareEmergency": #ソフトウェア非常停止モードの時の動作
                adr[7] = 1
                wadr[0] = 2
                cls.awaitingupdate.update(runMode="softwareEmergency")
            ###エラーリセット指示時###
            if cls.in_ustate.get("controleMode", "")=="errorReset":    
                adr[1] = 1
                wadr[0] = 2
                cls.awaitingupdate.update(runMode="stanbyErrorReaet")
            ###自動動作開始指示時###
            if cls.in_ustate.get("controleMode", "")=="autoModeStart" and not cls.ex_ustate.get("operating", True):
                if not cls.in_ustate.get("glassElevatorSensor", True) and not cls.in_ustate.get("conveyourDrinkSensor", True):  #昇降機と搬送機のセンサーがどちらもオフなら、自動運転開始
                    adr[6] = 1
                    wadr[0] = 2
                    cls.awaitingupdate.update(glassManualRemoving = False, drinkRemovedError = False, runMode = "autoOperation")
        
                elif cls.in_ustate.get("conveyourDrinkSensor", False):    #自動動作開始したが、グラスが搬送機上にある
                    cls.awaitingupdate.update(drinkRemovedError = True , runMode="onConveyorError")
                elif cls.in_ustate.get("glassElevatorSensor", False):   #自動動作開始したが、グラスが昇降機上にあるため手動グラス取り出し開始
                    adr[4] = 1
                    wadr[0] = 2
                    cls.awaitingupdate.update(glassManualRemoving = True , runMode="stanbyRemoveGlass")
            ###自動動作停止指示時###
            if cls.in_ustate.get("controleMode", "")=="autoModeStop" and cls.ex_ustate.get("operating", False):    
                if cls.in_ustate.get("makingDrinks", False) :   #自動動作停止指示がされたがドリンク製作中なら、ドリンク製作終了スタンバイモードへ
                    cls.awaitingupdate.update(runMode = "stanbyStopOperation")
                elif not cls.in_ustate.get("makingDrinks", False) :     #搬送レーンにあるドリンクが無ければ自動動作停止
                    adr[7] = 1
                    wadr[0] = 2
                    cls.awaitingupdate.update(runMode = "sequenceStopStanby")

            ###ドリンク取り除き完了指示時###
            if cls.in_ustate.get("controleMode", "")=="drinkRemoved" :
                if not cls.ex_ustate.get("operating", True) and not cls.in_ustate.get("conveyourDrinkSensor", True):   #ドリンクリセット指示がされた
                    adr[7] = 2  #自動動作シーケンス終了
                    adr[3] = 1  #ドリンク取り除き完了
                    wadr[0] = 2    
                    cls.awaitingupdate.update(runMode = "PLCdrinkResetStanby",drinkRemovedError = False)
                elif cls.in_ustate.get("conveyourDrinkSensor", False):
                    cls.awaitingupdate.update(drinkRemovedError = True , runMode="onConveyorError")
            
            ###ポンプON指示時###
            if cls.in_ustate.get("controleMode", "")=="manualPumpON" :
                if not cls.ex_ustate.get("autoMode", True) :   #ポンプ手動運転ON
                    adr[8] = 1
                    pumpnum = (cls.in_ustate.get("manualPumpNum", 0)-1) * 5 + 15
                    adr[pumpnum] = 1
                    wadr[0] = 2
                    cls.awaitingupdate.update(runMode = "manualPumpON")
            
            ###ポンプOFF指示時###
            if cls.in_ustate.get("controleMode", "")=="manualPumpOFF" :
                if not cls.ex_ustate.get("autoMode", True) :  #ポンプ手動運転OFF
                    adr[8] = 0
                    wadr[0] = 2
                    cls.awaitingupdate.update(runMode = "manualPumpOFF")
                    # cls.awaitingupdate["runMode"] = "maintenance"

        #モバイルオーダーシステムからのnextOrder.json更新指示で変更になる動作モード
        if machineStopFlag and cls.ex_ustate.get("autoMode", False) and mode == 3:
            if cls.in_ustate.get("useIce", False) and cls.in_ustate.get("iceRequest", False):   #氷を使用するドリンクで、氷補充がONの時
                cls.icelimitcount += 1
                if cls.icelimitcount > 100000:
                    cls.awaitingupdate.update(iceNone = True)
            glasslist = cls.in_ustate.get("glassLaneNum", [])
            random.shuffle(glasslist)   #グラスグループをランダムにシャッフル
            glasslanecount = 0

            for i in range(len(glasslist)):
                if cls.in_ustate.get("glassSensing", [])[glasslist[i]-1]:  #候補のグラスレーンにグラスが存在するかの確認
                    useglass = glasslist[i]   #使用するグラスの決定
                else:
                    glasslanecount += 1
            if glasslanecount == len(glasslist):    #使用するグラスがない場合、glassNone=trueにしてstate.jsonに書き込んで報告
                cls.awaitingupdate.update(glassNone = True)
            else:
                cls.awaitingupdate.update(glassNone = False)

            # if not cls.alldict.get("iceNone", True) and not cls.alldict.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
            if not cls.awaitingupdate.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
                wadr[0] = 2
                adr[2] = 1
                adr[5] = cls.in_ustate.get("completionLaneNum", 0)
                adr[10] = cls.in_ustate.get("orderNum", 0)
                adr[11] = useglass
                adr[12] = 0 if cls.in_ustate.get("useIce", False) else 1  #useIceがtrueなら氷入れる,0=氷あり
                drinks = cls.in_ustate.get('drinks', [])
                for i in range(len(drinks)): #使用する材料の数だけループ
                    firstArynum = 15
                    drinknum = firstArynum + 5 * (drinks[i]['pumpNum'] - 1)
                    adr[drinknum] = 1
                    adr[drinknum + 1] = drinks[i]['time']
        # print(cls.ex_ustate["runMode"])
        wadr += adr
        return wadr

def jsonData(path):
    global controleid
    global nextorderid
    if path == "controle.json":
        with open(path, 'r') as f:
            data = json.load(f)
            if data["controleID"] != controleid:
                controleid = data["controleID"]
                return data, 2
    elif path == "nextOrder.json":
        with open(path, 'r') as f:
            data = json.load(f)
            if data["nextOrderID"] != nextorderid:
                nextorderid = data["nextOrderID"]
                return data, 3
    return None, None

class MyFileWatchHandler(PatternMatchingEventHandler):
    def on_modified(self, event):
        senddata = []
        with lock:
            filepath = event.src_path
            if self.checkjsonfile(filepath):
                filename = os.path.basename(filepath)
                #jsonファイルの存在確認
                rdata, mode = jsonData(filename)
                if rdata is not None:
                    DrinkBotMotionHandler.update_instate(rdata)    #ステータス情報更新
                    senddata = DrinkBotMotionHandler.motion_data(mode)
                    info_queue.put(senddata)

    def checkjsonfile(self, path):
        try:
            with open(path, 'r') as c:
                return bool(json.load(c))
        except json.JSONDecodeError:
            return False

def updateJson():
    #jsonファイルの更新を監視する関数
    # 対象ディレクトリ
    DIR_WATCH = '.'
    PATTERNS = ['*.json']
    # 対象ファイルパスのパターン
    event_handler = MyFileWatchHandler(PATTERNS)
    observer = Observer()
    observer.schedule(event_handler, DIR_WATCH, recursive=True)
    observer.start()
    print("start")
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()
    print("end")

def checkPLC():
    while True:
        time.sleep(1)   # 定期的な間隔で実行（例：5秒ごと）
        with lock:
            info_queue.put([1])

def resetState():
    return {
        "machineEmergency":False, #非常停止ボタン押下
        "runMode":"",  #現在実行中のモード
        "orderReady":False,
        "glassManualRemoving":False,    #手動グラス取り出し中true
        "drinkRemovedError":False,  #自動動作開始指示後、グラスが搬送機上から取り除かれていない
        "glassNone":False,  #使用するグラスがグラスラックにない
        "iceNone":False,    #氷がない
        "plcConnectError":False,    #PLCとの接続エラー
        "plcReceiveError":False,    #PLCのデータ受け取りエラー
        "orderError":False, #注文内容が規定外
        "orderErrorNum":0  #規定外だった注文内容の注文番号
    }

def convertPlcState(errorcode,adr):
    if errorcode == 0:
        providedLanesSensor = [False] * 3
        glasslist = [False] * 15
        externalPLCdata = {
            "autoMode": adr[2] == 1,    #自動動作中
            "operating": adr[3] == 1,   #何かしら動作しているか
            "errorNum": adr[4], #エラー番号
            "orderCompleteNum": adr[7], #注文受付済み番号
            "drinkCompletionNum": adr[8],   #ドリンク作製済み番号
            "drinkReseted": adr[9] == 1,    #ドリンクリセット完了
            "iceRequest": adr[13] == 1, #氷補充要求
            "providedLanesFull": [adr[i + 14] == 1 for i in range(len(providedLanesSensor))],    #提供レーンの満杯状況
            "plcConnectError":False,
            "plcReceiveError":False
        }
        internalPLCdata = {
            "machineReady": adr[0] == 1,    #機械動作可否
            "makingDrinks": adr[1] == 1,    #機械がドリンク製作中
            "restartError": adr[5] == 1,    #自動動作開始時エラー
            "plcOrderReady": adr[6] == 1,  #注文受付可能
            "conveyourDrinkSensor": adr[11] == 1,   #搬送機上のセンサー値
            "glassManualRemovalCompleted": adr[10] == 1,     #搬送機グラス取り出し完了
            "glassElevatorSensor": adr[12] == 1,    #搬送機昇降部センサー値
            "glassSensing": [adr[i + 17] == 1 for i in range(len(glasslist))]   #各グラスラックのセンサー状況
        }
    elif errorcode == 408:
        externalPLCdata = {
            "plcConnectError":True
        }
        internalPLCdata= {

        }
    else:
        externalPLCdata = {
            "plcReceiveError":True
        }
        internalPLCdata= {

        }
    return externalPLCdata, internalPLCdata


def info_sender():
    while True:
        # キューから情報を取得して送信
        info = info_queue.get()
        newjsondata={}
        if info:
            # ここで情報を送信する（例：ログに書き込む、ネットワーク送信など
            if info[0] == 1:    #PLCから定期ステータス取得・json更新
                if operatingMode == 0:
                    rdata = KVKLE02mcp.toPLC(info)  #PLCからステータス取得
                elif operatingMode == 1:
                    rdata = testPLC.test(info)  #PLCからステータス取得
                edata, idata = convertPlcState(rdata[0],rdata[1:])   #PLCから取得したデータをシステム内で使用する辞書型にコンバート
                DrinkBotMotionHandler.update_instate(idata)    #最新のPLCステータスにシステム内辞書を更新
                DrinkBotMotionHandler.checkUpdateState(edata)   #state.jsonに書き込む必要のあるステータスのみの更新確認
                senddata = DrinkBotMotionHandler.motion_data(info[0])   #ステータスの更新により動作を行うアドレスを取得
                if DrinkBotMotionHandler.ex_bstate != DrinkBotMotionHandler.ex_ustate:
                    # print("stateChange!!")
                    print("runMode",DrinkBotMotionHandler.ex_ustate["runMode"])
                    with open("state.json", 'r') as f:
                        jdata = json.load(f)
                    stateid=jdata.get("stateID", 0)
                    stateid+=1 if stateid < 100000 else 0
                    newjsondata.update(stateID = stateid)
                    newjsondata={**newjsondata,**DrinkBotMotionHandler.ex_ustate}
                    with open('state.json', 'w') as f:
                        json.dump(newjsondata, f, indent=2, ensure_ascii=False)
                    if senddata[0] == 2:    #機械に操作指示があるなら送信
                        if operatingMode == 0:
                            KVKLE02mcp.toPLC(senddata)
                        elif operatingMode == 1:
                            testPLC.test(senddata)
            elif info[0] == 2:
                if operatingMode == 0:
                    KVKLE02mcp.toPLC(info)
                elif operatingMode == 1:
                    testPLC.test(info)
        info_queue.task_done()

if __name__ == "__main__":
    #起動時のjsonファイル読み込み
    #最終的にはinit用のステータス初期化プログラムを別で作成
    if operatingMode == 0:
        init=[1]
        data0=KVKLE02mcp.toPLC(init)
        edata, idata = convertPlcState(data0[0],data0[1:])
    elif operatingMode == 1:
        with open("plcState.json", 'r') as f:
            data0 = json.load(f)
        edata, idata = convertPlcState(0,data0["plcdata"])     
    with open("controle.json", 'r') as f:
        data1 = json.load(f)
    with open("nextOrder.json", 'r') as f:
        data2 = json.load(f)
    rdata = resetState()
    convertedData = {**rdata, **edata, **idata}
    stateData={**rdata, **edata}
    alldata = {**convertedData, **data1, **data2}
    
    DrinkBotMotionHandler.alldict = alldata
    DrinkBotMotionHandler.ex_ustate = stateData
    # DrinkBotMotionHandler.ex_bstate = stateData

    t1 = threading.Thread(target=updateJson)
    t2 = threading.Thread(target=checkPLC)
    t3 = threading.Thread(target=info_sender, daemon=True)

    t1.start()
    t2.start()
    t3.start()

    t1.join()
    t2.join()
    t3.join()
