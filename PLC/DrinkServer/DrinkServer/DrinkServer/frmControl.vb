Option Strict On
Imports System.Security.Permissions

Public Class frmControl

    Private fCtrl As Boolean

    Public stat(150) As Integer

    Private lbl_Dat() As System.Windows.Forms.Label
    Private btn_Dat() As System.Windows.Forms.Button
    Private grp_Dat() As System.Windows.Forms.GroupBox

    'Invokeメソッドで使用するデリゲート
    Delegate Sub ControlMCStateDisp_Delegate(ByVal stat() As Integer, ByVal stkps As Integer)   '   setting axis position disp

    <SecurityPermission(SecurityAction.Demand,
        Flags:=SecurityPermissionFlag.UnmanagedCode)>
    Protected Overrides Sub WndProc(ByRef m As Message)

        Const WM_SYSCOMMAND As Integer = &H112
        Const WM_NCLBUTTONDBLCLK As Integer = &HA3
        Const SC_MOVE As Long = &HF010L

        Dim auto As Boolean

        If m.Msg = WM_SYSCOMMAND AndAlso
            (m.WParam.ToInt64() And &HFFF0L) = SC_MOVE Then
            'm.Result = IntPtr.Zero
            'Return
        ElseIf m.Msg = WM_NCLBUTTONDBLCLK Then
            Return
        End If

        If m.Msg = modMAINCns.MD_UPDSP Then
            Call InvokeDispMCState(stat)
        End If

        MyBase.WndProc(m)

    End Sub

    Private Sub frmControl_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        lbl_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "Label"), Label())
        btn_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "Button"), Button())
        grp_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "GroupBox"), GroupBox())

        Dim i As Integer
        Dim dsp As String

        fCtrl = False

        ComboBox1.Items.Clear()
        For i = 1 To 15
            dsp = "グラス " + i.ToString()
            ComboBox1.Items.Add(dsp)
        Next
        ComboBox1.Items.Add("排出位置")
        ComboBox1.SelectedIndex = 0

        Call SetFormPosition()
        'Call InvokeCtrlEnabe(False)

    End Sub

    Private Sub frmControl_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing

        'If e.CloseReason = CloseReason.UserClosing Then
        '    e.Cancel = True
        'End If

    End Sub

    '   ELS control
    Private Sub ButtonELS_Click(sender As Object, e As EventArgs) Handles _
            Button1.Click, Button2.Click, Button3.Click, Button4.Click, Button5.Click, Button6.Click, Button7.Click, Button8.Click, Button9.Click, Button10.Click,
            Button11.Click, Button12.Click, Button13.Click, Button14.Click, Button15.Click, Button16.Click, Button17.Click, Button18.Click, Button19.Click, Button20.Click

        Dim name, nm As String
        Dim btn As Integer

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm) - 1

        If btn > 15 Then
            btn -= 16
            btn += comPLC.CTL_ELS09_GO
        End If

        Call comPLC.SendCylinder(btn)           '   cylinder control

    End Sub
    '   SOL control
    Private Sub ButtonSOL_Click(sender As Object, e As EventArgs) Handles _
            Button21.Click, Button22.Click, Button23.Click, Button24.Click, Button25.Click, Button26.Click, Button27.Click, Button28.Click, Button29.Click, Button30.Click,
            Button31.Click, Button32.Click, Button33.Click, Button34.Click, Button35.Click, Button36.Click, Button37.Click, Button38.Click, Button39.Click, Button40.Click,
            Button41.Click, Button42.Click, Button43.Click, Button44.Click, Button45.Click, Button46.Click, Button47.Click, Button48.Click, Button49.Click, Button50.Click

        Dim name, nm As String
        Dim btn As Integer

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm) - 21

        If btn < 16 Then
            btn += comPLC.CTL_SOL01_UP
        ElseIf btn < 32 Then
            btn -= 16
            btn += comPLC.CTL_SOL09_UP
        End If

        Call comPLC.SendCylinder(btn)           '   cylinder control

    End Sub
    '   ビールY軸 control
    Private Sub ButtonBeelY_Click(sender As Object, e As EventArgs) Handles _
            Button51.Click, Button52.Click, Button53.Click, Button54.Click

        Dim name, nm As String
        Dim btn As Integer

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm) - 51

        Call comPLC.SendBeelY(btn)              '   BeelY control

    End Sub
    '   ビールZ軸 control
    Private Sub ButtonBeelZ_Click(sender As Object, e As EventArgs) Handles _
            Button55.Click, Button56.Click, Button57.Click, Button58.Click, Button59.Click

        Dim name, nm As String
        Dim btn As Integer

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm) - 55

        Call comPLC.SendBeelZ(btn)              '   BeelZ control

    End Sub
    '   コンベア control
    Private Sub ButtonConveyor_Click(sender As Object, e As EventArgs) Handles _
            Button60.Click, Button61.Click

        Dim name, nm As String
        Dim btn As Integer
        Dim move As Boolean

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm)

        If btn = 60 Then
            move = True
        Else
            move = False
        End If

        Call comPLC.SendConveyor(move)          '   conveyor control

    End Sub
    '   ストッカXZ control
    Private Sub ButtonStocker_Click(sender As Object, e As EventArgs) Handles _
            Button62.Click, Button63.Click, Button64.Click

        Dim name, nm As String
        Dim btn, pos As Integer

        pos = ComboBox1.SelectedIndex + 1
        If pos = 16 Then
            pos = 20                '   排出位置 = 20
        End If

        name = CType(sender, Control).Name
        nm = name.Replace("Button", "")
        btn = CInt(nm) - 62

        Call comPLC.SendStockerXZ(btn, pos)     '   stockerXZ control

    End Sub

    '   jog off
    Private Sub ButtonJOG_MouseUp(sender As Object, e As MouseEventArgs) Handles _
        Button65.MouseUp, Button66.MouseUp, Button67.MouseUp, Button68.MouseUp, Button69.MouseUp, Button70.MouseUp, Button71.MouseUp, Button72.MouseUp

        Dim ctrl = False
        Dim name As String

        name = CType(sender, Control).Name
        Select Case name
            Case "Button65"
                comPLC.SendJogBeelYp(ctrl)          '   beelY軸 + jog control
            Case "Button66"
                comPLC.SendJogBeelYm(ctrl)          '   beelY軸 - jog control
            Case "Button67"
                comPLC.SendJogBeelZp(ctrl)          '   beelZ軸 + jog control
            Case "Button68"
                comPLC.SendJogBeelZm(ctrl)          '   beelZ軸 - jog control
            Case "Button69"
                comPLC.SendJogStockXp(ctrl)         '   stockX軸 + jog control
            Case "Button70"
                comPLC.SendJogStockXm(ctrl)         '   stockX軸 - jog control
            Case "Button71"
                comPLC.SendJogStockZp(ctrl)         '   stockZ軸 + jog control
            Case "Button72"
                comPLC.SendJogStockZm(ctrl)         '   stockZ軸 - jog control
        End Select

        frmMain.fCtrlAxis = ctrl

    End Sub
    '   jog on
    Private Sub ButtonJOG_MouseDown(sender As Object, e As MouseEventArgs) Handles _
        Button65.MouseDown, Button66.MouseDown, Button67.MouseDown, Button68.MouseDown, Button69.MouseDown, Button70.MouseDown, Button71.MouseDown, Button72.MouseDown

        Dim ctrl = True

        Dim name As String

        name = CType(sender, Control).Name
        Select Case name
            Case "Button65"
                comPLC.SendJogBeelYp(ctrl)          '   beelY軸 + jog control
            Case "Button66"
                comPLC.SendJogBeelYm(ctrl)          '   beelY軸 - jog control
            Case "Button67"
                comPLC.SendJogBeelZp(ctrl)          '   beelZ軸 + jog control
            Case "Button68"
                comPLC.SendJogBeelZm(ctrl)          '   beelZ軸 - jog control
            Case "Button69"
                comPLC.SendJogStockXp(ctrl)         '   stockX軸 + jog control
            Case "Button70"
                comPLC.SendJogStockXm(ctrl)         '   stockX軸 - jog control
            Case "Button71"
                comPLC.SendJogStockZp(ctrl)         '   stockZ軸 + jog control
            Case "Button72"
                comPLC.SendJogStockZm(ctrl)         '   stockZ軸 - jog control
        End Select

        frmMain.fCtrlAxis = ctrl

    End Sub

    '   disp position
    Private Sub SetFormPosition()

        Dim pos As Point
        Dim frm As Integer
        Dim wid, hei As Integer

        frm = Me.Height

        wid = Screen.PrimaryScreen.WorkingArea.Width
        hei = Screen.PrimaryScreen.WorkingArea.Height

        pos.X = frmMain.Left
        'pos.Y = hei - frm
        'pos.Y = frmMain.Height
        pos.Y = frmMain.Top
        Me.Location = pos

    End Sub

    '   enable control
    Friend Sub InvokeCtrlEnabe(ByVal enbl As Boolean)

        For Each grp As GroupBox In grp_Dat
            grp.Enabled = enbl
        Next

    End Sub
    '   state disp
    Friend Sub InvokeDispMCState(ByVal stat() As Integer, Optional ByVal stkps As Integer = 0)

        If InvokeRequired Then
            ' 別スレッドから呼び出された場合
            Invoke(New ControlMCStateDisp_Delegate(AddressOf InvokeDispMCState), New Object() {stat, stkps})
            Return
        End If

        Dim col As Color
        Dim i, nm, dt As Integer
        Dim dsp As String

        For i = 0 To 15
            dt = i
            nm = i
            If stat(dt) <> 0 Then
                col = Color.LightGreen
            Else
                col = SystemColors.ControlLight
            End If
            btn_Dat(nm).BackColor = col

            dt += 16
            nm = 16 + i
            If i < 2 Then
                If stat(dt) <> 0 Then
                    col = Color.LightGreen
                Else
                    col = SystemColors.ControlLight
                End If
                btn_Dat(nm).BackColor = col
            End If

            dt += 16
            nm = 20 + i
            If stat(dt) <> 0 Then
                col = Color.LightGreen
            Else
                col = SystemColors.ControlLight
            End If
            btn_Dat(nm).BackColor = col

            dt += 16
            nm = 36 + i
            If i < 13 Then
                If stat(dt) <> 0 Then
                    col = Color.LightGreen
                Else
                    col = SystemColors.ControlLight
                End If
                btn_Dat(nm).BackColor = col
            End If
        Next

        For i = 0 To 4
            dt = 110 + i
            nm = 50 + i
            If i < 4 Then
                If stat(dt) <> 0 Then
                    col = Color.LightGreen
                Else
                    col = SystemColors.ControlLight
                End If
                btn_Dat(nm).BackColor = col
            End If

            dt += 5
            nm = 54 + i
            If stat(dt) <> 0 Then
                col = Color.LightGreen
            Else
                col = SystemColors.ControlLight
            End If
            btn_Dat(nm).BackColor = col

            dt += 5
            nm = 61 + i
            If i < 3 Then
                If stat(dt) <> 0 Then
                    col = Color.LightGreen
                Else
                    col = SystemColors.ControlLight
                End If
                btn_Dat(nm).BackColor = col
            End If
        Next

        dt = 125
        If stat(dt) <> 0 Then
            col = Color.LightGreen
        Else
            col = SystemColors.ControlLight
        End If
        btn_Dat(59).BackColor = col
        'For Each btn As Button In btn_Dat
        '    If stat(i) <> 0 Then
        '        col = Color.LightGreen
        '    Else
        '        col = SystemColors.Control
        '    End If
        '    btn.BackColor = col
        'Next

        If stkps = 0 Then
            dsp = "-"
        ElseIf stkps = 20 Then
            dsp = "排出位置"
        Else
            dsp = "グラス " + stkps.ToString()
        End If
        lbl_StockerPos.Text = dsp

    End Sub

End Class