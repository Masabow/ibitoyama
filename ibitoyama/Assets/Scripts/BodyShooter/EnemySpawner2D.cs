using System.Collections.Generic;
using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class EnemySpawner2D : MonoBehaviour
  {
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 0.8f;
    [SerializeField] private float spawnXMin = -7f;
    [SerializeField] private float spawnXMax = 7f;
    [SerializeField] private float spawnY = 5.5f;
    [SerializeField] private int maxAliveEnemies = 24;

    private readonly List<GameObject> _aliveEnemies = new List<GameObject>();

    private float _nextSpawnAt;
    private GameLoopController _loop;

    public void SetGameLoop(GameLoopController loop)
    {
      _loop = loop;
    }

    public void Configure(GameObject prefab, float xMin, float xMax, float y)
    {
      enemyPrefab = prefab;
      spawnXMin = xMin;
      spawnXMax = xMax;
      spawnY = y;
    }

    public void ResetSpawner()
    {
      for (var i = 0; i < _aliveEnemies.Count; i++)
      {
        if (_aliveEnemies[i] != null)
        {
          Destroy(_aliveEnemies[i]);
        }
      }

      _aliveEnemies.Clear();
      _nextSpawnAt = Time.time + spawnInterval;
    }

    private void Update()
    {
      if (_loop == null || !_loop.IsRunning || enemyPrefab == null)
      {
        return;
      }

      for (var i = _aliveEnemies.Count - 1; i >= 0; i--)
      {
        if (_aliveEnemies[i] == null)
        {
          _aliveEnemies.RemoveAt(i);
        }
      }

      if (_aliveEnemies.Count >= maxAliveEnemies || Time.time < _nextSpawnAt)
      {
        return;
      }

      SpawnEnemy();
      _nextSpawnAt = Time.time + spawnInterval;
    }

    private void SpawnEnemy()
    {
      var x = Random.Range(spawnXMin, spawnXMax);
      var enemy = Instantiate(enemyPrefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
      enemy.SetActive(true);
      _aliveEnemies.Add(enemy);
    }
  }
}
