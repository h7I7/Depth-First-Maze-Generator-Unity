using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loader : MonoBehaviour {

    // This script executes once at the start of the game
    // It makes sure that there is a game manager object
    // in the game

	public GameObject gameManager;

	void Awake()
	{
		if (GameManager.instance == null)
		{
			Instantiate(gameManager);
		}
	}
}
