using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{
    public Animator animator;
    public CharacterController controller;
    public float speed;

    enum STATE
    {
        SEARCH_STATE,
        IDLE,
        WALK_LEFT,
        WALK_RIGHT,
        WALK_FORWARD,
        WALK_BACK  
    }
    enum INPUT_STATE
    {
        IN_IDLE,
        IN_IDLE_END,
        IN_WALK_LEFT,
        IN_WALK_LEFT_END,
        IN_WALK_RIGHT,
        IN_WALK_RIGHT_END,
        IN_WALK_FORWARD,
        IN_WALK_FORWARD_END,
        IN_WALK_BACK,
        IN_WALK_BACK_END
    }

    private STATE currentState = STATE.IDLE;
    private List<INPUT_STATE> inputList = new List<INPUT_STATE>();


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        ProcessInternalInput();
        ProcessExternalInput();

        ProcessState();
        UpdateState();

        Debug.Log(currentState.ToString());

        if (Input.GetKey(KeyCode.A))
        {
            
        }
    }

    void ProcessInternalInput()
    {
        if (Input.GetKey(KeyCode.A))
        {
            inputList.Add(INPUT_STATE.IN_WALK_LEFT);
        }
        if (Input.GetKey(KeyCode.D))
        {
            inputList.Add(INPUT_STATE.IN_WALK_RIGHT);
        }
        if (Input.GetKey(KeyCode.W))
        {
            inputList.Add(INPUT_STATE.IN_WALK_FORWARD);
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputList.Add(INPUT_STATE.IN_WALK_BACK);
        }
    }

    void ProcessExternalInput()
    {

    }

    void ProcessState()
    {
        while (inputList.Count > 0)
        {
            INPUT_STATE _input = inputList[0];

            switch (currentState)
            {
                case STATE.IDLE:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_WALK_LEFT:
                            currentState = STATE.WALK_LEFT;
                            break;
                        case INPUT_STATE.IN_WALK_RIGHT:
                            currentState = STATE.WALK_RIGHT;
                            break;
                        case INPUT_STATE.IN_WALK_FORWARD:
                            currentState = STATE.WALK_FORWARD;
                            break;
                        case INPUT_STATE.IN_WALK_BACK:
                            currentState = STATE.WALK_BACK;
                            break;
                    }
                    break;
                case STATE.WALK_LEFT:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            break;
                        case INPUT_STATE.IN_WALK_RIGHT:
                            currentState = STATE.WALK_RIGHT;
                            break;
                        case INPUT_STATE.IN_WALK_FORWARD:
                            currentState = STATE.WALK_FORWARD;
                            break;
                        case INPUT_STATE.IN_WALK_BACK:
                            currentState = STATE.WALK_BACK;
                            break;
                    }
                    break;
                case STATE.WALK_RIGHT:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_WALK_LEFT:
                            currentState = STATE.WALK_LEFT;
                            break;
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            break;
                        case INPUT_STATE.IN_WALK_FORWARD:
                            currentState = STATE.WALK_FORWARD;
                            break;
                        case INPUT_STATE.IN_WALK_BACK:
                            currentState = STATE.WALK_BACK;
                            break;
                    }
                    break;
                case STATE.WALK_FORWARD:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_WALK_LEFT:
                            currentState = STATE.WALK_LEFT;
                            break;
                        case INPUT_STATE.IN_WALK_RIGHT:
                            currentState = STATE.WALK_RIGHT;
                            break;
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            break;
                        case INPUT_STATE.IN_WALK_BACK:
                            currentState = STATE.WALK_BACK;
                            break;
                    }
                    break;
                case STATE.WALK_BACK:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_WALK_LEFT:
                            currentState = STATE.WALK_LEFT;
                            break;
                        case INPUT_STATE.IN_WALK_RIGHT:
                            currentState = STATE.WALK_RIGHT;
                            break;
                        case INPUT_STATE.IN_WALK_FORWARD:
                            currentState = STATE.WALK_FORWARD;
                            break;
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            break;
                    }
                    break;
            }
            inputList.RemoveAt(0);
        }
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case STATE.WALK_LEFT:
                UpdateWalk(Vector3.left);
                break;
            case STATE.WALK_RIGHT:
                UpdateWalk(Vector3.right);
                break;
            case STATE.WALK_FORWARD:
                UpdateWalk(Vector3.forward);
                break;
            case STATE.WALK_BACK:
                UpdateWalk(Vector3.back);
                break;
        }
    }

    void UpdateWalk(Vector3 dir)
    {
        animator.Play("Happy Walk");
        controller.Move(dir * speed * Time.deltaTime);
    }

}
