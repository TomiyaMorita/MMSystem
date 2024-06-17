Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class frmMain

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Public Shared Function PostMessage(ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function FindWindow(ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    End Function

    Private Const ST_NOT = 0    '   no action
    Private Const ST_WAT = 1    '   wait
    Private Const ST_ORG = 2    '   origin meve
    Private Const ST_RUN = 3    '   running
    Private Const ST_STP = 4    '   temp stop
    Private Const ST_ERR = 9    '   error

    Private Const CS_NOT = 0    '   no action
    Private Const CS_COM = 1    '   connect

    Public Const US_NOT = 0     '   no action
    Public Const US_WAT = 1     '   wait
    Public Const US_RUN = 2     '   running
    Public Const US_CMP = 3     '   complete

    Private f_ComPLC As Integer
    Private f_ATrun As Boolean
    Private f_TempStop As Boolean
    Private f_ErrClear As Boolean

    Private MainText As String
    Private f_Water As Boolean
    Private m_FJP(5) As Integer

    Private ctl As New frmControl()
    Private stg As New frmSetting()

    '   sel order
    Private SelOrder() As String =
        {"チューブポンプ", "FJP", "ビール", ""}

    Public fCtrlAxis As Boolean '   軸制御中flag

    Public preYaxis As Double   '   Y軸現在値
    Public preZaxis As Double   '   Z軸現在値

    'データテーブルの作成
    Private data_table As DataTable = New DataTable("default_table")

    Private lbl_Dat() As System.Windows.Forms.Label
    Private nup_Dat() As System.Windows.Forms.NumericUpDown
    Private chk_Dat() As System.Windows.Forms.CheckBox

    'Invokeメソッドで使用するデリゲート
    Delegate Sub MainStateDisp_Delegate(ByVal stat As Integer, ByVal ecod As String)    '   main state disp
    Delegate Sub MainUnitStateDisp_Delegate(ByVal stat() As Integer)    '   main unit state disp
    Delegate Sub MainMCStateDisp_Delegate(ByVal stat As Integer, ByVal sns() As Integer) '   main MC state disp
    Delegate Sub MainErrorLogDisp_Delegate(ByVal code As String)        '   main error log disp

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Dim i As Integer

        modApp.path = Application.StartupPath        '   app. path

        Dim ver As System.Diagnostics.FileVersionInfo =
            System.Diagnostics.FileVersionInfo.GetVersionInfo(
            System.Reflection.Assembly.GetExecutingAssembly().Location)

        modApp.ver = ver.FileVersion                '   app. ver.
        MainText = "DrinkServer Ver." + modApp.ver
        Me.Text = MainText                          '   main disp

        lbl_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "Label"), Label())
        nup_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "NumericUpDown"), NumericUpDown())
        chk_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "CheckBox"), CheckBox())

        ComboBox1.Items.Add("DM")
        ComboBox1.Items.Add("FM")
        ComboBox1.Items.Add("ZF")
        ComboBox2.Items.Add("DM")
        ComboBox2.Items.Add("FM")
        ComboBox2.Items.Add("ZF")
        ComboBox1.SelectedIndex = 0
        ComboBox2.SelectedIndex = 0

        Dim dbg(10) As Boolean
        Call modApp.ReadDebug(dbg)          '   read debug
        comPLC.f_Debug = dbg(1)
        f_ErrClear = Not dbg(2)

        Call modSetting.ReadFile()          '   setting data read
        'Call modAxis.ReadFile()             '   axis data read

        'Panel4.Location = Panel2.Location
        'Panel4.Visible = False
        GroupBox6.Visible = False
        'Call SetFormPosition()
        'ctl.Show() 'Me)

        Call DispMainState(ST_NOT)
        Call DispCommState(CS_NOT)
        Call DispReadData("")
        'Call DispUnitState()
        Call InitDataGrid()

        Dim dsp As String
        For i = 1 To 4
            dsp = SelOrder(0) + i.ToString()
            ComboBox3.Items.Add(dsp)
        Next
        For i = 1 To 36
            dsp = SelOrder(1) + i.ToString()
            ComboBox3.Items.Add(dsp)
        Next
        dsp = SelOrder(2)
        ComboBox3.Items.Add(dsp)

        NumericUpDown12.Maximum = NumericUpDown9.Maximum
        NumericUpDown13.Maximum = NumericUpDown10.Maximum

        f_ComPLC = CS_NOT
        f_ATrun = False
        f_TempStop = False
        f_Water = True

        For i = 0 To 3
            nup_Dat(i).Value = CInt(modSetting.IPadPLC(i))
        Next
        nup_Dat(4).Value = CInt(modSetting.PortPLC)

        Dim thPLCcom As New Thread(New ThreadStart(AddressOf Thread_Com_PLC))
        thPLCcom.IsBackground = True
        'スレッドを開始する
        thPLCcom.Start()

        tmDisp.Enabled = False

    End Sub

    '   reset timer
    Private Sub tmr_Reset_Tick(sender As System.Object, e As System.EventArgs) Handles tmDisp.Tick

        Dim yaxis, zaxis As Double

        If f_ComPLC <> CS_COM Then Exit Sub

        Call comPLC.GetAxisPosition(yaxis, zaxis)   '   軸現在値
        If frmSetting.Visible = True Then
            frmSetting.InvokeDispAxisPosition(yaxis, zaxis)     '   setting pre axis disp
        End If

    End Sub

    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing

        Dim svdt(20) As String
        Dim i As Integer

        f_ComPLC = CS_NOT

        i = 0
        For Each data As NumericUpDown In nup_Dat
            svdt(i) = data.Value.ToString()
            i += 1
        Next

        Call modSetting.SaveFile(svdt)          '   data save

        stg.Dispose()
        ctl.Dispose()
        'System.Threading.Thread.Sleep(100)

    End Sub
    '   origin
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Call comPLC.SendOrigin()            '   origin

    End Sub
    '   reset
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Call comPLC.SendReset()             '   reset

        If f_ATrun = True Then
            Button14.PerformClick()
        Else
            Call DispMainState(ST_NOT)
        End If

    End Sub
    '   setting
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        Dim online As Boolean
        Dim ret As DialogResult

        If f_ComPLC = CS_COM Then
            online = True
        Else
            online = False
        End If

        Call comPLC.GetAxisData()           '   axis data get
        'frmSetting.fEnable = online
        'frmSetting.ShowDialog(Me)
        'frmSetting.Dispose()
        stg.fEnable = online
        ret = stg.ShowDialog(Me)
        'stg.Dispose()

        If ret = DialogResult.OK Then
            comPLC.SendAxisData()           '   axis data update
        End If

    End Sub
    '   cycle 動作
    Private Sub ButtonCycle_Click(sender As Object, e As EventArgs) Handles _
        Button4.Click, Button5.Click, Button6.Click, Button10.Click, Button11.Click, Button12.Click

        Dim name, nm As String
        Dim btn, pos As Integer

        pos = 0

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm) - 4

        If btn > 5 Then
            btn -= 3
        End If

        If btn = 0 Then
            pos = CInt(NumericUpDown9.Value)
        ElseIf btn = 5 Then
            pos = CInt(NumericUpDown10.Value)
        End If

        Call comPLC.SendCycle(btn, pos)     '   cycle 動作

    End Sub
    '   メモリ書き込み
    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click

        Dim sdat As String

        sdat = ComboBox1.Text + nup_Dat(5).Value.ToString()   '   メモリaddress
        sdat += " " + nup_Dat(6).Value.ToString()             '   write data

        Call comPLC.AnyWrite(sdat)      '   PLC 任意write

    End Sub
    '   メモリ読み込み
    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click

        Dim sdat, getd As String

        sdat = ComboBox2.Text + nup_Dat(7).Value.ToString()   '   メモリaddress
        getd = ""

        Call comPLC.AnyRead(sdat, getd)     '   PLC 任意read

        lbl_RdData.Text = getd

    End Sub
    '   communication on/off
    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click

        Dim data(10) As String
        Dim i As Integer
        Dim dsp As String

        If f_ComPLC = CS_NOT Then
            Call DispCommState(CS_NOT)

            For i = 0 To 4
                data(i) = nup_Dat(i).Value.ToString()
            Next
            modSetting.SetData(data)            '   PLC data set

            If comPLC.CheckConnect() = True Then
                MessageBox.Show("PLC communication Error!" & vbCrLf & "Please check to communication cable or setting")
                Exit Sub
            End If

            Call DispCommState(CS_COM)
            dsp = "接続終了"

            If comPLC.f_Debug = True Then
                lbl_Dat(21).BackColor = Color.LightGreen
            End If
        Else
            Call DispCommState(CS_NOT)
            Call CtrlDisp(False)

            dsp = "接続開始"
        End If

        Button9.Text = dsp

    End Sub
    '   auto start
    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click

        Dim run As Boolean = Not f_ATrun
        Dim fcol, bcol As Color
        Dim dsp As String

        If Label22.BackColor <> Color.LightGreen Then Exit Sub

        If run = True Then
            Call comPLC.SendAtRun()         '   auto star

            fcol = Color.Black
            bcol = Color.Yellow
            dsp = "AT END"
            Call DispMainState(ST_RUN)
        Else
            If f_TempStop = False Then Exit Sub '   一次停止中のみ

            Call comPLC.SendAtStop(False)   '   auto stop

            fcol = Color.Green
            bcol = SystemColors.ControlLight
            dsp = "START"
            Call DispMainState(ST_NOT)
        End If
        Call CtrlDisp(run)

        Button13.BackColor = bcol
        Button13.ForeColor = fcol
        Button13.Text = dsp

        f_ATrun = run

        If run = False Then
            f_TempStop = False
            fcol = Color.Red
            bcol = SystemColors.ControlLight
            dsp = "TEMP STOP"
            Button14.BackColor = bcol
            Button14.ForeColor = fcol
            Button14.Text = dsp
        End If

    End Sub
    '   temp stop
    Private Sub Button14_Click(sender As Object, e As EventArgs) Handles Button14.Click

        If f_ATrun = False Then Exit Sub '  自動動作中のみ

        Dim stp As Boolean = Not f_TempStop
        Dim fcol, bcol As Color
        Dim dsp As String
        Dim md As Integer

        If stp = True Then
            Call comPLC.SendAtStop(True)    '   auto temp stop

            fcol = Color.Black
            bcol = Color.LightBlue
            dsp = "ReSTART"
            md = ST_STP
        Else
            Call comPLC.SendAtRun()         '   auto restart

            fcol = Color.Red
            bcol = SystemColors.ControlLight
            dsp = "TEMP STOP"
            md = ST_RUN
        End If
        Call DispMainState(md)

        Button14.BackColor = bcol
        Button14.ForeColor = fcol
        Button14.Text = dsp

        f_TempStop = stp

    End Sub
    '   manual disp
    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click

        ctl.ShowDialog()

    End Sub
    '   send order
    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click

        Dim lane, glass, odr, ice, water, i, fjp As Integer
        Dim items(10), iice, iwater, drink As String
        Dim sel(50) As Boolean

        'sel = ComboBox3.SelectedIndex           '   order item
        'If sel < 0 Then
        '    MessageBox.Show("オーダー内容を選択してください", "Warning")
        '    Exit Sub
        'End If
        'If sel > 39 Then
        '    sel = 42            '   ビール
        'End If

        glass = CInt(NumericUpDown9.Value)      '   glass no.
        lane = CInt(NumericUpDown10.Value)      '   排出レーン
        odr = CInt(NumericUpDown11.Value)       '   order no.

        If CheckBox1.Checked = True Then        '   氷有無
            ice = 1
            iice = "○"
        Else
            ice = 0
            iice = "―"
        End If
        If CheckBox2.Checked = True Then        '   水/炭酸
            If f_Water = True Then
                water = 1
                iwater = "炭酸"
            Else
                water = 2
                iwater = "水"
            End If
            f_Water = Not f_Water

            sel(39 + water) = True
        Else
            water = 0
            iwater = "―"
        End If

        drink = ""
        For i = 0 To 3
            If chk_Dat(2 + i).Checked = True Then   '   チューブポンプ 1~4
                sel(i) = True
                If String.IsNullOrEmpty(drink) = False Then
                    drink += ","
                End If
                drink += "ﾎﾟﾝﾌﾟ" + (i + 1).ToString()
            End If
        Next
        For i = 0 To 3
            If chk_Dat(6 + i).Checked = True Then   '   FJP 1~36
                fjp = i * 9 + m_FJP(i)
                sel(4 + fjp) = True
                If String.IsNullOrEmpty(drink) = False Then
                    drink += ","
                End If
                drink += "FJP" + (fjp + 1).ToString()

                m_FJP(i) += 1
                If m_FJP(i) > 8 Then
                    m_FJP(i) = 0
                End If
            End If
        Next
        If CheckBox11.Checked = True Then       '   ビール
            sel(42) = True
            If String.IsNullOrEmpty(drink) = False Then
                drink += ","
            End If
            drink += "ビール"
        End If

        'Call comPLC.SendOrder(sel, glass, lane, odr, ice, water)    '   order
        Call comPLC.SendOrder(sel, glass, lane, odr, ice)       '   order

        items(1) = glass.ToString()
        items(2) = lane.ToString()
        items(3) = drink ' ComboBox3.Text
        items(4) = iice
        items(5) = iwater

        Call DispOrder(odr, items)      '   disp

        NumericUpDown11.Value = odr + 1

    End Sub

    Private Sub NumericUpDown12_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown12.ValueChanged

        NumericUpDown9.Value = NumericUpDown12.Value

    End Sub
    Private Sub NumericUpDown13_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown13.ValueChanged

        NumericUpDown10.Value = NumericUpDown13.Value

    End Sub

    '   disp position
    Private Sub SetFormPosition()

        Dim pos As Point
        Dim frm As Integer
        Dim wid, hei As Integer

        frm = Me.Height

        wid = Screen.PrimaryScreen.WorkingArea.Width
        hei = Screen.PrimaryScreen.WorkingArea.Height

        pos.X = Me.Left
        pos.Y = 0
        Me.Location = pos

    End Sub

    '   init datagrid
    Private Sub InitDataGrid()

        Dim i, nm As Integer
        Dim max As Integer

        max = 205
        With DataGridView1
            '.Top -= 6
            .Height += 4
            '.Width += 8

            .AllowUserToAddRows = False
            .AllowUserToDeleteRows = False
            .AllowUserToResizeColumns = False           '   幅変更不可   
            .AllowUserToResizeRows = False              '   高さ変更不可

            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

            'データソースを設定
            DataGridView1.DataSource = data_table

            ' データテーブルにカラムを作成・登録
            data_table.Columns.Add("注文o.", GetType(Integer))
            data_table.Columns.Add("グラス", GetType(String))
            data_table.Columns.Add("排出レーン", GetType(String))
            data_table.Columns.Add("ドリンク種類", GetType(String))
            data_table.Columns.Add("氷", GetType(String))
            data_table.Columns.Add("水/炭酸", GetType(String))

            'データを追加
            For i = 1 To max
                data_table.Rows.Add(New String() {CStr(i), "", "", "", "", ""})
            Next

            '.ColumnCount = 5
            'For i = 1 To max
            '    .Rows.Add()
            'Next
            .Columns(0).Width = 50
            .Columns(1).Width = 50
            .Columns(2).Width = 60
            .Columns(3).Width = 368
            .Columns(4).Width = 50
            .Columns(5).Width = 60

            .ColumnHeadersHeight = 19
            .ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .ColumnHeadersDefaultCellStyle.Font = New Font(.Font.Name, 9)

            .Columns(0).HeaderText = "注文no."
            .Columns(1).HeaderText = "グラス"
            .Columns(2).HeaderText = "排出ﾚｰﾝ"
            .Columns(3).HeaderText = "ドリンク種類"
            .Columns(4).HeaderText = "氷"
            .Columns(5).HeaderText = "水/炭酸"

            For Each c As DataGridViewColumn In .Columns         '  sort禁止
                c.SortMode = DataGridViewColumnSortMode.NotSortable
            Next

            '            dataGridView1[i, 0].Value = i + 1
            For i = 0 To max - 1
                .Rows(i).Height = 17
                nm = i + 1
                DataGridView1(0, i).Value = nm
                .Rows(i).DefaultCellStyle.Font = New Font(.Font.Name, 10)
            Next

            .Columns(0).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns(1).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns(2).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns(3).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns(4).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns(5).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            '.FirstDisplayedScrollingRowIndex = 1

            .DefaultCellStyle.SelectionForeColor = Color.Black          ' セルが選択されたときの色 white
            .DefaultCellStyle.SelectionBackColor = Color.White          ' -> 選択されていないように表示
            .AlternatingRowsDefaultCellStyle.SelectionBackColor = .AlternatingRowsDefaultCellStyle.BackColor
            '.ResumeLayout(True)

            .CurrentCell = Nothing
        End With

    End Sub

    '   disp order
    Private Sub DispOrder(ByVal odr As Integer, ByVal item() As String)

        Dim i, nm As Integer

        DataGridView1.SuspendLayout()

        nm = odr - 1
        For i = 1 To 5
            data_table(nm).Item(i) = item(i)
        Next

        If odr > 11 Then
            DataGridView1.FirstDisplayedScrollingRowIndex = odr - 11
        End If

        DataGridView1.ResumeLayout()

    End Sub

    '   disp state
    Private Sub DispMainState(ByVal stat As Integer, Optional ByVal ecd As String = Nothing)

        On Error GoTo jp_end

        If InvokeRequired Then
            ' 別スレッドから呼び出された場合
            Invoke(New MainStateDisp_Delegate(AddressOf DispMainState), New Object() {stat, ecd})
            Return
        End If

        Dim dsp As String
        Dim col As Color

        Select Case stat
            Case ST_NOT
            Case ST_WAT
                dsp = "WAIT"
                col = Color.LightGreen
            Case ST_ORG
                dsp = "ORIGN MOVE"
                col = Color.Orange
            Case ST_RUN
                dsp = "RUNNING"
                col = Color.Yellow
            Case ST_STP
                dsp = "TEMP STOP"
                col = Color.LightBlue
            Case ST_ERR
                dsp = "ERROR [" + ecd + "]"
                col = Color.LightPink
        End Select

        lbl_State.Text = dsp
        lbl_State.BackColor = col

jp_end:
    End Sub
    '   disp state
    Private Sub DispMCState(ByVal stat As Integer, ByVal sns() As Integer)

        On Error GoTo jp_end

        If InvokeRequired Then
            ' 別スレッドから呼び出された場合
            Invoke(New MainMCStateDisp_Delegate(AddressOf DispMCState), New Object() {stat, sns})
            Return
        End If

        Dim i, dt, nm As Integer
        Dim dsp As String
        Dim col As Color

        dsp = ""
        For i = 0 To 4
            col = Color.White
            nm = 18 + i
            If (stat And 1) > 0 Then
                col = Color.LightGreen
            End If
            lbl_Dat(nm).BackColor = col
            lbl_Dat(nm).Refresh()
            stat = stat >> 1
        Next

        For i = 0 To 12
            dt = 96 + i
            nm = 23 + i
            If sns(dt) <> 0 Then
                col = Color.LightPink
            Else
                col = SystemColors.ControlLight
            End If
            lbl_Dat(nm).BackColor = col
            lbl_Dat(nm).Refresh()
        Next

jp_end:
    End Sub
    '   disp MCunit state
    Private Sub DispUnitState(ByVal stat() As Integer)

        On Error GoTo jp_end

        If InvokeRequired Then
            ' 別スレッドから呼び出された場合
            Invoke(New MainUnitStateDisp_Delegate(AddressOf DispUnitState), New Object() {stat})
            Return
        End If

        Dim i As Integer
        Dim dsp As String
        Dim col As Color

        For i = 0 To 9
            dsp = "--"
            col = SystemColors.Control
            If stat IsNot Nothing Then
                Select Case stat(i)
                    Case US_NOT
                        dsp = ""
                        col = Color.White
                    Case US_WAT
                        dsp = "WAIT"
                        col = Color.Yellow
                    Case US_RUN
                        dsp = "RUN"
                        col = Color.LightPink
                    Case US_CMP
                        col = Color.LightGreen
                    Case Else
                        dsp = stat(i).ToString()
                        col = Color.LightBlue
                End Select
            End If
            'lbl_Ustat(i).Text = dsp
            lbl_Dat(i).BackColor = col
        Next

jp_end:
    End Sub
    '   disp communication state
    Private Sub DispCommState(ByVal stat As Integer)

        Dim dsp As String
        Dim col As Color
        Dim online As Boolean
        Dim i As Integer

        dsp = "--"
        col = SystemColors.Control
        online = False
        Select Case stat
            Case CS_NOT
            Case CS_COM
                dsp = "Connecting"
                col = Color.LightPink
                online = True
        End Select

        f_ComPLC = stat
        lbl_Comm.Text = dsp
        lbl_Comm.BackColor = col
        lbl_Comm.Refresh()

        For i = 0 To 4
            nup_Dat(i).Enabled = Not online
        Next

        Button3.Enabled = online
        Button7.Enabled = online
        Button8.Enabled = online
        Panel2.Enabled = online
        Panel3.Enabled = online
        'ctl.InvokeCtrlEnabe(online)

    End Sub
    '   disp read data
    Private Sub DispReadData(ByVal data As String)

        lbl_RdData.Text = data

    End Sub
    '   disp error log
    Private Sub DispErrorLog(ByVal code As String)

        On Error GoTo jp_end

        If InvokeRequired Then
            ' 別スレッドから呼び出された場合
            Invoke(New MainErrorLogDisp_Delegate(AddressOf DispErrorLog), New Object() {code})
            Return
        End If

        Dim log As String

        log = DateTime.Now.ToString("HH:mm:ss")
        log += " ECD[" + code + "]"

        ListBox1.Items.Add(log)
        ListBox1.TopIndex = ListBox1.Items.Count - 1

jp_end:
    End Sub

    '   ctrl order disp
    Private Sub CtrlDisp(ByVal dsp As Boolean)

        Dim title As String

        GroupBox6.Visible = dsp
        If dsp = True Then
            title = "Order"
        Else
            title = "Cycle動作"
        End If
        GroupBox3.Text = title

        NumericUpDown12.Value = NumericUpDown9.Value
        NumericUpDown13.Value = NumericUpDown10.Value

        Button1.Enabled = Not dsp
        Button15.Enabled = Not dsp

    End Sub

    '   error record
    Private Sub LogRecord(ByRef code As String)

        Dim i, nm, jp As Integer
        Dim ecd, tmp, tmp1, tmp2, tmp3, tmp4 As String

        On Error Resume Next

        jp = 3
        For i = 4 To 1 Step -1
            If CInt(comPLC.stkEcd(i * jp)) <> 0 Then
                tmp = Hex(comPLC.stkEcd(i * jp))
                tmp1 = tmp.Substring(0, 2)
                tmp2 = tmp.Substring(2)
                tmp = Hex(comPLC.stkEcd(i * jp + 1))
                tmp3 = tmp.Substring(0, 2)
                tmp4 = tmp.Substring(2)

                tmp1 = Chr(CInt("&h" + tmp1))
                tmp2 = Chr(CInt("&h" + tmp2))
                tmp3 = Chr(CInt("&h" + tmp3))
                tmp4 = Chr(CInt("&h" + tmp4))
                ecd = tmp1 + tmp2 + tmp3 + tmp4

                DispErrorLog(ecd)           '   err log disp
            End If
        Next
        i = 0
        tmp = Hex(comPLC.stkEcd(i * jp))
        tmp1 = tmp.Substring(0, 2)
        tmp2 = tmp.Substring(2)
        tmp = Hex(comPLC.stkEcd(i * jp + 1))
        tmp3 = tmp.Substring(0, 2)
        tmp4 = tmp.Substring(2)

        tmp1 = Chr(CInt("&h" + tmp1))
        tmp2 = Chr(CInt("&h" + tmp2))
        tmp3 = Chr(CInt("&h" + tmp3))
        tmp4 = Chr(CInt("&h" + tmp4))
        ecd = tmp1 + tmp2 + tmp3 + tmp4
        DispErrorLog(ecd)                   '   err log disp

        code = ecd                          '   latest code

    End Sub

    '   thread plc communication
    Private Sub Thread_Com_PLC()

        Dim main, stat(150), unit(20), stkps As Integer
        Dim yaxis, zaxis As Double
        Dim ferr As Boolean
        Dim code As String

        yaxis = 0
        zaxis = 0
        While (True)
            System.Threading.Thread.Sleep(10)

            If comPLC.f_Debug = True Then Exit While

            If f_ComPLC <> CS_COM Then Continue While

            Call comPLC.GetAxisPosition(yaxis, zaxis)   '   軸現在値
            preYaxis = yaxis
            preZaxis = zaxis

            If stg.Visible = True Then
                stg.InvokeDispAxisPosition(yaxis, zaxis)
            End If

            If fCtrlAxis = True Then Continue While

            main = comPLC.GetMainStatus(ferr)   '   get main status & error flag
            Call comPLC.GetCycleStatus(unit)    '   cycle status
            Call comPLC.GetSensorStatus(stat)   '   sensor status
            Call comPLC.GetAxisStatus(stat, stkps)  '   axis status

            code = ""
            If ferr = True Then                 '   error発生
                Call LogRecord(code)            '   log record
                'main = ST_ERR
                Call DispMainState(ST_ERR, code)    '   state disp
            End If

            Call DispMCState(main, stat)        '   state disp
            Call DispUnitState(unit)

            If ctl.Visible = True Then

                stat(125) = unit(0)                 '   conveyor stat
                ctl.InvokeDispMCState(stat, stkps)  '   sensor state
            End If
            'ctl.stat = stat
            'Call SendMsgCtrlForm(modMAINCns.MD_UPDSP)

            If ferr = True And f_ErrClear = True Then
                Call comPLC.ClearErrLog()       '   error log clear
            End If

        End While

    End Sub

    '   control form send message
    Private Shared Sub SendMsgCtrlForm(ByVal cmd As Integer)

        Dim hWnd As Int32 = CInt(FindWindow(Nothing, "MC Control"))
        Dim sdat, data(250) As String
        Dim auto As Integer

        PostMessage(CType(hWnd, IntPtr), CUInt(cmd), IntPtr.Zero, IntPtr.Zero)



    End Sub

End Class

'   form control item 割付 class
Public Class ctrlFormItem

    '   control array
    Public Shared Function GetControlArrayByName(
        ByVal frm As Form, ByVal name As String) As Object
        Dim ctrs As New System.Collections.ArrayList
        Dim obj As Object
        Dim i As Integer = 1
        While True
            obj = FindControlByFieldName(frm, name + i.ToString())
            i += 1
            If obj Is Nothing Then
                Exit While
            Else
                ctrs.Add(obj)
            End If
        End While

        If ctrs.Count = 0 Then
            Return Nothing
        Else
            Return ctrs.ToArray(ctrs(0).GetType())
        End If
    End Function

    ''' <summary>
    ''' フォームに配置されているコントロールを名前で探す
    ''' （フォームクラスのフィールドをフィールド名で探す）
    ''' </summary>
    ''' <param name="frm">コントロールを探すフォーム</param>
    ''' <param name="name">コントロール（フィールド）の名前</param>
    ''' <returns>見つかった時は、コントロールのオブジェクト。
    ''' 見つからなかった時は、null(VB.NETではNothing)。</returns>
    Public Shared Function FindControlByFieldName(
        ByVal frm As Form, ByVal name As String) As Object
        'まずプロパティ名を探し、見つからなければフィールド名を探す
        Dim t As System.Type = frm.GetType()

        Dim pi As System.Reflection.PropertyInfo =
            t.GetProperty(name,
                System.Reflection.BindingFlags.Public Or
                System.Reflection.BindingFlags.NonPublic Or
                System.Reflection.BindingFlags.Instance Or
                System.Reflection.BindingFlags.DeclaredOnly)

        If Not pi Is Nothing Then
            Return pi.GetValue(frm, Nothing)
        End If

        Dim fi As System.Reflection.FieldInfo =
            t.GetField(name,
                System.Reflection.BindingFlags.Public Or
                System.Reflection.BindingFlags.NonPublic Or
                System.Reflection.BindingFlags.Instance Or
                System.Reflection.BindingFlags.DeclaredOnly)

        If fi Is Nothing Then
            Return Nothing
        End If

        Return fi.GetValue(frm)
    End Function

End Class

