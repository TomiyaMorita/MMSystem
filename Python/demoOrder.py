import json

def nextOrder(ordermode):
    with open("nextOrder.json", 'r') as c:
        ojdata = json.load(c)
    id=ojdata["nextOrderID"]
    id = 0 if id >100000 else id+1
    num=ojdata["orderNum"]
    num = 0 if num >100000 else num+1
    lanenum=ojdata["completionLaneNum"]
    lanenum = 0 if lanenum >1 else lanenum+1
    ojdata.update(nextOrderID=id,orderNum=num,completionLaneNum=lanenum)
    send=True
    match ordermode:
        case "1":   #ハイボール1
              mate=[{"pumpNum":6,"time":5000},{"pumpNum":41,"time":8000}]
              ojdata.update(glassLaneNum=[10,11],useIce=True,drinks=mate)
        case "2":   #レモンサワー
              mate=[{"pumpNum":30,"time":10000},{"pumpNum":41,"time":8000}]
              ojdata.update(glassLaneNum=[10,11],useIce=True,drinks=mate)
        case "3":   #ビール
              mate=[{"pumpNum":43,"time":10000}]
              ojdata.update(glassLaneNum=[4,5],useIce=False,drinks=mate)
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
            cjdata.update(controleMode="glassRemovalCompleted")     
        case "6":
            cjdata.update(controleMode="manualPumpON",manualPumpNum=8)
        case "7":
            cjdata.update(controleMode="manualPumpOFF")
        case "8":
            cjdata.update(controleMode="errorReset")
        case _:
            send=False
    if send == True:
        with open('controle.json', 'w') as f:
            json.dump(cjdata, f, indent=2, ensure_ascii=False)
    return send

if __name__ == "__main__":
    loop=True
    while(loop):
        dict={}
        m=input("モードを選択してください\n 1:オーダーモード 2:コントロールモード 3:終了\n")
        match m:
            case "1":
                p=input("コマンドを選択してください\n1:ハイボール\n2:レモンサワー\n3:ビール\n")               
                loop=nextOrder(p)

            case "2":               
                p=input("コマンドを選択してください\n1:非常停止\n2:自動動作開始\n3:自動動作停止\n4:エラー時ドリンク取り除き完了\n5:グラス除去完了\n6:指定ポンプON\n7:指定ポンプOFF\n8:エラーリセット\n")
                loop=controle(p)
            case "3":
                print("終了")
                loop = False
            case _:
                print("モード無し")
                loop = False