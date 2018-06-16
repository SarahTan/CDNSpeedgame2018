using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour {

    #region Fields

    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private float minSpeed;
    [SerializeField]
    private float maxSpeed;
    
    [SerializeField]
    private float rotationalSpeed;

    [SerializeField]
    private float minTimeBeforeDirectionChange;
    [SerializeField]
    private float maxTimeBeforeDirectionChange;

    private float currentSpeed;
    private float nextDirectionChangeTime;
    
    private Quaternion targetRotation = Quaternion.identity;

    #endregion

    #region Properties

    private Vector2 Position2D
    {
        get { return new Vector2(transform.position.x, transform.position.y); }
    }

    private Vector2 Right2D
    {
        get { return new Vector2(transform.right.x, transform.right.y); }
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Initialize the NPC with random values so it behaves differently fromt other NPCs
        transform.rotation = GetRandom2DRotation();
        nextDirectionChangeTime = GetNextDirectionChangeTime();

        // TODO: Change speed randomly throughout the game?
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime* rotationalSpeed);
        rb.MovePosition(Position2D + (Right2D * currentSpeed * Time.deltaTime)); 

        if (Time.time > nextDirectionChangeTime)
        {
            targetRotation = GetRandom2DRotation();
            nextDirectionChangeTime = GetNextDirectionChangeTime();
        }
    }

    #endregion

    #region Helpers

    private float GetNextDirectionChangeTime()
    {
        return Time.time + Random.Range(minTimeBeforeDirectionChange, maxTimeBeforeDirectionChange);
    }

    public Quaternion GetRandom2DRotation()
    {
        // TODO: Don't completely randomize it, factor in the edges of the screen and player position too
        return Quaternion.Euler(new Vector3(0f, 0f, Random.Range(0f, 360f)));
    }

    #endregion
}
