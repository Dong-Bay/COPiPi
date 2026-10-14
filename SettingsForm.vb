'===========================================================
' ★ SettingsForm：COPiPi の設定画面
'   - ファイル添付モード（LocalCopy / Original / URL）の変更
'   - 付箋サイズモード（Default / Large / XLarge）の変更
'   - 色テーマ編集（名前変更・背景色変更・文字色変更・削除・新規追加）
'   - 設定変更後は DataViewForm に反映し、メニュー／MemoForm を再構築
'===========================================================
Public Class SettingsForm

    Public Property FileAttachMode As String
    Private ownerForm As DataViewForm
    Private tabColors As TabControl

    '-----------------------------------------------------------
    ' ★ コンストラクタ：親フォーム（DataViewForm）を保持
    '   - 設定値の読み書きに使用する
    '-----------------------------------------------------------

    Public Sub New(parentForm As DataViewForm)
        InitializeComponent()
        ownerForm = parentForm
    End Sub

    '===========================================================
    ' ★ SettingsForm_Load：起動時に設定値を UI に反映
    '   - ファイル添付モード（LocalCopy / Original / URL）を読み込み
    '   - 付箋サイズモード（Default / Large / XLarge）を読み込み
    '   - ラジオボタンに反映して初期状態を整える
    '===========================================================
    Private Sub SettingsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        '===========================
        ' ★ ファイル添付モードの読み込み
        '===========================
        Dim mode = ownerForm.LoadSetting("FileAttachMode")

        Select Case mode
            Case "LocalCopy" : rbLocalCopy.Checked = True
            Case "UseOriginal" : rbOriginal.Checked = True
            Case "UseURL" : rbURL.Checked = True
        End Select


        '===========================
        ' ★ 付箋サイズモードの読み込み
        '===========================
        Dim sizeMode = ownerForm.LoadSetting("MemoSizeMode")
        If String.IsNullOrWhiteSpace(sizeMode) Then
            sizeMode = "Default"
        End If

        Select Case sizeMode
            Case "Large"
                sizeLarge.Checked = True

            Case "XLarge"
                sizeXlarge.Checked = True

            Case Else
                sizeDefault.Checked = True
        End Select

    End Sub

    '===========================================================
    ' ★ SettingsForm_Shown：色テーマ編集タブを動的生成
    '   - TabControl を作成し、既存テーマごとにタブを追加
    '   - タブの描画を OwnerDraw にし、背景色と文字色をテーマに合わせる
    '   - 新規テーマ作成用タブ（「新規」）も追加
    '===========================================================
    Private Sub SettingsForm_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown

        tabColors = New TabControl()
        tabColors.Name = "tabColors"
        tabColors.Location = New Point(10, 10)
        tabColors.Size = New Size(529, 167)
        tabColors.DrawMode = TabDrawMode.OwnerDrawFixed
        tabColors.ItemSize = New Size(55, 20)   ' ← タブの幅と高さを固定
        tabColors.SizeMode = TabSizeMode.Fixed
        AddHandler tabColors.DrawItem,
        Sub(sender2 As Object, e2 As DrawItemEventArgs)

            Dim tc = CType(sender2, TabControl)
            If e2.Index < 0 OrElse e2.Index >= tc.TabPages.Count Then Return

            Dim tp = tc.TabPages(e2.Index)
            Dim theme = ColorThemeManager.GetThemeByIndex(CInt(tp.Tag))

            ' 背景色
            Using b As New SolidBrush(tp.BackColor)
                e2.Graphics.FillRectangle(b, e2.Bounds)
            End Using

            ' ★ タブの文字色を ForeColor にする
            Dim textColor As Color = Color.FromArgb(theme.ForeColor)

            TextRenderer.DrawText(
                e2.Graphics,
                tp.Text,
                tc.Font,
                e2.Bounds,
                textColor,
                TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter
            )

        End Sub

        TabPage2.Controls.Add(tabColors)

        ' 既存テーマのタブを生成
        For Each theme In ColorThemeManager.Themes
            Dim tp As New TabPage(theme.Name)
            tp.Tag = theme.Index
            tabColors.TabPages.Add(tp)
            BuildThemeEditor(tp, theme)
        Next

        ' 新規タブ
        Dim tpNew As New TabPage("新規")
        tabColors.TabPages.Add(tpNew)
        AddHandler tpNew.Enter, AddressOf CreateNewTheme

    End Sub

    '===========================================================
    ' ★ OK ボタン：ファイル添付モードを保存して閉じる
    '   - 選択されたモードを DataViewForm に反映
    '   - AppSettings.xml に保存
    '===========================================================
    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        If rbLocalCopy.Checked Then FileAttachMode = "LocalCopy"
        If rbOriginal.Checked Then FileAttachMode = "UseOriginal"
        If rbURL.Checked Then FileAttachMode = "UseURL"

        ownerForm.SetFileAttachMode(FileAttachMode)
        ownerForm.SaveSetting("FileAttachMode", FileAttachMode)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    '-----------------------------------------------------------
    ' ★ Cancel ボタン：変更を破棄して閉じる
    '-----------------------------------------------------------
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    '===========================================================
    ' ★ BuildThemeEditor：テーマ編集 UI を構築
    '   - 名前変更 TextBox
    '   - 背景色変更ボタン（ColorDialog）
    '   - 文字色変更ボタン
    '   - 保存ボタン（color.xml に書き込み）
    '   - 削除ボタン（テーマ削除＋タブ削除）
    '   - タブ背景色・文字色をテーマに合わせて更新
    '===========================================================
    Private Sub BuildThemeEditor(tp As TabPage, theme As ColorTheme)
        Dim label1 As New Label
        label1.Text = "表示名を変更できます。"
        label1.Location = New Point(10, 5)
        label1.AutoSize = True
        label1.ForeColor = Color.FromArgb(theme.ForeColor)
        ' 名前編集 TextBox
        Dim txt As New TextBox()
        txt.Text = theme.Name
        txt.Location = New Point(15, 20)
        txt.Width = 200

        AddHandler txt.TextChanged,
        Sub()
            theme.Name = txt.Text
            tp.Text = txt.Text   ' タブ名も更新
        End Sub

        tp.Controls.Add(label1)
        tp.Controls.Add(txt)
        tp.BackColor = Color.FromArgb(theme.ColorValue)

        ' 色変更ボタン
        Dim btnColor As New Button()
        btnColor.Text = "付箋の色を変更"
        btnColor.AutoSize = True
        btnColor.Location = New Point(10, 100)
        btnColor.UseVisualStyleBackColor = False
        btnColor.BackColor = Color.White
        AddHandler btnColor.Click,
Sub()
    Dim dlg As New ColorDialog()
    If dlg.ShowDialog() = DialogResult.OK Then

        Dim c = dlg.Color
        theme.ColorValue = c.ToArgb()
        tp.BackColor = c

        ' ★ 自動フォント色判定（濃い色なら白、薄い色なら黒）
        Dim brightness = c.GetBrightness()
        If brightness < 0.5 Then
            theme.ForeColor = Color.White.ToArgb()
        Else
            theme.ForeColor = Color.Black.ToArgb()
        End If
        ' ★ UI をまとめて更新
        RefreshThemeUI(tp, theme)
    End If
End Sub


        tp.Controls.Add(btnColor)

        ' タイトル文字色変更ボタン
        Dim btnForeColor As New Button()
        btnForeColor.Text = "タイトル文字色を変更"
        btnForeColor.AutoSize = True
        btnForeColor.Location = New Point(btnColor.Right + 10, btnColor.Top)
        btnForeColor.UseVisualStyleBackColor = False
        btnForeColor.BackColor = Color.White

        AddHandler btnForeColor.Click,
        Sub()
            Dim dlg As New ColorDialog()
            If dlg.ShowDialog() = DialogResult.OK Then
                theme.ForeColor = dlg.Color.ToArgb()
            End If
            RefreshThemeUI(tp, theme)
        End Sub

        tp.Controls.Add(btnForeColor)

        '===========================================
        ' ★ 保存ボタン（右下）
        '===========================================
        Dim btnSave As New Button()
        btnSave.Text = "保存"
        btnSave.Size = New Size(80, 30)
        btnSave.UseVisualStyleBackColor = False
        btnSave.BackColor = Color.White
        btnSave.Location = New Point(tabColors.Width - btnSave.Width - 20,
                                 tabColors.Height - btnSave.Height - 40)

        AddHandler btnSave.Click,
        Sub()
            ColorThemeManager.SaveThemes()
            MessageBox.Show("カラーテーマを保存しました。", "保存",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        tp.Controls.Add(btnSave)

        '===========================================
        ' ★ 削除ボタン（右下・保存の左）
        '===========================================
        Dim btnDelete As New Button()
        btnDelete.Text = "削除"
        btnDelete.Size = New Size(80, 30)
        btnDelete.UseVisualStyleBackColor = False
        btnDelete.BackColor = Color.White
        btnDelete.Location = New Point(btnSave.Left - btnDelete.Width - 10,
                                   tabColors.Height - btnDelete.Height - 40)

        AddHandler btnDelete.Click,
Sub()
    ' 新規テーマはキャンセル扱い
    If theme.Name = "NewColor" Then
        ColorThemeManager.Themes.Remove(theme)
        tabColors.TabPages.Remove(tp)
        Return
    End If

    If MessageBox.Show($"{theme.Name} を削除しますか？",
                       "削除確認",
                       MessageBoxButtons.YesNo,
                       MessageBoxIcon.Question) = DialogResult.No Then
        Return
    End If

    ' テーマ削除
    ColorThemeManager.Themes.Remove(theme)

    ' タブ削除
    tabColors.TabPages.Remove(tp)
End Sub


        tp.Controls.Add(btnDelete)

    End Sub

    '===========================================================
    ' ★ CreateNewTheme：新規テーマを追加
    '   - 新しい ColorTheme を作成し Themes に追加
    '   - 「新規」タブの直前に新規タブを挿入
    '   - BuildThemeEditor で編集 UI を構築
    '===========================================================
    Private Sub CreateNewTheme(sender As Object, e As EventArgs)

        Dim tabColors = CType(TabPage2.Controls("tabColors"), TabControl)

        ' 新しいテーマを作成
        Dim newTheme As New ColorTheme()
        newTheme.Index = ColorThemeManager.GetNextIndex()
        newTheme.Name = "NewColor"
        newTheme.ColorValue = Color.LightGray.ToArgb()

        ' テーマリストに追加
        ColorThemeManager.Themes.Add(newTheme)

        ' 新しいタブを作成
        Dim tp As New TabPage(newTheme.Name)
        tp.Tag = newTheme.Index

        ' 新規タブの直前に挿入
        tabColors.TabPages.Insert(tabColors.TabPages.Count - 1, tp)

        ' 編集 UI を構築
        BuildThemeEditor(tp, newTheme)

        ' 新規タブに戻らないように新しいタブを選択
        tabColors.SelectedTab = tp

    End Sub

    '===========================================================
    ' ★ 設定画面終了時：テーマとメニューを再構築
    '   - 色テーマを保存（念のため）
    '   - メイン画面の色メニュー／表示メニューを再構築
    '   - 全 MemoForm の色を再適用（RefreshAllMemoColors）
    '===========================================================
    Private Sub SettingForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing

        ColorThemeManager.SaveThemes()
        ColorThemeManager.LoadThemes()
        ' ★ メニューを完全再構築（左＋右）
        ownerForm.BuildMenus()

        ' ★ 全 MemoForm の色を再適用
        ownerForm.RefreshAllMemoColors()
    End Sub


    '===========================================================
    ' ★ RefreshThemeUI：テーマ変更後の UI を即時反映
    '   - タブ背景色を更新
    '   - DrawItem によりタブ文字色も更新
    '   - タブ内の Label の ForeColor をテーマに合わせて変更
    '===========================================================
    Private Sub RefreshThemeUI(tp As TabPage, theme As ColorTheme)

        ' ★ タブ背景色
        tp.BackColor = Color.FromArgb(theme.ColorValue)

        ' ★ タブ文字色（DrawItem が theme.ForeColor を使う）
        tabColors.Invalidate()   ' ← タブ全体を再描画

        ' ★ ラベルの文字色
        For Each ctrl As Control In tp.Controls
            If TypeOf ctrl Is Label Then
                ctrl.ForeColor = Color.FromArgb(theme.ForeColor)
            End If
        Next

    End Sub

    '===========================================================
    ' ★ 付箋サイズモードの保存
    '   - Default / Large / XLarge を DataViewForm に反映
    '   - 設定画面を閉じる
    '===========================================================

    Private Sub sizeBtnOK_Click(sender As Object, e As EventArgs) Handles sizeBtnOK.Click

        Dim mode As String = "Default"

        If sizeLarge.Checked Then
            mode = "Large"
        ElseIf sizeXlarge.Checked Then
            mode = "XLarge"
        End If

        ' ★ DataViewForm に保存
        ownerForm.SetMemoSizeMode(mode)

        Me.Close()
    End Sub

    '-----------------------------------------------------------
    ' ★ サイズ設定キャンセル：変更を破棄して閉じる
    '-----------------------------------------------------------
    Private Sub sizeBtnCancel_Click(sender As Object, e As EventArgs) Handles sizeBtnCancel.Click
        Me.Close()
    End Sub

End Class
