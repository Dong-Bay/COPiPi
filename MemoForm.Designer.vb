<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MemoForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MemoForm))
        Me.TitleText = New System.Windows.Forms.TextBox()
        Me.MemoText = New System.Windows.Forms.TextBox()
        Me.RemindButton = New System.Windows.Forms.Button()
        Me.MemoDelete = New System.Windows.Forms.Button()
        Me.UrlButton = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'TitleText
        '
        Me.TitleText.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TitleText.Location = New System.Drawing.Point(4, 19)
        Me.TitleText.Name = "TitleText"
        Me.TitleText.Size = New System.Drawing.Size(292, 23)
        Me.TitleText.TabIndex = 1
        '
        'MemoText
        '
        Me.MemoText.AllowDrop = True
        Me.MemoText.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.MemoText.Location = New System.Drawing.Point(4, 48)
        Me.MemoText.Multiline = True
        Me.MemoText.Name = "MemoText"
        Me.MemoText.Size = New System.Drawing.Size(292, 130)
        Me.MemoText.TabIndex = 2
        '
        'RemindButton
        '
        Me.RemindButton.Font = New System.Drawing.Font("Segoe UI Emoji", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RemindButton.Location = New System.Drawing.Point(4, 180)
        Me.RemindButton.Name = "RemindButton"
        Me.RemindButton.Size = New System.Drawing.Size(23, 23)
        Me.RemindButton.TabIndex = 3
        Me.RemindButton.Text = "⏰"
        Me.RemindButton.UseVisualStyleBackColor = True
        '
        'MemoDelete
        '
        Me.MemoDelete.Location = New System.Drawing.Point(273, 180)
        Me.MemoDelete.Name = "MemoDelete"
        Me.MemoDelete.Size = New System.Drawing.Size(23, 23)
        Me.MemoDelete.TabIndex = 4
        Me.MemoDelete.Text = "🗑️"
        Me.MemoDelete.UseVisualStyleBackColor = True
        '
        'UrlButton
        '
        Me.UrlButton.Location = New System.Drawing.Point(273, 19)
        Me.UrlButton.Name = "UrlButton"
        Me.UrlButton.Size = New System.Drawing.Size(23, 23)
        Me.UrlButton.TabIndex = 5
        Me.UrlButton.Text = "🌎"
        Me.UrlButton.UseVisualStyleBackColor = True
        '
        'MemoForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(300, 206)
        Me.ControlBox = False
        Me.Controls.Add(Me.UrlButton)
        Me.Controls.Add(Me.MemoDelete)
        Me.Controls.Add(Me.RemindButton)
        Me.Controls.Add(Me.MemoText)
        Me.Controls.Add(Me.TitleText)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "MemoForm"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.Text = "MemoForm"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents TitleText As TextBox
    Friend WithEvents MemoText As TextBox
    Friend WithEvents RemindButton As Button
    Friend WithEvents MemoDelete As Button
    Friend WithEvents UrlButton As Button
End Class
