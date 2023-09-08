using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Using ctrl + shift + alt + NUM you can easily switch to scenes at those indexes.
/// </summary>
public static class BuildSceneShortcuts {

	[MenuItem("Tools/Load Scene/Load Scene at Build Index 0 %#&0")]
	private static void LoadSceneZero()
	{
		CheckBuildIndex(0);
	}

	[MenuItem("Tools/Load Scene/Load Scene at Build Index 1 %#&1")]
	private static void LoadSceneOne()
	{
		CheckBuildIndex(1);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 2 %#&2")]
	private static void LoadSceneTwo()
	{
		CheckBuildIndex(2);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 3 %#&3")]
	private static void LoadSceneThree()
	{
		CheckBuildIndex(3);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 4 %#&4")]
	private static void LoadSceneFour()
	{
		CheckBuildIndex(4);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 5 %#&5")]
	private static void LoadSceneFive()
	{
		CheckBuildIndex(5);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 6 %#&6")]
	private static void LoadSceneSix()
	{
		CheckBuildIndex(6);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 7 %#&7")]
	private static void LoadSceneSeven()
	{
		CheckBuildIndex(7);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 8 %#&8")]
	private static void LoadSceneEight()
	{
		CheckBuildIndex(8);
	}
	
	[MenuItem("Tools/Load Scene/Load Scene at Build Index 9 %#&9")]
	private static void LoadSceneNine()
	{
        CheckBuildIndex(9);
    }

	private static void CheckBuildIndex(int index)
	{
		if (EditorBuildSettings.scenes.Length < index + 1)
			return;
		EditorSceneManager.SaveOpenScenes();
		EditorSceneManager.OpenScene(EditorBuildSettings.scenes[index].path);
	}
}
