Imports System.Xml.Linq


'===========================================================
' ★ ColorThemeManager：色テーマ（背景色／文字色／表示状態）の管理クラス
'   - color.xml の読み込み／保存を担当し、COPiPi 全体のテーマ設定を保持
'   - 起動時に XML が無ければ初期テーマ（6色）を自動生成
'   - MemoForm やメニュー構築で使用するテーマ一覧（Themes）を提供
'   - Index からテーマを取得したり、新規テーマの Index を採番する補助機能も持つ
'===========================================================

Public Class ColorThemeManager

    Public Shared Themes As New List(Of ColorTheme)
    Public Shared ThemeFilePath As String = IO.Path.Combine(Application.StartupPath, "color.xml")


    '===========================================================
    ' ★ LoadThemes：起動時にテーマ XML を読み込む
    '   - color.xml が存在しない場合は初期テーマを生成
    '   - XML を読み込み Themes リストに反映
    '===========================================================

    Public Shared Sub LoadThemes()

        If Not IO.File.Exists(ThemeFilePath) Then
            CreateDefaultThemeFile()
        End If

        LoadThemeFile()
    End Sub


    '===========================================================
    ' ★ CreateDefaultThemeFile：初期テーマ（6色）を XML に生成
    '   - 背景色（ColorValue）と文字色（ForeColor）を定義
    '   - Visible=True で全テーマを初期表示状態にする
    '===========================================================

    Private Shared Sub CreateDefaultThemeFile()

        Dim doc As New XDocument(
        New XElement("ColorThemes",
            New XElement("Theme",
                New XElement("Index", 0),
                New XElement("Name", "Yellow"),
                New XElement("ColorValue", Color.FromArgb(255, 240, 120).ToArgb()),
                New XElement("ForeColor", Color.Black.ToArgb()),
                New XElement("Visible", True)
            ),
            New XElement("Theme",
                New XElement("Index", 1),
                New XElement("Name", "Pink"),
                New XElement("ColorValue", Color.FromArgb(255, 160, 200).ToArgb()),
                New XElement("ForeColor", Color.Black.ToArgb()),
                New XElement("Visible", True)
            ),
            New XElement("Theme",
                New XElement("Index", 2),
                New XElement("Name", "Green"),
                New XElement("ColorValue", Color.FromArgb(160, 240, 160).ToArgb()),
                New XElement("ForeColor", Color.Black.ToArgb()),
                New XElement("Visible", True)
            ),
            New XElement("Theme",
                New XElement("Index", 3),
                New XElement("Name", "Blue"),
                New XElement("ColorValue", Color.FromArgb(140, 180, 255).ToArgb()),
                New XElement("ForeColor", Color.Black.ToArgb()),
                New XElement("Visible", True)
            ),
            New XElement("Theme",
                New XElement("Index", 4),
                New XElement("Name", "Orange"),
                New XElement("ColorValue", Color.FromArgb(255, 200, 120).ToArgb()),
                New XElement("ForeColor", Color.Black.ToArgb()),
                New XElement("Visible", True)
            ),
            New XElement("Theme",
                New XElement("Index", 5),
                New XElement("Name", "Gray"),
                New XElement("ColorValue", Color.FromArgb(200, 200, 200).ToArgb()),
                New XElement("ForeColor", Color.Black.ToArgb()),
                New XElement("Visible", True)
            )
        )
    )

        doc.Save(ThemeFilePath)
    End Sub


    '===========================================================
    ' ★ LoadThemeFile：color.xml を読み込み Themes に展開
    '   - Theme 要素を列挙し、ColorTheme オブジェクトに変換
    '   - ForeColor が無い場合は黒をデフォルトとして補正
    '===========================================================

    Private Shared Sub LoadThemeFile()

        Dim doc As XDocument = XDocument.Load(ThemeFilePath)

        Themes = doc.Root.Elements("Theme").
            Select(Function(t) New ColorTheme With {
                .Index = CInt(t.Element("Index").Value),
                .Name = t.Element("Name").Value,
                .ColorValue = CInt(t.Element("ColorValue").Value),
                .Visible = Boolean.Parse(t.Element("Visible").Value),
                .ForeColor = If(t.Element("ForeColor") IsNot Nothing,
                                CInt(t.Element("ForeColor").Value),
                                Color.Black.ToArgb())
            }).ToList()
    End Sub

    '===========================================================
    ' ★ GetThemeByIndex：Index からテーマを取得
    '   - Themes リストから一致するテーマを返す
    '   - 見つからない場合は Nothing を返す
    '===========================================================

    Public Shared Function GetThemeByIndex(index As Integer) As ColorTheme
        Return Themes.FirstOrDefault(Function(t) t.Index = index)
    End Function

    '===========================================================
    ' ★ SaveThemes：現在の Themes を XML に保存
    '   - 編集されたテーマ（色／文字色／表示状態）を color.xml に書き戻す
    '===========================================================
    Public Shared Sub SaveThemes()

        Dim doc As New XDocument(
        New XElement("ColorThemes",
            Themes.Select(Function(t) _
                New XElement("Theme",
                    New XElement("Index", t.Index),
                    New XElement("Name", t.Name),
                    New XElement("ColorValue", t.ColorValue),
                    New XElement("ForeColor", t.ForeColor),
                    New XElement("Visible", t.Visible)
                )
            )
        )
    )
        doc.Save(ThemeFilePath)
    End Sub

    '===========================================================
    ' ★ GetNextIndex：新規テーマ追加時の Index を採番
    '   - Themes が空なら 0、存在する場合は最大 Index + 1 を返す
    '===========================================================

    Public Shared Function GetNextIndex() As Integer
        If Themes Is Nothing OrElse Themes.Count = 0 Then
            Return 0
        End If

        Return Themes.Max(Function(t) t.Index) + 1
    End Function


End Class

'===========================================================
' ★ ColorTheme：1 色テーマの構造体
'   - Index：テーマ識別番号
'   - Name：テーマ名（Yellow / Pink など）
'   - ColorValue：背景色（ARGB）
'   - ForeColor：文字色（ARGB）
'   - Visible：表示／非表示の設定（メニューと同期）
'===========================================================
Public Class ColorTheme

    Public Property Index As Integer
    Public Property Name As String
    Public Property ColorValue As Integer
    Public Property ForeColor As Integer
    Public Property Visible As Boolean
End Class
