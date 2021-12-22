using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{
    public Animator animator;
    public CharacterController controller;
    public float speed;
    private Vector3 dir;

    enum STATE
    {
        SEARCH_STATE,
        IDLE,
        WALK
    }
    enum INPUT_STATE
    {
        IN_IDLE,
        IN_IDLE_END,
        IN_WALK,
        IN_WALK_END
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
        Debug.Log(dir.ToString());

    }

    void ProcessInternalInput()
    {
        dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));

        if (dir == Vector3.zero) inputList.Add(INPUT_STATE.IN_IDLE);
        else inputList.Add(INPUT_STATE.IN_WALK);

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
                        case INPUT_STATE.IN_WALK:
                            currentState = STATE.WALK;
                            break;
                    }
                    break;
                case STATE.WALK:
                    switch (_input)
                    {
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
            
            case STATE.IDLE:
                animator.Play("Fighting Idle");
                break;
            case STATE.WALK:
                animator.Play("Happy Walk");
                controller.Move(dir * speed * Time.deltaTime);
                break;
        }
    }

}
