using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SnapToGround {
	
	private static float _yOffset = 2;
	
	[MenuItem("Tools/Snap To Ground %w")]
	public static void SnapObject()
	{
		if (Selection.transforms.Length == 0)
			return;

		PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
		PhysicsScene physicsScene = new ();
		if (prefabStage)
			physicsScene = PhysicsSceneExtensions.GetPhysicsScene(prefabStage.prefabContentsRoot.scene);
		
		foreach (Transform transform in Selection.transforms)
		{
			RaycastHit[] results = new RaycastHit[20];
			
			if (prefabStage)
				physicsScene.Raycast(transform.position + (_yOffset * Vector3.up), Vector3.down, results, 15f);
			else
				Physics.RaycastNonAlloc(transform.position + (_yOffset * Vector3.up), Vector3.down, results, 15f);
			
			if (results.Length == 0)
				return;

			float highestY = float.MinValue;
			RaycastHit closestHit = new();

			foreach (RaycastHit hit in results)
			{
				if (hit.collider == null || hit.collider.gameObject == transform.gameObject || hit.point.y <= highestY)
					continue;
				highestY = hit.point.y;
				closestHit = hit;
			}
			if (closestHit.collider != null)
				transform.position = closestHit.point;
		}
	}
}
