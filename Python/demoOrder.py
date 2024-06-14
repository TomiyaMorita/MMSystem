import json
def stateCheck():
    with open("state.json", 'r') as s:
        stdata = json.load(s)
    if stdata["machineEmergency"]:
        print("非常停止中")
    else:
        if stdata["autoMode"] and stdata["orderReady"] and stdata["orderErrorNum"] ==0 and not stdata["glassNone"]:
            print("自動動作中、注文受付可能")
            print("注文受け付け番号:",stdata["orderCompleteNum"])
            print("排出完了番号:",stdata["drinkCompletionNum"])
        elif stdata["autoMode"] and not stdata["orderReady"] and stdata["orderErrorNum"] ==0 and not stdata["glassNone"]:
            print("自動動作中、注文受付不可")
            print("注文受け付け番号:",stdata["orderCompleteNum"])
            print("排出完了番号:",stdata["drinkCompletionNum"])
        elif stdata["autoMode"] and stdata["orderErrorNum"] !=0:
            print("エラー発生、エラー番号：",stdata["orderErrorNum"])
        elif not stdata["autoMode"] and stdata["operating"]:
            print("自動動作停止中、駆動部動作中")
        elif not stdata["autoMode"] and not stdata["operating"]:
            print("自動動作停止中、駆動部停止中")
        if stdata["glassNone"]:
            print("グラス無し、再注文要請")
        if stdata["glassManualRemoving"]:
            print("グラス手動取り出し中")
        if stdata["drinkRemovedError"]:
            print("自動動作再開時ドリンク取り出しエラー")
        if stdata["drinkReseted"]:
            print("ドリンクリセット完了")
def nextOrder(ordermode):
    with open("nextOrder.json", 'r') as c:
        ojdata = json.load(c)
    id=ojdata["nextOrderID"]
    id = 0 if id >100000 else id+1
    num=ojdata["orderNum"]
    num = 0 if num >100000 else num+1
    lanenum=ojdata["completionLaneNum"]
    lanenum = 1 if lanenum >2 else lanenum+1
    ojdata.update(nextOrderID=id,orderNum=num,completionLaneNum=lanenum)
    send=True
    match ordermode:
        case "1":   #ハイボール
              mate=[{"pumpNum":6,"time":5000},{"pumpNum":41,"time":10000}]
              ojdata.update(glassLaneNum=[10,11,12],useIce=True,drinks=mate)
        case "2":   #レモンサワー
              mate=[{"pumpNum":30,"time":10000},{"pumpNum":41,"time":5000}]
              ojdata.update(glassLaneNum=[10,11,12],useIce=True,drinks=mate)
        case "3":   #ビール
              mate=[{"pumpNum":43,"time":10000}]
              ojdata.update(glassLaneNum=[1,2,3],useIce=False,drinks=mate)
        # case "4":
        #       mate=[{"pumpNum":1,"time":15000}]
        #       ojdata.update(glassLaneNum=[7,8,9],useIce=False,drinks=mate)
        case _:
            send=False
    if send == True:
        with open('nextOrder.json', 'w') as f:
            json.dump(ojdata, f, indent=2, ensure_ascii=False)
    return send

def controle(ctrmode):
    with open("controle.json", 'r') as c:
        cjdata = json.load(c)
    id=cjdata["controleID"]
    id = 0 if id >100000 else id+1
    cjdata.update(controleID=id)
    send = True
    match ctrmode:
        case "1":
            cjdata.update(controleMode="softwareEmergency")
        case "2":
            cjdata.update(controleMode="autoModeStart")
        case "3":
            cjdata.update(controleMode="autoModeStop")
        case "4":
            cjdata.update(controleMode="drinkRemoved")    
        case "5":
            cjdata.update(controleMode="manualPumpON",manualPumpNum=8)
        case "6":
            cjdata.update(controleMode="manualPumpOFF")
        case "7":
            cjdata.update(controleMode="errorReset")
        case _:
            send=False
    if send == True:
        with open('controle.json', 'w') as f:
            json.dump(cjdata, f, indent=2, ensure_ascii=False)
    return send
def changePLCData(plcmode):
    send = True
    plcstate=[0]*45
    plcstate[0]=1
    for i in range (30,44):
        plcstate[i]=1
    with open("plcState.json", 'r') as c:
        pjdata = json.load(c)
    match plcmode:
        case "1":
            plcstate[4]=1
            plcstate[5]=1
            plcstate[10]=1
            pjdata.update(plcdata=plcstate)
        case "2":
            plcstate[4]=1
            plcstate[5]=1
            pjdata.update(plcdata=plcstate)
        case "3":
            pjdata.update(plcdata=plcstate)
        case "4":
            plcstate[5]=1
            pjdata.update(plcdata=plcstate)    
        case "5":
            plcstate[4]=1
            plcstate[5]=1
            plcstate[10]=1
            pjdata.update(plcdata=plcstate)
        case "6":
            plcstate[17]=1
            pjdata.update(plcdata=plcstate)
        case "7":
            plcstate[18]=1
            pjdata.update(plcdata=plcstate)
        case "8":
            plcstate[7]=1
            pjdata.update(plcdata=plcstate)    
        case "9":
            plcstate[4]=1
            plcstate[5]=1
            plcstate[10]=1
            for i in range (30,44):
                plcstate[i]=0
            pjdata.update(plcdata=plcstate)
        case "10":
            plcstate[4]=1
            plcstate[5]=1
            plcstate[10]=1
            plcstate[22]=1
            pjdata.update(plcdata=plcstate)
        case "11":
            plcstate[0]=0
            pjdata.update(plcdata=plcstate)
        case _:
            send=False 
    if send == True:
        with open('plcState.json', 'w') as f:
            json.dump(pjdata, f, ensure_ascii=False)
    return send
if __name__ == "__main__":
    loop=True
    while(loop):
        dict={}
        input("Enterキーを押してステータスを取得してください")
        stateCheck()
        m=input("モードを選択してください\n 1:オーダーモード 2:コントロールモード 3:PLCステータスチェンジ 4:終了\n")
        match m:
            case "1":
                p=input("コマンドを選択してください\n1:ハイボール\n2:レモンサワー\n3:ビール\n")               
                loop=nextOrder(p)

            case "2":               
                p=input("コマンドを選択してください\n1:非常停止\n2:自動動作開始\n3:自動動作停止\n4:エラー時ドリンク取り除き完了\n5:指定ポンプON\n6:指定ポンプOFF\n7:エラーリセット\n")
                loop=controle(p)
            case "3":
                p=input("コマンドを選択してください\n1:ドリンク製作中注文受付可\n2:自動運転中注文受付不可\n3:自動運転停止中\n4:自動動作停止時動作停止待ち\n5:自動運転時ドリンク製作終了\n6:ドリンクリセット完了\n7:自動動作開始時グラス手動取り出し完了\n8:自動動作開始時動作継続不可エラー\n9:グラスレーングラス無し\n10:氷無し\n11:排出レーン満杯\n12:非常停止ボタン押下\n")
                loop=changePLCData(p)
            case "4":
                print("終了")
                loop = False
            case _:
                print("モード無し")
                loop = False