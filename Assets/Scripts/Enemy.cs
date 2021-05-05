using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MovingObject
{
    // Attack power of the enemy
    [SerializeField]
    private float _attackPower;
    public float AttackPower
    {
        get { return _attackPower; }
        set { _attackPower = value; }
    }

    // Defense power of the enemy
    [SerializeField]
    private float _defencePower;
    public float DefencePower
    {
        get { return _defencePower; }
        set { _defencePower = value; }
    }

    // Enemy health
    [SerializeField]
    private float _health;
    public float Health
    {
        get { return _health; }
        set { _health = value; }
    }

    // Bool for allowing the enemy to move
    private bool _allowMove = false;
    public bool AllowMove
    {
        set { _allowMove = value; }
        get { return _allowMove; }
    }

    // A variable to hold a reference to the game manager
    public GameManager manager
    {
        get;
        set;
    }

    // Lists for xCoords and YCoords
    private List<float> xCoords = new List<float>();
    private List<float> yCoords = new List<float>();

    // We can draw the path of the enemy with gizmos if this is true
    public bool DrawPath = false;

    // A list for the gizmos
    private List<Vector3> gizmos = new List<Vector3>();
    
    
    protected override void Start()
    {
        // Get the manager object
        manager = FindObjectOfType<GameManager>();
        // Add this object to the game managers list of enemies
        manager.enemyList.Add(this);

        // Get the width and height of the level
        int columns = manager.levelScript.columns;
        int rows = manager.levelScript.rows;

        // Each grid position is 1 unit big, so we can set the positions for the
        // tile to (columns/2, rows/2)
        
        // Add all the yCoords of the map into the yCoords list
        for (float i = 0, j = (columns - 1) / 2; i < columns; i++, j--)
        {
            yCoords.Add(j);
        }
           
        // Add all the xCoords of the map into the yCoords list
        for (float i = 0, j = (rows - 1) / 2; i < rows; i++, j--)
        {
            xCoords.Add(j);
        }
         
        base.Start();
    }

    // Attempt to move using the movingObject abstract class
    protected override void AttemptMove<T>(int a_xDir, int a_yDir)
    {
        base.AttemptMove<T>(a_xDir, a_yDir);
        AllowMove = false;
    }

    public void TryMove()
    {
        // If the enemy cannot move then return
        if (!AllowMove)
        {
            return;
        }

        int iHorizontal, iVertical;

        //AI Method here

        //MoveTowards(out iHorizontal, out iVertical,
        //                  manager.player.transform.position.x,
        //                  manager.player.transform.position.y);
        //DjikstrasMovement(out iHorizontal, out iVertical);
        //GreedyBFS(out iHorizontal, out iVertical);
        AStar(out iHorizontal, out iVertical);

        if (iHorizontal != 0 || iVertical != 0)
        {
            AttemptMove<Tile>(iHorizontal, iVertical);
        }
    }

    // Calculate movement based off the AStar algorithm
    private void AStar(out int horizontal, out int vertical)
    {
        // Get the dimensions of the grid
        int rows = manager.levelScript.rows;
        int columns = manager.levelScript.columns;

        // Create a list for all the unvisited tiles
        List<Node> unvisitedSet = new List<Node>(rows * columns);

        // The current tile
        Node currentNode = null;
        // The player tile
        Node playerNode = null;
        // The starting tile
        Node startNode = null;

        // Distance from the starting tile
        int distanceFromStart = 0;

        // Initialise our list of nodes in the unvisited set
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                unvisitedSet.Add(new Node
                {
                    x = xCoords[i],
                    y = yCoords[j],
                    DistanceFromStart = float.PositiveInfinity,
                    DistanceToTarget = float.PositiveInfinity
                });
            }
        }

        // Find start node & player node & nodes to be removed
        // (Nodes we can't move to in this turn)
        foreach (Node n in unvisitedSet)
        {
            // If the current node in the foreach loop
            // is the node we are on
            if (n.x == transform.position.x &&
                n.y == transform.position.y)
            {
                // Set the nodes and distance
                currentNode = n;
                startNode = n;
                currentNode.DistanceFromStart = 0;
            }

            // If the current node in the foreach loop
            // is the node the player is on
            if (n.x == manager.player.transform.position.x &&
                n.y == manager.player.transform.position.y)
            {
                // Set the node
                playerNode = n;
            }
        }

        // Remove all where cantmove = true
        unvisitedSet.RemoveAll(CantMove);

        currentNode.DistanceFromStart = 0;

        // Loop until we reach our end goal or we cant anymore
        while (currentNode != playerNode && unvisitedSet.Count() != 1)
        {
            distanceFromStart++;
            foreach (Node n in unvisitedSet)
            {
                CheckNeighbours(distanceFromStart, currentNode, n, playerNode);
            }

            unvisitedSet.Remove(currentNode);

            // Set current node to that with the lowest distance from start
            currentNode = unvisitedSet.Aggregate(
                (minItem, nextItem) =>
                minItem.DistanceFromStart + minItem.DistanceToTarget <
                nextItem.DistanceFromStart + nextItem.DistanceToTarget ?
                minItem : nextItem
            );
        }

        // Maps out the path
        if (currentNode == playerNode && currentNode.prevNode != null)
        {
            gizmos.Clear();
            // We have a path - so trace it back and identify the first move on it
            while (currentNode.prevNode != startNode)
            {
                if (DrawPath)
                {
                    gizmos.Add(new Vector3(currentNode.x, currentNode.y, 0));
                }
                currentNode = currentNode.prevNode;
            }
            MoveTowards(out horizontal, out vertical, currentNode.x, currentNode.y);
            return;
        }
        else
        {
            // No path available, may as well stay still
            horizontal = 0;
            vertical = 0;
            return;
        }
    }


    private void OnDrawGizmos()
    {
        if (DrawPath)
        {
            foreach (Vector3 vec in gizmos)
            {
                Gizmos.DrawSphere(vec, 0.4f);
            }
        }
    }

    // Calculate movement based off the Greedy Best First Search algorithm
    private void GreedyBFS(out int horizontal, out int vertical)
    {
        //Get dimensions of grid
        int rows = manager.levelScript.rows;
        int columns = manager.levelScript.columns;

        //List<Node> visitedSet;
        List<Node> unvisitedSet = new List<Node>(rows * columns);
        Node currentNode = null;
        Node playerNode = null;
        Node startNode = null;

        //Initialise our list of nodes
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                unvisitedSet.Add(new Node
                {
                    x = xCoords[j],
                    y = yCoords[i],
                    DistanceFromStart = float.PositiveInfinity,
                    DistanceToTarget = float.PositiveInfinity
                });
            }
        }

        //Find start node & player node & nodes to be removed
        //(Nodes we can't move to in this turn)
        foreach (Node n in unvisitedSet)
        {
            if (n.x == transform.position.x &&
                n.y == transform.position.y)
            {
                currentNode = n;
                startNode = n;
            }
            if (n.x == manager.player.transform.position.x &&
                n.y == manager.player.transform.position.y)
            {
                playerNode = n;
            }
        }

        //Remove all where cantmove = true
        unvisitedSet.RemoveAll(CantMove);

        //Loop until we reach our end goal or we cant anymore
        while (currentNode != playerNode && unvisitedSet.Count() != 1)
        {
            foreach (Node n in unvisitedSet)
            {
                CheckNeighbours(0, currentNode, n, playerNode);
            }

            unvisitedSet.Remove(currentNode);
            //Set current node to that with the lowest distance from start
            currentNode = unvisitedSet.Aggregate(
                (minItem, nextItem) =>
                minItem.DistanceToTarget < nextItem.DistanceToTarget ? minItem : nextItem
            );
        }

        if (currentNode == playerNode && currentNode.prevNode != null)
        {
            //We have a path - so trace it back and identify the first move on it
            while (currentNode.prevNode != startNode)
            {
                currentNode = currentNode.prevNode;
            }
            MoveTowards(out horizontal, out vertical, currentNode.x, currentNode.y);
            return;
        }
        else
        {
            //No path available, may as well stay still
            horizontal = 0;
            vertical = 0;
            return;
        }
    }

    // Calculate movement based off the Djikstra algorithm
    private void DjikstrasMovement(out int horizontal, out int vertical)
    {
        //Get dimensions of grid
        int rows = manager.levelScript.rows;
        int columns = manager.levelScript.columns;

        //List<Node> visitedSet;
        List<Node> unvisitedSet = new List<Node>(rows * columns);
        Node currentNode = null;
        Node playerNode = null;
        Node startNode = null;

        int distanceFromStart = 0;

        //Initialise our list of nodes
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                unvisitedSet.Add(new Node
                {
                    x = xCoords[j],
                    y = yCoords[i],
                    DistanceFromStart = float.PositiveInfinity,
                    DistanceToTarget = float.PositiveInfinity
                });
            }
        }

        //Find start node & player node & nodes to be removed
        //(Nodes we can't move to in this turn)
        foreach (Node n in unvisitedSet)
        {
            if (n.x == transform.position.x &&
                n.y == transform.position.y)
            {
                currentNode = n;
                startNode = n;
                currentNode.DistanceFromStart = 0;
            }
            if (n.x == manager.player.transform.position.x &&
                n.y == manager.player.transform.position.y)
            {
                playerNode = n;
            }
        }

        //Remove all where cantmove = true
        unvisitedSet.RemoveAll(CantMove);

        currentNode.DistanceFromStart = 0;
        //Loop until we reach our end goal or we cant anymore
        while (currentNode != playerNode && unvisitedSet.Count() != 1)
        {
            distanceFromStart++;
            foreach (Node n in unvisitedSet)
            {
                CheckNeighbours(distanceFromStart, currentNode, n, playerNode);
            }

            //Node prevNode = currentNode;
            unvisitedSet.Remove(currentNode);
            //Set current node to that with the lowest distance from start
            currentNode = unvisitedSet.Aggregate(
                (minItem, nextItem) =>
                minItem.DistanceFromStart < nextItem.DistanceFromStart ? minItem : nextItem
            );
            //currentNode.prevNode = prevNode;

        }

        if (currentNode == playerNode && currentNode.prevNode != null)
        {
            //We have a path - so trace it back and identify the first move on it
            while (currentNode.prevNode != startNode)
            {
                currentNode = currentNode.prevNode;
            }
            MoveTowards(out horizontal, out vertical, currentNode.x, currentNode.y);
            return;
        }
        else
        {
            //No path available, may as well stay still
            horizontal = 0;
            vertical = 0;
            return;
        }
    }

    // Updates the distances from the start of the path and to the target
    private void CheckNeighbours(int distanceFromStart, Node currentNode, Node currentNeighbour, Node playerNode)
    {
        // distanceFromStart = the enemy objects distance from the starting point
        // currentNode = enemy
        // currentNeighbour = Tiles in unvisitedSet
        // playerNode = player

        // If the current neighbour is left the current node
        if (currentNeighbour.x == (currentNode.x - 1) &&
            currentNeighbour.y == currentNode.y)
        {
            // Set the distance to the target (player)
            currentNeighbour.DistanceToTarget =
                Mathf.Abs(currentNeighbour.x - playerNode.x) +
                Mathf.Abs(currentNeighbour.y - playerNode.y);

            // Set this neighbour as the previous node of the enemy
            if (currentNeighbour.DistanceFromStart > distanceFromStart)
            {
                currentNeighbour.DistanceFromStart = distanceFromStart;
                currentNeighbour.prevNode = currentNode;
            }
        }

        // If the current neighbour is right the current node
        if (currentNeighbour.x == (currentNode.x + 1) &&
            currentNeighbour.y == currentNode.y)
        {
            currentNeighbour.DistanceToTarget =
                Mathf.Abs(currentNeighbour.x - playerNode.x) +
                Mathf.Abs(currentNeighbour.y - playerNode.y);
            if (currentNeighbour.DistanceFromStart > distanceFromStart)
            {
                currentNeighbour.DistanceFromStart = distanceFromStart;
                currentNeighbour.prevNode = currentNode;
            }
        }

        // If the current neighbour is above the current node
        if (currentNeighbour.x == currentNode.x &&
            currentNeighbour.y == (currentNode.y + 1))
        {
            currentNeighbour.DistanceToTarget =
                Mathf.Abs(currentNeighbour.x - playerNode.x) +
                Mathf.Abs(currentNeighbour.y - playerNode.y);
            if (currentNeighbour.DistanceFromStart > distanceFromStart)
            {
                currentNeighbour.DistanceFromStart = distanceFromStart;
                currentNeighbour.prevNode = currentNode;
            }
        }

        // If the current neighbour is bellow the current node
        if (currentNeighbour.x == currentNode.x &&
            currentNeighbour.y == (currentNode.y - 1))
        {
            currentNeighbour.DistanceToTarget =
                Mathf.Abs(currentNeighbour.x - playerNode.x) +
                Mathf.Abs(currentNeighbour.y - playerNode.y);
            if (currentNeighbour.DistanceFromStart > distanceFromStart)
            {
                currentNeighbour.DistanceFromStart = distanceFromStart;
                currentNeighbour.prevNode = currentNode;
            }
        }
    }

    // Find all the tiles blocked by walls or enemies
    private bool CantMove(Node n)
    {
        foreach (Node node in manager.levelScript.blockingNodes)
        {
            if (n.x == node.x && n.y == node.y)
            {
                return true;
            }
        }

        foreach (Enemy enemy in manager.enemyList)
        {
            if (n.x == enemy.transform.position.x && n.y == enemy.transform.position.y)
            {
                return true;
            }
        }

        return false;
    }

    // Move the enemy object
    private void MoveTowards(out int horizontal, out int vertical, float targetX, float targetY)
    {
        horizontal = 0;
        vertical = 0;

        float xDiff = targetX - transform.position.x;
        float yDiff = targetY - transform.position.y;

        // We don't want to be able to move diagonally
        // So only move on the x or y axis
        if (Mathf.Abs(xDiff) > Mathf.Abs(yDiff))
        {
            if (xDiff < 0)
            {
                horizontal = -1;
            }
            else if (xDiff > 0)
            {
                horizontal = 1;
            }
        }
        else
        {
            if (yDiff < 0)
            {
                vertical = -1;
            }
            else if (yDiff > 0)
            {
                vertical = 1;
            }
        }
    }

    protected override void OnFinishedMove()
    {
        AllowMove = false;
    }

    protected override void OnCantMove<T>(T a_component)
    {
        AllowMove = false;
    }
}
