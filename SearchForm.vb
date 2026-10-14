Public Class SearchForm

    Private mainData As DataTable
    Private parent As DataViewForm
    Private sortColumn As Integer = -1
    Private sortAscending As Boolean = True

    Public Sub New(dt As DataTable, parentForm As DataViewForm)
        InitializeComponent()
        mainData = dt
        parent = parentForm
    End Sub

    Private Sub SerchForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' ListView 初期化
        lvResults.View = View.Details
        lvResults.FullRowSelect = True
        lvResults.MultiSelect = True
        lvResults.HideSelection = False

        lvResults.Columns.Clear()
        lvResults.Columns.Add("項目", 80)
        lvResults.Columns.Add("タイトル", 180)
        lvResults.Columns.Add("リマインド設定", 120)
        lvResults.Columns.Add("状態", 80)
        lvResults.Columns.Add("添付", 60)
        lvResults.Columns.Add("表示", 60)
        lvResults.Columns.Add("最前面", 60)

        ' 色フィルタ初期化
        cmbColorFilter.Items.Clear()
        cmbColorFilter.Items.Add("すべて")

        For Each theme In ColorThemeManager.Themes
            cmbColorFilter.Items.Add(theme.Name)
        Next

        cmbColorFilter.SelectedIndex = 0
        ' 初期表示（全件）
        RefreshSearchResults()
    End Sub

    Private Sub lvResults_KeyDown(sender As Object, e As KeyEventArgs) Handles lvResults.KeyDown

        ' Ctrl + A → 全選択
        If e.Control AndAlso e.KeyCode = Keys.A Then
            For Each item As ListViewItem In lvResults.Items
                item.Selected = True
            Next

            e.SuppressKeyPress = True   ' ビープ音防止
        End If

        If e.Control AndAlso e.KeyCode = Keys.F Then
            txtSearch.Focus()
        End If

        If e.KeyCode = Keys.Delete Then
            DeleteSelectedItems()
            e.SuppressKeyPress = True
        End If

    End Sub

    Private Sub lvResults_ColumnClick(sender As Object, e As ColumnClickEventArgs) Handles lvResults.ColumnClick

        ' 同じ列をクリックしたら昇順⇔降順を反転
        If sortColumn = e.Column Then
            sortAscending = Not sortAscending
        Else
            sortColumn = e.Column
            sortAscending = True
        End If

        RefreshSearchResults()
    End Sub


    Private Sub RefreshSearchResults()

        lvResults.Items.Clear()

        Dim rows = mainData.Rows.Cast(Of DataRow)().
    Where(Function(r) MatchesFilter(r))

        Dim sortedRows As IEnumerable(Of DataRow)

        Select Case sortColumn

            Case 0 ' 項目（色名）
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) ColorThemeManager.GetThemeByIndex(CInt(r("MemoColor"))).Name),
            rows.OrderByDescending(Function(r) ColorThemeManager.GetThemeByIndex(CInt(r("MemoColor"))).Name))

            Case 1 ' タイトル
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) r("title").ToString()),
            rows.OrderByDescending(Function(r) r("title").ToString()))

            Case 2 ' リマインド設定（日付）
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) If(IsDBNull(r("RemindTime")), DateTime.MaxValue, CType(r("RemindTime"), DateTime))),
            rows.OrderByDescending(Function(r) If(IsDBNull(r("RemindTime")), DateTime.MinValue, CType(r("RemindTime"), DateTime))))

            Case 3 ' 状態（期限切れ／今日／明日／あと◯日）
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) If(IsDBNull(r("RemindTime")), DateTime.MaxValue, CType(r("RemindTime"), DateTime))),
            rows.OrderByDescending(Function(r) If(IsDBNull(r("RemindTime")), DateTime.MinValue, CType(r("RemindTime"), DateTime))))

            Case 4 ' 添付（有／無）
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) r("FileReference").ToString() <> ""),
            rows.OrderByDescending(Function(r) r("FileReference").ToString() <> ""))

            Case 5 ' 表示（○／×）
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) CBool(r("VisibleFlag"))),
            rows.OrderByDescending(Function(r) CBool(r("VisibleFlag"))))

            Case 6 ' 最前面（★／－）
                sortedRows = If(sortAscending,
            rows.OrderBy(Function(r) CBool(r("TopMostFlag"))),
            rows.OrderByDescending(Function(r) CBool(r("TopMostFlag"))))

            Case Else
                ' デフォルトは色順
                sortedRows = rows.OrderBy(Function(r) CInt(r("MemoColor")))
        End Select


        For Each row As DataRow In sortedRows

            If Not MatchesFilter(row) Then Continue For

            ' ★ ListViewItem 作成処理（既存のまま）
            Dim theme = ColorThemeManager.GetThemeByIndex(CInt(row("MemoColor")))
            Dim item As New ListViewItem(theme.Name)

            ' ★ 色セルに背景色を付ける
            item.UseItemStyleForSubItems = False
            item.SubItems(0).BackColor = Color.FromArgb(theme.ColorValue)
            item.SubItems(0).ForeColor = Color.FromArgb(theme.ForeColor)


            item.SubItems.Add(row("title").ToString())

            If IsDBNull(row("RemindTime")) Then
                item.SubItems.Add("")
                item.SubItems.Add("なし")
            Else
                Dim rt As DateTime = CType(row("RemindTime"), DateTime)
                item.SubItems.Add(rt.ToString("yyyy/MM/dd"))
                item.SubItems.Add(GetStatusText(rt))
            End If

            Dim hasFile As Boolean = (row("FileReference").ToString() <> "")
            item.SubItems.Add(If(hasFile, "有", "無"))

            item.SubItems.Add(If(CBool(row("VisibleFlag")), "○", "×"))
            item.SubItems.Add(If(CBool(row("TopMostFlag")), "★", "－"))

            item.Tag = row("id").ToString()

            lvResults.Items.Add(item)
        Next

    End Sub


    Private Function MatchesFilter(row As DataRow) As Boolean

        Dim title As String = row("title").ToString()
        Dim text As String = row("text").ToString()

        ' 🔍 検索窓（タイトル＋本文）
        Dim word As String = txtSearch.Text.Trim()
        If word <> "" Then
            If Not (title.Contains(word) OrElse text.Contains(word)) Then
                Return False
            End If
        End If

        ' □ 期限切れ
        If chkExpired.Checked Then
            If IsDBNull(row("RemindTime")) Then Return False

            Dim rtDate = CType(row("RemindTime"), DateTime).Date
            Dim today = DateTime.Now.Date

            If rtDate >= today Then Return False
        End If


        ' □ リマインド設定あり
        If chkHasRemind.Checked Then
            If IsDBNull(row("RemindTime")) Then Return False
        End If


        ' 色フィルタ
        If cmbColorFilter.SelectedIndex > 0 Then
            Dim selectedColor = cmbColorFilter.SelectedItem.ToString()
            Dim theme = ColorThemeManager.GetThemeByIndex(CInt(row("MemoColor")))
            If theme.Name <> selectedColor Then Return False
        End If

        Return True
    End Function

    Private Function GetStatusText(rt As DateTime) As String

        Dim rtDate = rt.Date
        Dim today = DateTime.Now.Date

        If rtDate < today Then Return "期限切れ"

        Dim days = (rtDate - today).Days

        If days = 0 Then Return "今日"
        If days = 1 Then Return "明日"

        Return $"あと{days}日"
    End Function


    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
        RefreshSearchResults()
    End Sub

    Private Sub chkExpired_CheckedChanged(sender As Object, e As EventArgs) Handles chkExpired.CheckedChanged
        RefreshSearchResults()
    End Sub

    Private Sub chkHasRemind_CheckedChanged(sender As Object, e As EventArgs) Handles chkHasRemind.CheckedChanged
        RefreshSearchResults()
    End Sub

    Private Sub cmbColorFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbColorFilter.SelectedIndexChanged
        RefreshSearchResults()
    End Sub

    Private Sub lvResults_DoubleClick(sender As Object, e As EventArgs) Handles lvResults.DoubleClick
        btnOpen_Click(sender, e)
    End Sub

    Private Sub lvResults_MouseUp(sender As Object, e As MouseEventArgs) Handles lvResults.MouseUp
        If e.Button = MouseButtons.Right Then

            Dim item = lvResults.GetItemAt(e.X, e.Y)
            If item Is Nothing Then Return

            ' ★ 右クリックされたアイテムを選択状態にする
            lvResults.SelectedItems.Clear()
            item.Selected = True

            ' ★ 削除実行
            DeleteSelectedItems()
        End If
    End Sub


    Private Sub btnOpen_Click(sender As Object, e As EventArgs) Handles btnOpen.Click

        If lvResults.SelectedItems.Count = 0 Then
            MessageBox.Show("付箋が選択されていません。", "COPiPi")
            Return
        End If

        ' 選択されたメモIDを全部集める
        Dim ids As New List(Of String)

        For Each item As ListViewItem In lvResults.SelectedItems
            ids.Add(item.Tag.ToString())
        Next

        parent.OpenMemoNearSearchForm(ids, Me)
        RefreshSearchResults()
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click

        If lvResults.SelectedItems.Count = 0 Then
            MessageBox.Show("削除する付箋が選択されていません。", "COPiPi")
            Return
        End If

        If MessageBox.Show("選択した付箋を削除しますか？", "COPiPi",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = DialogResult.No Then
            Return
        End If

        Dim ids As New List(Of String)

        For Each item As ListViewItem In lvResults.SelectedItems
            ids.Add(item.Tag.ToString())
        Next

        parent.DeleteMemoList(ids)

        RefreshSearchResults()
    End Sub

    Private Sub btnToggleVisible_Click(sender As Object, e As EventArgs) Handles btnToggleVisible.Click

        If lvResults.SelectedItems.Count = 0 Then
            MessageBox.Show("付箋が選択されていません。", "COPiPi")
            Return
        End If

        Dim ids As New List(Of String)

        ' ★ DataRow から正しい VisibleFlag を取得する
        Dim firstId = lvResults.SelectedItems(0).Tag.ToString()
        Dim row = mainData.Rows.Cast(Of DataRow)().
        First(Function(r) r("id").ToString() = firstId)

        Dim currentVisible As Boolean = CBool(row("VisibleFlag"))
        Dim newVisible As Boolean = Not currentVisible

        For Each item As ListViewItem In lvResults.SelectedItems
            ids.Add(item.Tag.ToString())
        Next

        parent.SetMemoVisibility(ids, newVisible)

        RefreshSearchResults()
    End Sub


    Private Sub btnToggleTopMostt_Click(sender As Object, e As EventArgs) Handles btnToggleTopMost.Click

        If lvResults.SelectedItems.Count = 0 Then
            MessageBox.Show("付箋が選択されていません。", "COPiPi")
            Return
        End If

        Dim ids As New List(Of String)

        ' ★ 最初のアイテムの状態を見て反転
        Dim firstItem = lvResults.SelectedItems(0)
        Dim currentTopMost = (firstItem.SubItems(6).Text = "★")
        Dim newTopMost = Not currentTopMost

        For Each item As ListViewItem In lvResults.SelectedItems
            ids.Add(item.Tag.ToString())
        Next

        parent.SetMemoTopMost(ids, newTopMost)

        RefreshSearchResults()
    End Sub
    Private Sub DeleteSelectedItems()

        If lvResults.SelectedItems.Count = 0 Then
            MessageBox.Show("削除する付箋が選択されていません。", "COPiPi")
            Return
        End If

        If MessageBox.Show("選択した付箋を削除しますか？", "COPiPi",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = DialogResult.No Then
            Return
        End If

        Dim ids As New List(Of String)

        For Each item As ListViewItem In lvResults.SelectedItems
            ids.Add(item.Tag.ToString())
        Next

        parent.DeleteMemoList(ids)

        RefreshSearchResults()
    End Sub
    Private Sub SearchForm_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        RefreshSearchResults()
    End Sub


End Class