
Imports System.Drawing
Imports System.Reflection.Emit
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles.VisualStyleElement

'===========================================================
' ★ MemoForm：COPiPi の付箋ウィンドウ
'   - DataRow（MyRow）と完全同期して動作する
'   - タイトルバーのドラッグ、折り畳み、TopMost、位置保存など
'   - ファイル添付・URL・通知ラベルなどの UI を保持
'===========================================================
Public Class MemoForm

    '===========================================================
    ' ★ 定数・フィールド
    '===========================================================

    Private isCaptionDoubleClick As Boolean = False    ' タイトルバーのダブルクリック判定
    Public hideButton As CircleButton                 ' 左上の「非表示」ボタン
    Public pinLabel As System.Windows.Forms.Label      ' TopMost ピン表示
    Private sizeProfile As MemoSizeProfile             ' 各種サイズ
    Public TitleHeight As Integer
    Public OriginalHeight As Integer
    Private titleFont As Font
    Public remindLabel As System.Windows.Forms.Label  ' 通知ラベル
    Public clipLabel As System.Windows.Forms.Label     ' 添付ファイルアイコン
    Public fileLabel As System.Windows.Forms.Label    ' 添付ファイル名表示
    Private guidePanel As System.Windows.Forms.Panel   ' 添付時ガイド表示
    Private guideLabel As System.Windows.Forms.Label   ' 添付時ガイド表示
    Private parentForm As DataViewForm                 ' 親フォーム（DataViewForm）
    Private memoTip As New System.Windows.Forms.ToolTip ' ツールチップ
    Public TitleForeColor As Color = Color.Black

    ' DataRow（メモの実体）
    Public Property MyRow As DataRow

    ' 添付ファイルの種類
    Public Enum FileTypeEnum
        None
        Local
        Original
        URL
    End Enum



    '===========================================================
    ' ★ WndProc：Windows メッセージ処理（ドラッグ・折り畳み・TopMost）
    '===========================================================
    Protected Overrides Sub WndProc(ByRef m As Message)

        Const WM_NCHITTEST As Integer = &H84
        Const WM_NCLBUTTONDOWN As Integer = &HA1
        Const WM_NCLBUTTONDBLCLK As Integer = &HA3
        Const WM_NCRBUTTONUP As Integer = &HA5
        Const WM_EXITSIZEMOVE As Integer = &H232
        Const HTCAPTION As Integer = 2

        Select Case m.Msg


            '-------------------------------------------------------
            ' ★ タイトルバーのドラッグ判定（最軽量）
            '-------------------------------------------------------
            Case WM_NCHITTEST

                ' LParam を高速に分解（Point を生成しない）
                Dim lx As Integer = m.LParam.ToInt32() And &HFFFF
                Dim ly As Integer = (m.LParam.ToInt32() >> 16)

                ' フォーム内座標に変換
                Dim localY As Integer = ly - Me.Top

                ' タイトルバー領域ならドラッグ可能にする
                If localY < TitleHeight Then
                    m.Result = CType(HTCAPTION, IntPtr)
                    Return
                End If

            '-------------------------------------------------------
            ' ★ 左クリック → メモ内容をクリップボードへコピー
            '-------------------------------------------------------
            Case WM_NCLBUTTONDOWN

                If MemoText.Text.Length > 0 Then
                    Clipboard.SetText(MemoText.Text)
                    Debug.WriteLine(Clipboard.GetText)
                End If

                MyBase.WndProc(m)
                Return

            '-------------------------------------------------------
            ' ★ ダブルクリック → 折り畳み／展開
            '-------------------------------------------------------
            Case WM_NCLBUTTONDBLCLK

                If m.WParam.ToInt32() = HTCAPTION Then
                    Dim collapsed As Boolean = CBool(MyRow("CollapsedFlag"))
                    MyRow("CollapsedFlag") = Not collapsed

                    ' ★ 高さ切り替えは「Not collapsed」が折りたたみ
                    If collapsed Then
                        Me.Height = OriginalHeight   ' 展開
                    Else
                        Me.Height = TitleHeight      ' 折りたたみ
                    End If

                    parentForm.mainData.AcceptChanges()
                    parentForm.Save()
                End If

                Return


            '-------------------------------------------------------
            ' ★ 右クリック → TopMost トグル
            '-------------------------------------------------------
            Case WM_NCRBUTTONUP

                If m.WParam.ToInt32() = HTCAPTION Then
                    Me.TopMost = Not Me.TopMost
                    pinLabel.Text = If(Me.TopMost, "📌", "")
                    MyRow("TopMostFlag") = Me.TopMost
                    parentForm.SetMemoTopMost(New List(Of String) From {MyRow("id").ToString()}, Me.TopMost)
                End If

                Return

            '-------------------------------------------------------
            ' ★ ドラッグ終了 → 位置保存
            '-------------------------------------------------------
            Case WM_EXITSIZEMOVE

                MyRow("x") = Me.Left
                MyRow("y") = Me.Top
                parentForm.mainData.AcceptChanges()
                parentForm.Save()
                Return

        End Select

        ' その他のメッセージは OS に任せる
        MyBase.WndProc(m)
    End Sub


    '===========================================================
    ' ★ OnResize：ピン・クリップアイコンの位置調整
    '===========================================================
    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)

        If pinLabel IsNot Nothing Then
            pinLabel.Left = Me.Width - pinLabel.Width - 4
        End If

        If clipLabel IsNot Nothing Then
            clipLabel.Left = pinLabel.Left - clipLabel.Width - 2
        End If
    End Sub

    '===========================================================
    ' ★ コンストラクタ：DataRow と親フォームを受け取る（辞書方式対応）
    '   - UI 初期化をすべてここに集約
    '   - MemoBuild に依存しない構造にする
    '===========================================================
    Public Sub New(row As DataRow, parent As DataViewForm)

        InitializeComponent()

        '-----------------------------------------------------------
        ' ★ 親フォームとのリンク
        '-----------------------------------------------------------
        MyRow = row
        parentForm = parent

        '-----------------------------------------------------------
        ' ★ カスタム UI パーツ生成（従来の動作を維持）
        '-----------------------------------------------------------
        CreateMacStyleHideButton()
        CreateTopMostPin()
        CreateClipIcon()
        CreateFileLabel()
        CreateRemindLabel()

        '-----------------------------------------------------------
        ' ★ タイトル・本文の初期化
        '-----------------------------------------------------------
        Dim title As String = row("title").ToString()
        Dim text As String = row("text").ToString()

        Me.Text = title
        Me.TitleText.Text = title
        Me.MemoText.Text = text

        '-----------------------------------------------------------
        ' ★ 色テーマ適用
        '-----------------------------------------------------------
        Dim theme = ColorThemeManager.GetThemeByIndex(CInt(row("MemoColor")))

        If theme IsNot Nothing Then
            Me.BackColor = Color.FromArgb(theme.ColorValue)
            Me.TitleForeColor = Color.FromArgb(theme.ForeColor)
            Me.clipLabel.ForeColor = Color.FromArgb(theme.ForeColor)
            Me.fileLabel.ForeColor = Color.FromArgb(theme.ForeColor)
            Me.pinLabel.ForeColor = Color.FromArgb(theme.ForeColor)
            Me.remindLabel.ForeColor = Color.FromArgb(theme.ForeColor)

            Me.hideButton.CircleBorderColor = Color.FromArgb(theme.ForeColor)
            Me.hideButton.Invalidate()
        Else
            Me.BackColor = Color.LightGray
        End If

        '-----------------------------------------------------------
        ' ★ サイズプロファイル適用
        '-----------------------------------------------------------
        Me.ApplySizeProfile(parent.GetMemoSizeProfile())

        '-----------------------------------------------------------
        ' ★ 表示位置を DataRow から復元
        '-----------------------------------------------------------
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = New Point(CInt(row("x")), CInt(row("y")))

    End Sub


    '===========================================================
    ' ★ OnHandleCreated：ダブルバッファリング有効化（ちらつき防止）
    '===========================================================
    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        Me.DoubleBuffered = True
    End Sub


    '===========================================================
    ' ★ ApplySizeProfile：サイズ変更対応（完全版）
    '===========================================================
    Public Sub ApplySizeProfile(p As MemoSizeProfile)

        sizeProfile = p

        '===========================================================
        ' ★ 付箋本体サイズ
        '===========================================================
        Me.Width = p.Width
        TitleHeight = p.TitleHeight
        OriginalHeight = p.OriginalHeight

        '===========================================================
        ' ★ フォント
        '===========================================================
        TitleText.Font = p.MemoFont
        TitleText.ForeColor = Color.Black
        MemoText.Font = p.MemoFont
        titleFont = p.TitleFont   ' OnPaint 用

        '===========================================================
        ' ★ hideButton（左上の白丸）
        '===========================================================
        hideButton.Width = p.IconSize
        hideButton.Height = p.IconSize
        hideButton.Left = p.HideButtonLeft
        hideButton.Top = p.HideButtonTop


        '===========================================================
        ' ★ pinLabel（📌）
        '===========================================================
        pinLabel.Width = p.IconSize
        pinLabel.Height = p.IconSize
        pinLabel.Font = New Font("Segoe UI Emoji", p.IconFontSize)
        pinLabel.Left = Me.Width - p.IconSize - p.IconRightMargin
        pinLabel.Top = 0

        '===========================================================
        ' ★ clipLabel（📎）
        '===========================================================
        If clipLabel Is Nothing Then
            CreateClipIcon()
        End If

        clipLabel.Width = p.IconSize
        clipLabel.Height = p.IconSize
        clipLabel.Font = New Font("Segoe UI Emoji", p.IconFontSize)

        clipLabel.Left = pinLabel.Left - clipLabel.Width - 2
        clipLabel.Top = 0
        clipLabel.BringToFront()

        '===========================================================
        ' ★ TitleText（タイトル編集欄）
        '===========================================================
        TitleText.Left = p.TitleTextBoxLeft
        TitleText.Top = p.TitleTextBoxTop
        TitleText.Width = p.TitleTextBoxWidth

        '===========================================================
        ' ★ MemoText（メモ編集欄）
        '===========================================================
        MemoText.Left = p.TitleTextBoxLeft
        MemoText.Top = p.TitleTextBoxTop + TitleText.Height + 4
        MemoText.Width = p.TitleTextBoxWidth
        MemoText.Height = p.MemoTextBoxHeight


        '===========================================================
        ' ★ ActionButton（サイズ・フォント共通）
        '===========================================================
        UrlButton.Width = p.ActionButtonSize
        UrlButton.Height = p.ActionButtonSize
        UrlButton.Font = New Font("Segoe UI Emoji", p.ActionButtonEmojiFontSize)

        RemindButton.Width = p.ActionButtonSize
        RemindButton.Height = p.ActionButtonSize
        RemindButton.Font = New Font("Segoe UI Emoji", p.ActionButtonEmojiFontSize)

        MemoDelete.Width = p.ActionButtonSize
        MemoDelete.Height = p.ActionButtonSize
        MemoDelete.Font = New Font("Segoe UI Emoji", p.ActionButtonEmojiFontSize)

        '===========================================================
        ' ★ UrlButton（右上）
        '===========================================================
        UrlButton.Left = p.UrlButtonLeft
        UrlButton.Top = p.UrlButtonTop

        '===========================================================
        ' ★ RemindButton（左下）
        '===========================================================
        RemindButton.Left = p.RemindButtonLeft
        RemindButton.Top = p.RemindButtonTop

        '===========================================================
        ' ★ DeleteButton（右下）
        '===========================================================
        MemoDelete.Left = p.DeleteButtonLeft
        MemoDelete.Top = p.DeleteButtonTop

        '===========================================================
        ' ★ RemindLabel（通知ラベル）
        '===========================================================
        If remindLabel Is Nothing Then
            CreateRemindLabel()
        End If

        remindLabel.Left = RemindButton.Left - p.RemindLabelOffsetX
        remindLabel.Top = RemindButton.Top + p.RemindLabelOffsetY
        remindLabel.Font = New Font("Segoe UI", p.RemindLabelFontSize)

        '===========================================================
        ' ★ fileLabel（添付ファイル名）
        '===========================================================
        If fileLabel Is Nothing Then
            CreateFileLabel()
        End If

        fileLabel.Left = MemoDelete.Left + p.FileLabelOffsetX
        fileLabel.Top = MemoDelete.Top + p.FileLabelOffsetY
        fileLabel.Width = p.FileLabelWidth
        fileLabel.Font = New Font("Segoe UI", p.FileLabelFontSize, FontStyle.Underline)

    End Sub




    '===========================================================
    ' ★ 非表示ボタン（左上の白丸）
    '===========================================================
    Private Sub CreateMacStyleHideButton()

        hideButton = New CircleButton With {
        .Width = 14,   ' 仮の初期値
        .Height = 14,  ' 仮の初期値
        .Left = 2,
        .Top = 1,
        .CircleColor = Color.White,
        .CircleBorderColor = Color.Black,
        .FlatStyle = FlatStyle.Flat
        }

        hideButton.FlatAppearance.BorderSize = 0
        hideButton.Cursor = Cursors.Hand

        AddHandler hideButton.Click, AddressOf HideButton_Click
        memoTip.SetToolTip(hideButton, "この付箋を非表示にします")

        Me.Controls.Add(hideButton)
    End Sub


    '===========================================================
    ' ★ 添付ファイルアイコン（クリップ）を作成
    '===========================================================
    Private Sub CreateClipIcon()

        clipLabel = New System.Windows.Forms.Label With {
            .AutoSize = False,
            .Width = 14,
            .Height = 14,
            .Top = 0,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI Emoji", 8),
            .ForeColor = Color.Black,
            .BackColor = Color.Transparent,
            .Text = ""
        }

        clipLabel.Left = pinLabel.Left - clipLabel.Width - 2
        Me.Controls.Add(clipLabel)
        AddHandler clipLabel.DoubleClick, AddressOf FileLabel_Click
    End Sub



    '===========================================================
    ' ★ TopMost ピンアイコンを作成
    '===========================================================
    Private Sub CreateTopMostPin()

        pinLabel = New System.Windows.Forms.Label With {
    .AutoSize = False,
    .Width = 14,
    .Height = 14,
    .Top = 0,
    .TextAlign = ContentAlignment.MiddleCenter,
    .Font = New Font("Segoe UI Emoji", 8),
    .ForeColor = Color.Black,
    .BackColor = Color.Transparent,
    .Text = ""
}

        ' 右端に配置
        pinLabel.Left = Me.Width - pinLabel.Width - 4

        Me.Controls.Add(pinLabel)
    End Sub


    '===========================================================
    ' ★ 添付ファイル名ラベル（fileLabel）を作成
    '===========================================================
    Private Sub CreateFileLabel()

        ' ★ 仮の初期値
        Dim defaultWidth As Integer = 90
        Dim defaultFontSize As Integer = 8

        fileLabel = New System.Windows.Forms.Label With {
        .AutoSize = False,
        .Width = defaultWidth,
        .Height = 20,
        .Font = New Font("Segoe UI", 8, FontStyle.Underline),
        .RightToLeft = RightToLeft.No,
        .TextAlign = ContentAlignment.TopLeft,
        .ForeColor = Color.DarkBlue,
        .BackColor = Color.Transparent,
        .Cursor = Cursors.Hand,
        .Text = "",
        .Visible = False
    }

        AddHandler fileLabel.Click, AddressOf FileLabel_Click

        Me.Controls.Add(fileLabel)
    End Sub



    '===========================================================
    ' ★ OnPaint：タイトルバー描画（背景＋タイトル文字）
    '===========================================================
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        ' タイトルバー領域
        'Dim titleRect As New Rectangle(0, 0, Me.Width, TitleHeight)

        ' タイトルバー背景
        Using b As New SolidBrush(Me.BackColor)
            e.Graphics.FillRectangle(b, 0, 0, Me.Width, TitleHeight)
        End Using
        Dim titleX As Integer = hideButton.Left + hideButton.Width + 2
        ' タイトル文字（Font の new を毎回しない）
        Using b As New SolidBrush(TitleForeColor)
            e.Graphics.DrawString(Me.Text, titleFont, b, titleX, 0)
        End Using
    End Sub


    '===========================================================
    ' ★ HideButton_Click：付箋を非表示にする
    '===========================================================
    Private Sub HideButton_Click(sender As Object, e As EventArgs)

        ' ★ DataRow 更新
        MyRow("VisibleFlag") = False
        Me.Close()

        ' ★ 表示／非表示メニュー更新
        parentForm.BuildShowCheckMenu()

        ' ★ XML 保存
        parentForm.mainData.AcceptChanges()
        parentForm.Save()

    End Sub


    '===========================================================
    ' ★ FileLabel_Click：添付ファイル／URL を開く。フォルダリンクにも対応。
    '===========================================================
    Private Sub FileLabel_Click(sender As Object, e As EventArgs)

        Dim ref As String = MyRow("FileReference").ToString()
        If String.IsNullOrWhiteSpace(ref) Then Return

        ' DataRow の文字列を Enum に変換
        Dim typeStr As String = MyRow("FileType").ToString()
        Dim ftype As FileTypeEnum

        If [Enum].TryParse(typeStr, True, ftype) = False Then
            ftype = FileTypeEnum.None
        End If

        ' ★ フォルダリンク対応（最優先で判定）
        If IO.Directory.Exists(ref) Then
            Process.Start(ref)   ' Explorer が開く
            Return
        End If

        Select Case ftype

            Case FileTypeEnum.Local, FileTypeEnum.Original
                ' ローカルファイルを開く
                If IO.File.Exists(ref) Then
                    Dim psi As New ProcessStartInfo(ref)
                    psi.UseShellExecute = True
                    Process.Start(psi)
                Else
                    MessageBox.Show("ファイルが見つかりません: " & ref)
                End If

            Case FileTypeEnum.URL
                ' URL をブラウザで開く
                Dim psi As New ProcessStartInfo(ref)
                psi.UseShellExecute = True
                Process.Start(psi)

            Case FileTypeEnum.None
                Return

        End Select

    End Sub


    '===========================================================
    ' ★ MemoForm_Load：初期状態の UI を反映
    '===========================================================
    Private Sub MemoForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        memoTip.SetToolTip(MemoDelete, "この付箋を削除します")
        memoTip.SetToolTip(RemindButton, "通知する時間を設定します")

        ' ★ D&D ガイドレイヤーを作成
        guidePanel = New Panel With {
        .BackColor = Color.WhiteSmoke,
        .Visible = False,
        .Dock = DockStyle.Fill
    }

        Dim stack As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 2
    }

        guideLabel = New System.Windows.Forms.Label With {
        .Text = "",
        .ForeColor = Color.Black,
        .Font = New Font("Segoe UI", 10, FontStyle.Bold),
        .Dock = DockStyle.Fill,
        .TextAlign = ContentAlignment.MiddleCenter
    }
        guidePanel.Controls.Add(guideLabel)
        stack.Controls.Add(guideLabel, 0, 1)
        guidePanel.Controls.Add(stack)
        MemoText.Controls.Add(guidePanel)

        '-----------------------------------------------------------
        ' ★ 全状態同期
        '-----------------------------------------------------------
        RefreshUI()

        ' 軽いイベント登録
        AddHandler TitleText.LostFocus, AddressOf TitleText_Leave
        AddHandler MemoText.LostFocus, AddressOf MemoText_Leave
        AddHandler MemoText.DragEnter, AddressOf MemoText_DragEnter
        AddHandler MemoText.DragLeave, AddressOf MemoText_DragLeave
    End Sub


    '===========================================================
    ' ★ MemoForm_Shown：UI の最終調整（BeginInvoke）
    '===========================================================
    Private Sub MemoForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        ' Shown では重い処理を避けるため BeginInvoke に移す
        Me.BeginInvoke(Sub()

                           ' FileTypeEnum 判定
                           Dim typeStr As String = MyRow("FileType").ToString()
                           Dim ftype As FileTypeEnum
                           If [Enum].TryParse(typeStr, True, ftype) = False Then
                               ftype = FileTypeEnum.None
                           End If

                           ' URL のときだけ URL ボタン表示
                           If ftype = FileTypeEnum.URL Then
                               UrlButton.Visible = True
                               memoTip.SetToolTip(UrlButton, "URLを設定します")
                           Else
                               UrlButton.Visible = False
                           End If

                           ' 添付ファイルがある場合のみ fileLabel を表示
                           Dim fileReference As String = MyRow("FileReference").ToString()

                           If Not String.IsNullOrWhiteSpace(fileReference) Then

                               AdjustFileLabel()

                               If IO.Directory.Exists(fileReference) Then
                                   clipLabel.Text = "📦"
                                   fileLabel.Text = IO.Path.GetFileName(fileReference)
                                   memoTip.SetToolTip(clipLabel, "フォルダを開きます")
                                   fileLabel.Visible = True

                               Else
                                   Select Case ftype
                                       Case FileTypeEnum.Local
                                           clipLabel.Text = "📂"
                                           memoTip.SetToolTip(clipLabel, "ファイルを開きます")

                                       Case FileTypeEnum.Original
                                           clipLabel.Text = "📄"
                                           memoTip.SetToolTip(clipLabel, "ファイルを開きます")

                                       Case FileTypeEnum.URL
                                           clipLabel.Text = "🌎"
                                           fileLabel.Text = GetSmartUrlText(fileReference)
                                           memoTip.SetToolTip(clipLabel, "リンク先を開きます")

                                       Case Else
                                           clipLabel.Text = ""
                                   End Select

                                   fileLabel.Visible = True
                               End If

                           Else
                               fileLabel.Visible = False
                               clipLabel.Text = ""
                           End If

                       End Sub)

    End Sub

    '===========================================================
    ' ★ RefreshUI：辞書方式で再利用されたときに UI を再同期する
    '===========================================================
    Public Sub RefreshUI()

        '-----------------------------------------------------------
        ' ★ タイトル・本文
        '-----------------------------------------------------------
        Me.Text = MyRow("title").ToString()
        Me.TitleText.Text = MyRow("title").ToString()
        Me.MemoText.Text = MyRow("text").ToString()

        '-----------------------------------------------------------
        ' ★ 折り畳み状態
        '-----------------------------------------------------------
        If CBool(MyRow("CollapsedFlag")) Then
            Me.Height = TitleHeight
        Else
            Me.Height = OriginalHeight
        End If

        '-----------------------------------------------------------
        ' ★ TopMost 状態
        '-----------------------------------------------------------
        Me.TopMost = CBool(MyRow("TopMostFlag"))
        pinLabel.Text = If(Me.TopMost, "📌", "")

        '-----------------------------------------------------------
        ' ★ 位置同期
        '-----------------------------------------------------------
        Me.Location = New Point(CInt(MyRow("x")), CInt(MyRow("y")))

        '-----------------------------------------------------------
        ' ★ 添付ファイル表示（軽量版）
        '-----------------------------------------------------------
        Dim fileReference As String = MyRow("FileReference").ToString()
        Dim typeStr As String = MyRow("FileType").ToString()
        Dim ftype As FileTypeEnum

        If [Enum].TryParse(typeStr, True, ftype) = False Then
            ftype = FileTypeEnum.None
        End If

        If String.IsNullOrWhiteSpace(fileReference) Then
            fileLabel.Visible = False
            clipLabel.Text = ""
        Else
            If IO.Directory.Exists(fileReference) Then
                clipLabel.Text = "📦"
                fileLabel.Text = IO.Path.GetFileName(fileReference)
            Else
                Select Case ftype
                    Case FileTypeEnum.Local
                        clipLabel.Text = "📂"
                        fileLabel.Text = IO.Path.GetFileName(fileReference)
                    Case FileTypeEnum.Original
                        clipLabel.Text = "📄"
                        fileLabel.Text = IO.Path.GetFileName(fileReference)
                    Case FileTypeEnum.URL
                        clipLabel.Text = "🌎"
                        fileLabel.Text = GetSmartUrlText(fileReference)
                    Case Else
                        clipLabel.Text = ""
                End Select
            End If

            fileLabel.Visible = True
        End If

        AdjustFileLabel()
        '-----------------------------------------------------------
        ' ★ fileLabel の位置を再計算（重なり防止）
        '-----------------------------------------------------------
        fileLabel.Left = MemoDelete.Left + sizeProfile.FileLabelOffsetX
        fileLabel.Top = MemoDelete.Top + sizeProfile.FileLabelOffsetY
        fileLabel.Width = sizeProfile.FileLabelWidth
        fileLabel.Font = New Font("Segoe UI", sizeProfile.FileLabelFontSize, FontStyle.Underline)

        '-----------------------------------------------------------
        ' ★ リマインダー同期（軽量版）
        '-----------------------------------------------------------
        LoadRemindLabel()
        CheckReminder()

    End Sub



    '===========================================================
    ' ★ TitleText_Leave：タイトル編集欄からフォーカスが外れたとき
    '   - DataRow の title を更新
    '   - フォームタイトルも更新
    '   - 再描画して反映
    '   - メニュー画面にも反映
    '===========================================================
    Private Sub TitleText_Leave(sender As Object, e As EventArgs) Handles TitleText.Leave
        If parentForm.mainData Is Nothing Then Exit Sub

        MyRow("title") = TitleText.Text
        parentForm.mainData.AcceptChanges()
        parentForm.Save()

        Me.Text = TitleText.Text
        Me.Invalidate()   ' タイトルバー再描画
        ' メニュー側のタイトルを更新
        parentForm.RefreshMemoTitleInMenu(MyRow("id").ToString(), TitleText.Text)
    End Sub


    '===========================================================
    ' ★ MemoText_Leave：本文編集欄からフォーカスが外れたとき
    '   - DataRow の text を更新
    '===========================================================
    Private Sub MemoText_Leave(sender As Object, e As EventArgs) Handles MemoText.Leave
        If parentForm.mainData Is Nothing Then Exit Sub

        MyRow("text") = MemoText.Text
        parentForm.mainData.AcceptChanges()
        parentForm.Save()
    End Sub


    '===========================================================
    ' ★ MemoText_DragEnter：ドラッグされたデータの種類判定
    '   - ファイルなら Copy を許可
    '===========================================================
    Private Sub MemoText_DragEnter(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
            Dim files() As String = CType(e.Data.GetData(DataFormats.FileDrop), String())
            If files IsNot Nothing AndAlso files.Length > 0 Then
                Dim path As String = files(0)

                ' ★ フォルダかファイルかでガイド文言を変える
                If IO.Directory.Exists(path) Then
                    guideLabel.Text = "このフォルダへのリンクを添付"
                Else
                    guideLabel.Text = "ファイルを添付"
                End If
                guidePanel.Visible = True
            End If
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub MemoText_DragLeave(sender As Object, e As EventArgs) Handles MemoText.DragLeave
        guidePanel.Visible = False
    End Sub

    '===========================================================
    ' ★ MemoText_DragDrop：ファイルドロップ処理
    '   - ファイル添付モードに応じて保存方法を決定
    '   - DataRow に FileReference / FileType を保存
    '   - UI（fileLabel / clipLabel）を更新
    '===========================================================
    Private Sub MemoText_DragDrop(sender As Object, e As DragEventArgs) Handles MemoText.DragDrop
        guidePanel.Visible = False
        Dim files() As String = CType(e.Data.GetData(DataFormats.FileDrop), String())
        If files Is Nothing OrElse files.Length = 0 Then Exit Sub

        Dim filePath As String = files(0)
        Dim fileName As String = IO.Path.GetFileName(filePath)

        '===========================================================
        ' ★ 新規付箋判定（Me.Text が "新規コピピ" のときだけ）
        '===========================================================
        Dim isNewMemo As Boolean = (Me.Text = "新規コピピ")

        '===========================================================
        ' ★ まずフォルダ判定（最優先）
        '===========================================================
        If IO.Directory.Exists(filePath) Then

            ' DataRow に保存（FileType は Original 扱いでOK）
            MyRow("FileReference") = filePath
            MyRow("FileType") = FileTypeEnum.Original.ToString()

            ' ★ 新規付箋のときだけタイトル変更
            If isNewMemo Then
                TitleText.Text = IO.Path.GetFileName(filePath)
                Me.Text = TitleText.Text
                Me.Invalidate()
            End If

            parentForm.mainData.AcceptChanges()
            parentForm.Save()
            parentForm.RebuildShowCheckMenu()

                ' UI 更新
                AdjustFileLabel()
                fileLabel.Visible = True
                fileLabel.Text = IO.Path.GetFileName(filePath)

                If clipLabel Is Nothing Then
                    CreateClipIcon()
                End If

                clipLabel.Text = "📦"
                memoTip.SetToolTip(clipLabel, "フォルダを開きます")

                ' メニュー側のタイトルを更新
                parentForm.RefreshMemoTitleInMenu(MyRow("id").ToString(), TitleText.Text)

                Return
            End If

            ' DataViewForm の設定を取得
            Dim mode As String = parentForm.GetFileAttachMode()
        Dim finalPath As String = ""

        Select Case mode

        '-----------------------------------------------------------
        ' ★ ① LocalCopy → 警告なしで即コピー
        '-----------------------------------------------------------
            Case "LocalCopy"
                finalPath = SaveFileWithAutoNumber(filePath)
                MyRow("FileType") = FileTypeEnum.Local.ToString()

        '-----------------------------------------------------------
        ' ★ ② Original / UseURL → ユーザーに確認
        '-----------------------------------------------------------
            Case "UseOriginal", "UseURL"

                Dim result = MessageBox.Show(
                $"ファイル添付を検出しました。どう取り扱いますか？{vbCrLf}{vbCrLf}" &
                $"・はい → 元ファイルへのリンクを用意する{vbCrLf}" &
                $"・いいえ → 専用フォルダにコピーして保存",
                "ファイルの扱い",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1)

                If result = DialogResult.Yes Then
                    finalPath = filePath
                    MyRow("FileType") = FileTypeEnum.Original.ToString()
                Else
                    finalPath = SaveFileWithAutoNumber(filePath)
                    MyRow("FileType") = FileTypeEnum.Local.ToString()
                End If

        End Select

        '-----------------------------------------------------------
        ' ★ DataRow に保存
        '-----------------------------------------------------------
        MyRow("FileReference") = finalPath
        parentForm.mainData.AcceptChanges()
        parentForm.Save()
        parentForm.RebuildShowCheckMenu()

        '===========================================================
        ' ★ 新規付箋のときだけタイトル変更
        '===========================================================
        If isNewMemo Then
            TitleText.Text = IO.Path.GetFileName(finalPath)
            Me.Text = TitleText.Text
            Me.Invalidate()
        End If

        '-----------------------------------------------------------
        ' ★ UI 更新
        '-----------------------------------------------------------
        AdjustFileLabel()
        fileLabel.Visible = True

        If clipLabel Is Nothing Then
            CreateClipIcon()
        End If

        ' FileType に応じてアイコン変更
        Dim ftype As FileTypeEnum
        If [Enum].TryParse(MyRow("FileType").ToString(), True, ftype) = False Then
            ftype = FileTypeEnum.None
        End If

        Select Case ftype
            Case FileTypeEnum.Local
                clipLabel.Text = "📂"
                memoTip.SetToolTip(clipLabel, "ファイルを開きます")
            Case FileTypeEnum.Original
                clipLabel.Text = "📄"
                memoTip.SetToolTip(clipLabel, "ファイルを開きます")
            Case FileTypeEnum.URL
                clipLabel.Text = "🌎"
                memoTip.SetToolTip(clipLabel, "リンク先を開きます")
            Case Else
                clipLabel.Text = ""
        End Select

        ' メニュー側のタイトルを更新
        parentForm.RefreshMemoTitleInMenu(MyRow("id").ToString(), TitleText.Text)

    End Sub


    '===========================================================
    ' ★ SaveFileWithAutoNumber：ファイルを連番付きで保存
    '   - file フォルダにコピー
    '   - 同名ファイルがある場合は _1, _2, ... を付ける
    '===========================================================
    Private Function SaveFileWithAutoNumber(filePath As String) As String

        Dim baseDir As String = Application.StartupPath
        Dim fileDir As String = IO.Path.Combine(baseDir, "file")

        If Not IO.Directory.Exists(fileDir) Then
            IO.Directory.CreateDirectory(fileDir)
        End If

        Dim fileName As String = IO.Path.GetFileName(filePath)
        Dim destPath As String = IO.Path.Combine(fileDir, fileName)

        Dim count As Integer = 1
        While IO.File.Exists(destPath)
            Dim nameOnly As String = IO.Path.GetFileNameWithoutExtension(fileName)
            Dim ext As String = IO.Path.GetExtension(fileName)
            destPath = IO.Path.Combine(fileDir, $"{nameOnly}_{count}{ext}")
            count += 1
        End While

        IO.File.Copy(filePath, destPath)

        Return destPath
    End Function



    '===========================================================
    ' ★ MemoText_TextChanged：行数に応じてスクロールバー表示
    '===========================================================
    Private Sub MemoText_TextChanged(sender As Object, e As EventArgs) Handles MemoText.TextChanged
        If MemoText.Lines.Length > 8 Then
            MemoText.ScrollBars = ScrollBars.Vertical
        Else
            MemoText.ScrollBars = ScrollBars.None
        End If
    End Sub


    '===========================================================
    ' ★ SaveRemindTime：通知日時を保存
    '===========================================================
    Private Sub SaveRemindTime(value As DateTime)
        MyRow("RemindTime") = value
        parentForm.mainData.AcceptChanges()
        parentForm.Save()
    End Sub



    '===========================================================
    ' ★ RemindButton_Click：通知日時の設定／解除
    '===========================================================
    Private Sub RemindButton_Click(sender As Object, e As EventArgs) Handles RemindButton.Click

        '-----------------------------------------------------------
        ' ★ 既に通知日が設定されている → 解除
        '-----------------------------------------------------------
        If remindLabel.Text <> "" Then
            MyRow("RemindTime") = DBNull.Value
            parentForm.mainData.AcceptChanges()
            parentForm.Save()

            remindLabel.Text = ""
            CheckReminder()
            Return
        End If

        '-----------------------------------------------------------
        ' ★ 通知日を新規設定する
        '-----------------------------------------------------------
        Dim dtp As New DateTimePicker With {
        .Format = DateTimePickerFormat.Short,
        .Left = RemindButton.Left + 26,
        .Top = RemindButton.Top,
        .Width = 100,
        .ShowUpDown = False
    }

        Me.Controls.Add(dtp)
        dtp.BringToFront()

        AddHandler dtp.CloseUp, Sub()
                                    SaveRemindTime(dtp.Value.Date)
                                    remindLabel.Text = "次の通知日時：" & dtp.Value.ToString("yyyy/MM/dd")
                                    Me.Controls.Remove(dtp)
                                    dtp.Dispose()
                                    CheckReminder()
                                    LoadRemindLabel()
                                End Sub

        dtp.Select()
        SendKeys.Send("%{DOWN}")   ' カレンダーを開く
    End Sub


    '===========================================================
    ' ★ CreateRemindLabel：通知ラベルを作成
    '===========================================================
    Private Sub CreateRemindLabel()


        remindLabel = New System.Windows.Forms.Label With {
        .AutoSize = True,
        .Left = RemindButton.Left + 22,
        .Top = RemindButton.Top,
        .Height = 20,
        .Font = New Font("Segoe UI", 8),
        .ForeColor = Me.TitleForeColor,
        .BackColor = Color.Transparent,
        .Text = ""
    }

        Me.Controls.Add(remindLabel)
    End Sub


    '===========================================================
    ' ★ LoadRemindLabel：通知ラベルの内容を DataRow から反映
    '===========================================================
    Public Sub LoadRemindLabel()
        Dim remindObj = MyRow("RemindTime")

        If Not IsDBNull(remindObj) Then
            Dim d As DateTime = CType(remindObj, DateTime)
            remindLabel.Text = "次の通知日時：" & d.ToString("yyyy/MM/dd")
            remindLabel.Visible = True
        Else
            remindLabel.Text = ""
            remindLabel.Visible = False
        End If

    End Sub

    ''===========================================================
    '' ★ CheckReminder：通知状態に応じて hideButton の色を変更（段階版）
    ''===========================================================
    Public Sub CheckReminder()

        Dim remindObj = MyRow("RemindTime")

        If IsDBNull(remindObj) Then
            hideButton.CircleColor = Color.White   ' 未設定
            hideButton.Invalidate()
            Return
        End If

        Dim remindTime As DateTime = CType(remindObj, DateTime)
        Dim daysDiff As Integer = (remindTime.Date - DateTime.Now.Date).Days

        ' daysDiff の意味：
        '   5以上   → まだまだ先
        '   1～4    → 5日前〜前日
        '   0       → 当日
        '   負数    → 期限超過

        Select Case daysDiff

            Case Is >= 5
                ' ★ リマインドまで5日以上（余裕あり）
                hideButton.CircleColor = Color.MediumAquamarine   ' ←後で好きな色に変えてOK

            Case 1 To 4
                ' ★ リマインド前日〜5日前（近づいてきた）
                hideButton.CircleColor = Color.Gold  ' ←後で好きな色に変えてOK

            Case 0
                ' ★ リマインド当日
                hideButton.CircleColor = Color.Red     ' ←後で好きな色に変えてOK

            Case Else
                ' ★ 期限超過（daysDiff < 0）
                hideButton.CircleColor = Color.Black

        End Select

        '===========================================================
        ' ★ MemoTip（あと◯日）を設定
        '===========================================================
        Dim tipText As String

        If daysDiff > 0 Then
            tipText = $"あと {daysDiff} 日"
        ElseIf daysDiff = 0 Then
            tipText = "今日が期限"
        Else
            tipText = $"期限超過（{Math.Abs(daysDiff)} 日）"
        End If

        memoTip.SetToolTip(hideButton, tipText)

        hideButton.Invalidate()

    End Sub

    '===========================================================
    ' ★ CheckReminder：残り日数で白→赤グラデーション
    '===========================================================
    'Public Sub CheckReminder()

    '    Dim remindObj = MyRow("RemindTime")

    '    If IsDBNull(remindObj) Then
    '        hideButton.CircleColor = Color.White   ' 未設定
    '        hideButton.Invalidate()
    '        Return
    '    End If

    '    Dim remindTime As DateTime = CType(remindObj, DateTime)
    '    Dim daysDiff As Integer = (remindTime.Date - DateTime.Now.Date).Days

    '    ' 20日前からカウントダウン開始
    '    ' 20日以上前 → ごく薄い赤
    '    ' 10〜19日前 → 少し赤
    '    ' 5〜9日前  → さらに赤
    '    ' 0〜4日前  → かなり赤
    '    ' 超過      → 真っ赤

    '    Select Case daysDiff

    '        Case Is >= 20
    '            hideButton.CircleColor = Color.MistyRose   ' ごく薄い赤

    '        Case 10 To 19
    '            hideButton.CircleColor = Color.LightCoral  ' 少し赤

    '        Case 5 To 9
    '            hideButton.CircleColor = Color.Salmon      ' さらに赤

    '        Case 0 To 4
    '            hideButton.CircleColor = Color.IndianRed   ' かなり赤

    '        Case Else
    '            hideButton.CircleColor = Color.Red         ' 超過

    '    End Select

    '    hideButton.Invalidate()
    'End Sub


    '===========================================================
    ' ★ MemoDelete_Click：メモ削除処理
    '===========================================================
    Private Sub MemoDelete_Click(sender As Object, e As EventArgs) Handles MemoDelete.Click

        Dim result = MessageBox.Show(
        "このメモ（" & TitleText.Text & "）を削除しますか？",
        "メモ削除確認",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning
    )

        If result <> DialogResult.Yes Then Return

        Dim colorNumber As Integer = CInt(MyRow("MemoColor"))

        MyRow.Delete()
        parentForm.mainData.AcceptChanges()
        parentForm.Save()

        parentForm.RebuildShowCheckMenu()
        parentForm.RecalculateColorMenuCheckState(colorNumber)

        Me.Close()
    End Sub


    '===========================================================
    ' ★ AdjustFileLabel：添付ファイル名を短縮して表示
    '===========================================================
    Private Sub AdjustFileLabel()

        Dim fullText As String = IO.Path.GetFileName(MyRow("FileReference").ToString())
        fileLabel.Text = fullText

        Dim textSize As Size = TextRenderer.MeasureText(fullText, fileLabel.Font)

        If textSize.Width > fileLabel.Width Then

            Dim shortText As String = fullText

            ' 収まるまで末尾を削る
            While TextRenderer.MeasureText(shortText & "…", fileLabel.Font).Width > fileLabel.Width
                shortText = shortText.Substring(0, shortText.Length - 1)
            End While

            fileLabel.Text = shortText & "…"
        End If

    End Sub


    '===========================================================
    ' ★ UrlButton_Click：URL 添付／削除処理
    '===========================================================
    Private Sub UrlButton_Click(sender As Object, e As EventArgs) Handles UrlButton.Click

        Dim currentUrl As String = MyRow("FileReference").ToString()

        '-----------------------------------------------------------
        ' ★ 既存 URL → 削除モード
        '-----------------------------------------------------------
        If Not String.IsNullOrWhiteSpace(currentUrl) Then

            Dim result = MessageBox.Show(
            "現在のリンクを削除しますか？" & vbCrLf & vbCrLf &
            currentUrl,
            "リンク削除確認",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                MyRow("FileReference") = ""
                MyRow("FileType") = FileTypeEnum.None.ToString()
                parentForm.mainData.AcceptChanges()
                parentForm.Save()

                fileLabel.Visible = False
                clipLabel.Text = ""
            End If

            Return
        End If

        '-----------------------------------------------------------
        ' ★ 新規 URL 入力モード
        '-----------------------------------------------------------
        Dim inputUrl As String = InputBox("URL を入力してください", "URL 添付")

        If String.IsNullOrWhiteSpace(inputUrl) Then Return

        ' URL バリデーション
        If Not IsValidUrl(inputUrl) Then
            MessageBox.Show(
            "URL の形式が正しくありません。" & vbCrLf &
            "http:// または https:// で始まる URL を入力してください。",
            "URL エラー",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning)
            Return
        End If

        ' URL 保存
        MyRow("FileReference") = inputUrl
        MyRow("FileType") = FileTypeEnum.URL.ToString()

        ' Web ページタイトル取得（失敗時は空）
        Dim pageTitle As String = GetUrlTitleSafe(inputUrl)

        If pageTitle <> "" Then
            MyRow("title") = pageTitle
            TitleText.Text = pageTitle
            Me.Text = pageTitle
            parentForm.mainData.AcceptChanges()
            parentForm.Save()
            Me.Invalidate()
        End If

        ' UI 更新
        fileLabel.Text = GetSmartUrlText(inputUrl)
        fileLabel.Visible = True

        clipLabel.Text = "🌎"
        memoTip.SetToolTip(clipLabel, "リンク先を開きます")

        TitleText.Width = 266
        UrlButton.Visible = True

    End Sub



    '===========================================================
    ' ★ IsValidUrl：URL の基本的な形式チェック
    '===========================================================
    Private Function IsValidUrl(url As String) As Boolean

        If String.IsNullOrWhiteSpace(url) Then Return False

        ' 正規表現で http:// または https:// をチェック
        Dim pattern As String = "^(https?://)"
        If Not System.Text.RegularExpressions.Regex.IsMatch(url, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase) Then
            Return False
        End If

        ' Uri.TryCreate で構文チェック
        Dim uri As Uri = Nothing
        If Not Uri.TryCreate(url, UriKind.Absolute, uri) Then
            Return False
        End If

        ' スキームが http/https 以外なら拒否
        If uri.Scheme <> Uri.UriSchemeHttp AndAlso uri.Scheme <> Uri.UriSchemeHttps Then
            Return False
        End If

        Return True
    End Function


    '===========================================================
    ' ★ GetSmartUrlText：URL を短縮して表示用に整形
    '===========================================================
    Private Function GetSmartUrlText(url As String) As String
        Try
            Dim uri As New Uri(url)

            Dim host As String = uri.Host
            Dim segments = uri.Segments
            Dim firstSegment As String = ""

            If segments.Length > 1 Then
                firstSegment = segments(1).Trim("/"c)
            End If

            If firstSegment = "" Then
                Return host
            Else
                Return $"{host} / {firstSegment} / …"
            End If

        Catch ex As Exception
            Return url
        End Try
    End Function


    '===========================================================
    ' ★ GetUrlTitleSafe：URL の <title> を安全に取得
    '   - 企業ネットワーク向けにプロキシ対応
    '   - 証明書エラーを無視（MITM 対策）
    '   - タイムアウト短め（固まらないように）
    '===========================================================
    Private Function GetUrlTitleSafe(url As String) As String

        Try
            Dim handler As New Net.Http.HttpClientHandler()

            handler.UseProxy = True
            handler.Proxy = Net.WebRequest.DefaultWebProxy
            handler.Proxy.Credentials = Net.CredentialCache.DefaultCredentials

            handler.ServerCertificateCustomValidationCallback =
                Function(sender, cert, chain, sslPolicyErrors) True

            Using client As New Net.Http.HttpClient(handler)
                client.Timeout = TimeSpan.FromSeconds(3)

                Dim html As String = client.GetStringAsync(url).Result

                Dim start As Integer = html.IndexOf("<title>", StringComparison.OrdinalIgnoreCase)
                Dim endPos As Integer = html.IndexOf("</title>", StringComparison.OrdinalIgnoreCase)

                If start >= 0 AndAlso endPos > start Then
                    Dim title As String = html.Substring(start + 7, endPos - (start + 7))
                    Return title.Trim()
                End If
            End Using

        Catch ex As Exception
            Debug.WriteLine("タイトル取得失敗: " & ex.Message)
        End Try

        Return ""
    End Function

    Public Sub RefreshTitle()
        TitleText.ForeColor = Me.TitleForeColor
        remindLabel.ForeColor = Me.TitleForeColor

        TitleText.Invalidate()
        remindLabel.Invalidate()

        Me.Invalidate()
    End Sub
    Public Sub UpdatePinLabel()

        Dim isTop As Boolean = CBool(MyRow("TopMostFlag"))

        Me.TopMost = isTop
        pinLabel.Text = If(isTop, "📌", "")

    End Sub

End Class


'===========================================================
' ★ CircleButton：丸ボタン（非表示ボタンの描画）
'===========================================================
Public Class CircleButton
    Inherits System.Windows.Forms.Button

    Public Property CircleColor As Color = Color.White
    Public Property CircleBorderColor As Color = Color.Black

    Protected Overrides Sub OnPaint(pevent As PaintEventArgs)
        MyBase.OnPaint(pevent)

        Dim g = pevent.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)

        ' 塗りつぶし
        Using b As New SolidBrush(CircleColor)
            g.FillEllipse(b, rect)
        End Using

        ' 黒縁（丸）
        Using p As New Pen(CircleBorderColor, 1)
            g.DrawEllipse(p, rect)
        End Using
    End Sub

End Class
