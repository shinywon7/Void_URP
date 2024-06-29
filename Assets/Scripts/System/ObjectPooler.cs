using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ObjectPooler))]
public class ObjectPoolerEditor : Editor
{
	const string INFO = "풀링한 오브젝트에 다음을 적으세요 \nvoid OnDisable()\n{\n" +
		"    ObjectPooler.ReturnToPool(gameObject);    // 한 객체에 한번만 \n" +
		"    CancelInvoke();    // Monobehaviour에 Invoke가 있다면 \n}";

	public override void OnInspectorGUI()
	{
		EditorGUILayout.HelpBox(INFO, MessageType.Info);
		base.OnInspectorGUI();
	}
}
#endif

public class ObjectPooler : MonoBehaviour
{
	public GameObject prefab;
	public int size;
	
	List<GameObject> spawnObjects;
	Queue<GameObject> queue = new Queue<GameObject>();

	public GameObject SpawnFromPool(Vector3 position) => 
		_SpawnFromPool(position, Quaternion.identity);

	public GameObject SpawnFromPool(Vector3 position, Quaternion rotation) => 
		_SpawnFromPool(position, rotation);

	public T SpawnFromPool<T>(Vector3 position) where T : Component
	{
		GameObject obj = _SpawnFromPool(position, Quaternion.identity);
		if (obj.TryGetComponent(out T component))
			return component;
		else
		{
			obj.SetActive(false);
			throw new Exception($"Component not found");
		}
	}

	public T SpawnFromPool<T>(Vector3 position, Quaternion rotation) where T : Component
	{
		GameObject obj = _SpawnFromPool(position, rotation);
		if (obj.TryGetComponent(out T component))
			return component;
		else
		{
			obj.SetActive(false);
			throw new Exception($"Component not found");
		}
	}

	//public static List<GameObject> GetAllPools(string tag)
	//{
	//	if (!poolDictionary.ContainsKey(tag))
	//		throw new Exception($"Pool with tag {tag} doesn't exist.");

	//	return spawnObjects.FindAll(x => x.name == tag);
	//}

	//public static List<T> GetAllPools<T>(string tag) where T : Component
	//{
	//	List<GameObject> objects = GetAllPools(tag);

	//	if (!objects[0].TryGetComponent(out T component)) 
	//		throw new Exception("Component not found");

	//	return objects.ConvertAll(x => x.GetComponent<T>());
	//}

	public void ReturnToPool(GameObject obj)
	{
		queue.Enqueue(obj);
	}

	GameObject _SpawnFromPool(Vector3 position, Quaternion rotation)
	{
		// 큐에 없으면 새로 추가
		if (queue.Count <= 0)
		{
			var obj = CreateNewObject();
			//ArrangePool(obj);
		}

		// 큐에서 꺼내서 사용
		GameObject objectToSpawn = queue.Dequeue();
		objectToSpawn.transform.position = position;
		objectToSpawn.transform.rotation = rotation;
		objectToSpawn.SetActive(true);

		return objectToSpawn;
	}

	void Start()
	{
		spawnObjects = new List<GameObject>();

		for (int i = 0; i < size; i++)
		{
			var obj = CreateNewObject();
			//ArrangePool(obj);
		}
	}

	GameObject CreateNewObject()
	{
		var obj = Instantiate(prefab);
		obj.SetActive(false); // 비활성화시 ReturnToPool을 하므로 Enqueue가 됨
		return obj;
	}

	void ArrangePool(GameObject obj) 
	{
		// 추가된 오브젝트 묶어서 정렬
		bool isFind = false;
		for (int i = 0; i < transform.childCount; i++)
		{
			if (i == transform.childCount - 1)
			{
				obj.transform.SetSiblingIndex(i);
				spawnObjects.Insert(i, obj);
				break;
			}
			else if (transform.GetChild(i).name == obj.name)
				isFind = true;
			else if (isFind) 
			{
				obj.transform.SetSiblingIndex(i);
				spawnObjects.Insert(i, obj);
				break;
			}
		}
	}
}
