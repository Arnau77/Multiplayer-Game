using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{
    public Animator animator;
    public CharacterController controller;
    public float speed;
    private Vector3 dir;
    private bool A;
    private bool D;
    public int health;
    private bool beingHit = false;
    public NewClient client;
    private bool attack = false;

    [Header("Attack Info")]
    public Transform castDamagePoint;
    public float hitRadius;

    public bool canMove = true;
    enum STATE
    {
        SEARCH_STATE,
        IDLE,
        WALK,
        HIT,
        ATTACK
    }
    enum INPUT_STATE
    {
        IN_IDLE,
        IN_IDLE_END,
        IN_WALK,
        IN_WALK_END,
        IN_HIT,
        IN_HIT_END,
        IN_ATTACK
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
        if (!canMove)
            return;
        ProcessInternalInput();
        ProcessExternalInput();

        ProcessState();
        UpdateState();

        Debug.Log(currentState.ToString());
        Debug.Log(beingHit.ToString());

    }

    void ProcessInternalInput()
    {

        //Send messages to client
        A = Input.GetAxisRaw("Horizontal") < 0 ? true : false;
        D = Input.GetAxisRaw("Horizontal") > 0 ? true : false;

        Debug.Log($"Pressed A: {A} Pressed D: {D}");
        //dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));
        //dir = dir.normalized;
        if (!A && !D)
        {

            //client.SendInputMessageToServer(MessageClass.INPUT.Idle);

            inputList.Add(INPUT_STATE.IN_IDLE);
        }
        else if(!attack)
        {
            if (A)
            {
                //client.SendInputMessageToServer(MessageClass.INPUT.A);
                Walk(MessageClass.INPUT.A);
            }
            else if (D)
            {
                Walk(MessageClass.INPUT.D);
                //client.SendInputMessageToServer(MessageClass.INPUT.D);

            }
            //inputList.Add(INPUT_STATE.IN_WALK);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Attack();
        }

        //if (!beingHit)
        //{
        //    dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));
        //    dir = dir.normalized;
        //    if (dir == Vector3.zero) inputList.Add(INPUT_STATE.IN_IDLE);
        //    else inputList.Add(INPUT_STATE.IN_WALK);
        //}

    }

    void ProcessExternalInput()
    {
        if (Input.GetKeyDown(KeyCode.Y)/* || attack*/)
        {
            //inputList.Add(INPUT_STATE.IN_HIT);
            //beingHit = true;
            //if (!attack)
            //{
            //    client.SendInputMessageToServer(MessageClass.INPUT.Attack);
            //}
            //attack = false;
            //client.SendInputMessageToServer(MessageClass.INPUT.Attack);
        }
        //else if (attack)
        //{
        //    inputList.Add(INPUT_STATE.IN_HIT);
        //    beingHit = true;
        //    attack = false;
        //    Debug.Log("ATACK YES!!!!!!!!!!!!!!!!!!");
        //}
        if (beingHit && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.8f)
        {
            //inputList.Add(INPUT_STATE.IN_IDLE);
            beingHit = false;
        }
        if(attack &&animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9)
        {
            attack = false;
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
                        case INPUT_STATE.IN_ATTACK:
                            currentState = STATE.ATTACK;
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
                        case INPUT_STATE.IN_ATTACK:
                            currentState = STATE.ATTACK;
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
                case STATE.ATTACK:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_IDLE:
                            if (attack)
                            {
                                return;
                            }
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
                animator.SetBool("A", false);
                animator.SetBool("D", false);
                break;
            case STATE.WALK:
                animator.SetBool("A",A);
                animator.SetBool("D",D);
                controller.Move(dir.normalized * speed * Time.deltaTime);
                break;
            case STATE.HIT:
                animator.Play("Hit Reaction");
                break;
            case STATE.ATTACK:

                inputList.Add(INPUT_STATE.IN_IDLE);
                break;
        }
    }

    public void Attack()
    {
        attack = true;
        Debug.LogWarning(System.DateTime.Now.Millisecond);
        //animator.SetTrigger("Punch");
        inputList.Add(INPUT_STATE.IN_ATTACK);
        animator.SetTrigger("Punch");
    }

    public void Walk(MessageClass.INPUT input)
    {
        if(currentState != STATE.WALK)
        {
            inputList.Add(INPUT_STATE.IN_WALK);
        }

        if(input == MessageClass.INPUT.A)
        {
            dir = new Vector3(-1, 0, 0);
        }
        else if (input == MessageClass.INPUT.D)
        {
            dir = new Vector3(1, 0, 0);
        }
    }

    public void ReceiveDamage()
    {
        health -= 50;
        if(health <= 0)
        {
            health = 0;
            Debug.Log("Died");
        }
        animator.SetTrigger("HeadHit");
    }

    public void CheckDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(castDamagePoint.position, hitRadius);

        foreach (Collider c in colliders)
        {
            if(c.gameObject.TryGetComponent(out CharacterScript character))
            {
                character.ReceiveDamage();
                Debug.Log("Hitted");
                StartCoroutine(PushedBack(character));
            }
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(castDamagePoint.position,hitRadius);
    }

    IEnumerator PushedBack(CharacterScript character)
    {
        float time = 0.05f;
        float impulse = 0.1f;
        while(time > 0)
        {
            time -= Time.deltaTime;
            character.controller.Move(transform.forward * impulse);
            yield return null;
        }
    }

}
