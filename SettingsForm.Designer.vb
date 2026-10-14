<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SettingsForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SettingsForm))
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.rbURL = New System.Windows.Forms.RadioButton()
        Me.rbOriginal = New System.Windows.Forms.RadioButton()
        Me.rbLocalCopy = New System.Windows.Forms.RadioButton()
        Me.btnOK = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.TabPage3 = New System.Windows.Forms.TabPage()
        Me.sizeBtnCancel = New System.Windows.Forms.Button()
        Me.sizeBtnOK = New System.Windows.Forms.Button()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.sizeXlarge = New System.Windows.Forms.RadioButton()
        Me.sizeLarge = New System.Windows.Forms.RadioButton()
        Me.sizeDefault = New System.Windows.Forms.RadioButton()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.GroupBox1.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(6, 16)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(265, 12)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "COPiPi設定【添付ファイルの保存先を設定してください】"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.rbURL)
        Me.GroupBox1.Controls.Add(Me.rbOriginal)
        Me.GroupBox1.Controls.Add(Me.rbLocalCopy)
        Me.GroupBox1.Location = New System.Drawing.Point(15, 43)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(523, 108)
        Me.GroupBox1.TabIndex = 1
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "COPiPiでは付箋にファイルを添付することが可能です。"
        '
        'rbURL
        '
        Me.rbURL.AutoSize = True
        Me.rbURL.Location = New System.Drawing.Point(13, 78)
        Me.rbURL.Name = "rbURL"
        Me.rbURL.Size = New System.Drawing.Size(202, 16)
        Me.rbURL.TabIndex = 2
        Me.rbURL.TabStop = True
        Me.rbURL.Text = "URL（OneDrive）も使う（クラウドリンク）"
        Me.rbURL.UseVisualStyleBackColor = True
        '
        'rbOriginal
        '
        Me.rbOriginal.AutoSize = True
        Me.rbOriginal.Location = New System.Drawing.Point(13, 52)
        Me.rbOriginal.Name = "rbOriginal"
        Me.rbOriginal.Size = New System.Drawing.Size(481, 16)
        Me.rbOriginal.TabIndex = 1
        Me.rbOriginal.TabStop = True
        Me.rbOriginal.Text = "添付したファイルをそのまま使います（リンクとして扱います。添付元ファイルは動かさないでくださいね）"
        Me.rbOriginal.UseVisualStyleBackColor = True
        '
        'rbLocalCopy
        '
        Me.rbLocalCopy.AutoSize = True
        Me.rbLocalCopy.Location = New System.Drawing.Point(13, 25)
        Me.rbLocalCopy.Name = "rbLocalCopy"
        Me.rbLocalCopy.Size = New System.Drawing.Size(355, 16)
        Me.rbLocalCopy.TabIndex = 0
        Me.rbLocalCopy.TabStop = True
        Me.rbLocalCopy.Text = "添付されたファイルをコピーして保存します（専用フォルダに保存）【推奨】"
        Me.rbLocalCopy.UseVisualStyleBackColor = True
        '
        'btnOK
        '
        Me.btnOK.Location = New System.Drawing.Point(463, 157)
        Me.btnOK.Name = "btnOK"
        Me.btnOK.Size = New System.Drawing.Size(75, 23)
        Me.btnOK.TabIndex = 2
        Me.btnOK.Text = "OK"
        Me.btnOK.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Location = New System.Drawing.Point(382, 157)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(75, 23)
        Me.btnCancel.TabIndex = 3
        Me.btnCancel.Text = "キャンセル"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage3)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Location = New System.Drawing.Point(5, 4)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(557, 213)
        Me.TabControl1.TabIndex = 4
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.Label1)
        Me.TabPage1.Controls.Add(Me.btnCancel)
        Me.TabPage1.Controls.Add(Me.GroupBox1)
        Me.TabPage1.Controls.Add(Me.btnOK)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(549, 187)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "保存設定"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'TabPage3
        '
        Me.TabPage3.Controls.Add(Me.sizeBtnCancel)
        Me.TabPage3.Controls.Add(Me.sizeBtnOK)
        Me.TabPage3.Controls.Add(Me.GroupBox2)
        Me.TabPage3.Controls.Add(Me.Label2)
        Me.TabPage3.Location = New System.Drawing.Point(4, 22)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage3.Size = New System.Drawing.Size(549, 187)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "付箋サイズ設定"
        Me.TabPage3.UseVisualStyleBackColor = True
        '
        'sizeBtnCancel
        '
        Me.sizeBtnCancel.Location = New System.Drawing.Point(382, 157)
        Me.sizeBtnCancel.Name = "sizeBtnCancel"
        Me.sizeBtnCancel.Size = New System.Drawing.Size(75, 23)
        Me.sizeBtnCancel.TabIndex = 5
        Me.sizeBtnCancel.Text = "キャンセル"
        Me.sizeBtnCancel.UseVisualStyleBackColor = True
        '
        'sizeBtnOK
        '
        Me.sizeBtnOK.Location = New System.Drawing.Point(463, 157)
        Me.sizeBtnOK.Name = "sizeBtnOK"
        Me.sizeBtnOK.Size = New System.Drawing.Size(75, 23)
        Me.sizeBtnOK.TabIndex = 4
        Me.sizeBtnOK.Text = "OK"
        Me.sizeBtnOK.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.sizeXlarge)
        Me.GroupBox2.Controls.Add(Me.sizeLarge)
        Me.GroupBox2.Controls.Add(Me.sizeDefault)
        Me.GroupBox2.Location = New System.Drawing.Point(15, 43)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(523, 108)
        Me.GroupBox2.TabIndex = 1
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "３段階のサイズ変更が可能です。"
        '
        'sizeXlarge
        '
        Me.sizeXlarge.AutoSize = True
        Me.sizeXlarge.Location = New System.Drawing.Point(13, 78)
        Me.sizeXlarge.Name = "sizeXlarge"
        Me.sizeXlarge.Size = New System.Drawing.Size(344, 16)
        Me.sizeXlarge.TabIndex = 2
        Me.sizeXlarge.TabStop = True
        Me.sizeXlarge.Text = "XLarge（視認性重視。もっと大きいのが欲しかったらご意見ください。）"
        Me.sizeXlarge.UseVisualStyleBackColor = True
        '
        'sizeLarge
        '
        Me.sizeLarge.AutoSize = True
        Me.sizeLarge.Location = New System.Drawing.Point(13, 52)
        Me.sizeLarge.Name = "sizeLarge"
        Me.sizeLarge.Size = New System.Drawing.Size(80, 16)
        Me.sizeLarge.TabIndex = 1
        Me.sizeLarge.TabStop = True
        Me.sizeLarge.Text = "Largeサイズ"
        Me.sizeLarge.UseVisualStyleBackColor = True
        '
        'sizeDefault
        '
        Me.sizeDefault.AutoSize = True
        Me.sizeDefault.Location = New System.Drawing.Point(13, 25)
        Me.sizeDefault.Name = "sizeDefault"
        Me.sizeDefault.Size = New System.Drawing.Size(179, 16)
        Me.sizeDefault.TabIndex = 0
        Me.sizeDefault.TabStop = True
        Me.sizeDefault.Text = "デフォルト（邪魔にならないサイズ）"
        Me.sizeDefault.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(6, 16)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(224, 12)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "COPiPi設定【付箋のサイズを設定してください】"
        '
        'TabPage2
        '
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(549, 187)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "カラーテーマ設定"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'SettingsForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(562, 216)
        Me.Controls.Add(Me.TabControl1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "SettingsForm"
        Me.Text = "設定"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.TabPage3.ResumeLayout(False)
        Me.TabPage3.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents rbURL As RadioButton
    Friend WithEvents rbOriginal As RadioButton
    Friend WithEvents rbLocalCopy As RadioButton
    Friend WithEvents btnOK As Button
    Friend WithEvents btnCancel As Button
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents TabPage2 As TabPage
    Friend WithEvents TabPage3 As TabPage
    Friend WithEvents Label2 As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents sizeXlarge As RadioButton
    Friend WithEvents sizeLarge As RadioButton
    Friend WithEvents sizeDefault As RadioButton
    Friend WithEvents sizeBtnCancel As Button
    Friend WithEvents sizeBtnOK As Button
End Class
