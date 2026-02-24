using System.Collections.Generic;
using UnityEngine;

// ID와 Object를 한 쌍으로 묶는 제네릭 클래스
[System.Serializable]
public class RepositoryEntry<T> where T : Object
{
    public string ID;
    public T Object;
}

// 대부분의 리포지토리가 상속받을 부모 클래스
public abstract class BaseRepository<T> : ScriptableObject where T : Object
{
    public List<RepositoryEntry<T>> entries = new List<RepositoryEntry<T>>();

    // ID를 통해 해당 에셋을 빠르게 찾기 위한 딕셔너리 (런타임용)
    private Dictionary<string, T> _repositoryCache;

    private void OnEnable()
    {
        // 런타임 시작 시 리스트를 딕셔너리로 변환하여 탐색 속도 향상
        _repositoryCache = new Dictionary<string, T>();
        foreach (var entry in entries)
        {
            if (!_repositoryCache.ContainsKey(entry.ID))
            {
                _repositoryCache.Add(entry.ID, entry.Object);
            }
        }
    }

    public T GetObject(string id)
    {
        if (_repositoryCache.TryGetValue(id, out T obj))
        {
            return obj;
        }
        Debug.LogWarning($"[Repository] ID '{id}' not found in {this.name}.");
        return null;
    }

    // 사양서의 '자동화 버튼' 기능은 Custom Editor 스크립트를 별도로 작성해야 구현할 수 있습니다.
    // 이 기본 스크립트에는 포함되지 않습니다.
}