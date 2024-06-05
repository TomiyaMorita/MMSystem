import struct
import json

class MCProtcol3E:
    addr = ()

    def __init__(self, host, port):
        self.addr = host, port

    def offset(self, adr):
        moffset = [0] * 3

        mtype = adr[:2]
        if mtype == 'SB' or mtype == 'SW' or mtype == 'DX' or mtype == 'DY':
            address = int(adr[2:], 16)
            moffset = list((address).to_bytes(3,'little'))

        elif mtype == 'TS' or mtype == 'TC' or mtype == 'TN' or mtype == 'SS'\
                or mtype == 'SC' or mtype == 'SN' or mtype == 'CS' or mtype == 'CC'\
                or mtype == 'CN' or mtype == 'SM' or mtype == 'SD':
            address = int(adr[2:])
            moffset = list((address).to_bytes(3,'little'))

        if mtype == 'TS':
            deviceCode = 0xC1
        elif mtype == 'TC':
            deviceCode = 0xC0
        elif mtype == 'TN':
            deviceCode = 0xC2
        elif mtype == 'SS':
            deviceCode = 0xC7
        elif mtype == 'SC':
            deviceCode = 0xC6
        elif mtype == 'SN':
            deviceCode = 0xC8
        elif mtype == 'CS':
            deviceCode = 0xC4
        elif mtype == 'CC':
            deviceCode = 0xC3
        elif mtype == 'CN':
            deviceCode = 0xC5
        elif mtype == 'SB':
            deviceCode = 0xA1
        elif mtype == 'SW':
            deviceCode = 0xB5
        elif mtype == 'DX':
            deviceCode = 0xA2
        elif mtype == 'DY':
            deviceCode = 0xA3
        elif mtype == 'SM':
            deviceCode = 0x91
        elif mtype == 'SD':
            deviceCode = 0xA9
        else:
            mtype = adr[:1]
            if mtype == 'X' or mtype == 'Y' or mtype == 'B' or mtype == 'W':
                address = int(adr[1:], 16)
            else:
                address = int(adr[1:])
            moffset = list((address).to_bytes(3,'little'))

            if mtype == 'X':
                deviceCode = 0x9C
            elif mtype == 'Y':
                deviceCode = 0x9D
            elif mtype == 'M':
                deviceCode = 0x90
            elif mtype == 'L':
                deviceCode = 0x92
            elif mtype == 'F':
                deviceCode = 0x93
            elif mtype == 'V':
                deviceCode = 0x94
            elif mtype == 'B':
                deviceCode = 0xA0
            elif mtype == 'D':
                deviceCode = 0xA8
            elif mtype == 'W':
                deviceCode = 0xB4
            elif mtype == 'S':
                deviceCode = 0x98

        return deviceCode, moffset
    def mcpheader(self, cmd):
        ary = bytearray(11)
        requestdatalength = struct.pack("<H", len(cmd) + 2)

        ary[0] = 0x50                      # sub header
        ary[1] = 0x00
        ary[2] = 0x00                      # Network No.
        ary[3] = 0xFF                      # PC No.
        ary[4] = 0xFF                      # Request destination module i/o No.
        ary[5] = 0x03
        ary[6] = 0x00                      # Request destination module station No.
        ary[7] = requestdatalength[0]      # Request data length
        ary[8] = requestdatalength[1]
        ary[9] = 0x10                      # CPU monitoring timer
        ary[10] = 0x00

        return ary
    def write(self, memaddr, writedata, bitSize = 0):
       
        if bitSize > 0:
            unitOfBit = True
            if bitSize <= (len(writedata) * 2):
                elementCnt = bitSize
                writedata = writedata[:(bitSize + 1) // 2]
            else:
                return
        else:
            unitOfBit = False
            if len(writedata) % 2 == 0:
                elementCnt = len(writedata) // 2   
            else:
                return
        # MC Protocol
        elementSize = struct.pack("<H", elementCnt)  

        cmd = bytearray(10 + len(writedata))
        cmd[0] = 0x01                     # Write Command
        cmd[1] = 0x14
        if unitOfBit:
            cmd[2] = 0x01                 # Sub Command
            cmd[3] = 0x00
        else:
            cmd[2] = 0x00
            cmd[3] = 0x00

        deviceCode, memoffset = self.offset(memaddr)
        cmd[4] = memoffset[0]             # head Dvice
        cmd[5] = memoffset[1]                     
        cmd[6] = memoffset[2]
        cmd[7] = deviceCode               # Device code
        cmd[8] = elementSize[0]           # Element Size
        cmd[9] = elementSize[1]
        cmd[10:] = writedata
        print(cmd[4:7])
        print(cmd[10:])
        senddata = self.mcpheader(cmd) + cmd
        
    def RandomWrite(self, worddevice, dworddevice, writedata,dwritedata, bitSize = 0):
        if worddevice != []:
            wd = worddevice.replace(' ', '').split(',')
        else:
            worddevice=[]
            wd=[]
        wdary = []  #16bitデバイス指定(4byte)        
        if len(wd) > 0:
            for d in wd:
                code, offset = self.offset(d)
                wdary.extend(offset)
                wdary.append(code)
        if dworddevice != []:
            dwd = dworddevice.replace(' ', '').split(',')
        else:
            dworddevice=[]
            dwd=[]
        dwdary = [] #32bitデバイス指定(4byte)
        if len(dwd) > 0:
            for dw in dwd:
                code, offset = self.offset(dw)
                dwdary.extend(offset)
                dwdary.append(code)     
        doary = []
        if bitSize > 0:
            unitOfBit = True
            if len(wd) > 0:
                for f in range(len(wd)):
                    tbd = 4*f
                    obd = f
                    doary+=wdary[tbd:tbd+4] #デバイス番号を4byteごとに書き込み
                    doary+=writedata[obd] #データを1byteごとに書き込み
            if len(dwd) > 0:
                for s in range(len(dwd)):
                    wtbd = 4*s
                    wobd = s
                    doary+=dwdary[wtbd:wtbd+4] #デバイス番号を4byteごとに書き込み
                    doary+=dwritedata[wobd] #データを1byteごとに書き込み   
        else:
            unitOfBit = False
            if len(wd) > 0:
                for f in range(len(wd)):
                    tbd = 4*f
                    obd = 2*f
                    doary+=wdary[tbd:tbd+4] #デバイス番号を4byteごとに書き込み
                    doary+=writedata[obd:obd+2] #データを2byteごとに書き込み
            if len(dwd) > 0:
                for s in range(len(dwd)):
                    wtbd = 4*s
                    wobd = 4*s
                    doary+=dwdary[wtbd:wtbd+4] #デバイス番号を4byteごとに書き込み
                    doary+=dwritedata[wobd:wobd+4] #データを4byteごとに書き込み            
        
        # MC Protocol

        cmd = bytearray(6  + len(doary))
        cmd[0] = 0x02                     # Write Command
        cmd[1] = 0x14
        if unitOfBit:
            cmd[2] = 0x01                 # Sub Command
            cmd[3] = 0x00
        else:
            cmd[2] = 0x00
            cmd[3] = 0x00

        cmd[4] = len(wd)          # Element Size 16bit
        cmd[5] = len(dwd)   # Element Size 32bit
        cmd[6:] =  doary              
        print(cmd[6:])
            
class test0:
    testdict={}
    testdict2={}
    def test1(self,dict):
        self.testdict=self.testdict2.copy()
        self.testdict2.update(dict)
        print("testdict",self.testdict)
        print("testdict2",self.testdict2)
def test(data):
    mode=data[0]
    res=[0]
    match mode :            
        case 1:
            print("read")

            res[0]=0
            with open("plcState.json",'r') as c:    #テスト用にjsonファイルにより疑似PLCレスポンス生成
                try:
                    testjsonData = json.load(c)
                    testplcData = testjsonData["plcdata"]
                except KeyboardInterrupt:
                    print("")
            rdata=res+testplcData
        
            return rdata
        case 2:
            print("write")
            senddata=data[1:]
            print(senddata)
            if senddata[1] == 1:
                print("エラーリセット")
            if senddata[3] == 1:
                print("注文リクエスト")
            if senddata[4] == 1:
                print("グラス手動取り出し")
            if senddata[6] == 1:
                print("自動動作開始")
            if senddata[7] == 1:
                print("自動動作停止")
            if senddata[7] == 2:
                print("自動動作終了")
            if senddata[8] == 1:
                pumpary=senddata[15:]
                for i in range(0,len(pumpary),5):
                    if pumpary[i] == 1:
                        poumnum=pumpary[i]
                print("マニュアルポンプ：",poumnum)
            if senddata[2] == 1:
                print("排出レーン番号",senddata[5])
                print("注文番号",senddata[10])
                print("グラス",senddata[11])
                print("氷",senddata[12])
                pumpary=senddata[15:]
                usepump=[]
                pumptime=[]
                for i in range(0,len(pumpary),5):
                    if pumpary[i] == 1:
                        usepump.append(pumpary[i]+1)
                        pumptime.append(pumpary[i+1])
                print("使用ポンプ",usepump)
                print("抽出時間",pumptime)
            res[0]=0
            rdata=res
            return rdata
        case 2:
            print("init")
            with open('plcState.json') as f:
                di = json.load(f)
            di["plcdata"]=data[1:]
            with open('plcState.json', 'wt') as f:
                json.dump(di, f, ensure_ascii=False)
            res[0]=0
            rdata=res
            return rdata
            # firstdevice="D2000"
            # data=struct.pack("200i",*data)
            # print(data)
            

if __name__ == '__main__':
    # data=[0,1,1,1,1,0,0,1,1234,1245,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1]
    # print(test(data))
    dict1={"a":1,"b":2}
    dict2={"a":3,"b":4}
    s=test0()
    s.test1(dict1)
    s2=test0()
    s2.test1(dict2)
    # mcp = MCProtcol3E('192.168.3.2', 4999)
    # data=struct.pack('hh', 600,500)
    # data2 = struct.pack('ii', 6000,50000)
    # mcp.RandomWrite('D2000,D2010','D2015,D2021',data,data2)
    # data=struct.pack('h', 600)
    # mcp.write('D2000',data)