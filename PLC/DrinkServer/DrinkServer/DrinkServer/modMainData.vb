Option Strict On

Imports System.Globalization

'   定数定義
Module modMAINCns

    Public Const MD_UPDSP = 199

    Public Const MC_HD1 = 0         '   M/C HD1
    Public Const MC_HD2 = 1         '   M/C HD2
    Public Const MC_HD3 = 2         '   M/C HD3
    Public Const MC_HD4 = 3         '   M/C HD4

    Public Const NA_DLD1 = 0        '   NetworkAnalyzer DLD1
    Public Const NA_DLD2 = 1        '   NetworkAnalyzer DLD2
    Public Const NA_DLD3 = 2        '   NetworkAnalyzer DLD3
    Public Const NA_DLD4 = 3        '   NetworkAnalyzer DLD4
    Public Const DOG_HD1 = 0        '   DOG HD1
    Public Const DOG_HD2 = 1        '   DOG HD2
    Public Const DOG_HD3 = 2        '   DOG HD3
    Public Const DOG_HD4 = 3        '   DOG HD4

    Public Const POS_DOG1 = 0       '   DOG1 Line position
    Public Const POS_DOG2 = 1       '   DOG2 Line position
    Public Const POS_DOG3 = 2       '   DOG3 Line position
    Public Const POS_DOG4 = 3       '   DOG4 Line position
    Public Const POS_DLD1 = 2       '   DLD1 Line position
    Public Const POS_DLD2 = 3       '   DLD2 Line position
    Public Const POS_DLD3 = 0       '   DLD3 Line position
    Public Const POS_DLD4 = 1       '   DLD4 Line position

    Public Const MCTAP = 0          '   M/C type taping
    Public Const MCTRY = 1          '   M/C type tray
    Public Const TRYV1 = 1          '   tray ver.1
    Public Const TRYV2 = 2          '   tray ver.2

    Public Const PCKG_01 = 0        '   package type 1 (pocket1)
    Public Const PCKG_02 = 1        '   package type 2 (pocket2)
    Public Const PCKG_03 = 2        '   package type 3 -> なし

    Public Const POSCAL_NORM = 0    '   cal position normal
    Public Const POSCAL_USE1 = 1    '   cal position use 1pocket
    Public Const POSCAL_USE2 = 2    '   cal position use 2pocket

    Public Const PWREQP_01 = 0      '   E3631A
    Public Const PWREQP_02 = 1      '   E3640A

    '   IFBW list
    Public Const RBW00 = 0      '   15kHz
    Public Const RBW01 = 1      '   10kHz
    Public Const RBW02 = 2      '   7kHz
    Public Const RBW03 = 3      '   5kHz
    Public Const RBW04 = 4      '   4kHz
    Public Const RBW05 = 5      '   3kHz
    Public Const RBW06 = 6      '   2kHz
    Public Const RBW07 = 7      '   1.5kHz
    Public Const RBW08 = 8      '   1kHz
    Public Const RBW09 = 9      '   700Hz
    Public Const RBW10 = 10     '   500Hz
    Public Const RBW11 = 11     '   400Hz
    Public Const RBW12 = 12     '   300Hz
    Public Const RBW13 = 13     '   200Hz
    Public Const RBW14 = 14     '   150Hz
    Public Const RBW15 = 15     '   100Hz
    Public Const RBW16 = 16     '   70Hz
    Public Const RBW17 = 17     '   50Hz
    Public Const RBW18 = 18     '   40Hz
    Public Const RBW19 = 19     '   30Hz
    Public Const RBW20 = 20     '   20Hz
    Public Const RBW21 = 21     '   15Hz
    Public Const RBW22 = 22     '   10Hz

    Public Const MODE_TOPMOST = 101     '   form most top disp
    Public Const MODE_NOTOPMOST = 102   '   form not most top disp
    Public Const MODE_MINIMIZED = 103   '   form disp minimize
    Public Const MODE_NORMAL = 104      '   form disp normal

    Public Const GLIMT = 200            '   graph limit

    Public Const CAL_DLD_NOT = 0        '   DLD not cal
    Public Const CAL_DLD_OPEN = 1       '   DLD open
    Public Const CAL_DLD_SHORT = 2      '   DLD short
    Public Const CAL_DLD_LOAD = 3       '   DLD load
    Public Const CAL_DLD_0OHM = 4       '   DLD master
    Public Const CAL_DLD_50OHM = 5      '   DLD 50ohm check
    Public Const CAL_DLD_COMP = 6       '   DLD ok
    Public Const CAL_INH_NOT = 0        '   INH not cal
    Public Const CAL_INH_OPEN = 1       '   INH open
    Public Const CAL_INH_SHORT = 2      '   INH short
    Public Const CAL_INH_LOAD = 3       '   INH load
    Public Const CAL_INH_0OHM = 4       '   INH master
    Public Const CAL_INH_50OHM = 5      '   INH 50ohm check
    'Public Const CAL_INH_MASTER1 = 5    '   INH master1
    'Public Const CAL_INH_MASTER2 = 6    '   INH master2
    Public Const CAL_INH_COMP = 6       '   INH ok

End Module

'   application data
Module modApp

    '   Pckage type
    Public PckgType() As String =
        {"1210", "1612", "2016", "2520", "3225", "", ""}

    Public ver As String        '   vertion
    Public path As String       '   app. path

    Public f_MdEdit As Boolean  '   edit mode
    Public f_Debug As Boolean   '   debug flag
    Public f_ComDbg As Boolean  '   通信debug flag

    Public m_Type As Integer    '   M/C type 0:taping, 1:tray-v1, 2:tray-v2

    Public pocket1 As String    '   pocket1 type
    Public pocket2 As String    '   pocket2 type

    Public Package(50) As String    '   Package name
    Public Xtal(200, 2) As String   '   X'tal name / pocket

    '   debug file read
    Public Sub ReadDebug(ByRef fdbg() As Boolean)

        Dim path As String = modApp.path & "\prgDebug.txt"

        If (System.IO.File.Exists(path)) Then      '    file 確認
            Dim data(10) As String
            Dim nm As Integer

            For nm = 0 To 9
                data(nm) = ""
            Next

            nm = 0
            Dim sr As New System.IO.StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"))
            '内容を一行ずつ読み込む
            While sr.Peek() > -1
                data(nm) = (sr.ReadLine())
                nm = nm + 1
            End While
            '閉じる
            sr.Close()

            On Error Resume Next

            For i = 0 To 9
                If String.IsNullOrEmpty(data(i)) = True Then Exit For

                nm = data(i).IndexOf(",1")
                If nm >= 0 Then
                    fdbg(i) = True
                End If
            Next

        End If

    End Sub

    '   mode file read
    Public Sub ReadModeFile()

        Dim path As String = modApp.path & "\Mode.txt"

        f_MdEdit = False
        If (System.IO.File.Exists(path)) Then      '    file 確認
            Dim data As String

            Dim sr As New System.IO.StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"))
            data = (sr.ReadLine())
            sr.Close()

            If CInt(data) = 1 Then
                f_MdEdit = True         '   edit mode
            End If
        End If

    End Sub

    '   check file name charactor
    Public Function CheckFileCharactor(ByVal name As String) As Boolean

        If name.IndexOf("\") > 0 Or name.IndexOf("/") > 0 Or name.IndexOf(":") > 0 Or _
            name.IndexOf("*") > 0 Or name.IndexOf("?") > 0 Or name.IndexOf("""") > 0 Or _
            name.IndexOf("<") > 0 Or name.IndexOf(">") > 0 Or name.IndexOf("|") > 0 Then
            Return True
        Else
            Return False
        End If

    End Function

End Module

'   setting data
Module modSetting

    Public SpecFolder As String     '   specdata folder
    Public RunLotFolder As String   '   run lot folder
    Public ReelLotFolder As String  '   reel lot folder
    Public DrvFolder As String      '   DriveLevel file folder
    Public OpIDFolder As String     '   operatorID file folder
    'Public MastFolder As String     '   master file folder
    Public FLdtFolder As String     '   FL file folder
    Public CalHstFolder As String   '   Cal History folder
    Public MCRatioFolder As String  '   稼働率 file folder
    Public MCLogFolder As String    '   M/C log file folder
    Public CIdatFolder As String    '   CIdta folder

    Public contErr As Integer       '	continue error
    Public ReMeas As Integer        '   NoOSC 再測定回数

    Public MCno As String           '   M/C no.
    Public McType As Integer        '   M/C type

    Public IPadPLC(4) As String     '   M/C PLC IP address
    Public PortPLC As String        '   PLC port no.
    Public IPadDOG1(4) As String    '   DOG1 IP address
    Public PortDOG1 As String       '   DOG1 port no.
    Public IPadDOG2(4) As String    '   DOG2 IP address
    Public PortDOG2 As String       '   DOG2 port no.

    Public NGSepBox(50) As Integer  '   NG separate box
    Public NGSepPri(50) As Integer  '   NG priority
    Public selMaster As Integer     '   latest master sel
    Public Master(999, 3) As String '   Master name / 1no. / 2no.

    Public PcktNm(2) As Integer     '   pocket X'tal size (1,2)
    Public PckPkg(2) As String      '   pocket package size (1,2)
    Public PckTyp(2) As String      '   pocket type name (1,2)

    Public Tap_LdNoSl As Integer    '   taping lead no seal
    Public Tap_LdEmp As Integer     '   taping lead empty
    Public Tap_TlNoSl As Integer    '   taping tail no seal
    Public Tap_TlEmp As Integer     '   taping tail empty

    Public StatRange(10) As Single  '   統計data 有効範囲 (±)
    Public Cam_Check(5) As Boolean  '   camera check(lid/char/befTap/aftTap)
    Public WaitMeas As Integer      '   測定前 wait
    Public WaitCal As Integer       '   CAL測定前 wait
    Public WaitInter As Integer     '   干渉防止 wait

    Public Password1 As String      '   password 1
    Public Password2 As String      '   password 2

    Public SampleChk As Boolean     '   sample check
    Public Sample As Integer        '   sample q'ty
    Public SampleBox As Integer     '   sample box
    Public SampleLim As Single      '   sample rate limit

    Public CamNGBox As Integer      '   camera NG box
    Public NoTapBox As Integer      '   no taping box
    Public CalPosi As Integer       '   Cal position set

    Public TapDatSv As Boolean      '   taping data save
    'Public TimeRatio(2) As Integer  '   M/C稼働率 読み込み時刻

    Public PwrUnit As Integer       '   DC power unit
    Public ATTvalue As Double       '   attenuator value

    Public useLine(5) As Integer    '   M/C Line use flag

    Public LogOkRmv As Boolean      '   log OKremove 有無
    Public LogNgRmv As Boolean      '   log NGremove 有無
    Public LogL1Drp As Boolean      '   log LdrNGbox1 drop 有無
    Public LogRARmv As Boolean      '   log RankA remove 有無
    Public LogRBRmv As Boolean      '   log RankB remove 有無

    '   read file
    Public Function ReadFile() As Boolean

        Dim path As String = modApp.path & "\setting.txt"

        If (System.IO.File.Exists(path)) Then      '    file 確認
            Dim lines(100) As String
            Dim nm, i As Integer

            nm = 0
            Dim sr As New System.IO.StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"))
            '内容を一行ずつ読み込む
            While sr.Peek() > -1
                lines(nm) = (sr.ReadLine())
                nm = nm + 1
            End While
            '閉じる
            sr.Close()
            lines(nm) = String.Empty

            Call SetData(lines)         '   data set

            Return False
        Else
            Dim msg As String
            msg = "setting file open failed!"
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return True
        End If

    End Function

    '    file save
    Public Sub SaveFile(ByVal data() As String)

        Dim path As String = modApp.path & "\Setting.txt"

        System.IO.File.WriteAllLines(path, data)

    End Sub

    '   set data
    Public Sub SetData(ByVal lines() As String)

        Dim tmp As String

        IPadPLC(0) = lines(0)
        IPadPLC(1) = lines(1)
        IPadPLC(2) = lines(2)
        IPadPLC(3) = lines(3)
        PortPLC = lines(4)

        tmp = IPadPLC(0) & "." & IPadPLC(1) & "." & IPadPLC(2) & "." & IPadPLC(3)
        comPLC.ipAdd = System.Net.IPAddress.Parse(tmp)
        comPLC.port = CInt(PortPLC)

    End Sub

End Module

'   OperatorID data
Module modOperatorID

    Public Member(200) As String
    Public dtMax As Integer

    Private lines(130) As String

    '   read file
    Public Function ReadFile() As Boolean

        Dim path As String = modSetting.OpIDfolder & "\OperatorID.txt"

        If (System.IO.File.Exists(path)) Then      '    file 確認
            Dim nm, i As Integer

            nm = 0
            Dim sr As New System.IO.StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"))
            '内容を一行ずつ読み込む
            While sr.Peek() > -1
                lines(nm) = (sr.ReadLine())
                nm = nm + 1
            End While
            '閉じる
            sr.Close()
            lines(nm) = String.Empty

            For i = 0 To nm
                Member(i) = lines(i)
            Next
            dtMax = nm
            Return False
        Else
            Dim msg As String
            msg = "OperatorID file open failed!"
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return True
        End If

    End Function


End Module

'   MC axis data
Module modAxis

    Public Structure strData

        Dim pos As Double       '   position
        Dim spd As Integer      '   speed
        Dim acc As Integer      '   acc/dcc

    End Structure

    Public yWait As strData     '   Y軸待機
    Public yBack As strData     '   Y軸back
    Public yConb As strData     '   Y軸コンベア
    Public yBeel As strData     '   Y軸ビール
    Public ySpe1 As strData     '   Y軸spea1
    Public ySpe2 As strData     '   Y軸spea2

    Public zWait As strData     '   Z軸待機
    Public zDwnH As strData     '   Z軸down-H
    Public zDwnL As strData     '   Z軸down-L
    Public zUP_H As strData     '   Z軸up-H
    Public zUP_L As strData     '   Z軸up-L
    Public zSpe1 As strData     '   Z軸spea1

    '   read file (no use)
    Public Function ReadFile() As Boolean

        Dim path As String = modApp.path & "\Axis.txt"

        If (System.IO.File.Exists(path)) Then      '    file 確認
            Dim lines(100) As String
            Dim nm, i As Integer

            nm = 0
            Dim sr As New System.IO.StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"))
            '内容を一行ずつ読み込む
            While sr.Peek() > -1
                lines(nm) = (sr.ReadLine())
                nm = nm + 1
            End While
            '閉じる
            sr.Close()
            lines(nm) = String.Empty

            Call SetData(lines)         '   data set

            Return False
        Else
            Dim msg As String
            msg = "axis file open failed!"
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return True
        End If

    End Function

    '    file save
    Public Sub SaveFile(ByVal data() As String)

        Dim path As String = modApp.path & "\Axis.txt"

        System.IO.File.WriteAllLines(path, data)

    End Sub

    '   set data
    Public Sub SetData(ByVal data() As String)

        On Error Resume Next

        yWait.pos = CDbl(data(0))
        yWait.spd = CInt(data(1))
        yWait.acc = CInt(data(2))
        yBack.pos = CDbl(data(3))
        yBack.spd = CInt(data(4))
        yBack.acc = CInt(data(5))
        yConb.pos = CDbl(data(6))
        yConb.spd = CInt(data(7))
        yConb.acc = CInt(data(8))
        yBeel.pos = CDbl(data(9))
        yBeel.spd = CInt(data(10))
        yBeel.acc = CInt(data(11))
        ySpe1.pos = CDbl(data(12))
        ySpe1.spd = CInt(data(13))
        ySpe1.acc = CInt(data(14))
        ySpe2.pos = CDbl(data(15))
        ySpe2.spd = CInt(data(16))
        ySpe2.acc = CInt(data(17))

        zWait.pos = CDbl(data(20))
        zWait.spd = CInt(data(21))
        zWait.acc = CInt(data(22))
        zDwnH.pos = CDbl(data(23))
        zDwnH.spd = CInt(data(24))
        zDwnH.acc = CInt(data(25))
        zDwnL.pos = CDbl(data(26))
        zDwnL.spd = CInt(data(27))
        zDwnL.acc = CInt(data(28))
        zUP_H.pos = CDbl(data(29))
        zUP_H.spd = CInt(data(30))
        zUP_H.acc = CInt(data(31))
        zUP_L.pos = CDbl(data(32))
        zUP_L.spd = CInt(data(33))
        zUP_L.acc = CInt(data(34))
        zSpe1.pos = CDbl(data(35))
        zSpe1.spd = CInt(data(36))
        zSpe1.acc = CInt(data(37))

    End Sub

End Module