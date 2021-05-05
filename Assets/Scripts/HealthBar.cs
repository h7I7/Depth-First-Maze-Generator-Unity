using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HealthBar : MonoBehaviour {

    // A variable to hold the game manager
    private GameManager manager;

    // The healthicon UI image objects
    private Image[] healthIcons;
    
    // Srites for the hearts
    private Sprite fullHeart;
    private Sprite emptyHeart;

    // The player health
    private int _playerHealth = 0;

    // Every time the player health is set it will update hearts
    public int PlayerHealth
    {
        get { return _playerHealth; }
        set
        {
            if (value >= 0 && value <= 100)
            {
                _playerHealth = value;
                UpdateHearts();
            }
        }
    }

    private void Start()
    {
        // Get a reference to the GameManager
        manager = FindObjectOfType<GameManager>();

        // Get a list of health icons
        healthIcons = GetComponentsInChildren<Image>();

        // Sort list of health icons (using LINQ) by their X coorinate
        healthIcons = healthIcons.OrderBy(p => p.transform.position.x).ToArray();

        // Run a query to retrieve thr tow where name='Full', get the first row (should be one only), extract the paremeter "path" as a string
        // and use that as the path to load a texture2D
        Texture2D fullHeartTexture = Resources.Load<Texture2D>(
            manager.DBManager.ExecuteQuery("SELECT name, path FROM HUDIcons WHERE name='Full'")[0]["path"] as string);
        // Same for name='Empty'
        Texture2D emptyHeartTexture = Resources.Load<Texture2D>(
            manager.DBManager.ExecuteQuery("SELECT name, path FROM HUDIcons WHERE name='Empty'")[0]["path"] as string);

        // Create a sprite from a Texture2D
        fullHeart = Sprite.Create(
            fullHeartTexture,
            new Rect(0, 0, fullHeartTexture.width, fullHeartTexture.height),
            new Vector2(0.0f, 0.0f),
            256.0f);

        // Same
        emptyHeart = Sprite.Create(
            emptyHeartTexture,
            new Rect(0, 0, emptyHeartTexture.width, emptyHeartTexture.height),
            new Vector2(0.0f, 0.0f),
            256.0f);
    }

    private void UpdateHearts()
    {
        // Make all hearts empty
        for (int i = 0; i < 5; i++)
        {
            healthIcons[i].sprite = emptyHeart;
        }

        // Then fill the correct ones
        for (int i = 0; i < (_playerHealth * 0.05); i++)
        {
            healthIcons[i].sprite = fullHeart;
        }
    }

}
