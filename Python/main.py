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

operatingMode = 0   #0:PLC使用　1:テスト用ダミーPLCプログラム使用
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
    waitingUpdate={}
    def __init__(self,in_ustate,ex_ustate):
        DrinkBotMotionHandler.in_ustate=in_ustate
        DrinkBotMotionHandler.ex_ustate = ex_ustate
    @classmethod
    def updateState(cls, whitchstate,inState={},exState={}):
        # updateflag=False
        #更新前のステータスbeforedictに保存
        cls.in_bstate = cls.in_ustate.copy()
        #全てのステータスをalldictに統合
        cls.in_ustate.update(inState)
        cls.ex_bstate = cls.ex_ustate.copy()
        cls.ex_ustate.update(exState)
        match whitchstate:
            case "plc":
                adr,udstate = cls.plcUpdated(cls)
                cls.ex_ustate.update(**udstate,**cls.waitingUpdate)
                cls.waitingUpdate.clear()
                updateflag = True if cls.ex_ustate != cls.ex_bstate else False
            case "controle":
                adr,udstate = cls.controleUpdated(cls)
                cls.waitingUpdate.update(udstate)
                # print("controle",cls.ex_ustate["drinkRemovedError"])
                updateflag = True if cls.ex_ustate != cls.ex_bstate else False
            case "order":
                adr,udstate = cls.nextOrderUpdated(cls)
                cls.waitingUpdate.update(udstate)
                updateflag = True if cls.ex_ustate != cls.ex_bstate else False
            case _:
                adr = [0] * 251
        # print("updateflag",updateflag)
        # print("cls.ex_bstate",cls.ex_bstate)
        # print("cls.ex_ustate",cls.ex_ustate)
        return adr,cls.ex_ustate,updateflag    
    def plcUpdated(self):
        wadr = [0]
        adr = [0] * 250
        udstate = {}
        if not self.in_ustate.get("machineReady", True):  #物理非常停止時
            udstate.update(runMode="hardwareEmergency",machineEmergency=True)
            self.in_ustate.update(waittingFinishFlag=False)
        elif not self.in_bstate.get("machineReady", True) and self.in_ustate.get("machineReady", False):
            udstate.update(runMode="autoOperationStop",machineEmergency = False)
        if self.ex_ustate.get("errorNum", 0) != 0 :    #エラー発生時
            self.ex_ustate["runMode"]="errorEmergency"
        if self.in_ustate.get("plcConnectError", False):
            self.ex_ustate["runMode"]="PLCConnectError!"
        if self.in_ustate.get("restartError", False):
            udstate.update(runMode = "restartError",drinkRemovedError=True)
            self.in_ustate.update(waittingFinishFlag=False)
        #ステータスの定期更新により実行されるモード
        if self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 :

             ###自動動作停止中###
            if self.ex_ustate.get("runMode","")=="stanbyStopOperation":
                #搬送レーンにあるドリンクがなくなり次第自動動作停止
                if self.in_bstate.get("makingDrinks", False) and not self.in_ustate.get("makingDrinks", True) and self.ex_ustate.get("autoMode", False):    
                    adr[7] = 1
                    wadr[0] = 2
                    self.in_ustate.update(waittingFinishFlag=True)
                    udstate.update(runMode="sequenceStopStanby")
            if self.ex_ustate.get("runMode","")=="sequenceStopStanby":
                if not self.in_ustate.get("makingDrinks", True) and not self.ex_ustate.get("operating", True):    #全動作終了後、自動動作シーケンス終了
                    adr[7] = 2
                    wadr[0] = 2
                    self.in_ustate.update(waittingFinishFlag=False)
                    udstate.update(runMode = "autoOperationStop")
            ###自動動作開始中###  
            if not self.ex_ustate.get("autoMode", True):
                # if not cls.in_bstate.get("glassManualRemovalCompleted", True) and cls.in_ustate.get("glassManualRemovalCompleted", False):   
                if not self.in_bstate.get("glassManualRemovalCompleted", True) and self.in_ustate.get("glassManualRemovalCompleted", False):  
                    if not self.in_ustate.get("glassElevatorSensor", True):   #昇降部から手動グラス取り出し完了
                        udstate.update(glassManualRemoving = False,runMode = "waitingGlassRemoved")
                    elif self.in_ustate.get("glassElevatorSensor", False):
                        udstate.update(runMode = "elevatorError!")
                    self.in_ustate.update(waittingFinishFlag=False)
            
            ###ドリンクリセット時###
            if not self.ex_bstate.get("drinkReseted", True) and self.ex_ustate.get("drinkReseted", False):
                 udstate.update(runMode = "PLCdrinkReseted")        
                 self.in_ustate.update(waittingFinishFlag=False)

            ######自動運転動作中#####

            ###注文受付可否###
            if self.in_ustate.get("plcOrderReady", False) and self.ex_ustate.get("autoMode", False):
                udstate.update(orderReady = True)
            elif not self.in_ustate.get("plcOrderReady", True):
                udstate.update(orderReady = False)  
            ###氷減少カウント###
            if self.ex_bstate.get("iceRequest", False) and not self.ex_ustate.get("iceRequest", True):  #氷減少センサーがオンからオフになったとき、氷管理カウンターを0に戻す
                self.in_ustate.update(icelimitcount=0)
            
            ###中間管理システム開始時動作判定###
            if self.ex_ustate.get("autoMode", False) and self.ex_ustate["runMode"]=='':
                udstate.update(runMode = "autoOperation")
            elif not self.ex_ustate.get("autoMode", True) and self.ex_ustate["runMode"]=='':
                udstate.update(runMode = "autoOperationStop")
        wadr += adr
        return wadr,udstate

    def controleUpdated(self):
        wadr = [0]
        adr = [0] * 250
        udstate = {}
        ###非常停止指示時###
        if self.in_ustate.get("controleMode", "")=="softwareEmergency": #ソフトウェア非常停止モードの時の動作
            adr[7] = 1
            wadr[0] = 2
            udstate.update(runMode="softwareEmergency")
            self.in_ustate.update(waittingFinishFlag=False)
        ###エラーリセット指示時###
        if self.in_ustate.get("controleMode", "")=="errorReset":    
            adr[1] = 1
            wadr[0] = 2
            udstate.update(runMode="stanbyErrorReaet",drinkRemovedError=False)

        if self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 and not self.in_ustate.get("waittingFinishFlag", False):    
            ###自動動作開始指示時###
            if self.in_ustate.get("controleMode", "")=="autoModeStart" and not self.ex_ustate.get("operating", True):
                if not self.in_ustate.get("glassElevatorSensor", True) :  #昇降機のセンサーがオフなら、自動運転開始
                    adr[6] = 1
                    wadr[0] = 2
                    udstate.update(glassManualRemoving = False, drinkRemovedError = False, runMode = "autoOperation")
                    self.in_ustate.update(waittingFinishFlag=False)
                elif self.in_ustate.get("glassElevatorSensor", False):   #自動動作開始したが、グラスが昇降機上にあるため手動グラス取り出し開始
                    adr[4] = 1
                    wadr[0] = 2
                    udstate.update(glassManualRemoving = True , runMode="stanbyRemoveGlass")
                    self.in_ustate.update(waittingFinishFlag=True)
            ###自動動作停止指示時###
            if self.in_ustate.get("controleMode", "")=="autoModeStop" :    
                if self.in_ustate.get("makingDrinks", False) :   #自動動作停止指示がされたがドリンク製作中なら、ドリンク製作終了スタンバイモードへ
                    udstate.update(runMode = "stanbyStopOperation")
                    self.in_ustate.update(waittingFinishFlag=True)
                elif not self.in_ustate.get("makingDrinks", False) and self.ex_ustate.get("autoMode", True):     #搬送レーンにドリンクが無く、自動動作中なら自動動作シーケンス停止
                    adr[7] = 1
                    wadr[0] = 2
                    udstate.update(runMode = "sequenceStopStanby")
                    self.in_ustate.update(waittingFinishFlag=True)
                elif not self.in_ustate.get("makingDrinks", False) and not self.ex_ustate.get("autoMode", False):     #搬送レーンにドリンクが無く、自動動作中なら自動動作シーケンス停止
                    adr[7] = 2
                    wadr[0] = 2
                    udstate.update(runMode = "autoOperationStop")
                    self.in_ustate.update(waittingFinishFlag=False)

            ###ドリンク取り除き完了指示時###
            if self.in_ustate.get("controleMode", "")=="drinkRemoved" :
                if not self.ex_ustate.get("operating", True) and not self.in_ustate.get("conveyourDrinkSensor", True):   #ドリンクリセット指示がされた
                    adr[3] = 1  #ドリンク取り除き完了
                    adr[7] = 2  #自動動作シーケンス終了
                    wadr[0] = 2    
                    udstate.update(runMode = "PLCdrinkResetStanby",drinkRemovedError = False)
                    self.in_ustate.update(waittingFinishFlag=True)
                elif self.in_ustate.get("conveyourDrinkSensor", False): #ドリンクリセット指示がされたが、コンベア上にドリンクが残っている
                    udstate.update(drinkRemovedError = True , runMode="onConveyorError")
            
            ###ポンプON指示時###
            if self.in_ustate.get("controleMode", "")=="manualPumpON" :
                if not self.ex_ustate.get("autoMode", True) :   #ポンプ手動運転ON
                    adr[8] = 1
                    pumpnum = (self.in_ustate.get("manualPumpNum", 0)-1) * 5 + 15
                    adr[pumpnum] = 1
                    wadr[0] = 2
                    udstate.update(runMode = "manualPumpON")
            
            ###ポンプOFF指示時###
            if self.in_ustate.get("controleMode", "")=="manualPumpOFF" :
                if not self.ex_ustate.get("autoMode", True) :  #ポンプ手動運転OFF
                    adr[8] = 0
                    wadr[0] = 2
                    udstate.update(runMode = "manualPumpOFF")
                    # cls.awaitingupdate["runMode"] = "maintenance"
        wadr += adr
        return wadr,udstate
    def nextOrderUpdated(self):
        wadr = [0]
        adr = [0] * 250
        udstate = {}
        
        if  self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 and self.ex_ustate.get("autoMode", False):
            ###氷減少センサーに反応してから氷入りドリンクを何杯作れるかの設定###
            if self.in_ustate.get("useIce", False) and self.ex_ustate.get("iceRequest", False):   #氷を使用するドリンクで、氷補充がONの時
                print(self.in_ustate.get("icelimitcount", 0))
                count=self.in_ustate.get("icelimitcount", 0)
                count += 1
                self.in_ustate.update(icelimitcount=count)
                # DrinkBotMotionHandler.icelimitcount += 1
                if count > 100000:
                    udstate.update(iceNone = True)

            ###使用するグラスが候補のグラスレーンに存在するかの判定###
            glasslist = self.in_ustate.get("glassLaneNum", [])
            random.shuffle(glasslist)   #グラスグループをランダムにシャッフル
            glasslanecount = 0
            for i in range(len(glasslist)):
                if self.in_ustate.get("glassSensing", [])[glasslist[i]-1]:  #候補のグラスレーンにグラスが存在するかの確認
                    useglass = glasslist[i]   #使用するグラスの決定
                else:
                    glasslanecount += 1
            if glasslanecount == len(glasslist):    #使用するグラスがない場合、glassNone=trueにしてstate.jsonに書き込んで報告
                udstate.update(glassNone = True)
            else:
                udstate.update(glassNone = False)

            # if not cls.alldict.get("iceNone", True) and not cls.alldict.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
            if not udstate.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
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
        return wadr,udstate

def jsonData(path):
    global controleid
    global nextorderid
    if path == "controle.json":
        with open(path, 'r') as f:
            data = json.load(f)
            if data["controleID"] != controleid:
                controleid = data["controleID"]
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
                        senddata,udstate ,udflag = DrinkBotMotionHandler.updateState("controle",rdata)
                    elif mode ==2 :
                        senddata,udstate ,udflag = DrinkBotMotionHandler.updateState("order",rdata)
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

def updateSender(plcdata,jsondata,updateflag):
    newjsondata={}
    if updateflag:
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
            newjsondata={**newjsondata,**jsondata}
        with open('state.json', 'w') as f:
            json.dump(newjsondata, f, indent=2, ensure_ascii=False)
        if plcdata[0] == 2:    #機械に操作指示があるなら送信
            if operatingMode == 0:
                KVKLE02mcp.toPLC(plcdata)
            elif operatingMode == 1:
                testPLC.test(plcdata)

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
                senddata ,udstate , udflag = DrinkBotMotionHandler.updateState("plc",idata,edata)   #ステータスの更新により動作を行うアドレスを取得
                updateSender(senddata ,udstate , udflag)
            elif info[0] == 2:
                if operatingMode == 0:
                    KVKLE02mcp.toPLC(info)
                elif operatingMode == 1:
                    testPLC.test(info)
        info_queue.task_done()

if __name__ == "__main__":
    data0=[0]*50
    edata, idata = convertPlcState(data0[0],data0[1:])
        
    #起動時のjsonファイル読み込み
    with open("controle.json", 'r') as f:
        data1 = json.load(f)
    with open("nextOrder.json", 'r') as f:
        data2 = json.load(f)
    resdata = resetState()
    internalData = {**idata, **data1, **data2}
    externalData={**resdata, **edata}
    s=DrinkBotMotionHandler(internalData,externalData)
    
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
