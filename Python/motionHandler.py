import random
###define###
#ダミーレーン分けを使用するか(Trueにすると指定に関係なく排出レーンが4杯ごとに変更する)
dummyLane = True
#氷減少センサーが反応してから氷がなくなるまで作製できるドリンクの数
defineIceLimitCount = 1000
class DrinkBotMotionHandler:
    in_bstate={}
    in_ustate={}
    ex_bstate={}
    ex_ustate={}
    def updateState(self, whitchstate,beforeInState={},beforeExState={},inState={},exState={}):
        #更新前の内部ステータスをコピー
        self.in_bstate = beforeInState.copy()
        #更新する
        self.in_ustate = {**beforeInState,**inState}
        #更新前の外部出力ステータスをコピー
        self.ex_bstate = beforeExState.copy()
        #更新する
        self.ex_ustate = {**beforeExState,**exState}
        match whitchstate:
            case "plc":
                adr = self.plcUpdated()
                self.ex_ustate.update(self.in_ustate["waitingUpdate"])
                print(self.in_ustate["waitingUpdate"])
                self.in_ustate["waitingUpdate"].clear()
            case "controle":
                adr,udstate = self.controleUpdated()
                self.in_ustate["waitingUpdate"].update(udstate)
                # print("controle",self.ex_ustate["continueError"])
            case "order":
                adr,udstate = self.nextOrderUpdated()
                self.in_ustate["waitingUpdate"].update(udstate)
            case _:
                adr = [0] * 251
        # print("ex_ustate",self.ex_ustate)
        # print("waitingUpdate",self.in_ustate["waitingUpdate"])
        # print("self.ex_ustate",self.ex_ustate)
        return adr,self.in_ustate, self.ex_ustate   
    def plcUpdated(self):
        wadr = [0]
        adr = [0] * 250
        if not self.in_ustate.get("machineReady", True):  #物理非常停止時
            self.ex_ustate.update(runMode="hardwareEmergency",machineEmergency=True)
            self.in_ustate.update(waittingFinishFlag=False)
        elif not self.in_bstate.get("machineReady", True) and self.in_ustate.get("machineReady", False):
            self.ex_ustate.update(runMode="autoOperationStop",machineEmergency = False)
        if self.ex_ustate.get("errorNum", 0) != 0 :    #エラー発生時
            self.ex_ustate["runMode"]="errorEmergency"
        ###搬送機PLC接続エラー###
        if self.in_ustate.get("plcConnectError", False):
            self.ex_ustate["runMode"]="PLCConnectError!"
        ###自動動作継続不可エラー###
        if self.in_ustate.get("plccontinueError", False):
            self.ex_ustate.update(runMode = "glassRemoveWaiting",glassRemoveRequest = True)
            self.in_ustate.update(waittingFinishFlag=False)
            if self.in_ustate.get("makingDrinks", True):
                self.ex_ustate.update(drinkResetRequest=True)

        #ステータスの定期更新により実行されるモード
        if self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 :

             ###自動動作停止中###
            if self.ex_ustate.get("runMode","")=="stanbyStopOperation":
                #搬送レーンにあるドリンクがなくなり次第自動動作停止
                if self.in_bstate.get("makingDrinks", False) and not self.in_ustate.get("makingDrinks", True) and self.ex_ustate.get("autoMode", False):    
                    adr[7] = 1
                    wadr[0] = 2
                    self.in_ustate.update(waittingFinishFlag=True)
                    self.ex_ustate.update(runMode="sequenceStopStanby")
            if self.ex_ustate.get("runMode","")=="sequenceStopStanby":
                if not self.in_ustate.get("makingDrinks", True) and not self.ex_ustate.get("operating", True):    #全動作終了後、自動動作シーケンス終了
                    adr[7] = 2
                    wadr[0] = 2
                    self.in_ustate.update(waittingFinishFlag=False)
                    self.ex_ustate.update(runMode = "autoOperationStop")
                    
            ###自動動作開始中###  
            ###搬送機PLCから手動グラス取り出し指示が指示された時###
            # if not self.in_bstate.get("glassManualRemovalCompleted", True) and self.in_ustate.get("glassManualRemovalCompleted", False):
            
            if self.in_ustate.get("glassManualRemovalCompleted", False):  
                if not self.in_ustate.get("glassElevatorSensor", True):   #昇降部から手動グラス取り出し完了
                    self.ex_ustate.update(glassRemoveRequest = True,runMode = "waitingGlassRemoved")
                elif self.in_ustate.get("glassElevatorSensor", False):
                    self.ex_ustate.update(runMode = "elevatorError!")
                self.in_ustate.update(waittingFinishFlag=False)
            
            ###ドリンクリセット時###
            if not self.ex_bstate.get("drinkReseted", True) and self.ex_ustate.get("drinkReseted", False):
                 self.ex_ustate.update(runMode = "PLCdrinkReseted")        
                 self.in_ustate.update(waittingFinishFlag=False)
                 if self.in_ustate.get("conveyourDrinkSensor", False): #ドリンクリセットが完了したが、コンベア上にドリンクが残っている
                    self.in_ustate.update(glassRemoveRequest = True , runMode="glassRemoveRequest")

            ######自動運転動作中#####

            ###注文受付可否###
            if self.in_ustate.get("plcOrderReady", False) and self.ex_ustate.get("autoMode", False):
                self.ex_ustate.update(orderReady = True)
            elif not self.in_ustate.get("plcOrderReady", True):
                self.ex_ustate.update(orderReady = False)  
            ###氷減少カウント###
            if self.ex_bstate.get("iceRequest", False) and not self.ex_ustate.get("iceRequest", True):  #氷減少センサーがオンからオフになったとき、氷管理カウンターを0に戻す
                self.in_ustate.update(icelimitcount=0)
            
            ###中間管理システム開始時動作判定###
            if self.ex_ustate.get("autoMode", False) and self.ex_ustate["runMode"]=="autoOperationStop":
                self.ex_ustate.update(runMode = "autoOperation")
            elif not self.ex_ustate.get("autoMode", True) and self.ex_ustate["runMode"]=="":
                self.ex_ustate.update(runMode = "autoOperationStop")
        wadr += adr
        return wadr

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
            udstate.update(runMode="stanbyErrorReaet")

        if self.in_ustate.get("machineReady", False) and self.ex_ustate.get("errorNum", 0) == 0 and not self.in_ustate.get("waittingFinishFlag", False):    
            ###自動動作開始指示時###
            if self.in_ustate.get("controleMode", "")=="autoModeStart" :
                if not self.in_ustate.get("glassElevatorSensor", True) :  #昇降機のセンサーがオフなら、自動運転開始
                    adr[6] = 1
                    wadr[0] = 2
                    udstate.update(runMode = "autoOperation")
                    self.in_ustate.update(waittingFinishFlag=False)
                elif self.in_ustate.get("glassElevatorSensor", False):   #自動動作開始したが、グラスが昇降機上にあるため手動グラス取り出し開始
                    adr[4] = 1
                    wadr[0] = 2
                    udstate.update(runMode="stanbyRemoveGlass")
                    self.in_ustate.update(waittingFinishFlag=True)
                else:
                    print("自動動作開始指示時ステータスエラー")
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
                else:
                    print("自動動作停止指示時ステータスエラー")
            ###ドリンクリセット完了###
            if self.in_ustate.get("controleMode", "")=="drinkResetCompleted" :
                if not self.ex_ustate.get("autoMode", True) :   #ドリンクリセット指示がされた
                    adr[3] = 1  #ドリンク取り除き完了
                    adr[7] = 2  #自動動作シーケンス終了
                    wadr[0] = 2    
                    udstate.update(runMode = "PLCdrinkResetStanby",drinkResetRequest=False)
                    self.in_ustate.update(waittingFinishFlag=True)
                else:
                    print("ドリンクリセット時自動動作中のためリセット不可")
            ###グラス取り出し完了###    
            if self.in_ustate.get("controleMode", "")=="glassRemoveCompleted" :
                if self.in_ustate.get("conveyourDrinkSensor", False): #グラス取り出ししたが、コンベア上にドリンクが残っている
                    udstate.update(glassRemoveRequest = True , runMode="glassRemoveRequest")
                else:
                    udstate.update(glassRemoveRequest = False , runMode="glassRemoveCompleted")
            ###ポンプON指示時###
            if self.in_ustate.get("controleMode", "")=="manualPumpON" :
                if not self.ex_ustate.get("autoMode", True) :   #ポンプ手動運転ON
                    adr[8] = 1
                    pumpnum = (self.in_ustate.get("manualPumpNum", 0)-1) * 5 + 15
                    adr[pumpnum] = 1
                    wadr[0] = 2
                    udstate.update(runMode = "manualPumpON")
                    print("マニュアルポンプON")
                else:
                    print("ポンプON時自動動作中のため動作不可")
            
            ###ポンプOFF指示時###
            if self.in_ustate.get("controleMode", "")=="manualPumpOFF" :
                if not self.ex_ustate.get("autoMode", True) :  #ポンプ手動運転OFF
                    adr[8] = 0
                    wadr[0] = 2
                    udstate.update(runMode = "manualPumpOFF")
                    # self.awaitingupdate["runMode"] = "maintenance"
                else:
                    print("ポンプOFF時自動動作中のため動作不可")
                
        elif self.in_ustate.get("waittingFinishFlag", True):
            print("動作中の指示終了待ち")
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
                if count > defineIceLimitCount:
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

            # if not self.alldict.get("iceNone", True) and not self.alldict.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
            if not udstate.get("glassNone", True): #注文可能条件に合う場合、注文データ作成
                wadr[0] = 2
                adr[2] = 1
                adr[5] = self.in_ustate.get("completionLaneNum", 0)
                if dummyLane:
                    print(self.in_ustate.get("dummyLaneCount", 0))
                    lanecount=self.in_ustate.get("dummyLaneCount", 0)
                    if lanecount < 4:
                        adr[5] = 1
                    elif 4 <= lanecount  < 8:
                        adr[5] = 2
                    elif 4 <= lanecount  < 12:
                        adr[5] = 3
                    if lanecount < 11:
                        lanecount+=1
                    else:
                        lanecount = 0
                    self.in_ustate.update(dummyLaneCount=lanecount)
                adr[10] = self.in_ustate.get("orderNum", 0)
                adr[11] = useglass
                adr[12] = 1 if self.in_ustate.get("useIce", False) else 0  #useIceがtrueなら氷入れる,1=氷あり
                drinks = self.in_ustate.get('drinks', [])
                for i in range(len(drinks)): #使用する材料の数だけループ
                    firstArynum = 15
                    drinknum = firstArynum + 5 * (drinks[i]['pumpNum'] - 1)
                    adr[drinknum] = 1
                    adr[drinknum + 1] = drinks[i]['time']
                    print("使用ポンプ",2000+drinknum*2)
            elif udstate.get("glassNone", False):
                print("グラス無し注文不可")
            elif udstate.get("iceNone", False):
                print("氷無し注文不可")
                
        # print(self.ex_ustate["runMode"])
        wadr += adr
        return wadr,udstate