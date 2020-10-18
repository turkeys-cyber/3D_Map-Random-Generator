using System;
using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public GameObject tilePrefab; //瓦片预制体
    public Vector2 mapSize; //public float maxSizeX, mapSizeY
    public Transform mapHolder;
    [Range(0,1)]public float outlinePercent; //瓦片之间得缝隙
    public GameObject obsPrefab; //障碍物预制体
    //public int obsCount; //障碍物数量
    public List<Coord> allTilesCoord = new List<Coord>();
    private Queue<Coord> shuffledQueue;
    public Color foregroundColor, backgroundColor; //障碍物前景色与后景色
    public float minObsHeight, maxObsHeight; //障碍物随机高度

    [Header("Map Fully Accessible")]
    [Range(0, 1)] public float obsPercent; //障碍物所占百分比
    private Coord mapCenter; //任何随机地图，中心点都不能有任何障碍物，这个点是用来任务生成，和填充算法判定使用的
    bool[,] mapObstacles; //判断任何坐标位置是否有障碍物

    [Header("Nav Mesh Agent")]
    public Vector2 mapMaxSize;
    public GameObject navMeshObs;
    public GameObject player;
    private void Start() 
    {
        GeneratorMap();
        Init();
    }

    private void Init()
    {
        Instantiate(player, new Vector3(-mapSize.x / 2 + 0.5f + mapCenter.x, 0, -mapSize.y / 2 + 0.5f + mapCenter.y), Quaternion.identity);
    }

    private void GeneratorMap()
    {
        mapSize.x = mapSize.x > mapMaxSize.x ? mapMaxSize.x : mapSize.x;
        mapSize.y = mapSize.y > mapMaxSize.y ? mapMaxSize.y : mapSize.y;

        //循环生成瓦片
        for(int i = 0; i < mapSize.x; i++)
        {
            for(int j = 0; j < mapSize.y; j++)
            {
                Vector3 newPos = new Vector3(-mapSize.x / 2 + 0.5f + i, 0, -mapSize.y / 2 + 0.5f + j);
                GameObject spwanTile = Instantiate(tilePrefab, newPos, Quaternion.Euler(90, 0 ,0));
                spwanTile.transform.SetParent(mapHolder); //spwanTile.transform.Parent = mapHolder;
                spwanTile.transform.localScale *= (1 - outlinePercent);

                allTilesCoord.Add(new Coord(i, j));
            }
        }

        shuffledQueue = new Queue<Coord>(Utilities.ShuffleCoords(allTilesCoord.ToArray()));

        int obsCount = (int)(mapSize.x * mapSize.y * obsPercent);

        //MAKER 2020.10.12日添加
        mapCenter = new Coord((int)mapSize.x / 2, (int)mapSize.y / 2); //设置地图中心点坐标
        mapObstacles = new bool[(int)mapSize.x, (int)mapSize.y]; //初始化

        int currentObsCount = 0;

        //循环生成障碍物
        for(int i =0; i < obsCount; i++)
        {
            //Coord randomCoord = allTilesCoord[UnityEngine.Random.Range(0, allTilesCoord.Count)]; //ERROR 随机重复
            Coord randomCoord = GetRandomCoord();

            mapObstacles[randomCoord.x, randomCoord.y] = true; //默认当前随机的位置可以有障碍物
            currentObsCount++;


            //随机障碍物的高低
            //float obsHeight = UnityEngine.Random.Range(minObsHeight, maxObsHeight);
            //并不是所有的障碍物，都能够随机位置生成
            if (randomCoord != mapCenter && MapIsFullyAccessible(mapObstacles, currentObsCount))
            {
                float obsHeight = Mathf.Lerp(minObsHeight, maxObsHeight, UnityEngine.Random.Range(0f, 1f));  //ERROE UnityEngine.Random.Range(0, 1)

                Vector3 newPos = new Vector3(-mapSize.x / 2 + 0.5f + randomCoord.x, obsHeight / 2, -mapSize.y / 2 + 0.5f + randomCoord.y);
                GameObject spawnObs = Instantiate(obsPrefab, newPos, Quaternion.identity);
                spawnObs.transform.SetParent(mapHolder);
                spawnObs.transform.localScale = new Vector3(1 - outlinePercent, obsHeight, 1 - outlinePercent);

                #region 

                MeshRenderer meshRender = spawnObs.GetComponent<MeshRenderer>();
                Material material = meshRender.material; //sharedMaterial

                float colorPercent = randomCoord.y / mapSize.y;
                material.color = Color.Lerp(foregroundColor, backgroundColor, colorPercent);
                meshRender.material = material;

                #endregion
            }
            else
            {
                mapObstacles[randomCoord.x, randomCoord.y] = false; //随机的坐标位置没有障碍物
                currentObsCount--;
            }

        }

        #region 动态创建“空气墙”NavMeshObstacle的部分
        //Up(Forward)->Down(Back)->Left->Right
        //Up
        GameObject navMeshObsForward = Instantiate(navMeshObs, Vector3.forward * (mapMaxSize.y + mapSize.y) / 4, Quaternion.identity);
        navMeshObsForward.transform.localScale = new Vector3(mapSize.x, 5, (mapMaxSize.y / 2 - mapSize.y / 2));

        //Down
        GameObject navMeshObsDown = Instantiate(navMeshObs, Vector3.back * (mapMaxSize.y + mapSize.y) / 4, Quaternion.identity);
        navMeshObsDown.transform.localScale = new Vector3(mapSize.x, 5, (mapMaxSize.y / 2 - mapSize.y / 2));

        //Left
        GameObject navMeshObsLeft = Instantiate(navMeshObs, Vector3.left * (mapMaxSize.x + mapSize.x) / 4, Quaternion.identity);
        navMeshObsLeft.transform.localScale = new Vector3((mapMaxSize.x / 2 - mapSize.x / 2), 5, mapSize.y);

        //Rigth
        GameObject navMeshObsRight = Instantiate(navMeshObs, Vector3.right * (mapMaxSize.x + mapSize.x) / 4, Quaternion.identity);
        navMeshObsRight.transform.localScale = new Vector3((mapMaxSize.x / 2 - mapSize.x / 2), 5, mapSize.y);
        #endregion
    }

    private bool MapIsFullyAccessible(bool[,] _mapObstacles, int _currentObsCount)
    {
        bool[,] mapFlags = new bool[_mapObstacles.GetLength(0), _mapObstacles.GetLength(1)]; //记录坐标是否被检测过

        Queue<Coord> queue = new Queue<Coord>(); //所有可行走的坐标都会存储在这个队列中
        queue.Enqueue(mapCenter); //中心点
        mapFlags[mapCenter.x, mapCenter.y] = true;
        
        int accessibleCount = 1; //可行走的瓦片数量，由于中心点为可行走，所以默认初始值为1

        while(queue.Count > 0)
        {
            Coord currentTile = queue.Dequeue(); //出队

            for(int x = -1; x <= 1; x++) //检测相邻四周的坐标点X轴
            {
                for(int y = -1; y <= 1; y++) //检测相邻四周的坐标点Y轴
                {
                    int neighborX = currentTile.x + x;
                    int neighborY = currentTile.y + y;

                    //排除对角线方向(四领域填充法)
                    if (x == 0 || y == 0)
                    {
                        //边界检测
                        if(neighborX>=0&&neighborX<_mapObstacles.GetLength(0)
                            && neighborY >= 0 && neighborY < _mapObstacles.GetLength(1)) //防止相邻点超出地图临界位置
                        {
                            //保证相邻点:1还没被检测到.mapFlags为false,2mapObstacle也为false没有障碍物
                            if (!mapFlags[neighborX, neighborY] && !_mapObstacles[neighborX, neighborY])
                            {
                                mapFlags[neighborX, neighborY] = true;
                                accessibleCount++; //实际可行走的瓦片数量
                                queue.Enqueue(new Coord(neighborX, neighborY));
                            }
                        }
                    }
                }
            }
        }

        int obsTargetCount = (int)(mapSize.x * mapSize.y - _currentObsCount);
        return accessibleCount == obsTargetCount;
    }

    private Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledQueue.Dequeue(); //队列：先进先出
        shuffledQueue.Enqueue(randomCoord); //将移出的元素，放在队列的最后一个, 保证队列的完整型, 大小不变
        return randomCoord; //返回队列中的第一个元素
    }
}

[Serializable]
public struct Coord
{
    //记录瓦片的x, y坐标
    public int x, y;
    public Coord(int _x, int _y)
    {
        this.x = _x;
        this.y = _y;
    }

    public static bool operator != (Coord _c1, Coord _c2)
    {
        return !(_c1 == _c2);
    }

    public static bool operator ==(Coord _c1, Coord _c2)
    {
        return (_c1.x == _c2.x) && (_c1.y == _c2.y);
    }
}
