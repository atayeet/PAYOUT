using UnityEngine;
using UnityEngine.InputSystem;

public class InputActions : MonoBehaviour
{
    InputSystem_Actions input;


    


    private void Awake()
    {
        input = GetComponent<InputSystem_Actions>();
        input.Enable();
    }
    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        

    }

 
}
