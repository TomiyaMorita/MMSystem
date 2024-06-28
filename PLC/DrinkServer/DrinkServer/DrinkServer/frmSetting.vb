Option Strict On
Option Explicit On

Public Class frmSetting

    Public fEnable As Boolean

    Private fPushYgo As Boolean
    Private fPushYback As Boolean
    Private fPushZup As Boolean
    Private fPushZdown As Boolean

    Private txt_Dat() As System.Windows.Forms.TextBox

    'Invokeメソッドで使用するデリゲート
    Delegate Sub SettingAxisPositionDisp_Delegate(ByVal yaxis As Double, ByVal zaxis As Double)   '   setting axis position disp

    Private Sub frmSetting_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        txt_Dat = CType(ctrlFormItem.GetControlArrayByName(Me, "TextBox"), TextBox())

        Call DispData(fEnable)
        Call InvokeDispAxisPosition(frmMain.preYaxis, frmMain.preZaxis)

    End Sub

    '   data save
    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        Dim msg, data(40) As String
        Dim i As Integer
        Dim dat As Double

        i = 0
        For Each dt As TextBox In txt_Dat
            data(i) = dt.Text

            If i = 18 Or i = 19 Then GoTo jp_next
            Try
                dat = CDbl(data(i))
            Catch ex As Exception
                msg = "正しい数値を入力してください"
                MessageBox.Show(msg, "Warning", MessageBoxButtons.OK)
                dt.Focus()
                dt.SelectAll()
                Exit Sub
            End Try
jp_next:
            i += 1
        Next

        Call modAxis.SetData(data)          '   data set
        Call modAxis.SaveFile(data)         '   data save
        'Call modAxis.ReadFile()

        'MessageBox.Show("データ送信しました")
        Me.Close()

    End Sub
    '   Y軸 + jog off
    Private Sub Button1_MouseUp(sender As Object, e As MouseEventArgs) Handles Button1.MouseUp

        Dim ctrl = False

        comPLC.SendJogBeelYp(ctrl)          '   Y軸 + jog control
        fPushYgo = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Y軸 + jog on
    Private Sub Button1_MouseDown(sender As Object, e As MouseEventArgs) Handles Button1.MouseDown

        Dim ctrl = True

        comPLC.SendJogBeelYp(ctrl)          '   Y軸 + jog control
        fPushYgo = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Y軸 - jog off
    Private Sub Button2_MouseUp(sender As Object, e As MouseEventArgs) Handles Button2.MouseUp

        Dim ctrl = False

        comPLC.SendJogBeelYm(ctrl)          '   Y軸 - jog control
        fPushYback = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Y軸 - jog on
    Private Sub Button2_MouseDown(sender As Object, e As MouseEventArgs) Handles Button2.MouseDown

        Dim ctrl = True

        comPLC.SendJogBeelYm(ctrl)          '   Y軸 - jog control
        fPushYback = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Z軸 - jog off
    Private Sub Button3_MouseUp(sender As Object, e As MouseEventArgs) Handles Button3.MouseUp

        Dim ctrl = False

        comPLC.SendJogBeelZm(ctrl)          '   Z軸 - jog control
        fPushZdown = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Z軸 - jog on
    Private Sub Button3_MouseDown(sender As Object, e As MouseEventArgs) Handles Button3.MouseDown

        Dim ctrl = True

        comPLC.SendJogBeelZm(ctrl)          '   Z軸 - jog control
        fPushZdown = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Z軸 + jog off
    Private Sub Button4_MouseUp(sender As Object, e As MouseEventArgs) Handles Button4.MouseUp

        Dim ctrl = False

        comPLC.SendJogBeelZp(ctrl)          '   Z軸 + jog control
        fPushZup = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub
    '   Z軸 + jog on
    Private Sub Button4_MouseDown(sender As Object, e As MouseEventArgs) Handles Button4.MouseDown

        Dim ctrl = True

        comPLC.SendJogBeelZp(ctrl)          '   Z軸 + jog control
        fPushZup = ctrl
        frmMain.fCtrlAxis = ctrl

    End Sub

    '   disp data
    Private Sub DispData(ByVal enable As Boolean)

        txt_Dat(0).Text = modAxis.yWait.pos.ToString("0.00")
        txt_Dat(1).Text = modAxis.yWait.spd.ToString()
        txt_Dat(2).Text = modAxis.yWait.acc.ToString()
        txt_Dat(3).Text = modAxis.yBack.pos.ToString("0.00")
        txt_Dat(4).Text = modAxis.yBack.spd.ToString()
        txt_Dat(5).Text = modAxis.yBack.acc.ToString()
        txt_Dat(6).Text = modAxis.yConb.pos.ToString("0.00")
        txt_Dat(7).Text = modAxis.yConb.spd.ToString()
        txt_Dat(8).Text = modAxis.yConb.acc.ToString()
        txt_Dat(9).Text = modAxis.yBeel.pos.ToString("0.00")
        txt_Dat(10).Text = modAxis.yBeel.spd.ToString()
        txt_Dat(11).Text = modAxis.yBeel.acc.ToString()
        txt_Dat(12).Text = modAxis.ySpe1.pos.ToString("0.00")
        txt_Dat(13).Text = modAxis.ySpe1.spd.ToString()
        txt_Dat(14).Text = modAxis.ySpe1.acc.ToString()
        txt_Dat(15).Text = modAxis.ySpe2.pos.ToString("0.00")
        txt_Dat(16).Text = modAxis.ySpe2.spd.ToString()
        txt_Dat(17).Text = modAxis.ySpe2.acc.ToString()

        txt_Dat(20).Text = modAxis.zWait.pos.ToString("0.00")
        txt_Dat(21).Text = modAxis.zWait.spd.ToString()
        txt_Dat(22).Text = modAxis.zWait.acc.ToString()
        txt_Dat(23).Text = modAxis.zDwnH.pos.ToString("0.00")
        txt_Dat(24).Text = modAxis.zDwnH.spd.ToString()
        txt_Dat(25).Text = modAxis.zDwnH.acc.ToString()
        txt_Dat(26).Text = modAxis.zDwnL.pos.ToString("0.00")
        txt_Dat(27).Text = modAxis.zDwnL.spd.ToString()
        txt_Dat(28).Text = modAxis.zDwnL.acc.ToString()
        txt_Dat(29).Text = modAxis.zUP_H.pos.ToString("0.00")
        txt_Dat(30).Text = modAxis.zUP_H.spd.ToString()
        txt_Dat(31).Text = modAxis.zUP_H.acc.ToString()
        txt_Dat(32).Text = modAxis.zUP_L.pos.ToString("0.00")
        txt_Dat(33).Text = modAxis.zUP_L.spd.ToString()
        txt_Dat(34).Text = modAxis.zUP_L.acc.ToString()
        txt_Dat(35).Text = modAxis.zSpe1.pos.ToString("0.00")
        txt_Dat(36).Text = modAxis.zSpe1.spd.ToString()
        txt_Dat(37).Text = modAxis.zSpe1.acc.ToString()

        Button1.Enabled = enable
        Button2.Enabled = enable
        Button3.Enabled = enable
        Button4.Enabled = enable

    End Sub
    '   position disp
    Friend Sub InvokeDispAxisPosition(ByVal yaxis As Double, ByVal zaxis As Double)

        On Error GoTo jp_end

        If InvokeRequired Then
            ' 別スレッドから呼び出された場合
            Invoke(New SettingAxisPositionDisp_Delegate(AddressOf InvokeDispAxisPosition), New Object() {yaxis, zaxis})
            Return
        End If

        lbl_preYaxis.Text = yaxis.ToString("0.00")
        lbl_preZaxis.Text = zaxis.ToString("0.00")

jp_end:
    End Sub

End Class