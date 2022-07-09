using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class FloorGenerator : MonoBehaviour
{
    [Header("   ---   TILES   ---------------------------------------------------------")]
    [Header("Room Size")]
    [SerializeField] private int _xLength = 15;
    [SerializeField] private int _zLength = 15;
    [SerializeField] private int _floors = 1;
    [SerializeField] private float _heightOffset;
    [SerializeField] private Vector3 _sizeOfTile = new Vector3(10f, 10f, 10f);
    [Space]
    [SerializeField] private GameObject[] _tilesToGenerate;
    [Space] 
    [SerializeField] private float[] _tileWeights;
    [SerializeField] private List<PoolData> _tilesInSceneList;

    private ObjectPool<GameObject>[] _tilePools;

    [Header("   ---   ENEMIES   ------------------------------------------------------")] 
    [SerializeField] private int _enemiesAmountToSpawn = 5;
    [Space]
    [SerializeField] private GameObject[] _enemiesToGenerate;
    [Space]
    [SerializeField] private float[] _enemyWeights;
    [SerializeField] private List<PoolData> _enemiesInSceneList;

    private ObjectPool<GameObject>[] _enemyPools;

    [Header("   ---   WALLS   --------------------------------------------------------")] 
    [SerializeField] private bool _spawnWalls;
    [SerializeField] private float _wallThickness = 1f;
    [SerializeField] private GameObject[] _wallsToGenerate;
    [Space]
    [SerializeField] private float[] _wallWeights;
    [SerializeField] private List<PoolData> _wallsInSceneList;

    private ObjectPool<GameObject>[] _wallPools;
    private bool _usePrefabs;

    [Header("   ---   POOL SETTINGS   ------------------------------------------------")]
    [SerializeField] private bool _collectionChecks = true;
    [SerializeField] private int _defaultCapacity = 50;
    [SerializeField] private int _maxPoolSize = 100;

    private void Awake()
    {
        _tilePools = new ObjectPool<GameObject>[_tilesToGenerate.Length];
        _enemyPools = new ObjectPool<GameObject>[_enemiesToGenerate.Length];
        _wallPools = new ObjectPool<GameObject>[_wallsToGenerate.Length];

        _tilesInSceneList = new List<PoolData>();
        _enemiesInSceneList = new List<PoolData>();
        _wallsInSceneList = new List<PoolData>();

        OnValidate();
    }

    private void Start()
    {
        //CreateWall();
        SetupTilePools();
        SetupEnemyPools();
        SetupWallPools();

        GenerateRoom(_xLength, _zLength);
    }

    private void GenerateRoom(int xSize, int zSize)
    {
        // Tiles
        int id = 0;
        int probability = 0;
        for (int floor = 0; floor < _floors; floor++)
        {
            for (int i = 0; i < xSize; i++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    int indexOfTilePool = GetRandomWeightedIndex(_tileWeights);
                    GameObject go = _tilePools[indexOfTilePool].Get();
                    go.transform.position = new Vector3(i * _sizeOfTile.x, floor * _sizeOfTile.y + _heightOffset, z * _sizeOfTile.z);
                    go.transform.rotation = Quaternion.Euler(0f, 90 * Random.Range(0, 3), 0f);

                    _tilesInSceneList.Add(new PoolData()
                    {
                        GO = go,
                        IndexOfPool = indexOfTilePool,
                        Id = id
                    });

                    // Enemies
                    if (go.GetComponent<TileData>().IsObstacle == false && _enemiesInSceneList.Count < _enemiesAmountToSpawn && _enemiesToGenerate.Length > 0)
                    {
                        probability++;
                        int indexOfEnemyPool = GetRandomWeightedIndex(_enemyWeights);

                        if (Random.Range(0, xSize * zSize) <= probability + (_enemiesAmountToSpawn - _enemiesInSceneList.Count))
                        {
                            GameObject enemy = _enemyPools[indexOfEnemyPool].Get();
                            enemy.transform.position = new Vector3(i * 10f, 0f, z * 10f);
                            EnemyData data = enemy.AddComponent<EnemyData>();
                            data.Enemy = enemy;
                            data.IndexOfPool = indexOfEnemyPool;

                            _enemiesInSceneList.Add(new PoolData()
                            {
                                GO = enemy,
                                IndexOfPool = indexOfEnemyPool,
                                Id = id
                            });

                            probability = 0;
                        }
                    }

                    id++;
                }
            }
        }

        CreateWalls();
        //Invoke(nameof(ReleaseEnemiesAll), 3f);
        //Invoke(nameof(ReleaseTiles), 3f);
    }

    #region Walls

    private void SetupWallPools()
    {
        var parent = new GameObject("[ WALLS ]");
        parent.transform.SetParent(gameObject.transform);
        for (int i = 0; i < _wallPools.Length; i++)
        {
            var i1 = i;
            _wallPools[i] = new ObjectPool<GameObject>(() =>
                    Instantiate(_wallsToGenerate[i1], parent.transform),
                go => go.SetActive(true),
                go => go.SetActive(false),
                go => Destroy(go.gameObject),
                _collectionChecks,
                _defaultCapacity,
                _maxPoolSize
            );
        }
    }

    private void CreateWalls()
    {
        if (_spawnWalls == false) return;
        
        if (_usePrefabs)
        {
            #region Prefabs

            // Left Side
            for (int i = -1; i < _zLength + 1; i++)
            {
                var indexOfWallPool = GetRandomWeightedIndex(_wallWeights);
                var leftPrefab = _wallPools[indexOfWallPool].Get();
                leftPrefab.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                leftPrefab.transform.position = new Vector3(
                    -_sizeOfTile.x * 0.5f - _wallThickness * 0.5f,
                    0f + _heightOffset,
                    (i * _sizeOfTile.z) - (_sizeOfTile.z * 0.5f) + _wallThickness * 0.5f
                );
                _wallsInSceneList.Add(new PoolData()
                {
                    GO = leftPrefab,
                    IndexOfPool = indexOfWallPool,
                    Id = i
                });
            }

            // Right Side
            for (int i = -1; i < _zLength + 1; i++)
            {
                var indexOfWallPool = GetRandomWeightedIndex(_wallWeights);
                var rightPrefab = _wallPools[indexOfWallPool].Get();
                rightPrefab.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                rightPrefab.transform.position = new Vector3(
                    -_sizeOfTile.x * 0.5f + (_xLength * _sizeOfTile.x) + _wallThickness * 0.5f,
                    0f + _heightOffset,
                    (i * _sizeOfTile.z) - (_sizeOfTile.z * 0.5f) + _wallThickness * 0.5f
                );
                _wallsInSceneList.Add(new PoolData()
                {
                    GO = rightPrefab,
                    IndexOfPool = indexOfWallPool,
                    Id = i
                });
            }

            // Back Side
            for (int i = 0; i < _zLength; i++)
            {
                var indexOfWallPool = GetRandomWeightedIndex(_wallWeights);
                var backPrefab = _wallPools[indexOfWallPool].Get();
                backPrefab.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                backPrefab.transform.position = new Vector3(
                    -_sizeOfTile.x * 0.5f + (i * _sizeOfTile.x) + _wallThickness * 0.5f,
                    0f + _heightOffset,
                    -(_sizeOfTile.z * 0.5f) - _wallThickness * 0.5f
                );
                _wallsInSceneList.Add(new PoolData()
                {
                    GO = backPrefab,
                    IndexOfPool = indexOfWallPool,
                    Id = i
                });
            }

            // Front Side
            for (int i = 0; i < _zLength; i++)
            {
                var indexOfWallPool = GetRandomWeightedIndex(_wallWeights);
                var frontPrefab = _wallPools[indexOfWallPool].Get();
                frontPrefab.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                frontPrefab.transform.position = new Vector3(
                    -_sizeOfTile.x * 0.5f + (i * _sizeOfTile.x) + _wallThickness * 0.5f,
                    0f + _heightOffset,
                    -(_sizeOfTile.z * 0.5f) + (_zLength * _sizeOfTile.z) + _wallThickness * 0.5f
                );
                _wallsInSceneList.Add(new PoolData()
                {
                    GO = frontPrefab,
                    IndexOfPool = indexOfWallPool,
                    Id = i
                });
            }

            #endregion
        }
        else
        {
            #region Create Primitives

            var parent = new GameObject("[ WALLS ]");
            parent.transform.SetParent(gameObject.transform);

            // Left Side
            var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "Left";
            left.transform.SetParent(parent.transform);
            var leftLength = _zLength * _sizeOfTile.z;
            left.transform.position = new Vector3(
                -_sizeOfTile.x * 0.5f - 0.5f,
                _sizeOfTile.y * 0.5f + _heightOffset,
                leftLength * 0.5f - (_sizeOfTile.z * 0.5f));
            left.transform.localScale = new Vector3(
                1f,
                _sizeOfTile.y,
                _zLength * _sizeOfTile.z);

            // Right Side
            var right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.transform.SetParent(parent.transform);
            right.name = "Right";
            var rightLength = _zLength * _sizeOfTile.z;
            right.transform.position = new Vector3(
                -(_sizeOfTile.x * 0.5f) + 0.5f + (_xLength * _sizeOfTile.x),
                _sizeOfTile.y * 0.5f + _heightOffset,
                rightLength * 0.5f - (_sizeOfTile.x * 0.5f));
            right.transform.localScale = new Vector3(
                1f,
                _sizeOfTile.y,
                _zLength * _sizeOfTile.z);

            // Back Side
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(parent.transform);
            back.name = "Back";
            var backLength = _xLength * _sizeOfTile.x;
            back.transform.position = new Vector3(
                -(_sizeOfTile.x * 0.5f) + (backLength * 0.5f),
                _sizeOfTile.y * 0.5f + _heightOffset,
                 -(_sizeOfTile.z * 0.5f) - 0.5f);
            back.transform.localScale = new Vector3(
                _xLength * _sizeOfTile.x,
                _sizeOfTile.y,
                1f);

            // Front Side
            var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
            front.transform.SetParent(parent.transform);
            front.name = "Front";
            var frontLength = _zLength * _sizeOfTile.z;
            front.transform.position = new Vector3(
                -(_sizeOfTile.x * 0.5f) + (frontLength * 0.5f),
                _sizeOfTile.y * 0.5f + _heightOffset,
                -(_sizeOfTile.z * 0.5f) + 0.5f + frontLength);
            front.transform.localScale = new Vector3(
                _xLength * _sizeOfTile.x,
                _sizeOfTile.y,
                1f);
            #endregion
        }
    }

    #endregion

    #region Tile Pool
    private void SetupTilePools()
    {
        var parent = new GameObject("[ TILES ]");
        parent.transform.SetParent(gameObject.transform);
        for (int i = 0; i < _tilePools.Length; i++)
        {
            var i1 = i;
            _tilePools[i] = new ObjectPool<GameObject>(() => 
                    Instantiate(_tilesToGenerate[i1], parent.transform),
                go => go.SetActive(true),
                go => go.SetActive(false),
                go => Destroy(go.gameObject),
                _collectionChecks,
                _defaultCapacity,
                _maxPoolSize
            );
        }
    }

    private void ReleaseTiles()
    {
        foreach (var tile in _tilesInSceneList)
        {
            _tilePools[tile.IndexOfPool].Release(tile.GO);
        }
        _tilesInSceneList.Clear();
 
        //GenerateRoom(_xLength, _zLength);
    }
    #endregion

    #region Enemy Pool
    private void SetupEnemyPools()
    {
        var parent = new GameObject("[ ENEMIES ]");
        parent.transform.SetParent(gameObject.transform);
        for (int i = 0; i < _enemyPools.Length; i++)
        {
            var i1 = i;
            _enemyPools[i] = new ObjectPool<GameObject>(() =>
                    Instantiate(_enemiesToGenerate[i1], parent.transform),
                go => go.SetActive(true),
                go => go.SetActive(false),
                go => Destroy(go.gameObject),
                _collectionChecks,
                _defaultCapacity,
                _maxPoolSize
            );
        }
    }

    public void ReleaseEnemy(EnemyData data)
    {
        _enemyPools[data.IndexOfPool].Release(data.Enemy);
        _enemiesInSceneList.RemoveAt(data.Id);
    }

    public void ReleaseEnemiesAll()
    {
        foreach (var enemy in _enemiesInSceneList)
        {
            _enemyPools[enemy.IndexOfPool].Release(enemy.GO);
        }

        _enemiesInSceneList.Clear();
    }
    #endregion

    #region Utils
    public int GetRandomWeightedIndex(float[] weights)
    {
        if (weights == null || weights.Length == 0) return -1;

        float w;
        float t = 0;
        int i;
        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];

            if (float.IsPositiveInfinity(w))
            {
                return i;
            }
            else if (w >= 0f && !float.IsNaN(w))
            {
                t += weights[i];
            }
        }

        float r = Random.value;
        float s = 0f;

        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (float.IsNaN(w) || w <= 0f) continue;

            s += w / t;
            if (s >= r) return i;
        }

        return -1;
    }

    private void OnValidate()
    {
        if (_tileWeights.Length != _tilesToGenerate.Length)
        {
            Array.Resize(ref _tileWeights, _tilesToGenerate.Length);
        }
        
        if (_enemyWeights.Length != _enemiesToGenerate.Length)
        {
            Array.Resize(ref _enemyWeights, _enemiesToGenerate.Length);
        }

        _usePrefabs = _wallsToGenerate.Length > 0;

        // Tiles looking foe
        var tempTiles = _tilesToGenerate.ToList();
        for (var i = tempTiles.Count - 1; i > -1; i--)
        {
            if (tempTiles[i] == null)
                tempTiles.RemoveAt(i);
        }
        _tilesToGenerate = tempTiles.ToArray();

        // Enemies
        var tempEnemies = _enemiesToGenerate.ToList();
        for (var i = tempEnemies.Count - 1; i > -1; i--)
        {
            if (tempEnemies[i] == null)
                tempEnemies.RemoveAt(i);
        }
        _enemiesToGenerate = tempEnemies.ToArray();
    }

    #endregion
}

public class EnemyData : MonoBehaviour
{
    public GameObject Enemy;
    public int IndexOfPool;
    public int Id;
}

[System.Serializable]
public struct PoolData
{
    public GameObject GO;
    public int IndexOfPool;
    public int Id;
}
