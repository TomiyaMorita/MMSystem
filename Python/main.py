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
    def updateState(cls, whitchstate,inState={},exState={}):
        #更新前のステータスbeforedictに保存
        cls.in_bstate = cls.in_ustate.copy()
        #全てのステータスをalldictに統合
        cls.in_ustate.update(inState)
        cls.ex_bstate = cls.ex_ustate.copy()
        cls.ex_ustate.update(exState)
        match whitchstate:
            case "plc":
                return cls.plcUpdated(cls)
            case "controle":
                return cls.controleUpdated(cls)
            case "order":
                return cls.nextOrderUpdated(cls)
    def plcUpdated(self):
        wadr = [0]
        adr = [0] * 250
        if not self.in_ustate.get("machineReady", True):  #物理非常停止時
            self.ex_ustate.update(runMode="hardwareEmergency",machineEmergency=True)
        elif not self.in_bstate.get("machineReady", True) and self.in_ustate.get("machineReady", False):
            self.ex_ustate.update(runMode="autoOperationStop",machineEmergency = False)
        if self.ex_ustate.get("errorNum", 0) != 0 :    #エラー発生時
            self.ex_ustate["runMode"]="errorEmergency"
        if self.in_ustate.get("plcConnectError", False):
            self.ex_ustate["runMode"]="PLCConnectError!"
        #ステータスの定期更新により実行されるモード
        if self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 :
            self.ex_ustate.update(DrinkBotMotionHandler.awaitingupdate)   #更新待機中のステートをアップデート
            DrinkBotMotionHandler.awaitingupdate.clear()

             ###自動動作停止中###
            if self.ex_ustate.get("runMode","")=="stanbyStopOperation":
                if self.in_bstate.get("makingDrinks", False) and not self.in_ustate.get("makingDrinks", True) and self.ex_ustate.get("autoMode", False):    #搬送レーンにあるドリンクがなくなり次第自動動作停止
                    adr[7] = 1
                    wadr[0] = 2
                    self.ex_ustate.update(runMode="sequenceStopStanby")
            if self.ex_ustate.get("runMode","")=="sequenceStopStanby":
                if not self.in_ustate.get("makingDrinks", True) and not self.ex_ustate.get("operating", True):    #全動作終了後、自動動作シーケンス終了
                    adr[7] = 2
                    wadr[0] = 2
                    self.ex_ustate.update(runMode = "autoOperationStop")
            ###自動動作開始中###  
            if not self.ex_ustate.get("autoMode", True):
                # if not cls.in_bstate.get("glassManualRemovalCompleted", True) and cls.in_ustate.get("glassManualRemovalCompleted", False):   
                if not self.in_bstate.get("glassManualRemovalCompleted", True) and self.in_ustate.get("glassManualRemovalCompleted", False):  
                    if not self.in_ustate.get("glassElevatorSensor", True):   #昇降部から手動グラス取り出し完了
                        self.ex_ustate.update(glassManualRemoving = False,runMode = "waitingGlassRemoved")
                    elif self.in_ustate.get("glassElevatorSensor", False):
                        self.ex_ustate.update(runMode = "elevatorError!")
                if self.in_bstate.get("conveyourDrinkSensor", False) and not self.in_ustate.get("conveyourDrinkSensor", True)  and not self.ex_ustate.get("glassManualRemoving",False) and not self.in_ustate.get("glassManualRemovalCompleted", True): #搬送部からグラス取り除き完了
                        self.ex_ustate.update(drinkRemovedError=False,runMode = "autoOperationStop")
            if self.in_ustate.get("restartError", False):
                 self.ex_ustate.update(runMode = "restartError",drinkRemovedError=True)
            ###注文受付可否###
            if self.in_ustate.get("plcOrderReady", False) and self.ex_ustate.get("autoMode", False):
                self.ex_ustate.update(orderReady = True)
            elif not self.in_ustate.get("plcOrderReady", True):
                self.ex_ustate.update(orderReady = False)  
            ###ドリンクリセット時###
            if not self.ex_bstate.get("drinkReseted", True) and self.ex_ustate.get("drinkReseted", False):
                 self.ex_ustate.update(runMode = "PLCdrinkReseted")         
            
            ###中間管理システム開始時動作判定###
            if self.ex_ustate.get("autoMode", False) and self.ex_ustate["runMode"]=='':
                self.ex_ustate.update(runMode = "autoOperation")
            elif not self.ex_ustate.get("autoMode", True) and self.ex_ustate["runMode"]=='':
                self.ex_ustate.update(runMode = "autoOperationStop")
            ###非常停止ボタン解除時動作判定###
            
            ###常時###
            if self.ex_bstate.get("iceRequest", False) and not self.ex_ustate.get("iceRequest", True):  #氷減少センサーがオンからオフになったとき、氷管理カウンターを0に戻す
                DrinkBotMotionHandler.icelimitcount = 0
        wadr += adr
        return wadr

    def controleUpdated(self):
        wadr = [0]
        adr = [0] * 250
        ###非常停止指示時###
        if self.in_ustate.get("controleMode", "")=="softwareEmergency": #ソフトウェア非常停止モードの時の動作
            adr[7] = 1
            wadr[0] = 2
            DrinkBotMotionHandler.awaitingupdate.update(runMode="softwareEmergency")
        ###エラーリセット指示時###
        if self.in_ustate.get("controleMode", "")=="errorReset":    
            adr[1] = 1
            wadr[0] = 2
            DrinkBotMotionHandler.awaitingupdate.update(runMode="stanbyErrorReaet")
        if self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 :    
            ###自動動作開始指示時###
            if self.in_ustate.get("controleMode", "")=="autoModeStart" and not self.ex_ustate.get("operating", True):
                if not self.in_ustate.get("glassElevatorSensor", True) and not self.in_ustate.get("conveyourDrinkSensor", True):  #昇降機と搬送機のセンサーがどちらもオフなら、自動運転開始
                    adr[6] = 1
                    wadr[0] = 2
                    DrinkBotMotionHandler.awaitingupdate.update(glassManualRemoving = False, drinkRemovedError = False, runMode = "autoOperation")
        
                elif self.in_ustate.get("glassElevatorSensor", False):   #自動動作開始したが、グラスが昇降機上にあるため手動グラス取り出し開始
                    adr[4] = 1
                    wadr[0] = 2
                    DrinkBotMotionHandler.awaitingupdate.update(glassManualRemoving = True , runMode="stanbyRemoveGlass")
            ###自動動作停止指示時###
            if self.in_ustate.get("controleMode", "")=="autoModeStop" :    
                if self.in_ustate.get("makingDrinks", False) :   #自動動作停止指示がされたがドリンク製作中なら、ドリンク製作終了スタンバイモードへ
                    DrinkBotMotionHandler.awaitingupdate.update(runMode = "stanbyStopOperation")
                elif not self.in_ustate.get("makingDrinks", False) and self.ex_ustate.get("autoMode", True):     #搬送レーンにドリンクが無く、自動動作中なら自動動作シーケンス停止
                    adr[7] = 1
                    wadr[0] = 2
                    DrinkBotMotionHandler.awaitingupdate.update(runMode = "sequenceStopStanby")
                elif not self.in_ustate.get("makingDrinks", False) and not self.ex_ustate.get("autoMode", False):     #搬送レーンにドリンクが無く、自動動作中なら自動動作シーケンス停止
                    adr[7] = 2
                    wadr[0] = 2
                    DrinkBotMotionHandler.awaitingupdate.update(runMode = "autoOperationStop")

            ###ドリンク取り除き完了指示時###
            if self.in_ustate.get("controleMode", "")=="drinkRemoved" :
                if not self.ex_ustate.get("operating", True) and not self.in_ustate.get("conveyourDrinkSensor", True):   #ドリンクリセット指示がされた
                    adr[3] = 1  #ドリンク取り除き完了
                    adr[7] = 2  #自動動作シーケンス終了
                    wadr[0] = 2    
                    DrinkBotMotionHandler.awaitingupdate.update(runMode = "PLCdrinkResetStanby",drinkRemovedError = False)
                elif self.in_ustate.get("conveyourDrinkSensor", False):
                    DrinkBotMotionHandler.awaitingupdate.update(drinkRemovedError = True , runMode="onConveyorError")
            
            ###ポンプON指示時###
            if self.in_ustate.get("controleMode", "")=="manualPumpON" :
                if not self.ex_ustate.get("autoMode", True) :   #ポンプ手動運転ON
                    adr[8] = 1
                    pumpnum = (self.in_ustate.get("manualPumpNum", 0)-1) * 5 + 15
                    adr[pumpnum] = 1
                    wadr[0] = 2
                    DrinkBotMotionHandler.awaitingupdate.update(runMode = "manualPumpON")
            
            ###ポンプOFF指示時###
            if self.in_ustate.get("controleMode", "")=="manualPumpOFF" :
                if not self.ex_ustate.get("autoMode", True) :  #ポンプ手動運転OFF
                    adr[8] = 0
                    wadr[0] = 2
                    DrinkBotMotionHandler.awaitingupdate.update(runMode = "manualPumpOFF")
                    # cls.awaitingupdate["runMode"] = "maintenance"
        wadr += adr
        return wadr
    def nextOrderUpdated(self):
        wadr = [0]
        adr = [0] * 250
        if  self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 and self.ex_ustate.get("autoMode", False):
            if self.in_ustate.get("useIce", False) and self.in_ustate.get("iceRequest", False):   #氷を使用するドリンクで、氷補充がONの時
                DrinkBotMotionHandler.icelimitcount += 1
                if self.icelimitcount > 100000:
                    DrinkBotMotionHandler.awaitingupdate.update(iceNone = True)
            glasslist = self.in_ustate.get("glassLaneNum", [])
            random.shuffle(glasslist)   #グラスグループをランダムにシャッフル
            glasslanecount = 0

            for i in range(len(glasslist)):
                if self.in_ustate.get("glassSensing", [])[glasslist[i]-1]:  #候補のグラスレーンにグラスが存在するかの確認
                    useglass = glasslist[i]   #使用するグラスの決定
                else:
                    glasslanecount += 1
            if glasslanecount == len(glasslist):    #使用するグラスがない場合、glassNone=trueにしてstate.jsonに書き込んで報告
                DrinkBotMotionHandler.awaitingupdate.update(glassNone = True)
            else:
                DrinkBotMotionHandler.awaitingupdate.update(glassNone = False)

            # if not cls.alldict.get("iceNone", True) and not cls.alldict.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
            if not DrinkBotMotionHandler.awaitingupdate.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
                wadr[0] = 2
                adr[2] = 1
                adr[5] = self.in_ustate.get("completionLaneNum", 0)
                adr[10] = self.in_ustate.get("orderNum", 0)
                adr[11] = useglass
                adr[12] = 1 if self.in_ustate.get("useIce", False) else 0  #useIceがtrueなら氷入れる,1=氷あり
                drinks = self.in_ustate.get('drinks', [])
                for i in range(len(drinks)): #使用する材料の数だけループ
                    firstArynum = 15
                    drinknum = firstArynum + 5 * (drinks[i]['pumpNum'] - 1)
                    adr[drinknum] = 1
                    adr[drinknum + 1] = drinks[i]['time']
                    print("使用ドリンク",drinknum)
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
                print("controleChanged")
                return data, 1
    elif path == "nextOrder.json":
        with open(path, 'r') as f:
            data = json.load(f)
            if data["nextOrderID"] != nextorderid:
                nextorderid = data["nextOrderID"]
                return data, 2
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
                    if mode == 1:
                        senddata = DrinkBotMotionHandler.updateState("controle",rdata)
                    elif mode ==2 :
                        senddata = DrinkBotMotionHandler.updateState("order",rdata)
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
            "autoMode": adr[4] == 1,    #自動動作中
            "operating": adr[5] == 1,   #何かしら動作しているか
            "errorNum": adr[6], #エラー番号
            "orderCompleteNum": adr[15], #注文受付済み番号
            "drinkCompletionNum": adr[16],   #ドリンク作製済み番号
            "drinkReseted": adr[17] == 1,    #ドリンクリセット完了
            "iceRequest": adr[22] == 1, #氷補充要求
            "providedLanesFull": [adr[i + 25] == 1 for i in range(len(providedLanesSensor))],    #提供レーンの満杯状況
            "plcConnectError":False,
            "plcReceiveError":False
        }
        internalPLCdata = {
            "machineReady": adr[0] == 1,    #機械動作可否
            "makingDrinks": adr[3] == 1,    #機械がドリンク製作中
            "restartError": adr[7] == 1,    #自動動作開始時エラー
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
                senddata = DrinkBotMotionHandler.updateState("plc",idata,edata)   #ステータスの更新により動作を行うアドレスを取得
                if DrinkBotMotionHandler.ex_bstate != DrinkBotMotionHandler.ex_ustate:
                    for key in DrinkBotMotionHandler.ex_bstate:
                        if key in DrinkBotMotionHandler.ex_ustate and DrinkBotMotionHandler.ex_bstate[key] != DrinkBotMotionHandler.ex_ustate[key]:
                            print("differentis",key)
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
