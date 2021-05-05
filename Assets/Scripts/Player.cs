using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class Player : MovingObject
{
    // The delay for the player attack
    [SerializeField]
    private float _attackDelay;

    private float _timeOfAttack;

    // The power of the player attack
    [SerializeField]
    private float _attackPower;
    public float AttackPower
    {
        get { return _attackPower; }
        set { _attackPower = value; }
    }

    // The power of the player defense
    [SerializeField]
    private float _defencePower;
    public float DefencePower
    {
        get { return _defencePower; }
        set { _defencePower = value; }
    }

    // The player health
    [SerializeField]
    private float _health;
    public float Health
    {
        get { return _health; }
        set { _health = value; }
    }

    // The game text objects (combat status, death status and health)
    private Text _statusText;
    //private Text _deathText;
    private Text _healthText;

    // The game manager object
    public GameManager manager
    {
        get;
        set;
    }

    // A bool to allow the player to move or not
    private bool _allowMove = true;
    public bool AllowMove
    {
        set { _allowMove = value; }
        get { return _allowMove; }
    }

    // A health bar for the the player
    private HealthBar _healthBar;

    // Use this for initialization
    protected override void Start()
    {
        // Find the healthbar object (in the canvas)
        _healthBar = GameObject.FindWithTag("Healthbar").GetComponent<HealthBar>();

        // Find all the text status objects
        GameObject textGameObject = GameObject.FindWithTag("StatusText");
        _statusText = textGameObject.GetComponent<Text>();

        //textGameObject = GameObject.FindWithTag("DeathText");
        //_deathText = textGameObject.GetComponent<Text>();

        textGameObject = GameObject.FindWithTag("HealthText");
        _healthText = textGameObject.GetComponent<Text>();

        // Find the game manager
        manager = FindObjectOfType<GameManager>();
        // Tell the game manager that this is the player
        manager.player = this;

        // Start the base class which controls player movement
        // The start function just initializes some variables in the class
        base.Start();
    }

    // Attempt to move using the movingObject abstract class
    protected override void AttemptMove<T>(int a_xDir, int a_yDir)
    {
        AllowMove = false;
        base.AttemptMove<T>(a_xDir, a_yDir);

    }
   

    // Update is called once per frame
    void Update()
    {
        // Get the player position
        float x = transform.position.x;
        float y = transform.position.y;

        // Get the width and height of the current level
        float rows = manager.levelScript.rows * 0.27f;
        float columns = manager.levelScript.columns * 0.265f;

        // x, y, rows and columns are used for setting the camera
        // position on the game.
        // The numbers that rows and columns are multiplied by are
        // are seemingly random however they are the scale that we
        // need to stop the camera from moving into position where we
        // can see out of the map

        // Honestly this only works for a map size of 31 by 17
        // if that map is bigger than that you will start to lose some
        // tiles. If its smaller you will start to see outside the map

        // Make sure the camera cannot see outside of the map 
        if (x > rows)
        {
            x = rows;
        }
        else if (x < -rows)
        {
            x = -rows;
        }

        if (y > columns)
        {
            y = columns;
        }
        else if (y < -columns)
        {
            y = -columns;
        }

        // Set the camera position, it smoothly tracks the player
        Camera.main.transform.position = new Vector3(Mathf.Lerp(Camera.main.transform.position.x, x, Time.deltaTime * 5f), Mathf.Lerp(Camera.main.transform.position.y, y, Time.deltaTime * 5f), -10);
        // Set the camera size
        Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, 4f, Time.deltaTime * 10);

        // Update the player health on the health bar object
        // This will change the amount of hearts displayed in the top left of the canvas
        _healthBar.PlayerHealth = (int)_health;

        // Update the health text
        _healthText.text = "HP: " + Health;

        // If we cannot move then we need to break
        if (!AllowMove)
        {
            return;
        }

        /*
        if (Health <= 0)
        {
            _deathText.text = "You Died!";
            return;
        }*/

        int iHorizontal = 0;
        int iVertical = 0;
        
        // Get the horizontal and vertical inputs
        // however we only want to be able to move horizontally OR
        // vertically, not both
        iHorizontal = (int)Input.GetAxisRaw("Horizontal");
        if (iHorizontal == 0)
        {
            iVertical = (int)Input.GetAxisRaw("Vertical");
        }

        // Attempt to move in the direction we have from the player input
        if (iHorizontal != 0 || iVertical != 0)
        {
            AttemptMove<Tile>(iHorizontal, iVertical);
        }

        // Update the enemies
        foreach (Enemy enemy in manager.enemyList)
        {
            enemy.TryMove();
        }

    }

    // If we collide with the exit object then we need to reset the level
    private void OnTriggerEnter2D(Collider2D a_object)
    {
        if (a_object.tag == "exit")
        {

            GameManager.instance.Restart();
        }
        
    }

    // When we finish moving we need to let all the enemies move and set our allowMove to true
    protected override void OnFinishedMove()
    {
        foreach(Enemy enemy in manager.enemyList)
        {
            enemy.AllowMove = true;
        }
        AllowMove = true;
    }

    // If we can't move
    protected override void OnCantMove<T>(T a_component)
    {
        // If we tried to move into an enemy then we need to engage in combat
        if (a_component.gameObject.CompareTag("Enemy"))
        {
            // Get the enemy we tried to move into
            Enemy enemy = a_component.gameObject.GetComponent<Enemy>();

            // If we can attack based on the time since the last attack we made
            if ((Time.realtimeSinceStartup - _timeOfAttack) > _attackDelay)
            {
                // Set the time of attack
                _timeOfAttack = Time.realtimeSinceStartup;

                // Do combat stuff based off a 'Dice Roll'
                int diceRoll = (int)(Random.value * 5.0f) + 1;
                if (AttackPower + diceRoll > enemy.DefencePower)
                {
                    enemy.Health -= AttackPower + diceRoll;
                    _statusText.text = "Hit enemy for " + (AttackPower + diceRoll) + " damage!" +
                        "\nEnemy health: " + enemy.Health;

                    if (enemy.Health <= 0)
                    {
                        _statusText.text += "\nEnemy Died!";
                        manager.enemyList.Remove(enemy);
                        Destroy(a_component.gameObject);
                        AllowMove = true;
                        return;
                    }
                }
                else
                {
                    _statusText.text = "\nMissed enemy!";
                }

                // Allow the enemy to attack the player too
                diceRoll = (int)(Random.value * 5.0f) + 1;
                if (enemy.AttackPower + diceRoll > DefencePower)
                {
                    Health -= enemy.AttackPower + diceRoll;
                    _statusText.text += "\nEnemy counter attacked for " + (enemy.AttackPower + diceRoll) + " damage!";
                }
                else
                {
                    _statusText.text += "\nEnemy missed!";
                }
            }
        }
        // We can now move again after attacking
        AllowMove = true;
    }
}
