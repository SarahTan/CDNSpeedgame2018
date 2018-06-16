using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Singleton<Player> {

    #region Fields

    [SerializeField]
    private float maxSpeed;

    #endregion

    #region Properties

    // Cache the main camera for perf reasons since we need to access it every frame
    // Unity calls Object.FindObjectWithTag("MainCamera") *every single time* you access Camera.main, which is ridiculous
    private Camera _mainCamera = null;
    private Camera MainCamera
    {
        get
        {
            if(_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    private Vector2 TargetPosition
    {
        get
        {
            // Clamp it to ensure the position is always within the screen
            Vector2 clampedMousePos = new Vector2(Mathf.Clamp(Input.mousePosition.x, 0, Screen.width), Mathf.Clamp(Input.mousePosition.y, 0, Screen.height));
            return MainCamera.ScreenToWorldPoint(clampedMousePos);
        }
    }

    #endregion
    
    private void Update()
    {
        transform.position = Vector2.Lerp(transform.position, TargetPosition, Time.deltaTime * maxSpeed);
    }
}
