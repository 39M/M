using UnityEngine;
using UnityEditor;

public class PlayerPrefersEditor
{
    [MenuItem("Tools/Clear Saved Data")]
    private static void ClearPlayerPrefs()
    {
        if (EditorUtility.DisplayDialog("清空存档", "确定要清除所有存档吗？", "确定", "取消"))
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
