using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateUnitComponents : EditorWindow
{
    private string unitName = "NewUnit";

    /// <summary>
    /// 画面上部のバーで項目選択された時の動作
    /// </summary>
    [MenuItem("Tools/CreateUnitComponents")]
    static void Init()
    {
        CreateUnitComponents window = (CreateUnitComponents)GetWindow(typeof(CreateUnitComponents));
        window.Show();
    }

    private void OnGUI()
    {
        // ウィンドウタイトル設定
        GUILayout.Label("新規ユニットのファイル&ディレクトリの作成", EditorStyles.boldLabel);
        // ファイル及びディレクトリの共通名称を入力できるテキストフィールド
        unitName = EditorGUILayout.TextField("Unit Name", unitName);
        // Create Foldersと表示されたボタンを描画し、クリックされたらCreateAppFoldersを実行する
        if (GUILayout.Button("Create Components"))
        {
            CreateFolder(unitName);
        }
    }

    private void CreateFolder(string unitName)
    {
        // Assetsディレクトリ以下の相対パスを生成
        string relativeBasePath = Path.Combine("Scripts", "Unit", unitName);
        // Application.dataPathでAssetsまでの絶対パスを取得し、上記相対パスと結合
        string absolutePath = Path.Combine(Application.dataPath, relativeBasePath);
        // ディレクトリが重複していないか確認
        if (!Directory.Exists(absolutePath))
        {
            // ディレクトリの作成
            Directory.CreateDirectory(absolutePath);
            // 各種ファイルの作成
            CreateScript(absolutePath, unitName);
            // エディタに変更された内容を反映
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogWarning("すでにディレクトリが存在します: " + absolutePath);
        }
    }

    private void CreateScript(string absolutePath, string unitName)
    {
        string[] suffixes = { "Unit", "Stats", "Controller", "Animation" };

        foreach (var suffix in suffixes)
        {
            string className = unitName + suffix;
            string scriptName = className + ".cs";
            string scriptPath = Path.Combine(absolutePath, scriptName);

            if (!File.Exists(scriptPath))
            {
                string scriptContent = GetTemplate(unitName, className, suffix);
                // 書き出し
                File.WriteAllText(scriptPath, scriptContent, System.Text.Encoding.UTF8); 
                Debug.Log($"ファイル自動生成: {scriptName}");           
            }
            else
            {
                Debug.LogWarning($"既にファイルが存在しています: {scriptName}");
            }
        }
    }

    private static string GetTemplate(string unitName, string className, string suffix)
    {
        switch (suffix)
        {
            case "Unit":
                return
$@"using UnityEngine;

[RequireComponent(typeof({unitName}Stats))]
[RequireComponent(typeof({unitName}Controller))]
[RequireComponent(typeof({unitName}Animation))]
public class {className} : UnitBase<{unitName}Stats, {unitName}Controller> {{}}";
            case "Stats":
                return $@"public class {className} : UnitStatsBase {{}}";
            case "Controller":
                return $@"public class {className} : UnitControllerBase {{}}";
            case "Animation":
                return $@"public class {className} : UnitAnimationBase {{}}";
            default:
                return "";
        }
    }
}

