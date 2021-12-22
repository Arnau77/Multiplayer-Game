using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{
    public Animator animator;
    public CharacterController controller;
    public float speed;
    private Vector3 dir;
    public int health;
    private bool beingHit = false;

    enum STATE
    {
        SEARCH_STATE,
        IDLE,
        WALK,
        HIT
    }
    enum INPUT_STATE
    {
        IN_IDLE,
        IN_IDLE_END,
        IN_WALK,
        IN_WALK_END,
        IN_HIT,
        IN_HIT_END
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
        Debug.Log(beingHit.ToString());

    }

    void ProcessInternalInput()
    {
        if (!beingHit)
        {
            dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));

            if (dir == Vector3.zero) inputList.Add(INPUT_STATE.IN_IDLE);
            else inputList.Add(INPUT_STATE.IN_WALK);
        }

    }

    void ProcessExternalInput()
    {
        if (Input.GetKey(KeyCode.Y))
        {
            inputList.Add(INPUT_STATE.IN_HIT);
            beingHit = true;
        }
        if(beingHit && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.8f)
        {
            inputList.Add(INPUT_STATE.IN_IDLE);
            beingHit = false;
        }
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
                        case INPUT_STATE.IN_HIT:
                            currentState = STATE.HIT;
                            break;
                    }
                    break;
                case STATE.WALK:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            break;
                        case INPUT_STATE.IN_HIT:
                            currentState = STATE.HIT;
                            break;
                    }
                    break;
                case STATE.HIT:
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
            case STATE.HIT:
                animator.Play("Hit Reaction");
                break;
        }
    }

}
