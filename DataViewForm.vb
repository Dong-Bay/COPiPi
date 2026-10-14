Imports System.Security.Cryptography
Imports System.Windows
Imports COPiPi_4._8.MemoForm

'===========================================================
' ★ COPiPi メイン管理フォーム
'   - メモ一覧（DataTable）を保持し、MemoForm を生成・管理する
'   - タスクトレイ常駐、メニュー表示、通知処理など全体の司令塔
'===========================================================
Public Class DataViewForm

    '===========================================================
    ' ★ 基本設定・フィールド
    '===========================================================

    ' メッセージボックスのタイトル
    Const msgtitle As String = "COPiPi"

    ' メモデータ XML のファイル名
    Const dataFileXML As String = "\Memo.xml"

    ' メモデータのフルパス
    Dim dataFilename As String = System.Environment.CurrentDirectory & dataFileXML

    ' 設定ファイル（ファイル添付モードなど）
    Private settingsFile As String = Application.StartupPath & "\AppSettings.xml"

    ' 全メモを保持する DataTable（COPiPi の中心データ）
    Public mainData As DataTable

    ' ファイル添付モード（LocalCopy / Original / URL）デフォルトでローカル設定。
    Private FileAttachMode As String = "LocalCopy"

    ' タスクトレイ左クリック／右クリックメニュー
    Private menuLeft As ContextMenuStrip
    Private menuRight As ContextMenuStrip

    ' 付箋並べ直し、移動用のフラグ
    Private pendingColorIndex As Integer = -1
    Private pendingMode As String = ""   ' "sort" or "move"

    ' ★ 付箋ウィンドウを ID で管理する辞書
    Private memoForms As New Dictionary(Of String, MemoForm)



    '===========================================================
    ' ★ Init：COPiPi 起動時の初期化処理
    '   - メニュー構築
    '   - 設定読み込み
    '   - メモデータ読み込み
    '   - VisibleFlag=True のメモだけ表示
    '   - 表示メニュー構築
    '   - リマインダー確認
    '===========================================================
    Public Sub Init()

        ' ★ 色テーマ読み込み（なければ自動生成）
        ColorThemeManager.LoadThemes()
        ' color.xml が壊れていたら初期テーマを復活
        If ColorThemeManager.Themes.Count = 0 Then
            ColorThemeManager.LoadThemes()
        End If

        ' メモデータ読み込み（XML → DataTable）
        mainData = readData()

        ' ★ MemoColor の整合性チェック
        FixMemoColorIndexes()

        ' タスクトレイメニュー構築
        BuildMenus()

        ' 表示／非表示メニュー構築
        BuildShowCheckMenu()

        ' 設定読み込み（ファイル添付モード）
        FileAttachMode = LoadSetting("FileAttachMode")
        If FileAttachMode = "" Then FileAttachMode = "LocalCopy"

        ' 添付ファイル保存フォルダの準備
        Dim baseDir As String = Application.StartupPath
        Dim fileDir As String = IO.Path.Combine(baseDir, "file")
        If Not IO.Directory.Exists(fileDir) Then IO.Directory.CreateDirectory(fileDir)


        ' タスクトレイアイコンを表示
        NotifyIcon1.Visible = True

        ' VisibleFlag=True のメモだけ生成して表示
        For Each row As DataRow In mainData.Rows
            If CBool(row("VisibleFlag")) Then
                MemoBuild(row)
            End If
        Next

        AddHandler Microsoft.Win32.SystemEvents.SessionSwitch, AddressOf OnSessionSwitch

        ' リマインダー確認（期限到達メモを通知）
        CheckAllReminders()
    End Sub

    '===========================================================
    ' ★ BuildMenus：左／右クリックメニューを完全再構築
    '   - 新規作成メニュー（色テーマ）
    '   - 表示／非表示メニュー（色テーマ）
    '   - 色ごと整列／色ごと移動（右メニュー）
    '   - 設定／終了などの固定メニュー
    '===========================================================
    Public Sub BuildMenus()

        '---------------------------------------------
        ' ★ 左クリックメニュー
        '---------------------------------------------
        menuLeft = New ContextMenuStrip()

        ' 新規作成メニュー（色テーマ）
        BuildColorMenu()

        ' 表示／非表示メニュー（後で中身を構築）
        Dim showMenu As New ToolStripMenuItem("表示／非表示")
        showMenu.Name = "ShowCheckMenu"
        menuLeft.Items.Add(showMenu)

        ' 一覧表示
        Dim SearchFormShow As New ToolStripMenuItem("検索")
        AddHandler SearchFormShow.Click,
        Sub()
            Dim f As New SearchForm(mainData, Me)
            f.Show()
        End Sub
        menuLeft.Items.Add(SearchFormShow)

        ' メニューを閉じる
        Dim hideMenu As New ToolStripMenuItem("メニューを閉じる")
        AddHandler hideMenu.Click, Sub() CloseAllMenus()
        menuLeft.Items.Add(hideMenu)
        '---------------------------------------------
        ' ★ 右クリックメニュー
        '---------------------------------------------
        menuRight = New ContextMenuStrip()

        '=============================================
        ' ★ 色ごと整列
        '=============================================
        Dim sortByColorMenu As New ToolStripMenuItem("色ごと整列")
        sortByColorMenu.Name = "SortByColorMenu"

        For Each theme In ColorThemeManager.Themes

            Dim colorIndex = theme.Index

            ' ★ mainData にこの色のメモが存在するか？
            Dim rows = mainData.Rows.Cast(Of DataRow)().
               Where(Function(r) CInt(r("MemoColor")) = colorIndex)

            If rows.Any() Then
                Dim item As New ToolStripMenuItem(theme.Name)
                item.Tag = colorIndex
                AddHandler item.Click, AddressOf SortColorMenu_Click
                sortByColorMenu.DropDownItems.Add(item)
            End If

        Next

        menuRight.Items.Add(sortByColorMenu)


        '=============================================
        ' ★ 色ごと移動
        '=============================================
        Dim moveByColorMenu As New ToolStripMenuItem("色ごと移動")
        moveByColorMenu.Name = "MoveByColorMenu"

        For Each theme In ColorThemeManager.Themes

            Dim colorIndex = theme.Index

            Dim rows = mainData.Rows.Cast(Of DataRow)().
               Where(Function(r) CInt(r("MemoColor")) = colorIndex)

            If rows.Any() Then
                Dim item As New ToolStripMenuItem(theme.Name)
                item.Tag = colorIndex
                AddHandler item.Click, AddressOf MoveColorMenu_Click
                moveByColorMenu.DropDownItems.Add(item)
            End If

        Next

        menuRight.Items.Add(moveByColorMenu)


        ' 全付箋整列
        Dim sortMenu = New ToolStripMenuItem("全付箋整列")
        AddHandler sortMenu.Click,
    Sub()
        pendingMode = "sortAll"
        ShowOverlayOnAllScreens()
    End Sub
        menuRight.Items.Add(sortMenu)

        ' 通知予定確認
        Dim remindCheckMenu = New ToolStripMenuItem("通知予定確認")
        AddHandler remindCheckMenu.Click, AddressOf 通知確認ToolStripMenuItem_Click
        menuRight.Items.Add(remindCheckMenu)

        ' 設定
        Dim settingsMenu = New ToolStripMenuItem("設定")
        AddHandler settingsMenu.Click, AddressOf 設定ToolStripMenuItem_Click
        menuRight.Items.Add(settingsMenu)

        ' 終了
        Dim exitMenu As New ToolStripMenuItem("終了")
        AddHandler exitMenu.Click, AddressOf 終了ToolStripMenuItem_Click
        menuRight.Items.Add(exitMenu)

        '---------------------------------------------
        ' ★ 表示／非表示メニューの中身を構築
        '---------------------------------------------
        BuildShowCheckMenu()

    End Sub



    '===========================================================
    ' ★ フォーム終了時：メモデータを XML に保存
    '===========================================================
    Private Sub DataViewForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        mainData.WriteXml(dataFilename)
    End Sub


    '===========================================================
    ' ★ DataGridView のヘッダー設定（管理画面用）
    '===========================================================
    Private Sub DataGridView1_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs) Handles DataGridView1.DataBindingComplete

        ' DataTable の Caption をヘッダーに反映
        For Each col As DataGridViewColumn In DataGridView1.Columns
            col.HeaderText = DataGridView1.DataSource.Columns(col.Index).Caption
        Next

        ' 列幅を固定（管理画面の見やすさ向上）
        For i As Integer = 0 To DataGridView1.Columns.Count - 1
            DataGridView1.Columns(i).Width = 80
        Next
    End Sub


    '===========================================================
    ' ★ readData：メモデータを XML から読み込む
    '===========================================================
    Private Function readData() As DataTable

        Dim dt As New DataTable("TableName")

        ' メモの全カラム定義（COPiPi のデータ構造）
        dt.Columns.Add("id", GetType(String))
        dt.Columns.Add("title", GetType(String))
        dt.Columns.Add("text", GetType(String))
        dt.Columns.Add("MemoColor", GetType(Int16))
        dt.Columns.Add("x", GetType(Int32))
        dt.Columns.Add("y", GetType(Int32))
        dt.Columns.Add("CollapsedFlag", GetType(Boolean))
        dt.Columns.Add("VisibleFlag", GetType(Boolean))
        dt.Columns.Add("TopMostFlag", GetType(Boolean))
        dt.Columns.Add("FileReference", GetType(String))
        dt.Columns.Add("RemindTime", GetType(DateTime))
        dt.Columns.Add("FileType", GetType(String))

        ' XML 読み込み
        Try
            dt.ReadXml(dataFilename)
        Catch ex As IO.FileNotFoundException
            MessageBox.Show("ファイル " & dataFilename & " が見つかりません。空データを生成します", msgtitle)
        End Try

        Return dt
    End Function


    '===========================================================
    ' ★ タスクトレイアイコンのクリック処理
    '   左クリック → 左メニュー
    '   右クリック → 右メニュー
    '===========================================================
    Private Sub NotifyIcon1_MouseDown(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDown

        CloseAllMenus()

        If e.Button = MouseButtons.Left Then
            menuLeft.Show(Cursor.Position)
        Else
            menuRight.Show(Cursor.Position)
        End If
    End Sub

    '===========================================================
    ' ★ 全メニューを閉じる（左／右）
    '===========================================================
    Public Sub CloseAllMenus()
        If menuLeft IsNot Nothing Then menuLeft.Hide()
        If menuRight IsNot Nothing Then menuRight.Hide()
    End Sub


    '===========================================================
    ' ★ タスクトレイアイコンのダブルクリック
    '   - メニューが開いていたら閉じるだけ
    '===========================================================
    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        CloseAllMenus()
    End Sub

    '===========================================================
    ' ★ 新規作成メニュー（色テーマから動的生成）
    '===========================================================
    Private Sub BuildColorMenu()

        Dim newMenu As New ToolStripMenuItem("新規作成")
        newMenu.Name = "ColorMenu"

        For Each theme In ColorThemeManager.Themes

            Dim item As New ToolStripMenuItem(theme.Name)
            item.Tag = theme.Index

            AddHandler item.Click,
            Sub(sender As Object, e As EventArgs)
                Dim idx As Integer = CInt(CType(sender, ToolStripMenuItem).Tag)
                CreateNewMemo(idx)
            End Sub

            newMenu.DropDownItems.Add(item)
        Next

        menuLeft.Items.Add(newMenu)
    End Sub

    '===========================================================
    ' ★ BuildShowCheckMenu：表示／非表示メニューを動的生成
    '   - テーマ XML の色一覧をもとに親メニュー（色名）を構築
    '   - 各色に属するメモを抽出し、個別メモの子メニューを生成
    '   - VisibleFlag に応じてチェック状態を反映
    '   - 色名・メモ名は DataTable と完全同期
    '===========================================================

    Public Sub BuildShowCheckMenu()

        Dim showMenu As ToolStripMenuItem =
        CType(menuLeft.Items("ShowCheckMenu"), ToolStripMenuItem)

        showMenu.DropDownItems.Clear()

        ' ★ テーマ XML の色一覧を使う
        For Each theme In ColorThemeManager.Themes

            Dim colorIndex = theme.Index
            Dim colorName = theme.Name

            ' ★ 該当色のメモを抽出（従来通り）
            Dim rows = mainData.Rows.Cast(Of DataRow)().
            Where(Function(r) CInt(r("MemoColor")) = colorIndex)

            If rows.Any() Then

                ' ★ 親メニュー（色名）
                Dim colorItem As New ToolStripMenuItem(colorName)
                colorItem.Tag = colorIndex

                ' ★ 色のメモが全部 VisibleFlag=True ならチェック
                colorItem.Checked = rows.All(Function(r) CBool(r("VisibleFlag")))

                AddHandler colorItem.Click, AddressOf ShowColorMenu_Click

                ' ★ 子メニュー（個別メモ）
                For Each r In rows
                    Dim memoItem As New ToolStripMenuItem(r("title").ToString())
                    memoItem.Tag = r("id").ToString()
                    memoItem.Checked = CBool(r("VisibleFlag"))
                    AddHandler memoItem.Click, AddressOf ShowSingleMemo_Click
                    colorItem.DropDownItems.Add(memoItem)
                Next

                showMenu.DropDownItems.Add(colorItem)
            End If
        Next

    End Sub


    '===========================================================
    ' ★ ApplyVisibilityToMemos：テーマの Visible 設定を既存メモに反映
    '   - OwnedForms（表示中の MemoForm）を走査
    '   - MemoColor から対応するテーマを取得
    '   - theme.Visible に応じて各 MemoForm の Visible を更新
    '   - 色テーマの「表示／非表示」設定と UI を同期させる
    '===========================================================
    Private Sub ApplyVisibilityToMemos()


        For Each f As MemoForm In Me.OwnedForms

            Dim idx As Integer = CInt(f.MyRow("MemoColor"))
            Dim theme = ColorThemeManager.GetThemeByIndex(idx)

            f.Visible = theme.Visible
        Next

    End Sub


    '===========================================================
    ' ★ CreateNewMemo：新規メモを DataTable に追加し、表示する
    '===========================================================
    Private Sub CreateNewMemo(colorNumber As Integer)

        ' 一意の ID（タイムスタンプ）
        Dim timeId As String = DateTime.Now.ToString("yyyyMMddHHmmssfff")

        Dim row As DataRow = mainData.NewRow()

        ' メモ初期値
        row("id") = timeId
        row("title") = "新規コピピ"
        row("text") = ""
        row("MemoColor") = colorNumber

        ' 重ならない位置を自動探索
        Dim safePos As Point = FindSafePosition()
        row("x") = safePos.X
        row("y") = safePos.Y

        row("CollapsedFlag") = False
        row("VisibleFlag") = True
        row("TopMostFlag") = False
        row("FileReference") = ""
        row("RemindTime") = DBNull.Value
        row("FileType") = "None"

        ' ファイル添付モードに応じて初期 FileType を設定
        Select Case FileAttachMode
            Case "LocalCopy" : row("FileType") = "Local"
            Case "UseOriginal" : row("FileType") = "Original"
            Case "UseURL" : row("FileType") = "URL"
        End Select

        ' DataTable に追加
        mainData.Rows.Add(row)

        ' XML 保存
        mainData.WriteXml(dataFilename)

        ' ★ 新仕様：DataRow を直接渡す
        Dim mf As MemoForm = MemoBuild(row)

        ' 表示メニュー更新
        BuildShowCheckMenu()
    End Sub


    '===========================================================
    ' ★ FindSafePosition（完全多画面対応版）
    '   - 全ディスプレイの結合領域を使って安全な位置を探索
    '   - 既存 MemoForm と重ならない位置を探す
    '   - 画面外に出ないよう minX/maxX/minY/maxY で補正
    '===========================================================
    Private Function FindSafePosition() As Point

        Dim startX As Integer = 200
        Dim startY As Integer = 100
        Dim offset As Integer = 22

        Dim x As Integer = startX
        Dim y As Integer = startY

        '===========================================================
        ' ★ 全ディスプレイの結合領域を取得
        '===========================================================
        Dim screens = Screen.AllScreens

        Dim minX As Integer = screens.Min(Function(s) s.WorkingArea.Left)
        Dim maxX As Integer = screens.Max(Function(s) s.WorkingArea.Right)
        Dim minY As Integer = screens.Min(Function(s) s.WorkingArea.Top)
        Dim maxY As Integer = screens.Max(Function(s) s.WorkingArea.Bottom)

        '===========================================================
        ' ★ メモサイズ（幅・高さ）取得
        '===========================================================
        Dim p = GetMemoSizeProfile()
        Dim memoW As Integer = p.Width
        Dim memoH As Integer = p.OriginalHeight

        While True

            '===========================================================
            ' ★ 画面外に出ないよう補正
            '===========================================================
            If x < minX Then x = minX + 10
            If x + memoW > maxX Then x = maxX - memoW - 10

            If y < minY Then y = minY + 10
            If y + memoH > maxY Then
                ' 下端に来たら X を右へ、Y を初期化
                x += memoW + 20
                y = startY
            End If

            '===========================================================
            ' ★ 既存メモと重なるかチェック
            '===========================================================
            Dim overlap = memoForms.Values.Any(Function(mf)
                                                   Dim rect1 As New Rectangle(x, y, memoW, memoH)
                                                   Dim rect2 As New Rectangle(mf.Left, mf.Top, mf.Width, mf.Height)
                                                   Return rect1.IntersectsWith(rect2)
                                               End Function)

            '===========================================================
            ' ★ 重ならなければこの位置を採用
            '===========================================================
            If Not overlap Then
                Return New Point(x, y)
            End If

            '===========================================================
            ' ★ 重なっていたら Y をずらす
            '===========================================================
            y += offset

        End While

    End Function


    '===========================================================
    ' ★ MemoForm 生成（MyRow を渡す）
    '===========================================================
    Public Function MemoBuild(row As DataRow) As MemoForm

        Dim memoId As String = row("id").ToString()

        ' ★ 先に生成
        Dim mf As New MemoForm(row, Me)
        mf.TopMost = CBool(row("TopMostFlag"))

        ' ★ 先に辞書登録（これが重要）
        memoForms(memoId) = mf

        AddHandler mf.FormClosed,
        Sub()
            RemoveMemoForm(memoId)
        End Sub

        ' ★ 最後に表示
        mf.Show()

        Return mf
    End Function



    '===========================================================
    ' ★ OpenMemo：辞書方式で付箋を開く（重複生成しない）
    '===========================================================
    Public Function OpenMemo(memoId As String) As MemoForm

        ' 既に開いている？
        If memoForms.ContainsKey(memoId) Then
            Dim mf = memoForms(memoId)
            mf.RefreshUI()
            mf.Show()
            mf.BringToFront()
            mf.Activate()
            Return mf
        End If

        ' 新規生成
        Dim row = mainData.Rows.Cast(Of DataRow)().
        First(Function(r) r("id").ToString() = memoId)

        Dim newForm As MemoForm = MemoBuild(row)
        Return newForm

    End Function




    '===========================================================
    ' ★ タイトル変更時にメニュー更新
    '===========================================================
    Public Sub RefreshMemoTitleInMenu(memoId As String, newTitle As String)

        Dim showMenu As ToolStripMenuItem =
        CType(menuLeft.Items("ShowCheckMenu"), ToolStripMenuItem)

        If showMenu Is Nothing Then Exit Sub

        For Each colorItem As ToolStripMenuItem In showMenu.DropDownItems
            For Each memoItem As ToolStripMenuItem In colorItem.DropDownItems
                If memoItem.Tag.ToString() = memoId Then
                    memoItem.Text = newTitle
                    Exit Sub
                End If
            Next
        Next
    End Sub

    '===========================================================
    ' ★ 色メニュークリック（色のメモを一括表示／非表示）
    '   - VisibleFlag を一括更新
    '   - 辞書方式で既存フォームを再利用
    '   - 無い場合は新規生成して辞書に登録
    '===========================================================
    '===========================================================
    ' ★ 色メニュークリック（辞書方式対応）
    '===========================================================
    Private Sub ShowColorMenu_Click(sender As Object, e As EventArgs)

        Dim item = CType(sender, ToolStripMenuItem)
        Dim colorNumber = CInt(item.Tag)

        ' チェック状態を反転
        item.Checked = Not item.Checked

        ' VisibleFlag を一括更新
        For Each r In mainData.Rows.Cast(Of DataRow)().
        Where(Function(row) CInt(row("MemoColor")) = colorNumber)

            r("VisibleFlag") = item.Checked
        Next

        ' 辞書方式でフォーム更新
        For Each row In mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) CInt(r("MemoColor")) = colorNumber)

            Dim memoId As String = row("id").ToString()

            If memoForms.ContainsKey(memoId) Then

                Dim mf = memoForms(memoId)

                If item.Checked Then
                    mf.RefreshUI()
                    mf.Visible = True
                Else
                    mf.Close()
                End If

            ElseIf item.Checked Then
                MemoBuild(row)
            End If
        Next

        ' 子メニューのチェック状態を同期
        For Each memoItem As ToolStripMenuItem In item.DropDownItems
            memoItem.Checked = item.Checked
        Next

        Save()
        CloseAllMenus()

    End Sub



    '===========================================================
    ' ★ 色ごとの整列（色のメモを一括で整列）
    '===========================================================
    Private Sub SortColorMenu_Click(sender As Object, e As EventArgs)
        pendingColorIndex = CInt(CType(sender, ToolStripMenuItem).Tag)
        pendingMode = "sort"

        ShowOverlayOnAllScreens()
    End Sub


    '===========================================================
    ' ★ 色ごとの移動（色のメモを一括で移動）
    '===========================================================

    Private Sub MoveColorMenu_Click(sender As Object, e As EventArgs)
        pendingColorIndex = CInt(CType(sender, ToolStripMenuItem).Tag)
        pendingMode = "move"

        ShowOverlayOnAllScreens()
    End Sub

    Private Sub ShowOverlayOnAllScreens()

        Dim screens = Screen.AllScreens

        For Each s In screens
            Dim ov As New OverlayForm(s.Bounds)
            ov.Callback = AddressOf OverlayClickHandler
            ov.Show()
        Next

    End Sub



    '===========================================================
    ' ★ OverlayClickHandler：オーバーレイクリック後の処理振り分け
    '   - pendingMode に応じて「色ごと整列」または「色ごと移動」を実行
    '   - OverlayForm から渡されたクリック座標を起点に処理
    '   - 処理完了後は pendingMode / pendingColorIndex を必ずリセット
    '===========================================================
    Private Sub OverlayClickHandler(pt As Point)

        If pendingMode = "sort" Then
            SortMemosByColor(pendingColorIndex, pt)

        ElseIf pendingMode = "move" Then
            MoveMemosByColor(pendingColorIndex, pt)

        ElseIf pendingMode = "sortAll" Then
            SortAllMemos(pt)
        End If

        pendingMode = ""
        pendingColorIndex = -1
        Me.Focus()
    End Sub


    '===========================================================
    ' ★ SortMemosByColor：色ごとに整列
    '   - カーソル位置を起点に縦方向に並べる
    '===========================================================
    Public Sub SortMemosByColor(colorIndex As Integer, startPoint As Point)

        Dim currentY As Integer = startPoint.Y

        '===========================================================
        ' ★ 全ディスプレイの結合領域を取得
        '===========================================================
        Dim allScreens = Screen.AllScreens

        Dim minX As Integer = allScreens.Min(Function(s) s.WorkingArea.Left)
        Dim maxX As Integer = allScreens.Max(Function(s) s.WorkingArea.Right)
        Dim minY As Integer = allScreens.Min(Function(s) s.WorkingArea.Top)
        Dim maxY As Integer = allScreens.Max(Function(s) s.WorkingArea.Bottom)

        '===========================================================
        ' ★ 対象色かつ VisibleFlag=True のメモを抽出
        '===========================================================
        Dim rows = mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) CInt(r("MemoColor")) = colorIndex AndAlso CBool(r("VisibleFlag")))

        ' 並べる順番：TopMost → RemindTime → id
        Dim sorted = rows.
        OrderByDescending(Function(r) CBool(r("TopMostFlag"))).
        ThenBy(Function(r) GetRemindTime(r)).
        ThenBy(Function(r) r("id").ToString())

        '===========================================================
        ' ★ 縦方向に整列（画面外に出ないよう補正）
        '===========================================================
        For Each row In sorted

            Dim memoId As String = row("id").ToString()
            If memoForms.ContainsKey(memoId) Then
                Dim mf As MemoForm = memoForms(memoId)
                mf.RefreshUI()
                Dim offsetY As Integer = mf.Height + 6
                ' 新しい位置
                Dim newX As Integer = startPoint.X
                Dim newY As Integer = currentY

                ' 画面外に出ないよう補正
                If newX < minX Then newX = minX + 10
                If newX > maxX - mf.Width Then newX = maxX - mf.Width

                If newY < minY Then newY = minY + 10
                If newY > maxY - mf.Height Then newY = maxY - mf.Height

                ' UI 位置変更
                mf.Location = New Point(newX, newY)

                ' DataRow 位置更新
                row("x") = newX
                row("y") = newY

                currentY += offsetY
            End If
        Next

        mainData.AcceptChanges()
        Save()
    End Sub

    Public Sub SortAllMemos(startPoint As Point)

        Dim currentY As Integer = startPoint.Y

        ' 全ディスプレイ結合領域
        Dim allScreens = Screen.AllScreens
        Dim minX As Integer = allScreens.Min(Function(s) s.WorkingArea.Left)
        Dim maxX As Integer = allScreens.Max(Function(s) s.WorkingArea.Right)
        Dim minY As Integer = allScreens.Min(Function(s) s.WorkingArea.Top)
        Dim maxY As Integer = allScreens.Max(Function(s) s.WorkingArea.Bottom)

        ' VisibleFlag=True の全メモ
        Dim rows = mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) CBool(r("VisibleFlag")))

        ' 並べる順番：TopMost → RemindTime → 色 → id
        Dim sorted = rows.
        OrderByDescending(Function(r) CBool(r("TopMostFlag"))).
        ThenBy(Function(r) GetRemindTime(r)).
        ThenBy(Function(r) CInt(r("MemoColor"))).
        ThenBy(Function(r) r("id").ToString())

        For Each row In sorted

            Dim memoId As String = row("id").ToString()

            If memoForms.ContainsKey(memoId) Then

                Dim mf As MemoForm = memoForms(memoId)
                mf.RefreshUI()
                Dim offsetY As Integer = mf.Height + 6

                Dim newX As Integer = startPoint.X
                Dim newY As Integer = currentY

                ' 画面外補正
                If newX < minX Then newX = minX + 10
                If newX > maxX - mf.Width Then newX = maxX - mf.Width

                If newY < minY Then newY = minY + 10
                If newY > maxY - mf.Height Then newY = maxY - mf.Height

                mf.Location = New Point(newX, newY)

                row("x") = newX
                row("y") = newY

                currentY += offsetY
            End If
        Next

        mainData.AcceptChanges()
        Save()
    End Sub



    '===========================================================
    ' ★ GetRemindTime：RemindTime の比較用値を取得
    '   - RemindTime が未設定（DBNull）の場合は DateTime.MaxValue を返す
    '   - 並べ替え処理（SortMemosByColor）で「期限なし」を最後に回すための補助関数
    '   - 設定されている場合は実際の RemindTime を返す
    '===========================================================
    Private Function GetRemindTime(row As DataRow) As DateTime

        If IsDBNull(row("RemindTime")) Then
            Return DateTime.MaxValue
        Else
            Return CType(row("RemindTime"), DateTime)
        End If
    End Function


    '===========================================================
    ' ★ MoveMemosByColor：色ごとにまとめて移動
    '   - 既存の並びを保ったまま全体を移動
    '===========================================================
    Public Sub MoveMemosByColor(colorIndex As Integer, startPoint As Point)

        ' 対象色かつ VisibleFlag=True のメモを抽出
        Dim rows = mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) CInt(r("MemoColor")) = colorIndex AndAlso CBool(r("VisibleFlag")))

        If Not rows.Any() Then Exit Sub

        ' 全ディスプレイの結合領域
        Dim allScreens = Screen.AllScreens
        Dim minX As Integer = allScreens.Min(Function(s) s.WorkingArea.Left)
        Dim maxX As Integer = allScreens.Max(Function(s) s.WorkingArea.Right)
        Dim minY As Integer = allScreens.Min(Function(s) s.WorkingArea.Top)
        Dim maxY As Integer = allScreens.Max(Function(s) s.WorkingArea.Bottom)

        ' 現在位置
        Dim offsetY As Integer = 0

        For Each row In rows

            Dim memoId As String = row("id").ToString()

            If memoForms.ContainsKey(memoId) Then

                Dim mf As MemoForm = memoForms(memoId)
                mf.RefreshUI()
                Dim newX As Integer = startPoint.X
                Dim newY As Integer = startPoint.Y + offsetY

                ' 画面外補正
                If newX < minX Then newX = minX + 10
                If newX > maxX - mf.Width Then newX = maxX - mf.Width

                If newY < minY Then newY = minY + 10
                If newY > maxY - mf.Height Then newY = maxY - mf.Height

                mf.Location = New Point(newX, newY)

                row("x") = newX
                row("y") = newY

                offsetY += mf.Height + 6
            End If
        Next

        mainData.AcceptChanges()
        Save()

    End Sub


    '===========================================================
    ' ★ 個別メモ表示切替（辞書方式対応）
    '   - VisibleFlag を更新
    '   - 辞書方式で既存フォームを再利用
    '   - 無い場合は新規生成して辞書に登録
    '   - 親メニュー（色）のチェック状態を再計算
    '===========================================================
    Private Sub ShowSingleMemo_Click(sender As Object, e As EventArgs)

        Dim item = CType(sender, ToolStripMenuItem)
        Dim memoId = item.Tag.ToString()

        ' チェック状態を反転
        item.Checked = Not item.Checked

        ' DataRow 更新
        Dim row = mainData.Rows.Cast(Of DataRow)().
        First(Function(r) r("id").ToString() = memoId)

        row("VisibleFlag") = item.Checked

        ' 辞書方式でフォーム更新
        If memoForms.ContainsKey(memoId) Then

            Dim mf = memoForms(memoId)

            If item.Checked Then
                mf.Visible = True
            Else
                mf.Close()
            End If

        ElseIf item.Checked Then
            MemoBuild(row)
        End If

        ' 親メニュー（色）のチェック状態を再計算
        Dim parentItem = CType(item.OwnerItem, ToolStripMenuItem)
        Dim colorNumber = CInt(parentItem.Tag)

        Dim rows = mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) CInt(r("MemoColor")) = colorNumber)

        parentItem.Checked = rows.All(Function(r) CBool(r("VisibleFlag")))

        Save()
        CloseAllMenus()

    End Sub




    '===========================================================
    ' ★ 表示メニュー再構築（外部から呼び出し）
    '===========================================================
    Public Sub RebuildShowCheckMenu()
        BuildShowCheckMenu()
    End Sub



    '===========================================================
    ' ★ 表示メニュー再構築（新規カラー作成時。これも含めるように）
    '===========================================================
    Public Sub RebuildColorMenu()

        Dim colorMenu As ToolStripMenuItem =
        CType(menuLeft.Items("ColorMenu"), ToolStripMenuItem)

        If colorMenu Is Nothing Then Exit Sub

        colorMenu.DropDownItems.Clear()

        For Each theme In ColorThemeManager.Themes
            Dim item As New ToolStripMenuItem(theme.Name)
            item.Tag = theme.Index

            AddHandler item.Click,
            Sub(sender As Object, e As EventArgs)
                Dim idx As Integer = CInt(CType(sender, ToolStripMenuItem).Tag)
                CreateNewMemo(idx)
            End Sub

            colorMenu.DropDownItems.Add(item)
        Next

    End Sub


    '===========================================================
    ' ★ 表示色再構築（色が変わった時。全色書き換え）
    '===========================================================
    Public Sub RefreshAllMemoColors()

        For Each f As Form In Application.OpenForms
            If TypeOf f Is MemoForm Then
                Dim mf = CType(f, MemoForm)
                Dim idx As Integer = CInt(mf.MyRow("MemoColor"))
                Dim theme = ColorThemeManager.GetThemeByIndex(idx)

                If theme IsNot Nothing Then
                    ' ★ 背景色
                    mf.BackColor = Color.FromArgb(theme.ColorValue)

                    ' ★ フォント色（タイトルバー）
                    mf.TitleForeColor = Color.FromArgb(theme.ForeColor)

                    ' ★ ClipLabel / fileLabel / pinLabel
                    mf.clipLabel.ForeColor = Color.FromArgb(theme.ForeColor)
                    mf.fileLabel.ForeColor = Color.FromArgb(theme.ForeColor)
                    mf.pinLabel.ForeColor = Color.FromArgb(theme.ForeColor)
                    mf.hideButton.CircleBorderColor = Color.FromArgb(theme.ForeColor)
                    mf.hideButton.Invalidate()

                    ' ★ タイトルバーの再描画（必須）
                    mf.RefreshTitle()
                End If
            End If
        Next

    End Sub


    '===========================================================
    ' ★ MemoColor の整合性チェック
    '   - color.xml が壊れた／消えた場合に備えて
    '   - 存在しない Index を使っている付箋は Gray(5) に退避
    '===========================================================
    Public Sub FixMemoColorIndexes()

        If mainData Is Nothing Then Exit Sub

        ' 有効なテーマの Index を取得
        Dim validIndexes As New List(Of Integer)(
        ColorThemeManager.Themes.Select(Function(t) t.Index)
    )

        ' 付箋データの MemoColor をチェック
        For Each row As DataRow In mainData.Rows

            If row.RowState = DataRowState.Deleted Then Continue For

            Dim idx As Integer = CInt(row("MemoColor"))

            ' 存在しない Index の場合は Gray(5) に退避
            If Not validIndexes.Contains(idx) Then
                row("MemoColor") = 5
            End If

        Next

        mainData.AcceptChanges()
        Save()

    End Sub


    '===========================================================
    ' ★ 色メニューのチェック状態再計算
    '   - 色のメモが全て VisibleFlag=True ならチェック
    '===========================================================
    Public Sub RecalculateColorMenuCheckState(colorNumber As Integer)

        Dim rows = mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) CInt(r("MemoColor")) = colorNumber)

        Dim allVisible = rows.All(Function(r) CBool(r("VisibleFlag")))

        Dim showMenu As ToolStripMenuItem =
        CType(menuLeft.Items("ShowCheckMenu"), ToolStripMenuItem)

        If showMenu Is Nothing Then Exit Sub

        ' 対象色のメニューだけ更新
        For Each item As ToolStripMenuItem In showMenu.DropDownItems
            If CInt(item.Tag) = colorNumber Then
                item.Checked = allVisible
                Exit For
            End If
        Next

    End Sub

    Private Sub OnSessionSwitch(sender As Object, e As Microsoft.Win32.SessionSwitchEventArgs)

        If e.Reason = Microsoft.Win32.SessionSwitchReason.SessionUnlock Then
            ' ★ ログイン完了した瞬間にリマインド再チェック
            CheckAllReminders()
        End If

    End Sub


    '===========================================================
    ' ★ CheckAllReminders：リマインダー（通知）チェック（辞書方式対応）
    '===========================================================
    Public Sub CheckAllReminders()

        '-----------------------------------------------------------
        ' ★ 表示中の MemoForm の色更新（辞書方式）
        '-----------------------------------------------------------
        For Each kv In memoForms
            Dim mf As MemoForm = kv.Value
            mf.CheckReminder()
        Next

        '-----------------------------------------------------------
        ' ★ 非表示メモの期限チェック（まとめて通知）
        '-----------------------------------------------------------
        Dim notifyList As New List(Of String)

        For Each row As DataRow In mainData.Rows

            If IsDBNull(row("RemindTime")) Then Continue For

            Dim remindTime As DateTime = CType(row("RemindTime"), DateTime)
            If DateTime.Now < remindTime Then Continue For

            '---------------------------------------------
            ' ★ 日数計算
            '---------------------------------------------
            Dim daysDiff As Integer = (remindTime.Date - DateTime.Now.Date).Days
            Dim status As String =
            If(daysDiff = 0, "今日", $"超過（{Math.Abs(daysDiff)} 日）")

            notifyList.Add($"{row("title")}（{status}）")

            '---------------------------------------------
            ' ★ メモを表示状態にする
            '---------------------------------------------
            row("VisibleFlag") = True
            row("TopMostFlag") = True
            mainData.AcceptChanges()

            Dim memoId As String = row("id").ToString()
            Dim targetForm As MemoForm = Nothing

            '---------------------------------------------
            ' ★ 辞書方式で既存フォームを探す
            '---------------------------------------------
            If memoForms.ContainsKey(memoId) Then
                targetForm = memoForms(memoId)
            End If

            If targetForm Is Nothing Then
                targetForm = MemoBuild(row)
            End If

            '---------------------------------------------
            ' ★ リマインダー状態を再チェック
            '---------------------------------------------
            If targetForm IsNot Nothing Then
                targetForm.CheckReminder()
                targetForm.TopMost = True
                targetForm.pinLabel.Text = "📌"
            End If

        Next

        '-----------------------------------------------------------
        ' ★ 表示メニュー更新
        '-----------------------------------------------------------
        BuildShowCheckMenu()

        '-----------------------------------------------------------
        ' ★ まとめて通知
        '-----------------------------------------------------------
        If notifyList.Count > 0 Then
            NotifyIcon1.BalloonTipTitle = $"期限超過通知（{notifyList.Count} 件）"
            NotifyIcon1.BalloonTipText = String.Join(vbCrLf, notifyList)
            NotifyIcon1.ShowBalloonTip(4000)
        End If

    End Sub

    '===========================================================
    ' ★ 終了メニュー：データ保存してアプリ終了
    '===========================================================
    Private Sub 終了ToolStripMenuItem_Click(sender As Object, e As EventArgs)
        Save()
        Application.Exit()
    End Sub


    '===========================================================
    ' ★ 並べ直し：全メモを画面左上から整列させる（辞書方式対応）
    '===========================================================
    Private Sub 並べ直しToolStripMenuItem_Click(sender As Object, e As EventArgs)

        Dim result = MessageBox.Show(
        "保存されていた表示位置はクリアされます。全ての付箋を並べ直しますか？",
        "ソート確認",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning
    )
        If result <> DialogResult.Yes Then Return

        '-----------------------------------------------------------
        ' ★ 全メモを強制表示＋折りたたみ
        '-----------------------------------------------------------
        For Each row As DataRow In mainData.Rows
            row("VisibleFlag") = True
            row("CollapsedFlag") = True
        Next
        mainData.AcceptChanges()
        BuildShowCheckMenu()

        '-----------------------------------------------------------
        ' ★ 全ディスプレイの結合領域を取得
        '-----------------------------------------------------------
        Dim allScreens = Screen.AllScreens

        Dim minX As Integer = allScreens.Min(Function(s) s.WorkingArea.Left)
        Dim maxX As Integer = allScreens.Max(Function(s) s.WorkingArea.Right)
        Dim minY As Integer = allScreens.Min(Function(s) s.WorkingArea.Top)
        Dim maxY As Integer = allScreens.Max(Function(s) s.WorkingArea.Bottom)

        Dim posX As Integer = minX + 50
        Dim posY As Integer = minY + 50
        Dim offset As Integer = 18

        '-----------------------------------------------------------
        ' ★ 辞書方式：表示中の MemoForm を並べ直す
        '-----------------------------------------------------------
        For Each kv In memoForms
            Dim mf As MemoForm = kv.Value

            Dim memoHeight As Integer =
            If(CBool(mf.MyRow("CollapsedFlag")),
               mf.TitleHeight,
               mf.OriginalHeight)

            If posY + memoHeight > maxY - 50 Then
                posX += 305
                posY = minY + 50
            End If

            mf.Left = posX
            mf.Top = posY

            mf.MyRow("x") = posX
            mf.MyRow("y") = posY

            posY += memoHeight + 4
        Next

        '-----------------------------------------------------------
        ' ★ 非表示だったメモを生成して並べ直す（辞書方式）
        '-----------------------------------------------------------
        For Each row As DataRow In mainData.Rows

            Dim memoId As String = row("id").ToString()

            If Not memoForms.ContainsKey(memoId) Then

                Dim mf As MemoForm = MemoBuild(row)

                Dim memoHeight As Integer =
                If(CBool(row("CollapsedFlag")),
                   mf.TitleHeight,
                   mf.OriginalHeight)

                If posY + memoHeight > maxY - 50 Then
                    posX += 305
                    posY = minY + 50
                End If

                mf.Left = posX
                mf.Top = posY

                row("x") = posX
                row("y") = posY

                posY += memoHeight + 4
            End If
        Next

        mainData.AcceptChanges()
        Save()

    End Sub



    '===========================================================
    ' ★ 通知確認：通知予定のメモを 1 件ずつ表示
    '===========================================================
    Private Sub 通知確認ToolStripMenuItem_Click(sender As Object, e As EventArgs)

        ' RemindTime が設定されている行を抽出
        Dim remindRows = mainData.Rows.Cast(Of DataRow)().
        Where(Function(r) Not IsDBNull(r("RemindTime"))).
        OrderBy(Function(r) CType(r("RemindTime"), DateTime)).
        ToList()

        If remindRows.Count = 0 Then
            MessageBox.Show("現在、通知が設定されているメモはありません。", "通知確認")
            Return
        End If

        Dim total As Integer = remindRows.Count

        ' 1 件ずつ表示
        For i As Integer = 0 To total - 1

            Dim row = remindRows(i)
            Dim title As String = row("title").ToString()
            Dim remindTime As DateTime = CType(row("RemindTime"), DateTime)
            Dim visible As Boolean = CBool(row("VisibleFlag"))

            Dim daysDiff As Integer = (remindTime.Date - DateTime.Now.Date).Days
            Dim status As String

            If daysDiff > 0 Then
                status = $"あと {daysDiff} 日"
            ElseIf daysDiff = 0 Then
                status = "今日"
            Else
                status = $"超過（{Math.Abs(daysDiff)} 日）"
            End If

            Dim msg As String =
            $"タイトル：{title}{vbCrLf}" &
            $"通知日：{remindTime:yyyy/MM/dd}：{status}{vbCrLf}" &
            $"現在の状態：{If(visible, "表示中", "非表示")}"

            MessageBox.Show(msg, $"通知予定全件表示（{i + 1} / {total} 件）")
        Next

        CloseAllMenus()
    End Sub


    '===========================================================
    ' ★ LoadSetting：設定ファイルから値を読み込む
    '===========================================================
    Public Function LoadSetting(key As String) As String
        If Not IO.File.Exists(settingsFile) Then Return ""

        Dim doc As XDocument = XDocument.Load(settingsFile)
        Dim elem = doc.Root.Element(key)
        If elem Is Nothing Then Return ""
        Return elem.Value

    End Function


    '===========================================================
    ' ★ SaveSetting：設定ファイルに値を書き込む
    '===========================================================
    Public Sub SaveSetting(key As String, value As String)

        Dim doc As XDocument

        If IO.File.Exists(settingsFile) Then
            doc = XDocument.Load(settingsFile)
        Else
            doc = New XDocument(New XElement("Settings"))
        End If

        Dim elem = doc.Root.Element(key)
        If elem Is Nothing Then
            doc.Root.Add(New XElement(key, value))
        Else
            elem.Value = value
        End If

        doc.Save(settingsFile)
    End Sub


    '===========================================================
    ' ★ 設定画面を開く
    '===========================================================
    Private Sub 設定ToolStripMenuItem_Click(sender As Object, e As EventArgs)
        Dim sf As New SettingsForm(Me)
        sf.ShowDialog()
        CloseAllMenus()
    End Sub


    '===========================================================
    ' ★ メモデータを XML に保存
    '===========================================================
    Public Sub Save()
        mainData.WriteXml(dataFilename)
    End Sub


    '===========================================================
    ' ★ ファイル添付モードの設定
    '===========================================================
    Public Sub SetFileAttachMode(mode As String)
        FileAttachMode = mode
    End Sub

    Public Function GetFileAttachMode() As String
        Return FileAttachMode
    End Function

    '===========================================================
    ' ★ 付箋サイズの設定
    '===========================================================


    Public Sub SetMemoSizeMode(mode As String)
        SaveSetting("MemoSizeMode", mode)
        RedrawAllMemos()
    End Sub

    Public Function GetMemoSizeMode() As String
        Dim v As String = LoadSetting("MemoSizeMode")
        If String.IsNullOrWhiteSpace(v) Then
            Return "Default"
        End If
        Return v
    End Function


    '===========================================================
    ' ★ サイズ設定を読みだしてまとめてMemoFormに渡す。
    '===========================================================
    Public Function GetMemoSizeProfile() As MemoSizeProfile

        Dim mode As String = LoadSetting("MemoSizeMode")
        If String.IsNullOrWhiteSpace(mode) Then mode = "Default"

        Dim p As New MemoSizeProfile()

        Select Case mode
            Case "Large"
                p.Width = 340
                p.TitleHeight = 21
                p.OriginalHeight = 260
                p.TitleFont = New Font("Segoe UI", 11, FontStyle.Bold)
                p.MemoFont = New Font("Segoe UI", 11)

                ' ★ Large
                p.IconSize = 18
                p.IconFontSize = 10
                p.IconRightMargin = 4
                p.IconSpacing = 2

                p.HideButtonLeft = 2
                p.HideButtonTop = 1

                p.TitleTextBoxLeft = 4
                p.TitleTextBoxTop = 22
                p.TitleTextBoxWidth = 332
                p.MemoTextBoxHeight = 175

                p.RemindLabelOffsetX = -26
                p.RemindLabelOffsetY = 5
                p.RemindLabelFontSize = 10

                p.FileLabelOffsetX = -88
                p.FileLabelOffsetY = 5
                p.FileLabelWidth = 90
                p.FileLabelFontSize = 10

                p.ActionButtonSize = 26
                p.ActionButtonEmojiFontSize = 10

                p.UrlButtonLeft = 310
                p.UrlButtonTop = 23
                p.RemindButtonLeft = 4
                p.RemindButtonTop = 230
                p.DeleteButtonLeft = 310
                p.DeleteButtonTop = 230

            Case "XLarge"
                p.Width = 380
                p.TitleHeight = 24
                p.OriginalHeight = 300
                p.TitleFont = New Font("Segoe UI", 13, FontStyle.Bold)
                p.MemoFont = New Font("Segoe UI", 13)

                ' ★ Default の “現在の COPiPi のデフォルト値”
                p.IconSize = 20
                p.IconFontSize = 12
                p.IconRightMargin = 4
                p.IconSpacing = 2

                p.HideButtonLeft = 2
                p.HideButtonTop = 1

                p.TitleTextBoxLeft = 4
                p.TitleTextBoxTop = 26
                p.TitleTextBoxWidth = 372
                p.MemoTextBoxHeight = 204

                p.RemindLabelOffsetX = -30
                p.RemindLabelOffsetY = 5
                p.RemindLabelFontSize = 12

                p.FileLabelOffsetX = -90
                p.FileLabelOffsetY = 4
                p.FileLabelWidth = 90
                p.FileLabelFontSize = 12

                p.ActionButtonSize = 30
                p.ActionButtonEmojiFontSize = 12

                p.UrlButtonLeft = 345
                p.UrlButtonTop = 26
                p.RemindButtonLeft = 4
                p.RemindButtonTop = 268
                p.DeleteButtonLeft = 345
                p.DeleteButtonTop = 268

            Case Else ' Default
                p.Width = 300
                p.TitleHeight = 16
                p.OriginalHeight = 206
                p.TitleFont = New Font("Segoe UI", 8, FontStyle.Bold)
                p.MemoFont = New Font("Segoe UI", 9)

                ' ★ Default の “現在の COPiPi のデフォルト値”
                p.IconSize = 14
                p.IconFontSize = 8
                p.IconRightMargin = 4
                p.IconSpacing = 2

                p.HideButtonLeft = 2
                p.HideButtonTop = 1

                p.TitleTextBoxLeft = 4
                p.TitleTextBoxTop = 19
                p.TitleTextBoxWidth = 292
                p.MemoTextBoxHeight = 130

                p.RemindLabelOffsetX = -21
                p.RemindLabelOffsetY = 5
                p.RemindLabelFontSize = 8

                p.FileLabelOffsetX = -88
                p.FileLabelOffsetY = 5
                p.FileLabelWidth = 90
                p.FileLabelFontSize = 8

                p.ActionButtonSize = 23
                p.ActionButtonEmojiFontSize = 9

                p.UrlButtonLeft = 273
                p.UrlButtonTop = 19
                p.RemindButtonLeft = 4
                p.RemindButtonTop = 180
                p.DeleteButtonLeft = 273
                p.DeleteButtonTop = 180
        End Select
        Return p
    End Function


    '===========================================================
    ' ★ RedrawAllMemos：全 MemoForm の UI を再適用・再描画
    '   - 現在の MemoSizeProfile を取得し、各 MemoForm に再適用
    '   - 折りたたみ／展開状態に応じて高さを再設定（最重要）
    '   - RemindLabel の内容・位置を再読み込み
    '   - CheckReminder() により期限状態の色（CircleButton／RemindLabel）を更新
    '   - Invalidate() でタイトルバー含む全体を再描画
    '===========================================================
    Public Sub RedrawAllMemos()


        Dim p As MemoSizeProfile = GetMemoSizeProfile()

        For Each f As Form In Application.OpenForms
            If TypeOf f Is MemoForm Then
                Dim mf As MemoForm = CType(f, MemoForm)

                ' サイズ・位置・フォントなどを再適用
                mf.ApplySizeProfile(p)

                ' ★ 折りたたみ／展開の高さを再適用（最重要）
                If CBool(mf.MyRow("CollapsedFlag")) Then
                    mf.Height = mf.TitleHeight
                Else
                    mf.Height = mf.OriginalHeight
                End If

                ' RemindLabel の内容と位置を再適用
                mf.LoadRemindLabel()

                ' 期限状態の色を再適用
                mf.CheckReminder()

                ' 再描画
                mf.Invalidate()
            End If
        Next

    End Sub

    '===========================================================
    ' ★ OpenMemoNearSearchForm：検索フォームの近くに付箋を表示
    '   - 単体でも複数でも縦に並べて表示
    '   - 既に開いている付箋は再生成せず位置だけ調整
    '===========================================================
    Public Sub OpenMemoNearSearchForm(memoIds As List(Of String), searchForm As Form)

        If memoIds Is Nothing OrElse memoIds.Count = 0 Then Exit Sub

        Dim baseX As Integer = searchForm.Left + searchForm.Width + 10
        Dim baseY As Integer = searchForm.Top

        Dim offsetY As Integer = 0

        For Each memoId In memoIds

            Dim mf As MemoForm = OpenMemo(memoId)

            ' ★ UI を最新化（折りたたみ状態を反映）
            mf.RefreshUI()

            ' ★ 予定位置
            Dim newX As Integer = baseX
            Dim newY As Integer = baseY + offsetY

            ' ★ 呼び出し対象だけで重なり判定する
            Dim safePos As Point = FindSafePositionForSearchForm(newX, newY, mf, memoIds)

            mf.Left = safePos.X
            mf.Top = safePos.Y

            ' ★ DataTable の位置更新
            Dim row = mainData.Rows.Cast(Of DataRow)().
            First(Function(r) r("id").ToString() = memoId)

            row("x") = safePos.X
            row("y") = safePos.Y

            ' ★ 非表示だったものを呼び出したら VisibleFlag=True
            row("VisibleFlag") = True

            ' ★ 次の付箋の Y を実際の高さでずらす
            offsetY += mf.Height + 6
        Next

        Save()
    End Sub


    Private Function FindSafePositionForSearchForm(startX As Integer, startY As Integer, mf As MemoForm, ids As List(Of String)) As Point

        Dim x As Integer = startX
        Dim y As Integer = startY

        While True

            Dim overlap = memoForms.Values.Any(Function(other)

                                                   ' ★ 自分自身は除外
                                                   If other Is mf Then Return False

                                                   ' ★ 呼び出し対象以外は重なり判定に使わない
                                                   If Not ids.Contains(other.MyRow("id").ToString()) Then Return False

                                                   Dim rect1 As New Rectangle(x, y, mf.Width, mf.Height)
                                                   Dim rect2 As New Rectangle(other.Left, other.Top, other.Width, other.Height)
                                                   Return rect1.IntersectsWith(rect2)
                                               End Function)

            If Not overlap Then
                Return New Point(x, y)
            End If

            ' ★ 実際の高さでずらす（展開／折りたたみ対応）
            y += mf.Height + 6
        End While

    End Function



    '===========================================================
    ' ★ DeleteMemoList：複数メモ削除（辞書方式）
    '   - memoIds の一覧を受け取り、フォーム＋データ＋XML を同期削除
    '===========================================================
    Public Sub DeleteMemoList(memoIds As List(Of String))

        For Each memoId In memoIds

            ' ★ 表示中なら閉じる（辞書方式）
            If memoForms.ContainsKey(memoId) Then
                Dim mf = memoForms(memoId)
                mf.Close()
            End If

            ' ★ DataTable から削除
            Dim row = mainData.Rows.Cast(Of DataRow)().
            FirstOrDefault(Function(r) r("id").ToString() = memoId)

            If row IsNot Nothing Then
                mainData.Rows.Remove(row)
            End If
        Next

        ' ★ XML 保存
        mainData.WriteXml(dataFilename)

        ' ★ 表示メニュー更新
        BuildShowCheckMenu()
    End Sub

    '===========================================================
    ' ★ RemoveMemoForm：辞書から MemoForm を安全に削除する
    '===========================================================
    Public Sub RemoveMemoForm(memoId As String)

        If String.IsNullOrEmpty(memoId) Then Exit Sub

        If memoForms.ContainsKey(memoId) Then
            memoForms.Remove(memoId)
        End If

    End Sub

    '===========================================================
    ' ★ SetMemoVisibility：複数メモの表示／非表示を一括更新
    '===========================================================
    Public Sub SetMemoVisibility(memoIds As List(Of String), visible As Boolean)

        For Each memoId In memoIds

            ' ★ DataRow を取得
            Dim row = mainData.Rows.Cast(Of DataRow)().
            FirstOrDefault(Function(r) r("id").ToString() = memoId)

            If row Is Nothing Then Continue For

            ' ★ VisibleFlag を更新
            row("VisibleFlag") = visible

            ' ★ 表示 → 非表示
            If Not visible Then
                If memoForms.ContainsKey(memoId) Then
                    memoForms(memoId).Close()
                End If

            Else
                If Not memoForms.ContainsKey(memoId) Then
                    MemoBuild(row)
                End If
            End If
        Next

        ' ★ XML 保存
        mainData.WriteXml(dataFilename)

        ' ★ 表示／非表示メニュー更新
        BuildShowCheckMenu()
    End Sub

    '===========================================================
    ' ★ SetMemoTopMost：複数メモの最前面設定を一括更新
    '===========================================================
    Public Sub SetMemoTopMost(memoIds As List(Of String), topMost As Boolean)

        For Each memoId In memoIds

            ' ★ DataRow を更新
            Dim row = mainData.Rows.Cast(Of DataRow)().
                FirstOrDefault(Function(r) r("id").ToString() = memoId)

            If row Is Nothing Then Continue For

            row("TopMostFlag") = topMost

            ' ★ 表示中なら MemoForm を更新
            If memoForms.ContainsKey(memoId) Then
                Dim mf = memoForms(memoId)
                mf.TopMost = topMost
                mf.UpdatePinLabel()
            End If
        Next

        ' ★ XML 保存（必ず呼ぶ）
        Save()

        ' ★ 表示メニュー更新
        BuildShowCheckMenu()
    End Sub



End Class


'===========================================================
' ★ MemoSizeProfile：付箋 UI のサイズ・位置・フォント設定を一括管理
'   - MemoForm のレイアウトをテーマ／サイズモードごとに切り替えるための構造体
'   - 本体サイズ、タイトル高さ、フォント、アイコン位置など全 UI 要素を保持
'   - ApplySizeProfile() で MemoForm に適用され、折りたたみ／展開にも対応
'===========================================================
Public Class MemoSizeProfile

    ' 既存
    Public Width As Integer
    Public TitleHeight As Integer
    Public OriginalHeight As Integer
    Public TitleFont As Font
    Public MemoFont As Font

    ' アイコン系
    Public IconSize As Integer
    Public IconFontSize As Integer
    Public IconRightMargin As Integer
    Public IconSpacing As Integer

    ' hideButton
    Public HideButtonLeft As Integer
    Public HideButtonTop As Integer

    ' TitleTextBox
    Public TitleTextBoxLeft As Integer
    Public TitleTextBoxTop As Integer
    Public TitleTextBoxWidth As Integer

    Public MemoTextBoxHeight As Integer

    ' ★ RemindLabel（通知ラベル）
    Public RemindLabelOffsetX As Integer   ' RemindButton からの距離（左方向）
    Public RemindLabelOffsetY As Integer   ' RemindButton からの距離（上下）
    Public RemindLabelFontSize As Integer

    ' ★ FileLabel（添付ファイル名）を MemoDelete からのオフセットで管理
    Public FileLabelOffsetX As Integer     ' MemoDelete からの距離（右方向）
    Public FileLabelOffsetY As Integer     ' MemoDelete からの距離（上下）
    Public FileLabelWidth As Integer
    Public FileLabelFontSize As Integer

    ' ★ ActionButton（web / remind / delete）共通サイズ
    Public ActionButtonSize As Integer
    Public ActionButtonEmojiFontSize As Integer
    Public UrlButtonLeft As Integer
    Public UrlButtonTop As Integer
    Public RemindButtonLeft As Integer
    Public RemindButtonTop As Integer
    Public DeleteButtonLeft As Integer
    Public DeleteButtonTop As Integer

End Class
