<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SearchForm
    Inherits System.Windows.Forms.Form

    'フォームがコンポーネントの一覧をクリーンアップするために dispose をオーバーライドします。
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows フォーム デザイナーで必要です。
    Private components As System.ComponentModel.IContainer

    'メモ: 以下のプロシージャは Windows フォーム デザイナーで必要です。
    'Windows フォーム デザイナーを使用して変更できます。  
    'コード エディターを使って変更しないでください。
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SearchForm))
        Me.txtSearch = New System.Windows.Forms.TextBox()
        Me.chkExpired = New System.Windows.Forms.CheckBox()
        Me.chkHasRemind = New System.Windows.Forms.CheckBox()
        Me.cmbColorFilter = New System.Windows.Forms.ComboBox()
        Me.lvResults = New System.Windows.Forms.ListView()
        Me.btnOpen = New System.Windows.Forms.Button()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnToggleVisible = New System.Windows.Forms.Button()
        Me.btnToggleTopMost = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'txtSearch
        '
        Me.txtSearch.Location = New System.Drawing.Point(13, 13)
        Me.txtSearch.Name = "txtSearch"
        Me.txtSearch.Size = New System.Drawing.Size(300, 19)
        Me.txtSearch.TabIndex = 0
        '
        'chkExpired
        '
        Me.chkExpired.AutoSize = True
        Me.chkExpired.Location = New System.Drawing.Point(320, 15)
        Me.chkExpired.Name = "chkExpired"
        Me.chkExpired.Size = New System.Drawing.Size(71, 16)
        Me.chkExpired.TabIndex = 1
        Me.chkExpired.Text = "期限切れ"
        Me.chkExpired.UseVisualStyleBackColor = True
        '
        'chkHasRemind
        '
        Me.chkHasRemind.AutoSize = True
        Me.chkHasRemind.Location = New System.Drawing.Point(398, 15)
        Me.chkHasRemind.Name = "chkHasRemind"
        Me.chkHasRemind.Size = New System.Drawing.Size(109, 16)
        Me.chkHasRemind.TabIndex = 2
        Me.chkHasRemind.Text = "リマインド設定あり"
        Me.chkHasRemind.UseVisualStyleBackColor = True
        '
        'cmbColorFilter
        '
        Me.cmbColorFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbColorFilter.FormattingEnabled = True
        Me.cmbColorFilter.Location = New System.Drawing.Point(514, 11)
        Me.cmbColorFilter.Name = "cmbColorFilter"
        Me.cmbColorFilter.Size = New System.Drawing.Size(142, 20)
        Me.cmbColorFilter.TabIndex = 3
        '
        'lvResults
        '
        Me.lvResults.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lvResults.FullRowSelect = True
        Me.lvResults.HideSelection = False
        Me.lvResults.Location = New System.Drawing.Point(12, 38)
        Me.lvResults.Name = "lvResults"
        Me.lvResults.Size = New System.Drawing.Size(644, 370)
        Me.lvResults.TabIndex = 4
        Me.lvResults.UseCompatibleStateImageBehavior = False
        Me.lvResults.View = System.Windows.Forms.View.Details
        '
        'btnOpen
        '
        Me.btnOpen.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnOpen.Location = New System.Drawing.Point(13, 410)
        Me.btnOpen.Name = "btnOpen"
        Me.btnOpen.Size = New System.Drawing.Size(75, 23)
        Me.btnOpen.TabIndex = 5
        Me.btnOpen.Text = "呼び出す"
        Me.btnOpen.UseVisualStyleBackColor = True
        '
        'btnDelete
        '
        Me.btnDelete.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnDelete.Location = New System.Drawing.Point(95, 410)
        Me.btnDelete.Name = "btnDelete"
        Me.btnDelete.Size = New System.Drawing.Size(75, 23)
        Me.btnDelete.TabIndex = 6
        Me.btnDelete.Text = "削除"
        Me.btnDelete.UseVisualStyleBackColor = True
        '
        'btnToggleVisible
        '
        Me.btnToggleVisible.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnToggleVisible.Location = New System.Drawing.Point(177, 410)
        Me.btnToggleVisible.Name = "btnToggleVisible"
        Me.btnToggleVisible.Size = New System.Drawing.Size(75, 23)
        Me.btnToggleVisible.TabIndex = 7
        Me.btnToggleVisible.Text = "表示切替"
        Me.btnToggleVisible.UseVisualStyleBackColor = True
        '
        'btnToggleTopMost
        '
        Me.btnToggleTopMost.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnToggleTopMost.Location = New System.Drawing.Point(259, 410)
        Me.btnToggleTopMost.Name = "btnToggleTopMost"
        Me.btnToggleTopMost.Size = New System.Drawing.Size(75, 23)
        Me.btnToggleTopMost.TabIndex = 8
        Me.btnToggleTopMost.Text = "最前面固定"
        Me.btnToggleTopMost.UseVisualStyleBackColor = True
        '
        'SearchForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(668, 437)
        Me.Controls.Add(Me.btnToggleTopMost)
        Me.Controls.Add(Me.btnToggleVisible)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnOpen)
        Me.Controls.Add(Me.lvResults)
        Me.Controls.Add(Me.cmbColorFilter)
        Me.Controls.Add(Me.chkHasRemind)
        Me.Controls.Add(Me.chkExpired)
        Me.Controls.Add(Me.txtSearch)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximumSize = New System.Drawing.Size(684, 9999)
        Me.MinimumSize = New System.Drawing.Size(684, 476)
        Me.Name = "SearchForm"
        Me.Text = "COPiPi検索：Ctrlキーを押しながらクリックで複数選択できます。"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents txtSearch As TextBox
    Friend WithEvents chkExpired As CheckBox
    Friend WithEvents chkHasRemind As CheckBox
    Friend WithEvents cmbColorFilter As ComboBox
    Friend WithEvents lvResults As ListView
    Friend WithEvents btnOpen As Button
    Friend WithEvents btnDelete As Button
    Friend WithEvents btnToggleVisible As Button
    Friend WithEvents btnToggleTopMost As Button
End Class
