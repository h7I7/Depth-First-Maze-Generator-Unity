using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour {

    // Holds the game manager object so there can only be one
	public static GameManager instance = null;

    // Holds the levelManager script, used for all the level generation
	public LevelManager levelScript = null;

    // Holds the player
	private Player _player;
	public Player player
	{
		get { return _player; }
		set { _player = value; }
	}

    // Holds a prefab of the player
	public GameObject playerPrefab;

    // Hold a prefab of the enemy
	[SerializeField]
	private GameObject enemyPrefab;

    // A number of enemies that can be changed in the inspector
	[SerializeField]
	private int numberEnemies;

    // A list to hold all the enemies
	private List<Enemy> _enemyList = new List<Enemy>();
	public List<Enemy> enemyList
	{
		get { return _enemyList; }
		set { _enemyList = value; }
	}

    // When the object containing this script is created
	private void Awake()
	{
        // Check if there is already an instance of this object type (game manager)
        if (instance == null)
		{
            // If there isn't then this is the instance
			instance = this;
		}
		else
		{
            // If there is already on object of this type (game manager) destroy this object
			Destroy(gameObject);
		}

		DontDestroyOnLoad(gameObject);

		InitGame();
	}

    // These variables will be used for accessing the DBManager script so that we can use
    // Databases for adding items into the game
	private DatabaseManager _dbManager;
	public DatabaseManager DBManager
	{
		get { return _dbManager; }
	}

    // Initialise the game objects
	void InitGame()
	{
        // Create a DBManager
		_dbManager = DatabaseManager.Create();
        // Load the Collectables database file
		_dbManager.LoadDatabase("Collectables.db");
		_dbManager.OpenConnection("Collectables.db");
        // Access the collectables table
		DataTable collectables = _dbManager.ExecuteQuery("SELECT name, spriteTexture, description FROM collectables");

        // Get the levelManager attached to this object
		levelScript = GetComponent<LevelManager>();
        // Set up the level
		levelScript.SetUpScene(1);

        // Create all the enemies that we need
		for (int i = 0; i < numberEnemies; i++)
		{
            // Get a location for the enemy
			Vector3 enemyLocation = levelScript.RandomTileLocation();
            // Create a clone of the enemy prefab
			Instantiate(enemyPrefab, enemyLocation, Quaternion.identity);
		}

        // For each item in the collectables table of the collectables database file
		foreach(DataRow row in collectables.Rows)
		{
            // Get the name
			string itemName = row["name"] as string;
            // Get the texture path (Should lead to "Assets/Resources/PATH")
			string itemTexture = row["spriteTexture"] as string;
            // Create 2 of the item
			levelScript.PlacePickup(2, itemName, itemTexture);
		}

        // Get a random position from all the possible walkable tiles            
        Vector3 randomPosition = levelScript.RandomTileLocation();

        // Place the player in the random position
        GameObject p = Instantiate(playerPrefab, randomPosition, Quaternion.identity);
        // Store the player for later use
		_player = p.GetComponent<Player>();
	}

	// Update is called once per frame
	void Update () {
        // If the R key is pressed then delete the level and rebuild it
		if(Input.GetKeyUp(KeyCode.R))
		{
			Restart();
		}
	}

    // This function will delete the current level and rebuild it
	public void Restart()
	{
        // Delete the current objects in the scene
		levelScript.DestroyScene();
        // Set up the scene again
		levelScript.SetUpScene(1);

        // Place all the items from the database file
		DataTable collectables = _dbManager.ExecuteQuery("SELECT name, spriteTexture, description FROM collectables");

		foreach (DataRow row in collectables.Rows)
		{
			string itemName = row["name"] as string;
			string itemTexture = row["spriteTexture"] as string;
			levelScript.PlacePickup(2, itemName, itemTexture);
		}

        // Destroy the player object so we can create a new one in a different position
		Destroy(_player.gameObject);
		_player = null;

        // Destroy enemies
		foreach(Enemy enemy in enemyList.Where(p=>p!=null))
		{
			Destroy(enemy.transform.gameObject);
		}

        // Clear the enemylist so we are not referencing any destroyed enemy objects
		enemyList = new List<Enemy>();

        // Create enemies
		for (int i = 0; i < numberEnemies; i++)
		{
			Vector3 enemyLocation = levelScript.RandomTileLocation();
			GameObject enemyObject = Instantiate(enemyPrefab, enemyLocation, Quaternion.identity) as GameObject;
			enemyList.Add(enemyObject.gameObject.GetComponent<Enemy>());
		}
		
        // Recreate and place the player
		Vector3 randomPosition = levelScript.RandomTileLocation();
		GameObject playerObject = Instantiate(playerPrefab, randomPosition, Quaternion.identity);
		_player = playerObject.GetComponent<Player>();

	}
}
