using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random ;

public class RoomFirstMapGenerator : AbstructMapGenerator
{
    //var to send to the Main in wich he will be posioned
    public static BoundsInt FirstRoom ;
    public static List<BoundsInt>  listRoomOrigin ;
    public static Dictionary<BoundsInt, double> djikstra_result ;
    static public int offsetvar;

   

    private HashSet<Vector2Int> floor;
    public  HashSet<Vector2Int> getFloor() {
        if (floor==null)
        {
            Debug.LogError("floor has problem");
        }
        return new HashSet<Vector2Int>(floor);
    }

    //addede for djikstra
    public Graph graph_main = new Graph();

    

    [SerializeField]
    private int minRoomWidth = 10 , minRoomHeight = 10 ;
    [SerializeField]
    private int mapWidth = 53 , mapHeight = 53;
    [SerializeField][Range(0,10)]
    private int offset = 3 ;


    public int getMapWidth { get => mapWidth; }
    public int getMapHeight { get => mapHeight;  }

    //private bool randomWalkRooms = false ;


    protected override void RunProceduralGeneration()
    { 
        offsetvar = offset;
        createRooms();
    }

    private void createRooms()
    {
        var roomlist = GenerateMapAlgorithm.BinarySpacePartition( new BoundsInt((Vector3Int)startPosition , new Vector3Int(mapWidth,mapHeight,0))
                                                                    ,minRoomWidth ,minRoomHeight);
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        floor = createSimpleRooms(roomlist);
        
        //add the first room createdd to the var firstroom to send it to the player

        FirstRoom = roomlist[0];
        listRoomOrigin = roomlist;

        //list of centers of rooms
        List<Vector2Int> roomCenters = new List<Vector2Int>();
        foreach (var room in roomlist)
        {
            Vector2Int center = (Vector2Int)Vector3Int.RoundToInt(room.center); 
            roomCenters.Add(center);
        }

        HashSet<Vector2Int> corridors = ConnectRooms(roomCenters , roomlist);
        floor.UnionWith(corridors);
        this.floor = floor;
        tilmapVisulaizer.paintFloorTiles(floor);
        WallGenerator.createWalls(floor,tilmapVisulaizer);
    }

 

    private HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomCenters , List<BoundsInt> listrooms)
    {
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        var currentRoomCenter = roomCenters[0];
        roomCenters.Remove(currentRoomCenter);
        while (roomCenters.Count > 0)
        {
            Vector2Int closest = FindClosestTo(currentRoomCenter , roomCenters);

            foreach (var room in listRoomOrigin)
            {
                if ((Vector2Int)Vector3Int.RoundToInt(room.center) == currentRoomCenter)
                {
                    foreach (var otherRoom in listRoomOrigin)
                    {
                        if ( !room.Equals(otherRoom) && (Vector2Int)Vector3Int.RoundToInt(otherRoom.center) == closest)
                        {
                            double weight = UnityEngine.Vector3.Distance(room.center, otherRoom.center);
                            graph_main.addEdge(room, otherRoom, weight);
                            //Debug.Log("add edge wright : " + weight);
                            
                        }
                    }
                }
            }



            roomCenters.Remove(closest);
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter , closest);
           
            currentRoomCenter = closest ;
            corridors.UnionWith(newCorridor);
        }
        

        return corridors;
    }

    //need to triple the corridor

    private HashSet<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        Vector2Int position = currentRoomCenter; // Start point
        corridor.Add(position);

        while (position.y != destination.y)
        {
            if (position.y > destination.y)
            {
                position += Vector2Int.down;
            }
            else
            {
                position += Vector2Int.up;
            }

            for (int i = -3; i <= 3; i++) // Changed from -2 and 2 to -3 and 3
            {
                corridor.Add(position + new Vector2Int(i, 0));
            }
        }

        while (position.x != destination.x)
        {
            if (position.x > destination.x)
            {
                position += Vector2Int.left;
            }
            else
            {
                position += Vector2Int.right;
            }

            for (int i = -3; i <= 3; i++) // Changed from -2 and 2 to -3 and 3
            {
                corridor.Add(position + new Vector2Int(0, i));
            }
        }

        return corridor;
    }





    private Vector2Int FindClosestTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;
        foreach (var position in roomCenters)
        {
            float distance_test = Vector2Int.Distance( position , currentRoomCenter);
            if (distance > distance_test)
            {
                distance = distance_test ;
                closest = position;
            }
        }
        return closest ; 
    }



    private HashSet<Vector2Int> createSimpleRooms(List<BoundsInt> roomlist)
    {   HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomlist)
        {
           

            for (int column = offset; column < room.size.x - offset; column++)
            {
                for (int row = offset; row < room.size.y - offset; row++){
                    Vector2Int position =  (Vector2Int)room.min + new Vector2Int(column , row );
                    
                    floor.Add(position);
                }
            }
        } 

        return floor;
    }

    //funtion to call the run Procedural Map and clear the previeous one 


    public void runRoomFirstMapGeneratorClass(){
        
        try
        {
            tilmapVisulaizer.clear();
            graph_main.clear();
            RunProceduralGeneration();
            djikstra_result = graph_main.Dijkstra(FirstRoom);

        }
        catch (KeyNotFoundException e)
        {

            Debug.Log(e.GetBaseException().Message);
            runRoomFirstMapGeneratorClass();
        }

    }




}



public class Graph
{
    Dictionary<BoundsInt, List<(BoundsInt, double)>> vertex;

    public Graph()
    {
        vertex = new Dictionary<BoundsInt, List<(BoundsInt, double)>>();
    }

    public void addEdge(BoundsInt source, BoundsInt target, double weight)
    {
        if (!vertex.ContainsKey(source))
        {
            vertex[source] = new List<(BoundsInt, double)>();
        }

        vertex[source].Add((target, weight));

        // Since it's an undirected graph, add an edge from target to source as well
        if (!vertex.ContainsKey(target))
        {
            vertex[target] = new List<(BoundsInt, double)>();
        }

        vertex[target].Add((source, weight));
    }

    public List<(BoundsInt, double)> GetNeighbors(BoundsInt vertex)
    {
        if (this.vertex.ContainsKey(vertex))
        {
            return this.vertex[vertex];
        }
        else
        {
            return new List<(BoundsInt, double)>();
        }
    }
    public void clear()
    {
        vertex.Clear();
    }

    public void Display()
    {
        foreach (var item in vertex)
        {
            Debug.Log($"Vertex: {item.Key}");

            foreach (var neighbor in item.Value)
            {
                Debug.Log($"  Neighbor: {neighbor.Item1}, Weight: {neighbor.Item2}");
            }
        }
    }

    public Dictionary<BoundsInt, double> Dijkstra(BoundsInt start)
    {
        // Initialize distances dictionary with infinity for all vertices except the start vertex
        Dictionary<BoundsInt, double> distances = new Dictionary<BoundsInt, double>();
        foreach (var vertex in vertex.Keys)
        {
            distances[vertex] = double.PositiveInfinity;
        }
        distances[start] = 0;

        // Priority queue to keep track of vertices to visit next
        var queue = new PriorityQueue<BoundsInt>();
        queue.Enqueue(start, 0);

        while (!queue.IsEmpty)
        {
            var currentVertex = queue.Dequeue();

            // Check all neighbors of the current vertex
            foreach (var (neighbor, weight) in vertex[currentVertex])
            {
                // Calculate the new distance
                double newDistance = distances[currentVertex] + weight;

                // Update distance if newDistance is shorter
                if (newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                    queue.Enqueue(neighbor, newDistance);
                }
            }
        }

        return distances;
    }
}

// Helper class for priority queue
public class PriorityQueue<T>
    {
        private SortedDictionary<double, Queue<T>> dict;

        public PriorityQueue()
        {
            dict = new SortedDictionary<double, Queue<T>>();
        }

        public bool IsEmpty => dict.Count == 0;

        public void Enqueue(T item, double priority)
        {
            if (!dict.ContainsKey(priority))
            {
                dict[priority] = new Queue<T>();
            }
            dict[priority].Enqueue(item);
        }

        public T Dequeue()
        {
            var pair = dict.First();
            var item = pair.Value.Dequeue();
            if (pair.Value.Count == 0)
            {
                dict.Remove(pair.Key);
            }
            return item;
        }
    }


