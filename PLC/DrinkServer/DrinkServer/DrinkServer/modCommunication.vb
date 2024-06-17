Option Strict On

Imports System.IO.Ports
Imports System.Net.Sockets
Imports System.Text

Module comLanport

    Public ReadOnly syncObj As New Object()  '   排他制御object

End Module

'   commnunication to PLC
Module comPLC

    Public Const GMD_STD = 0        '   get mode standard
    Public Const GMD_TPG = 1        '   get mode taping
    Public Const GMD_LOG = 2        '   get mode log
    Public Const GMD_RTO = 3        '   get mode ratio

    Public Const MODE_MANU = 0      '   mode manual
    Public Const MODE_AUTO = 1      '   mode auto
    Public Const MODE_CAL = 2       '   mode cal
    Public Const MODE_INIT = 3      '   mode init. (500msecON -> OFF)
    Public Const MODE_SMPL = 4      '   mode sample
    Public Const MODE_ATBX = 5      '   mode auto (no taping drop box)
    Public Const MODE_DCHK = 6      '   mode daily check 

    Public timeOut As Integer       '   communication timeout

    Public Const CD_MEAS_OK = 1     '   measure ok
    Public Const CD_MEAS_NG = 2     '   measure ng
    Public Const CD_REMEAS = 19     '   remeasure
    Public Const CD_ABNORMAL = 10   '   NetworkAnalyzer異常 or 絶縁計異常

    Public Const CD_MES_NCT = 1     '   measure カウント無
    Public Const CD_MES_CNT = 2     '   measure カウント有
    Public Const CD_LOTEND = 3      '   runlot end
    Public Const CD_PKGEND = 4      '   reellot end
    Public Const CD_CALREQ = 5      '   cal request
    Public Const CD_SMPEND = 6      '   sample end
    Public Const CD_PEDREQ = 7      '   強制 reellot end -> 次回CALから

    'Control bit data
    Public Const CTL_ELS01_GO = 0   '   受取1 go
    Public Const CTL_ELS01_BK = 1   '   受取1 back
    Public Const CTL_ELS02_GO = 2   '   本流送り go
    Public Const CTL_ELS02_BK = 3   '   本流送り back
    Public Const CTL_ELS03_GO = 4   '   氷投入 go
    Public Const CTL_ELS03_BK = 5   '   氷投入 back
    Public Const CTL_ELS04_GO = 6   '   stockY1 go
    Public Const CTL_ELS04_BK = 7   '   stockY1 back
    Public Const CTL_ELS05_GO = 8   '   stockY2 go
    Public Const CTL_ELS05_BK = 9   '   stockY2 back
    Public Const CTL_ELS06_GO = 10  '   stockY3 go
    Public Const CTL_ELS06_BK = 11  '   stockY3 back
    Public Const CTL_ELS07_GO = 12  '   stockZ1 go
    Public Const CTL_ELS07_BK = 13  '   stockZ1 back
    Public Const CTL_ELS08_GO = 14  '   stockZ2 go
    Public Const CTL_ELS08_BK = 15  '   stockZ2 back
    Public Const CTL_ELS09_GO = 100 '   stockZ3 go
    Public Const CTL_ELS09_BK = 101 '   stockZ3 back
    Public Const CTL_SOL01_UP = 200 '   stopper1 up
    Public Const CTL_SOL01_DW = 201 '   stopper1 down
    Public Const CTL_SOL02_UP = 202 '   stopper2 up
    Public Const CTL_SOL02_DW = 203 '   stopper2 down
    Public Const CTL_SOL03_UP = 204 '   stopper3 up
    Public Const CTL_SOL03_DW = 205 '   stopper3 down
    Public Const CTL_SOL04_UP = 206 '   stopper4 up
    Public Const CTL_SOL04_DW = 207 '   stopper4 down
    Public Const CTL_SOL05_UP = 208 '   stopper5 up
    Public Const CTL_SOL05_DW = 209 '   stopper5 down
    Public Const CTL_SOL06_UP = 210 '   stopper6 up
    Public Const CTL_SOL06_DW = 211 '   stopper6 down
    Public Const CTL_SOL07_UP = 212 '   stopper7 up
    Public Const CTL_SOL07_DW = 213 '   stopper7 down
    Public Const CTL_SOL08_UP = 214 '   stopper8 up
    Public Const CTL_SOL08_DW = 215 '   stopper8 down
    Public Const CTL_SOL09_UP = 300 '   stopper9 up
    Public Const CTL_SOL09_DW = 301 '   stopper9 down
    Public Const CTL_SOL10_UP = 302 '   stopper10 up
    Public Const CTL_SOL10_DW = 303 '   stopper10 down
    Public Const CTL_SOL11_UP = 304 '   stopper11 up
    Public Const CTL_SOL11_DW = 305 '   stopper11 down
    Public Const CTL_SOL12_UP = 306 '   stopper12 up
    Public Const CTL_SOL12_DW = 307 '   stopper12 down
    Public Const CTL_SOL13_UP = 308 '   stopper13 up
    Public Const CTL_SOL13_DW = 309 '   stopper13 down

    Public Const CTL_BEELX_WT = 0   '   ビールX 待機
    Public Const CTL_BEELX_BK = 1   '   ビールX back
    Public Const CTL_BEELX_CB = 2   '   ビールX コンベア
    Public Const CTL_BEELX_BL = 3   '   ビールX ビール
    Public Const CTL_BEELZ_WT = 0   '   ビールZ 待機
    Public Const CTL_BEELZ_DH = 1   '   ビールZ down-H
    Public Const CTL_BEELZ_DL = 2   '   ビールZ down-L
    Public Const CTL_BEELZ_UH = 3   '   ビールZ up-H
    Public Const CTL_BEELZ_UL = 4   '   ビールZ up-L

    Public f_Debug As Boolean

    Public port As Integer                  '   port no.
    Public ipAdd As System.Net.IPAddress    '   ipaddress

    Public taplot As Integer                '   taping work lotno.
    Public stkDat(8) As Integer             '   stock get data
    Public stkEcd(20) As String             '   stock error code

    Private dt_Receive1 As String           '   receive data1
    Private dt_Receive2 As String           '   receive data2
    Private dt_Receive3 As String           '   receive data3
    Private dt_Receive4 As String           '   receive data4

    Private _connect As System.Threading.ManualResetEvent = New System.Threading.ManualResetEvent(False)
    'Private ReadOnly syncObj As New Object  '   排他制御object
    Private client As TcpClient             '   TCPｸﾗｲｱﾝﾄ

    '   send message & get data
    'Private Function SendMsg(ByVal sdt As String, Optional ByVal tap As Boolean = False) As Boolean
    Private Function SendMsg(ByVal sdt As String, Optional ByVal mode As Integer = 0) As Boolean

        SyncLock comLanport.syncObj             '   排他制御
            'SyncLock syncObj                    '   排他制御
            Dim ret As Boolean
            Dim getDt As String
            'Dim client As New TcpClient         '   TCPｸﾗｲｱﾝﾄ
            Dim sdat As String

            If f_Debug = True Then
                Return False
                Exit Function
            End If

            '_connect.Reset()

            sdat = sdt + vbCr                   '   terminator cr
            Try
                '   TCP/IP接続を行う
                'client.Connect(ipAdd, port)
                'client.BeginConnect(ipAdd, port, AddressOf ConnectCallback, client)
                'If _connect.WaitOne(3000) = False Then
                '    client.Close()
                '    SendMsg = True
                '    Exit Function
                'End If

                '   通信ストリームの取得
                Dim stream As System.Net.Sockets.NetworkStream = client.GetStream()

                Dim SendBuffer() As Byte = System.Text.Encoding.ASCII.GetBytes(sdat)
                stream.Write(SendBuffer, 0, SendBuffer.Length)
                stream.Flush()  '   フラッシュ(強制書き出し)

                '   サーバーからの受信
                Dim ReceiveData(10000) As Byte
                stream.Read(ReceiveData, 0, ReceiveData.Length)
                getDt = "受信ﾃﾞｰﾀ : " + System.Text.Encoding.ASCII.GetString(ReceiveData)  '   正常に受信できた場合

                Dim nm As Integer
                getDt = System.Text.Encoding.ASCII.GetString(ReceiveData)
                nm = getDt.IndexOf(vbCrLf)

                If mode = GMD_TPG Then                  '   taping data get
                    dt_Receive2 = getDt.Substring(0, nm)
                ElseIf mode = GMD_LOG Then              '   log data get
                    dt_Receive3 = getDt.Substring(0, nm)
                ElseIf mode = GMD_RTO Then              '   ratio data get
                    dt_Receive4 = getDt.Substring(0, nm)
                Else                                    '   standard data get
                    dt_Receive1 = getDt.Substring(0, nm)
                End If

                '   TCPｸﾗｲｱﾝﾄをｸﾛｰｽﾞ
                'client.Close()
                ret = False
            Catch ex As Exception
                ret = True
            End Try

            SendMsg = ret
        End SyncLock

    End Function
    '   connect callback
    Private Sub ConnectCallback(ByVal result As IAsyncResult)

        Try
            Dim s As TcpClient = CType(result.AsyncState, TcpClient)
            s.EndConnect(result)
            _connect.Set()
        Catch
        End Try

    End Sub

    '   check connect
    Public Function CheckConnect() As Boolean

        Dim ret As Boolean
        Dim stk As Integer

        ret = False
        stk = timeOut               '   stock timout
        timeOut = 2000              '   通信確認のためのタイムアウト 5sec

        If OpenConnect() = True Then        '   DOCとの通信ポートopenエラー
            ret = True
        End If
        timeOut = stk               '   timeout もとに戻す

        Return ret

    End Function

    '   open cnnect
    Public Function OpenConnect() As Boolean

        Dim ret As Boolean

        If f_Debug = True Then
            Return False
            Exit Function
        End If

        _connect.Reset()

        client = New TcpClient
        Try
            '   TCP/IP接続を行う
            client.BeginConnect(ipAdd, port, AddressOf ConnectCallback, client)
            If _connect.WaitOne(3000) = False Then
                client.Close()
                ret = True
            Else
                If client.Connected = False Then
                    client.Close()
                    ret = True
                End If
            End If

            If ret = True Then          '   open 失敗
                Return ret
                Exit Function
            End If

            ret = False
        Catch ex As Exception
            ret = True
        End Try

        Return ret

    End Function
    '   close connect
    Public Sub CloseConect(Optional ByVal mst As Boolean = False)

        Try
            If client.Connected = True Or mst = True Then

                'client.Client.Close()
                'client.Client.Dispose()

                '   TCPｸﾗｲｱﾝﾄをｸﾛｰｽﾞ
                client.Close()
            End If
        Catch ex As Exception

        End Try

    End Sub

    '   時刻 & 稼働率更新時間set
    Public Sub SetPLCtime()

        Dim sdat As String
        Dim tdat(10) As String
        Dim i As Integer
        Dim dtNow As DateTime = DateTime.Now

        tdat(0) = dtNow.ToString("yy")
        tdat(1) = dtNow.Month.ToString("00")
        tdat(2) = dtNow.Day.ToString("00")
        tdat(3) = dtNow.Hour.ToString("00")
        tdat(4) = dtNow.Minute.ToString("00")
        tdat(5) = dtNow.Second.ToString("00")
        tdat(6) = CStr(dtNow.DayOfWeek)

        sdat = "WRT "                   '   時刻set
        For i = 0 To 6
            sdat += tdat(i)
            sdat += " "
        Next
        SendMsg(sdat)

        sdat = "WRS DM31903 2 0 0"         '   更新時間 00:00
        SendMsg(sdat)

    End Sub

    '   任意書き込み
    Public Sub AnyWrite(ByVal command As String)

        Dim sdat As String

        sdat = "WR " + command      '   write data
        SendMsg(sdat)

    End Sub
    '   任意読み込み
    Public Sub AnyRead(ByRef command As String, ByRef rdat As String)

        Dim sdat As String

        sdat = "RD " + command      '   read data
        SendMsg(sdat)

        rdat = dt_Receive1

    End Sub

    '   mode send
    Public Sub SendMode(ByVal mode As Integer)

        Dim sdat, md As String

        md = mode.ToString()
        sdat = "WR ZF5000 " + md    '   address 5000 mode
        SendMsg(sdat)

        If mode = MODE_INIT Then        '   init
            System.Threading.Thread.Sleep(500)
            sdat = "WR ZF5000 " + MODE_MANU.ToString()
            SendMsg(sdat)
        End If

    End Sub
    '   axis data send
    Public Sub SendAxisData()

        Dim sdat As String

        sdat = "WR ZF110002.L "         '   ZF110002 (2WD) y軸 待機位置
        sdat += (modAxis.yWait.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110004.L "         '   ZF110004 (2WD) y軸 back位置
        sdat += (modAxis.yBack.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110010.L "         '   ZF110010 (2WD) y軸 コンベア位置
        sdat += (modAxis.yConb.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110012.L "         '   ZF110012 (2WD) y軸 ビール位置
        sdat += (modAxis.yBeel.pos * 100).ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR ZF120001 "           '   ZF120001 y軸 待機速度
        sdat += modAxis.yWait.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120002 "           '   ZF120002 y軸 back速度
        sdat += modAxis.yBack.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120005 "           '   ZF120005 y軸 コンベア速度
        sdat += modAxis.yConb.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120006 "           '   ZF120006 y軸 ビール速度
        sdat += modAxis.yBeel.spd.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR ZF125001 "           '   ZF125001 y軸 待機加減速
        sdat += modAxis.yWait.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125002 "           '   ZF125002 y軸 back加減速
        sdat += modAxis.yBack.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125005 "           '   ZF125005 y軸 コンベア加減速
        sdat += modAxis.yConb.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125006 "           '   ZF125006 y軸 ビール加減速  
        sdat += modAxis.yBeel.acc.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR ZF110202.L "         '   ZF110202 (2WD) z軸 待機位置
        sdat += (modAxis.zWait.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110204.L "         '   ZF110204 (2WD) z軸 down-H位置
        sdat += (modAxis.zDwnH.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110206.L "         '   ZF110206 (2WD) z軸 down-L位置
        sdat += (modAxis.zDwnL.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110210.L "         '   ZF110210 (2WD) z軸 up-H位置
        sdat += (modAxis.zUP_H.pos * 100).ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF110212.L "         '   ZF110212 (2WD) z軸 up-L位置
        sdat += (modAxis.zUP_L.pos * 100).ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR ZF120101 "           '   ZF120101 z軸 待機速度
        sdat += modAxis.zWait.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120102 "           '   ZF120102 z軸 down-H速度
        sdat += modAxis.zDwnH.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120103 "           '   ZF120103 z軸 down-L速度
        sdat += modAxis.zDwnL.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120105 "           '   ZF120105 z軸 up-H速度
        sdat += modAxis.zUP_H.spd.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF120106 "           '   ZF120106 z軸 up-L速度
        sdat += modAxis.zUP_L.spd.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR ZF125101 "           '   ZF125101 z軸 待機加減速
        sdat += modAxis.zWait.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125102 "           '   ZF125102 z軸 down-H加減速
        sdat += modAxis.zDwnH.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125103 "           '   ZF125103 z軸 down-L加減速
        sdat += modAxis.zDwnL.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125105 "           '   ZF125105 z軸 up-H加減速
        sdat += modAxis.zUP_H.acc.ToString()
        SendMsg(sdat)                   '   data send
        sdat = "WR ZF125106 "           '   ZF125106 z軸 up-L加減速
        sdat += modAxis.zUP_L.acc.ToString()
        SendMsg(sdat)                   '   data send

    End Sub
    '   原点復帰動作指令
    Public Sub SendOrigin()

        Dim sdat As String

        'sdat = "WR MR90000 1"           '   MR90000 origin
        sdat = "WR DM2000 1"            '   DM2000 origin
        SendMsg(sdat)                   '   data send

    End Sub
    '   error reset
    Public Sub SendReset()

        Dim sdat As String

        'sdat = "WR MR120005 1"          '   MR120005 reset
        sdat = "WR DM2002 1"            '   DM2002 reset
        SendMsg(sdat)                   '   data send

    End Sub
    '   auto start
    Public Sub SendAtRun()

        Dim sdat As String

        sdat = "WR DM2012 1"            '   DM2012 start = 1

        SendMsg(sdat)                   '   data send

    End Sub
    '   auto stop
    Public Sub SendAtStop(ByVal temp As Boolean)

        Dim sdat As String

        If temp = True Then
            sdat = "WR DM2014 1"        '   DM2014 tempstop = 1
        Else
            sdat = "WR DM2014 2"        '   DM2014 stop = 2
        End If
        SendMsg(sdat)                   '   data send

    End Sub
    '   order send
    Public Sub SendOrder(ByVal sel() As Boolean, ByVal glass As Integer, ByVal lane As Integer, ByVal odrnm As Integer, ByVal ice As Integer)

        Dim sdat As String
        Dim adr, std, i As Integer

        std = 2030
        For i = 0 To 42
            If sel(i) = True Then
                adr = std + i * 10      '   2030～ 停止位置情報
                sdat = "WR DM" + adr.ToString() + " 1"
                SendMsg(sdat)           '   data send
            End If
        Next

        sdat = "WR DM2010 "             '   DM2010 排出レーン
        sdat += lane.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WRS DM2020 5"           '   DM2020 注文番号, 2022 グラス, 2024 氷
        sdat += " " + odrnm.ToString()
        sdat += " 0 " + glass.ToString()
        sdat += " 0 " + ice.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR DM2004 1"            '   DM2004 order 
        SendMsg(sdat)                   '   data send

    End Sub
    '   order send (no use)
    Public Sub SendOrder(ByVal sel As Integer, ByVal glass As Integer, ByVal lane As Integer, ByVal odrnm As Integer, ByVal ice As Integer, ByVal water As Integer)

        Dim sdat As String
        Dim adr, std As Integer

        If water > 0 Then               '   水/炭酸あり
            adr = 2430                  '   DM2430 炭酸, DM2440 水
            adr += (water - 1) * 10
            sdat = "WR DM" + adr.ToString() + " 1"
            SendMsg(sdat)               '   data send
        End If

        std = 2030
        adr = std + sel * 10
        sdat = "WR DM" + adr.ToString() + " 1"
        SendMsg(sdat)                   '   data send

        sdat = "WR DM2010 "             '   DM2010 排出レーン
        sdat += lane.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WRS DM2020 5"           '   DM2020 注文番号, 2022 グラス, 2024 氷
        sdat += " " + odrnm.ToString()
        sdat += " 0 " + glass.ToString()
        sdat += " 0 " + ice.ToString()
        SendMsg(sdat)                   '   data send

        sdat = "WR DM2004 1"            '   DM2004 order 
        SendMsg(sdat)                   '   data send

    End Sub
    '   cycle動作指令
    Public Sub SendCycle(ByVal mode As Integer, Optional ByVal pos As Integer = 0)

        Dim sdat As String
        Dim adr, std As Integer

        If pos > 0 Then
            If mode = 0 Then
                sdat = "WR DM8200"
            Else
                sdat = "WR DM8201"
            End If
            sdat += " " + pos.ToString()    '   DM820* 指定位置

            SendMsg(sdat)                   '   data send
        End If

        std = 85000
        adr = std + mode                '   MR8500* cycle指令

        sdat = "WR MR"
        sdat += adr.ToString() + " 1"

        SendMsg(sdat)                   '   data send

    End Sub
    '   コンベア動作指令
    Public Sub SendConveyor(ByVal mov As Boolean)

        Dim sdat As String

        If mov = True Then
            sdat = "WR MR99008"             '   MR99008 conveyor on
        Else
            sdat = "WR MR99009"             '   MR99009 conveyor off
        End If
        sdat += " 1"

        SendMsg(sdat)                   '   data send

    End Sub
    '   ストッカXZ動作指令
    Public Sub SendStockerXZ(ByVal sel As Integer, Optional ByVal pos As Integer = 0)

        Dim sdat As String
        Dim adr, std As Integer

        If pos > 0 Then
            sdat = "WR DM8010 " + pos.ToString()        '   DM8010 指定位置

            SendMsg(sdat)                   '   data send
        End If

        std = 83100
        adr = std + sel                 '   MR8310* 動作指令

        sdat = "WR MR"
        sdat += adr.ToString() + " 1"

        SendMsg(sdat)                   '   data send

    End Sub
    '   シリンダー動作指令 (no use)
    Public Sub SendCylinder(ByVal con As Integer, ByVal coff As Integer)

        Dim sdat, ctrl As String
        Dim str, std As Integer

        std = 80000
        sdat = "WRS MR"
        If con < coff Then
            str = std + con
            ctrl = " 1 0"
        Else
            str = std + coff
            ctrl = " 0 1"
        End If
        sdat += str.ToString() + " 2"
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   シリンダー動作指令
    Public Sub SendCylinder(ByVal con As Integer)

        Dim sdat As String
        Dim adr, std As Integer

        std = 80000
        adr = std + con                 '   MR80000 ~ MR80315

        sdat = "WR MR"
        sdat += adr.ToString() + " 1"

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールY軸動作指令 (no use)
    Public Sub SendBeelX(ByVal pos As Integer)

        Dim sdat, ctrl As String
        Dim std, i As Integer

        std = 83400
        sdat = "WRS MR"
        ctrl = ""
        For i = 0 To 3
            If pos = i Then
                ctrl += " 1"
            Else
                ctrl += " 0"
            End If
        Next
        sdat += std.ToString() + " 4"
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールY軸動作指令
    Public Sub SendBeelY(ByVal pos As Integer)

        Dim sdat As String
        Dim std, adr As Integer

        std = 83400
        adr = std + pos                 '   MR8340* 動作指令

        sdat = "WR MR"
        sdat += adr.ToString() + " 1"

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールZ軸動作指令
    Public Sub SendBeelZ(ByVal pos As Integer)

        Dim sdat As String
        Dim std, adr As Integer

        std = 83600
        adr = std + pos                 '   MR8360* 動作指令

        sdat = "WR MR"
        sdat += adr.ToString() + " 1"

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールY jog +
    Public Sub SendJogBeelYp(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99000"             '   MR99000 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールY jog -
    Public Sub SendJogBeelYm(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99001"             '   MR99001 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールZ jog +
    Public Sub SendJogBeelZp(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99002"             '   MR99002 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ビールZ jog -
    Public Sub SendJogBeelZm(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99003"             '   MR99003 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ストックX jog +
    Public Sub SendJogStockXp(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99004"             '   MR99004 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ストックX jog -
    Public Sub SendJogStockXm(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99005"             '   MR99005 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ストックZ jog +
    Public Sub SendJogStockZp(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99006"             '   MR99006 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub
    '   ストックZ jog -
    Public Sub SendJogStockZm(ByVal mov As Boolean)

        Dim sdat, ctrl As String

        sdat = "WR MR99007"             '   MR99007 jog動作
        If mov = True Then
            ctrl = " 1"
        Else
            ctrl = " 0"
        End If
        sdat += ctrl

        SendMsg(sdat)                   '   data send

    End Sub

    '   sample mode data send
    Public Sub SendSampleData()

        Exit Sub '  no use

        Dim sdat, ascd, dat As String
        Dim i, lng As Integer
        Dim tmp As String

        tmp = modApp.pocket1
        'tmp = modSetting.Pocket(modMAINCns.TYPE_01)
        sdat = "WRS ZF6120 5"           '   6120～ pocket type1
        lng = tmp.Length
        For i = 0 To 4
            ascd = " "
            If i * 2 + 1 < lng Then
                ascd += (Asc(tmp(i * 2)) * 256 + Asc(tmp(i * 2 + 1))).ToString()    '   character
            ElseIf i * 2 < lng Then
                ascd += (Asc(tmp(i * 2)) * 256).ToString()  '   character
            Else
                ascd += (Asc(vbNullChar)).ToString()
            End If
            sdat += ascd
        Next
        SendMsg(sdat)                   '   data send

        tmp = modApp.pocket2
        'tmp = modSetting.Pocket(modMAINCns.TYPE_02)
        sdat = "WRS ZF6130 5"           '   6130～ pocket type2
        lng = tmp.Length
        For i = 0 To 4
            ascd = " "
            If i * 2 + 1 < lng Then
                ascd += (Asc(tmp(i * 2)) * 256 + Asc(tmp(i * 2 + 1))).ToString()    '   character
            ElseIf i * 2 < lng Then
                ascd += (Asc(tmp(i * 2)) * 256).ToString()  '   character
            Else
                ascd += (Asc(vbNullChar)).ToString()
            End If
            sdat += ascd
        Next
        SendMsg(sdat)                   '   data send

        sdat = "WRS DM5071 2"                           '   5071(個数),5072(box)
        dat = " " + modSetting.Sample.ToString()
        sdat += dat
        dat = " " + (modSetting.SampleBox + 1).ToString()
        sdat += dat

        SendMsg(sdat)                   '   data send

    End Sub

    '   debug log
    Private Sub DebugLog(ByVal sdat As String)

        Dim file As String = modApp.path & "\SepSend.log"

        '   file 追記
        Dim sw As New System.IO.StreamWriter(file, True, System.Text.Encoding.GetEncoding("shift_jis"))
        sw.WriteLine(sdat)
        sw.Close()

    End Sub
    Public Sub DeleteLog()

        Dim file As String = modApp.path & "\SepSend.log"
        If System.IO.File.Exists(file) Then
            System.IO.File.Delete(file)
        End If

    End Sub

    '   測定DM clear
    Public Sub ClearStart()

        Dim sdat As String

        sdat = "WR ZF7000 0"            '   7000 -> 0
        SendMsg(sdat)                   '   data send

    End Sub
    '   Error log DM clear
    Public Sub ClearErrLog()

        Dim i As Integer
        Dim sdat, dat As String

        sdat = "WR MR800 0"             '   800 -> 0
        SendMsg(sdat)                   '   data send

        sdat = "WRS ZF7300 15"          '   7300 ～ 7314 -> 0
        For i = 1 To 15
            dat = " 0"
            sdat += dat
        Next
        SendMsg(sdat)                   '   data send

    End Sub

    '   M/C type 確認
    Public Function GetMCType() As Boolean

        Dim getd As String
        Dim nm As Integer

        If SendMsg("RD ZF36080") = False Then   '   36080 M/C type
            getd = dt_Receive1

            nm = CInt(getd)
            If nm = 0 Then                      '   0 = Taping type
                modApp.m_Type = modMAINCns.MCTAP
                modSetting.McType = modMAINCns.MCTAP
            Else                                '   0以外 Tray type
                modSetting.McType = modMAINCns.MCTRY
                If nm = 1 Then
                    modApp.m_Type = modMAINCns.TRYV1
                Else
                    modApp.m_Type = modMAINCns.TRYV2
                End If
            End If

            Return False
        Else
            Return True
        End If

    End Function
    '   axis data send
    Public Sub GetAxisData()

        Dim sdat, getd As String
        Dim i, nm, tmp As Integer
        Dim dat(10) As Integer

        sdat = "RD ZF110002.L"          '   ZF110002 (2WD) y軸 待機位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.yWait.pos = CDbl(tmp) / 100     '   ZF110002
        sdat = "RD ZF110004.L"          '   ZF110004 (2WD) y軸 back位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.yBack.pos = CDbl(tmp) / 100     '   ZF110004
        sdat = "RD ZF110010.L"          '   ZF110010 (2WD) y軸 コンベア位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.yConb.pos = CDbl(tmp) / 100     '   ZF110010
        sdat = "RD ZF110012.L"          '   ZF110012 (2WD) y軸 ビール位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.yBeel.pos = CDbl(tmp) / 100     '   ZF110012

        sdat = "RDS ZF120001 6"         '   ZF120001 y軸 待機速度, 120002 y軸 back速度, 120005 y軸 コンベア速度, 120006 y軸 ビール速度
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        For i = 0 To 5
            nm = getd.IndexOf(" ")
            If nm < 0 Then
                tmp = CInt(getd)
            Else
                tmp = CInt(getd.Substring(0, nm))
            End If
            dat(i) = tmp
        Next
        modAxis.yWait.spd = dat(0)      '   ZF120001
        modAxis.yBack.spd = dat(1)      '   ZF120002
        modAxis.yConb.spd = dat(4)      '   ZF120005
        modAxis.yBeel.spd = dat(5)      '   ZF120006

        sdat = "RDS ZF125001 6"         '   ZF125001 y軸 待機加減速, 125002 y軸 back加減速, 125005 y軸 コンベア加減速, 125006 y軸 ビール加減速  
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        For i = 0 To 5
            nm = getd.IndexOf(" ")
            If nm < 0 Then
                tmp = CInt(getd)
            Else
                tmp = CInt(getd.Substring(0, nm))
            End If
            dat(i) = tmp
        Next
        modAxis.yWait.acc = dat(0)      '   ZF125001
        modAxis.yBack.acc = dat(1)      '   ZF125002
        modAxis.yConb.acc = dat(4)      '   ZF125005
        modAxis.yBeel.acc = dat(5)      '   ZF125006

        sdat = "RD ZF110202.L"          '   ZF110202 (2WD) z軸 待機位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.zWait.pos = CDbl(tmp) / 100     '   110202
        sdat = "RD ZF110204.L"          '   ZF110204 (2WD) z軸 down-H位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.zDwnH.pos = CDbl(tmp) / 100     '   110204
        sdat = "RD ZF110206.L"          '   ZF110206 (2WD) z軸 down-L位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.zDwnL.pos = CDbl(tmp) / 100     '   110206
        sdat = "RD ZF110210.L"          '   ZF110210 (2WD) z軸 up-H位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.zUP_H.pos = CDbl(tmp) / 100     '   110210
        sdat = "RD ZF110212.L"          '   ZF110212 (2WD) z軸 up-L位置
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        tmp = CInt(getd)
        modAxis.zUP_L.pos = CDbl(tmp) / 100     '   110212

        sdat = "RDS ZF120101 6"         '   ZF120101 z軸 待機速度, 120102 z軸 down-H速度, 120103 z軸 down-L速度, 120105 z軸 up-H速度, 120106 z軸 up-L速度
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        For i = 0 To 5
            nm = getd.IndexOf(" ")
            If nm < 0 Then
                tmp = CInt(getd)
            Else
                tmp = CInt(getd.Substring(0, nm))
            End If
            dat(i) = tmp
        Next
        modAxis.zWait.spd = dat(0)      '   ZF120101
        modAxis.zDwnH.spd = dat(1)      '   ZF120102
        modAxis.zDwnL.spd = dat(2)      '   ZF120103
        modAxis.zUP_H.spd = dat(4)      '   ZF120105
        modAxis.zUP_L.spd = dat(5)      '   ZF120106

        sdat = "RDS ZF125101 6"         '   ZF125101 z軸 待機加減速, 125102 z軸 down-H加減速, 125103 z軸 down-L加減速, 125105 z軸 up-H加減速, 125106 z軸 up-L加減速
        SendMsg(sdat)                   '   data send
        getd = dt_Receive1
        For i = 0 To 5
            nm = getd.IndexOf(" ")
            If nm < 0 Then
                tmp = CInt(getd)
            Else
                tmp = CInt(getd.Substring(0, nm))
            End If
            dat(i) = tmp
        Next
        modAxis.zWait.acc = dat(0)      '   ZF125101
        modAxis.zDwnH.acc = dat(1)      '   ZF125102
        modAxis.zDwnL.acc = dat(2)      '   ZF125103
        modAxis.zUP_H.acc = dat(4)      '   ZF125105
        modAxis.zUP_L.acc = dat(5)      '   ZF125106

    End Sub
    '   軸現在値
    Public Sub GetAxisPosition(ByRef yaxis As Double, ByRef zaxis As Double)

        Dim ret, tmp As Integer
        Dim getd As String

        ret = 0
        If SendMsg("RD ZF100004.L") = False Then    '   ZF100004 (2WD) Y軸
            getd = dt_Receive1

            tmp = CInt(getd)
            yaxis = CDbl(tmp) / 100         '   Y軸現在値

        End If
        If SendMsg("RD ZF100104.L") = False Then    '   ZF100104 (2WD) Z軸
            getd = dt_Receive1

            tmp = CInt(getd)
            zaxis = CDbl(tmp) / 100         '   Z軸現在値

        End If

    End Sub
    '   測定start 確認
    Public Function GetStart(ByRef hdw(,) As Boolean) As Integer

        Dim ret, tmp, nm As Integer
        Dim getd As String
        Dim hd, ln, ckd, i As Integer

        ret = 0
        'If SendMsg("RD ZF7000") = False Then        '   7000 start sig  1*->measure start
        'ret = CInt(dt_Receive1)                     '   3->Runlot end, 4->Reellot end, 5-> cal mode
        If SendMsg("RDS ZF7000 15") = False Then
            getd = dt_Receive1
            nm = getd.IndexOf(" ")
            ret = CInt(getd.Substring(0, nm))       '   7000 start sig  1*->measure start, 3->Runlot end, 4->Reellot end, 5-> cal mode

            getd = getd.Substring(nm + 1)
            nm = getd.IndexOf(" ")
            'modRunLot.boxQty(SEP_CAMERA1_NG) = CInt(getd.Substring(0, nm))  '   7001 Camera1NG q'ty runlot
            getd = getd.Substring(nm + 1)
            nm = getd.IndexOf(" ")
            'modReelLot.RunLot(modReelLot.lotNum).boxQty(SEP_CAMERA1_NG) = CInt(getd.Substring(0, nm)) '   7002 Camera1NG q'ty reellot
            getd = getd.Substring(nm + 1)
            nm = getd.IndexOf(" ")
            'modRunLot.boxQty(SEP_CAMERA2_NG) = CInt(getd.Substring(0, nm))  '   7003 Camera2NG q'ty runlot
            getd = getd.Substring(nm + 1)
            nm = getd.IndexOf(" ")
            'modReelLot.RunLot(modReelLot.lotNum).boxQty(SEP_CAMERA2_NG) = CInt(getd.Substring(0, nm)) '   7004 Camera2NG q'ty reellot

            If ret >= 10 Then
                For i = 0 To 5
                    getd = getd.Substring(nm + 1)
                    nm = getd.IndexOf(" ")
                Next

                For hd = 0 To 3
                    getd = getd.Substring(nm + 1)   '   7011 HD1, 7012 HD2, 7013 HD3, 7014 HD4
                    nm = getd.IndexOf(" ")
                    If nm < 0 Then
                        tmp = CInt(getd)
                    Else
                        tmp = CInt(getd.Substring(0, nm))
                    End If

                    For ln = 0 To 3
                        hdw(hd, ln) = False
                    Next

                    '   bit0->Line1有無, bit1->Line2有無, bit2->Line3有無, bit3->Line4有無
                    ckd = &H1
                    For ln = 0 To 3
                        If (tmp And ckd) <> 0 Then
                            hdw(hd, ln) = True
                        End If
                        ckd = ckd << 1
                    Next
                Next

                '   HD4 ワークあり -> count up
                If tmp > 0 Then
                    ret = CD_MES_CNT    '   countあり
                Else
                    ret = CD_MES_NCT    '   countなし
                End If
            End If
        End If

        'If SendMsg("RD ZF5146") = False Then        '   5146 camera ng q'ty
        '    modRunLot.boxQty(22) = CInt(dt_Receive1)
        'End If

        Return ret

    End Function
    '   MC status 確認
    Public Function GetMainStatus(ByRef ferr As Boolean) As Integer

        Dim getd As String
        Dim ret As Integer

        If SendMsg("RD DM200", GMD_TPG) = False Then    '   200 main status
            getd = dt_Receive2
            ret = CInt(getd)                            '   
        End If
        If SendMsg("RD MR800", GMD_TPG) = False Then    '   800 error flag
            getd = dt_Receive2
            ferr = False
            If CInt(getd) > 0 Then              '   error発生
                ferr = True
                Call GetLogErr()                '   get error code
            End If
        End If

        Return ret

    End Function
    '   unit status 確認
    Public Sub GetUnitStatus(ByRef stat() As Integer)

        Dim ret As Integer
        Dim i, nm As Integer
        Dim getd, dat As String

        On Error Resume Next

        ret = 0
        If SendMsg("RDS FM6200 11", GMD_TPG) = False Then   '   6200～6210 unit status
            getd = dt_Receive2                              '   get data
            For i = 0 To 10
                nm = getd.IndexOf(" ")
                dat = getd.Substring(0, nm)
                stat(i) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If

    End Sub
    '   cycle status 確認
    Public Sub GetCycleStatus(ByRef stat() As Integer)

        Dim ret As Integer
        Dim i, nm, md, temp As Integer
        Dim getd, dat As String

        On Error Resume Next

        For i = 0 To 10
            stat(i) = frmMain.US_NOT
        Next

        ret = 0
        If SendMsg("RD R40112", GMD_TPG) = False Then
            getd = dt_Receive2                              '   get data
            temp = CInt(getd)
            If temp > 0 Then                    '   if running
                stat(0) = frmMain.US_RUN        '   over write
            End If
        End If
        If SendMsg("RDS MR35000 16", GMD_TPG) = False Then  '   MR35000～35015 cycle move status
            getd = dt_Receive2                              '   get data
            For i = 0 To 10
                md = i + 1
                nm = getd.IndexOf(" ")
                dat = getd.Substring(0, nm)

                temp = CInt(dat)
                If temp > 0 Then                    '   if running
                    stat(md) = frmMain.US_RUN       '   over write
                End If
                getd = getd.Substring(nm + 1)
            Next
        End If
        If SendMsg("RDS MR105000 16", GMD_TPG) = False Then '   MR100500～100515 cycle comp status
            getd = dt_Receive2                              '   get data
            For i = 0 To 10
                md = i + 1
                nm = getd.IndexOf(" ")
                dat = getd.Substring(0, nm)

                temp = CInt(dat)
                If temp > 0 Then                    '   if move comp
                    stat(md) = frmMain.US_CMP       '   over write
                End If

                getd = getd.Substring(nm + 1)
            Next
        End If

    End Sub
    '   sensor status 確認
    Public Sub GetSensorStatus(ByRef stat() As Integer)

        Dim ret As Integer
        Dim i, nm, dt, st As Integer
        Dim getd, dat As String

        On Error Resume Next

        ret = 0
        st = 0
        If SendMsg("RDS MR100000 16", GMD_TPG) = False Then '   MR100000～100015 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 16
        If SendMsg("RDS MR100100 16", GMD_TPG) = False Then '   100100～100115 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 16
        If SendMsg("RDS MR100200 16", GMD_TPG) = False Then '   100200～100215 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 16
        If SendMsg("RDS MR100300 16", GMD_TPG) = False Then '   100300～100315 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 16
        If SendMsg("RDS MR103400 16", GMD_TPG) = False Then '   103400～103415 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 16
        If SendMsg("RDS MR103600 16", GMD_TPG) = False Then '   103600～103615 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If

        st += 16
        If SendMsg("RDS R42200 16", GMD_TPG) = False Then   '   42200～42215 sensor status
            getd = dt_Receive2                              '   get data
            For i = 0 To 15
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If

    End Sub
    '   axis status
    Public Sub GetAxisStatus(ByRef stat() As Integer, ByRef stk As Integer)

        Dim ret As Integer
        Dim i, nm, dt, st As Integer
        Dim getd, dat As String

        On Error Resume Next

        ret = 0
        st = 110
        If SendMsg("RDS MR103400 5", GMD_TPG) = False Then  '   MR103400～103404 ビールY軸 status
            getd = dt_Receive2                              '   get data
            For i = 0 To 4
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 5
        If SendMsg("RDS MR103600 5", GMD_TPG) = False Then  '   MR103600～103604 ビールZ軸 status
            getd = dt_Receive2                              '   get data
            For i = 0 To 4
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If
        st += 5
        If SendMsg("RDS MR53100 5", GMD_TPG) = False Then   '   MR53100～53104 ストッカXZ軸 status
            getd = dt_Receive2                              '   get data
            For i = 0 To 4
                dt = st + i
                nm = getd.IndexOf(" ")
                If nm < 0 Then
                    dat = getd
                Else
                    dat = getd.Substring(0, nm)
                End If
                stat(dt) = CInt(dat)
                getd = getd.Substring(nm + 1)
            Next
        End If

        If SendMsg("RD DM8011", GMD_TPG) = False Then       '   DM8011 ストッカXZ軸 pos
            getd = dt_Receive2                              '   get data

            stk = CInt(getd)
        End If


    End Sub


    '   Log Error code get
    Public Sub GetLogErr()

        Dim ret, i, nm As Integer
        Dim getd, dat As String

        ret = 0
        If SendMsg("RDS ZF7300 15", GMD_LOG) = False Then   '   7300～7314 (3WDでひとつのErrorCode)
            getd = dt_Receive3                              '   get data

            For i = 0 To 13
                nm = getd.IndexOf(" ")
                dat = getd.Substring(0, nm)
                stkEcd(i) = dat
                getd = getd.Substring(nm + 1)
            Next
            stkEcd(14) = getd
        End If

    End Sub

End Module
