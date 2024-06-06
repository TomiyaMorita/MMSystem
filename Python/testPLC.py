import struct
import json
def test(data):
    mode=data[0]
    res=[0]
    with open("plcState.json",'r') as c:    #テスト用にjsonファイルにより疑似PLCレスポンス生成
        try:
            testjsonData = json.load(c)
            testplcData = testjsonData["plcdata"]
        except KeyboardInterrupt:
            print("")
    match mode :            
        case 1:
            # print("read")

            res[0]=0
            rdata=res+testplcData
        
            return rdata
        case 2:
            # print("write")
            senddata=data[1:]
            if senddata[1] == 1:
                testplcData[4]=0
                print("エラーリセット")
            if senddata[3] == 1:
                print("注文リクエスト")
            if senddata[4] == 1:
                print("グラス手動取り出し")
            if senddata[6] == 1:
                testplcData[2]=1    #自動動作開始
                testplcData[3]=1
                print("自動動作開始")
            if senddata[7] == 1:
                testplcData[2]=0
                testplcData[3]=0
                print("自動動作停止")
            if senddata[7] == 2:
                testplcData[3]=0
                print("自動動作終了")
            if senddata[8] == 1:
                pumpary=senddata[15:]
                for i in range(0,len(pumpary),5):
                    if pumpary[i] == 1:
                        poumnum=pumpary[i]
                print("マニュアルポンプ：",poumnum)
            if senddata[2] == 1:    #注文リクエスト
                print("排出レーン番号",senddata[5])
                testplcData[7]=senddata[10]
                testplcData[8]=senddata[10]
                print("注文番号",senddata[10])
                print("グラス",senddata[11])
                print("氷",senddata[12])
                pumpary=senddata[15:]
                usepump=[]
                pumptime=[]
                for i in range(0,len(pumpary),5):
                    if pumpary[i] == 1:
                        usepump.append(int((i/5)+1))
                        pumptime.append(pumpary[i+1])
                print("使用ポンプ",usepump)
                print("抽出時間",pumptime)
            testjsonData["plcdata"] = testplcData
            with open('plcState.json', 'wt') as f:
                json.dump(testjsonData, f, ensure_ascii=False)

            res[0]=0
            rdata=res
            return rdata
        case 3:
            print("init")
            with open('plcState.json') as f:
                di = json.load(f)
            di["plcdata"]=data[1:]
            with open('plcState.json', 'wt') as f:
                json.dump(di, f, ensure_ascii=False)
            res[0]=0
            rdata=res
            return rdata
            

if __name__ == '__main__':
    data=[1]
    test(data)
