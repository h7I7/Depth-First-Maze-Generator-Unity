using UnityEngine;
using System.Collections;

public abstract class MovingObject : MonoBehaviour
{

    [SerializeField]
    private float _moveTime = 0.1f;
    public float MoveTime
    {
        get { return _moveTime; }
        set { _moveTime = value; }
    }
     
    [SerializeField]
    private LayerMask _blockingLayer;
    public LayerMask blockingLayer
    {
        get { return _blockingLayer; }
        set { _blockingLayer = value; }
    }

    private BoxCollider2D _boxCollider;
    private Rigidbody2D _rigidBody;
    private float _inverseMoveTime;

    //Initialisation function
    protected virtual void Start()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _inverseMoveTime = 1f / MoveTime;
    }

    protected bool Move(int a_xDir, int a_yDir, out RaycastHit2D hit)
    {
        Vector2 start = transform.position;
        Vector2 end = start + new Vector2(a_xDir, a_yDir);

        _boxCollider.enabled = false;
        hit = Physics2D.Linecast(start, end, _blockingLayer);
        _boxCollider.enabled = true;

        if (hit.transform == null)
        {
            StartCoroutine(SmoothMovement(end));
            return true;
        }
        return false;
    }

    protected IEnumerator SmoothMovement(Vector3 a_end)
    {
        float sqrtRemainingDistance = (transform.position - a_end).sqrMagnitude;

        while (sqrtRemainingDistance > float.Epsilon)
        {
            Vector3 newPosition = Vector3.MoveTowards(_rigidBody.position, a_end, _inverseMoveTime * Time.deltaTime);
            _rigidBody.MovePosition(newPosition);
            sqrtRemainingDistance = (transform.position - a_end).sqrMagnitude;
            yield return null;
        }
        OnFinishedMove();
    }

    protected virtual void AttemptMove<T>(int a_xDir, int a_yDir)
        where T : Component
    {
        RaycastHit2D hit;
        bool canMove = Move(a_xDir, a_yDir, out hit);

        if (hit.transform == null)
        {
            return;
        }

        T hitComponent = hit.transform.GetComponent<T>();
        //If canMove is false and hitComponent is not equal to null, meaning MovingObject is blocked and has hit something it can interact with.
        if (!canMove && hitComponent != null)
        {
            //Call the OnCantMove function and pass it hitComponent as a parameter.
            OnCantMove(hitComponent);
        }

    }

    protected abstract void OnFinishedMove();

    protected abstract void OnCantMove<T>(T a_component)
        where T : Component;


}
