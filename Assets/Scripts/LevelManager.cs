using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour {

    // Serialize field forces unity to show a private variable without properties
    [SerializeField]
    public class Count
    {
        [SerializeField]
        private int m_minimum;
        public int minimum
        {
            get { return m_minimum; }
            set { m_minimum = value; }
        }

        [SerializeField]
        private int m_maximum;
        public int maximum
        {
            get { return m_maximum; }
            set { m_maximum = value; }
        }

        public Count(int a_min, int a_max)
        {
            m_minimum = a_min;
            m_maximum = a_max;
        }
    }  

    // Rows of tiles for the level map
    [SerializeField]
    private int _rows;
    public int rows
    {
        get { return _rows; }
        set { _rows = value; }
    }

    // columns of tiles for the level map
    [SerializeField]
    private int _columns;
    public int columns
    {
        get { return _columns; }
        set { _columns = value; }
    }

    // Used later for all the objects that we will not be able to walk on
    public List<Node> blockingNodes
    {
        get;
        set;
    }

    // Level tile prefab, can be a wall or floor tile
    public GameObject tilePrefab;

    // Arrays for the sprites for the floor and wall tiles
    // The arrays hold  variations of the same sprites
    // so that the level does not look too repetitive
    public Sprite[] floorTileSprites;
    public Sprite[] outerWallSprites;
    public Sprite[] innerWallSprites;

    // the level exit prefab
    public GameObject exit;
    private GameObject _exit;

    // Prefab for the collectable of the level,
    // does not contain a sprite or name
    // which is added later with the help of
    // the GameManager's DBManager
    public GameObject collectablePrefab;

    // A parent for all the level objects
    // Makes the game object hierarchy cleaner
    private Transform boardHolder;
    // Parent for the floor tiles
    private Transform floor;
    // Parent for the wall tiles
    private Transform outerWall;

    // A list for all the empty floor positions
    private List<Vector3> emptyFloorPositions = new List<Vector3>();
    // A list for all the wall tiles
    private List<GameObject> wallTiles = new List<GameObject>();

    // This variable is currently not used
    // Is the delay for an IEnumerator
    // that makes the level generate each
    // tile individually so that you can see
    // it all appear in sequence
    public float mazeGenerationStepDelay = 0.01f;

    // The maze is generated as a tilemap of 1's and 0's
    // After the tilemap is created it can be built with objects
    // This two-dimensional array holds the tilemap
    private int[,] maze;

    // When the script is created we want to set the camera size so that we can clearly see the
    // the player no matter the width and height of the map
    void Awake()
    {
        Camera.main.orthographicSize = (columns / 2) + 0.75f;
    }

    // This function will create a floor tile with a
    // random sprite from the floorTileSprites array
    GameObject CreateFloorTile(float a_x, float a_y)
    {
        // Create a tile object duplicate
        GameObject tile = Instantiate(tilePrefab);
        // Set the parent transform
        tile.transform.SetParent(floor);

        // Set the sprite
        tile.GetComponent<SpriteRenderer>().sprite = floorTileSprites[Random.Range(0, 3)];

        // Set the local position
        tile.transform.localPosition = new Vector3(a_x, a_y, 0);
        // Set the name
        tile.name = "FloorTile(" + a_x + "," + a_y + ")";

        // Add it to the empty floor positions list
        emptyFloorPositions.Add(tile.transform.position);

        // Return the new floor tile game object
        return tile;
    }

    // This function will create a floor tile with a
    // random sprite from the outerWallSprites array
    GameObject CreateWallTile(float a_x, float a_y)
    {
        // Create a tile object duplicate
        GameObject tile = Instantiate(tilePrefab);
        // Set the parent transform
        tile.transform.SetParent(outerWall);

        // Set the sprite
        tile.GetComponent<SpriteRenderer>().sprite = outerWallSprites[Random.Range(0, 1)];

        // Set the position
        tile.transform.localPosition = new Vector3(a_x, a_y, 0);
        // Set the name
        tile.name = "WallTile(" + a_x + "," + a_y + ")";

        // Set the sprite sorting layer
        tile.GetComponent<SpriteRenderer>().sortingLayerName = "Wall";
        // Set the game object layer
        tile.layer = LayerMask.NameToLayer("Blocking");
        // Add the game objects position to the list containing the
        // Nodes that the player and enemies cannot walk through
        blockingNodes.Add(new Node
        {
            x = tile.transform.position.x,
            y = tile.transform.position.y
        });

        // Add the new object the wall tiles list
        wallTiles.Add(tile);

        // Return the new object
        return tile;
    }
    
    // Setup the level
    void BoardSetup()
    {
        // Clear the blocking nodes list
        blockingNodes = new List<Node>();

        // Create a blank gameobject called "board"
        // for parenting all the other objects to
        boardHolder = new GameObject("Board").transform;
        // Set the position of the boardholder so that when we add gameobjects to it
        // they appear in the correct location. We want (0,0) world space to be 
        // (columns/2, rows/2) in the boadholder's space
        boardHolder.position = new Vector3((-rows / 2), (-columns / 2), 0);
        // Set the layer to "Wall"
        boardHolder.gameObject.layer = 8;

        // Create a new object to hold all the floors
        floor = new GameObject("Floor").transform;
        floor.transform.SetParent(boardHolder);
        floor.transform.localPosition = new Vector3(0, 0, 0);
        // Create a new object to hold the walls
        outerWall = new GameObject("OuterWall").transform;
        outerWall.transform.SetParent(boardHolder);
        outerWall.transform.localPosition = new Vector3(0, 0, 0);

        // Seed Random
        Random.InitState(System.DateTime.Now.Millisecond);

        // Generate the maze tilemap
        GenerateMaze();

        // Draw the tilemap with gameobjects
        DrawMaze();

        // Changes the sprites of all the walls that have their bottom edge exposed
        // so that it looks better
        ChangeWalls();
    }

    // We will need to change all the walls that have nothing below them
    // so that they looks like actual walls
    private void ChangeWalls()
    {
        // For each wall in the walltiles list
        foreach (GameObject wall in wallTiles)
        {
            // If this wall is on the bottom edge of the map
            if (wall.transform.localPosition.y < 1)
            {
                // Go to the next iteration of this foreach loop
                continue;
            }

            // Get the position of the current wall
            int x = (int)wall.transform.localPosition.x;
            int y = (int)wall.transform.localPosition.y;

            // If there is no wall beneath the current object
            if (maze[x, y - 1] == 0)
            {
                // Set the wall sprite to the appropriate sprite
                // Either just a wall or a wall with a torch
                if (Random.Range(0, 20) == 0)
                    wall.GetComponent<SpriteRenderer>().sprite = innerWallSprites[0];
                else
                    wall.GetComponent<SpriteRenderer>().sprite = innerWallSprites[1];
            }
        }
    }

    // This functions is simply drawing the level based off the tile map we created
    private void DrawMaze()
    {
        // If the current index of the tile map is 1 then create a wall at that position
        // if it is a 0 then create a floor tile
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (maze[i, j] == 0)
                {
                    CreateFloorTile(i, j);
                }
                else if (maze[i, j] == 1)
                {
                    CreateWallTile(i, j);
                }

            }
        }
    }

    // Generate the maze tilemap
    private int[,] GenerateMaze()
    {
        // Clear/initialize the tilemap array
        maze = new int[rows, columns];

        // Set all the positions in the array to 1 (wall tiles)
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                maze[i, j] = 1;
            }
        }

        // Get a random number inside the bounds of the array
        // to use as a random starting position
        // This position needs to be odd
        int r = Random.Range(0, rows);
        while (r % 2 == 0)
        {
            r = Random.Range(0, rows);
        }

        int c = Random.Range(0, columns);
        while (c % 2 == 0)
        {
            c = Random.Range(0, columns);
        }

        // Set that random position to equal a 0 (a floor tile)
        maze[r, c] = 0;


        // Generate the maze
        recursion(r, c);

        // The map generation works by making a list of directions it can travel in
        // (Up, Down, Left, Right)
        // Then it will randomize the list so they are no longer in that order.
        // E.g. (Down, Right, Up, Left)
        // This randomization needs to be good, if it is not randomized properly then
        // the maze will look very uniform with a lot of long corridors

        // After that we need to try each direction in the list,
        // when the maze generator is checking if it can move it always
        // checks two tiles ahead. Since started with an odd position this means
        // that we will never end up have walkable tiles on the edge of the maze.

        // So the maze generator will check whatever direction is first and if it can move
        // that way without leaving the bounds of the array or intersecting itself then it does.
        // It will then use recursion to call the function again but this time with a new set of
        // random directions and at the new position.
        
        // It will have set the digits of the array between the new position and the old position to 0's
        // (Floor tiles). If the maze generator cannot move anymore then it breaks since it used recursion
        // it will be in the previous tile, it will check the directions again and move if it can or it will
        // break and go back to the previous tile. once the maze is complete and no more moves can be made
        // all of the functions will break and this functions will end.

        return maze;
    }

    // This functions checks to see if it can move in a direction, if it can it will and then call
    // itself on the new position with a new set of directions
    private void recursion(int a_r, int a_c)
    {
        // Get an array of random directions
        int[] randDirs = RandomDirections();

        // Check each direction
        for (int i = 0; i < randDirs.Length; i++)
        {
            switch (randDirs[i])
            {
                // This maths in these case statements are jsut to set the 
                // positions in the tile map between the current position
                // and the new position to 0's (floor tiles)
                case 1: //Left
                    {
                        if (a_r - 2 <= 0)
                            continue;
                        if (maze[a_r - 2, a_c] != 0)
                        {
                            maze[a_r - 2, a_c] = 0;
                            maze[a_r - 1, a_c] = 0;
                            recursion(a_r - 2, a_c);
                        }
                    }
                    break;
                case 2: //Up
                    {
                        if (a_c + 2 >= columns - 1)
                            continue;
                        if (maze[a_r, a_c + 2] != 0)
                        {
                            maze[a_r, a_c + 2] = 0;
                            maze[a_r, a_c + 1] = 0;
                            recursion(a_r, a_c + 2);
                        }
                    }
                    break;
                case 3: //Right
                    {
                        if (a_r + 2 >= rows - 1)
                            continue;
                        if (maze[a_r + 2, a_c] != 0)
                        {
                            maze[a_r + 2, a_c] = 0;
                            maze[a_r + 1, a_c] = 0;
                            recursion(a_r + 2, a_c);
                        }
                    }
                    break;
                case 4: //Down
                    {
                        if (a_c - 2 <= 0)
                            continue;
                        if (maze[a_r, a_c - 2] != 0)
                        {
                            maze[a_r, a_c - 2] = 0;
                            maze[a_r, a_c - 1] = 0;
                            recursion(a_r, a_c - 2);
                        }
                    }
                    break;
            }
        }
    }

    // Randomizes an array of directions
    private int[] RandomDirections()
    {
        // Create a new list for the directions
        List<int> randoms = new List<int>();
        // add the directions (1-4) into the list
        for (int i = 0; i < 4; i++)
        {
            randoms.Add(i + 1);
        }

        // Randomise the list
        randoms = RandomizeList(randoms);

        // Return the list as an array
        return randoms.ToArray();
    }

    // Randomizes the directions list
    private List<int> RandomizeList(List<int> a_list)
    {
        // Creates a temporary number for swapping directions
        // The same result could be achieved via the infamous
        // XOR number switch but this method makes more sense
        // to read and we aren't really pressed for memory space
        int temp;

        // Swap around the list contents
        for (int i = 0; i < a_list.Count; i++)
        {
            // Choose a index from the list
            int rnd = Random.Range(0, a_list.Count);
            // Set temp to the number contained in that index and swap the numbers
            temp = a_list[rnd];
            a_list[rnd] = a_list[i];
            a_list[i] = temp;
        }

        // This method of randomizing the list might be more effective if we
        // disable the swapping of the same index twice, however the list is
        // so short I don't think it matters that much
        // If we wanted to implement this we could change
        // int rnd = Random.Range(0, a_list.Count);
        // into
        // int rnd = Random.Range(i, a_list.Count);

        return a_list;
    }

    // This function gets a random empty floor position
    public Vector3 RandomTileLocation()
    {
        // Get a random index from the emptyFloorPositions list
        int randomIndex = Random.Range(0, emptyFloorPositions.Count);
        // Get the position as a Vector3
        Vector3 randomPosition = emptyFloorPositions[randomIndex];
        // Remove that position from the list
        emptyFloorPositions.RemoveAt(randomIndex);
        // Return the position
        return randomPosition;
    }

    // This function places the exit object in an empty floor position
    void LayoutExit()
    {
        Vector3 randomPosition = RandomTileLocation();
        _exit = Instantiate(exit, randomPosition, Quaternion.identity);
    }

    // This functions places a number of items in the map in empty floor positions
    public void PlacePickup(int a_count, string a_name, string a_resourcePath)
    {
        // An empty transform for the item parent
        Transform pickups;
        // A parent transform for the item objects to be placed into
        Transform puObjects = boardHolder.transform.Find("PickUps");

        // If the transform we will use as a parent is found then create an object
        // to use as a parent
        // If it is found then set that object transform to pickups
        if (puObjects == null)
        {
            pickups = new GameObject("PickUps").transform;
            pickups.transform.SetParent(boardHolder);
        }
        else
        {
            pickups = puObjects.transform;
        }

        // Create all the item objects we need
        for (int i = 0; i < a_count; ++i)
        {
            // Get a random empty position for the item
            Vector3 randomPosition = RandomTileLocation();
            // Get the object sprite
            Texture2D pickupTexture = Resources.Load<Texture2D>(a_resourcePath);
            // Create the sprite for the object
            Sprite pickupSprite = Sprite.Create(pickupTexture, new Rect(0, 0, pickupTexture.width, pickupTexture.height), new Vector2(0.5f, 0.5f), 256f);

            // If the sprite was successfully created then create the object
            if (pickupSprite != null)
            {
                // Create a duplicate of the pickup item prefab
                GameObject _pickup = Instantiate(collectablePrefab, randomPosition, Quaternion.identity) as GameObject;

                // Set the name
                _pickup.name = a_name;
                // Set the sprite
                _pickup.GetComponent<SpriteRenderer>().sprite = pickupSprite;
                // Set the sorting layer
                _pickup.GetComponent<SpriteRenderer>().sortingLayerName = "Collectable";
                // Set the object layer
                _pickup.layer = LayerMask.NameToLayer("Collectable");
                // Set the parent
                _pickup.transform.SetParent(pickups);
            }
        }
    }

    // Setup the text objects used for health, combat status and death status
    private void TextSetup()
    {
        // Create the objects
        GameObject textGameObject = GameObject.FindWithTag("StatusText");
        Text _statusText = textGameObject.GetComponent<Text>();

        // The death text object is currently not being used as it causes some
        // bugs, as this project is just a demonstration of map generation and 
        // path finding I thought it wouldn't matter much if I left out some text
        // that tells you when you die
        //textGameObject = GameObject.FindWithTag("DeathText");
        //Text _deathText = textGameObject.GetComponent<Text>();

        textGameObject = GameObject.FindWithTag("HealthText");
        Text _healthText = textGameObject.GetComponent<Text>();

        // Set the text for the objects
        _statusText.text = "";
        //_deathText.text = "";
        _healthText.text = "";
    }

    // Create the level
    public void SetUpScene(int a_iLevel)
    {
        TextSetup();
        BoardSetup();
        LayoutExit();
    }

    // Destroy the level
    public void DestroyScene()
    {
        // Destroy all the objects in the map
        foreach(Transform tx in boardHolder.transform)
        {
            Destroy(tx.gameObject);
        }

        // Destroy all the walls too
        foreach(GameObject wall in wallTiles)
        {
            Destroy(wall);
        }
        // Clear the lists
        wallTiles.Clear();

        emptyFloorPositions.Clear();

        // Destroy the boardholder game object
        Destroy(boardHolder.gameObject);
        // Destroy the level exit
        Destroy(_exit);
    }

}
