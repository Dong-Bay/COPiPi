'===========================================================
' ★ OverlayForm：整列／移動位置を選択するための半透明オーバーレイ
'   - ディスプレイ単位でフォームを生成（巨大フォームを使わない）
'   - クリック座標はスクリーン座標に変換して返す
'   - ガイドメッセージはそのディスプレイ中央に描画
'   - Close 時に必ず Owner にフォーカスを返す（NotifyIcon のズレ防止）
'===========================================================
Public Class OverlayForm
    Public Property Callback As Action(Of Point)

    Private screenBounds As Rectangle

    Public Sub New(bounds As Rectangle)
        screenBounds = bounds

        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = bounds.Location
        Me.Size = bounds.Size

        Me.TopMost = True
        Me.BackColor = Color.Black
        Me.Opacity = 0.4
        Me.ShowInTaskbar = False
        Me.Cursor = Cursors.Cross
    End Sub

    '-----------------------------------------------------------
    ' ★ クリック位置をスクリーン座標で返す
    '-----------------------------------------------------------
    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)

        Dim screenPoint = Me.PointToScreen(e.Location)
        Callback?.Invoke(screenPoint)

        ' ★ 安全に全 OverlayForm を閉じる
        Dim overlays = Application.OpenForms.Cast(Of Form)().
                       Where(Function(f) TypeOf f Is OverlayForm).
                       ToList()

        For Each f In overlays
            f.Close()
        Next
    End Sub

    '-----------------------------------------------------------
    ' ★ Close 時に必ず Owner にフォーカスを返す（NotifyIcon のズレ防止）
    '-----------------------------------------------------------
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)

        If Me.Owner IsNot Nothing Then
            Me.Owner.Activate()
            Me.Owner.Focus()
        End If
    End Sub

    '-----------------------------------------------------------
    ' ★ ディスプレイ中央にガイドメッセージを描画
    '-----------------------------------------------------------
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim centerX As Integer = Me.Width \ 2
        Dim centerY As Integer = Me.Height \ 2

        Dim rect As New Rectangle(centerX - 150, centerY - 50, 300, 100)

        Using bg As New SolidBrush(Color.FromArgb(180, 255, 255, 255))
            e.Graphics.FillRectangle(bg, rect)
        End Using

        Using f As New Font("Meiryo", 14, FontStyle.Bold)
            Dim text As String = "整列位置を選択してください"
            Dim sz = e.Graphics.MeasureString(text, f)
            e.Graphics.DrawString(text, f, Brushes.Black,
                                  rect.X + (rect.Width - sz.Width) \ 2,
                                  rect.Y + (rect.Height - sz.Height) \ 2)
        End Using
    End Sub

End Class
