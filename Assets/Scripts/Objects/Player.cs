﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Player : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private GameObject pushAbilityPrefab;
    [SerializeField] private ControlScheme controls;
    [SerializeField] private Vector2 spawnPosition;
    [SerializeField] private float groundSpeedMax;
    [SerializeField] private float groundMoveAcceleration;
    [SerializeField] private float groundMoveDeceleration;
    [SerializeField] private float airMoveSpeedMax;
    [SerializeField] private float airMoveAcceleration;
    [SerializeField] private float airMoveDeceleration;
    [SerializeField] private float aerialJumpForce;
    [SerializeField] private float groundedJumpForce;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float hitStunTime;
    [SerializeField] private float pushForce;
    [SerializeField] private float pushDuration;
    [SerializeField] private float pushCooldown;

    private int playerNumber;
    private int maxJumpCount = 2;
    private int currentJumpCount = 0;
    private bool isHitStun = false;
    private Character playerCharacter;
    private GameObject pushAbility;
    private float speedLimit;
    private float speedAcceleration;
    private float speedDeceleration;

    private bool canQueueJump = false;
    private bool jumpQueued = false;
    private bool canCoyoteJump = false;
    private bool isGrounded = false;
    private bool canPush = true;
    private float coyoteTimeStart;
    private float hitStunTimeStart;
    private float currentPushCooldownTime = 0f;
    private Vector2 moveDir;
    private Vector2 playerPosition;


    public Vector2 SpawnPosition { get => spawnPosition; set => spawnPosition = value; }
    public int PlayerNumber { get => playerNumber; set => playerNumber = value; }
    public bool CanQueueJump { get => canQueueJump; set => canQueueJump = value; }
    public Vector2 MoveDirection { get => moveDir; }
    public float GroundSpeedMax { get => groundSpeedMax; }
    public float GroundMoveAcceleration { get => GroundMoveAcceleration; }
    public float GroundMoveDeceleration { get => groundMoveDeceleration; }
    public float AirMoveSpeedMax { get => airMoveSpeedMax; }
    public float AirMoveAcceleration { get => airMoveAcceleration; }
    public float AirMoveDeceleration { get => AirMoveDeceleration; }
    public float AerialJumpForce { get => aerialJumpForce; }
    public float GroundedJumpForce { get => groundedJumpForce; }
    public float FallMultiplier { get => fallMultiplier; }
    public float SpeedLimit { get => speedLimit; }
    public float AccelerationSpeed { get => speedAcceleration; }
    public float DecelerationSpeed { get => speedDeceleration; }
    public float PushCoolDownTime { get => Mathf.Clamp01(pushCooldown / currentPushCooldownTime); }

    public void InitPlayer(Vector2 spawnPos)
    {
        SpawnCharacter(spawnPos);
        InitPush(pushForce);
    }

    public void Die()
    {
        //Animations etc
        GameManager.Instance.BattleManager.UpdatePlayerStock(PlayerNumber, -1);
        GameManager.Instance.ParticleController.SpawnParticleSystem(ParticleType.DEATH, playerCharacter.transform.position);
    }

    public void GainDuality()
    {
        GameManager.Instance.BattleManager.UpdateDualityCount(PlayerNumber, +1);
    }

    private void Update()
    {
        if (playerCharacter != null)
        {
            if (!isHitStun)
                GetInput();

            TimerChecks();
        }
    }

    private void InitPush(float pushForce)
    {
        pushAbility = Instantiate(pushAbilityPrefab, playerCharacter.CharacterTransform);
        pushAbility.GetComponent<PushAbility>().PushForce = pushForce;
        pushAbility.GetComponent<PushAbility>().PushDuration = pushDuration;
        pushAbility.GetComponent<PushAbility>().AbilityPlayer = this;
        pushAbility.gameObject.SetActive(false);
    }

    private void SpawnCharacter(Vector2 spawnPos)
    {
        SpawnPosition = spawnPos;
        transform.position = SpawnPosition;

        playerCharacter = Instantiate(characterPrefab, this.transform).GetComponent<Character>();
        playerCharacter.CharacterPlayer = this;

        ResetJumpCount();
        moveDir = Vector2.zero;
    }

    private void TimerChecks()
    {
        CheckCoyoteTime();
        CheckPushCooldown();
        return;
    }

    private void CheckPushCooldown()
    {
        currentPushCooldownTime  += Time.deltaTime;

        if (!canPush && currentPushCooldownTime >= pushCooldown)
        {
            canPush = true;
        }
    }

    private void CheckCoyoteTime()
    {
        if (!canCoyoteJump)
            return;
        if ((Time.timeSinceLevelLoad - coyoteTimeStart) > coyoteTime)
            canCoyoteJump = false;
    }

    private void GetInput()
    {
        if (controls == ControlScheme.WASD)
            ListenWASDInput();

        if (controls == ControlScheme.Arrows)
            ListenARROWInput();
    }

    private void ListenWASDInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
            Jump();
        if (Input.GetKeyDown(KeyCode.F))
            UsePush();
        if (Input.GetKeyDown(KeyCode.G))
            UseDuality();

        if (Input.GetKeyDown(KeyCode.A))
            moveDir += Vector2.left;
        if (Input.GetKeyDown(KeyCode.D))
            moveDir += Vector2.right;

        if (Input.GetKeyUp(KeyCode.A))
            moveDir -= Vector2.left;
        if (Input.GetKeyUp(KeyCode.D))
            moveDir -= Vector2.right;
    }

    private void ListenARROWInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Jump();
        if (Input.GetKeyDown(KeyCode.RightShift))
            UsePush();
        if (Input.GetKeyDown(KeyCode.Semicolon))
            UseDuality();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            moveDir += Vector2.left;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            moveDir += Vector2.right;


        if (Input.GetKeyUp(KeyCode.LeftArrow))
            moveDir -= Vector2.left;
        if (Input.GetKeyUp(KeyCode.RightArrow))
            moveDir -= Vector2.right;
    }

    private void Jump()
    {
        if (currentJumpCount >= maxJumpCount && !canQueueJump)
            return;

        speedLimit = airMoveSpeedMax;
        speedDeceleration = airMoveDeceleration;
        speedAcceleration = airMoveAcceleration;

        if (isGrounded)
        {
            currentJumpCount += 1;

            playerCharacter.GroundedJump = true;
            return;
        }

        if (!isGrounded && canQueueJump)
        {
            jumpQueued = true;
            canQueueJump = false;
            return;
        }

        if (!isGrounded && !canQueueJump && currentJumpCount < maxJumpCount)
        {
            if (canCoyoteJump)
            {
                currentJumpCount += 1;
                canCoyoteJump = false;

                playerCharacter.GroundedJump = true;
                return;
            }

            currentJumpCount = maxJumpCount;
            playerCharacter.AerialJump = true;
        }
    }

    private void UsePush()
    {
        if(canPush)
        {
            canPush = false;
            pushAbility.gameObject.SetActive(true);
            currentPushCooldownTime = 0f;
        }
    }

    private void UseDuality()
    {
        GameManager.Instance.BattleManager.UpdateDualityCount(PlayerNumber, -1);
    }

    //Changes grounding status. If the player is not grounded, we set the player's movement stats 
    //to reflect air movespeed. We also check if the player has jumped. If they have not jumped 
    //and they are not grounded, it means the platform disappeared, and so we start our coyoteTime.
    public void ChangePlayerGrounding(bool newGrounding)
    {
        isGrounded = newGrounding;

        if (!isGrounded)
        {
            speedLimit = airMoveSpeedMax;
            speedDeceleration = airMoveDeceleration;
            speedAcceleration = airMoveAcceleration;

            if (currentJumpCount == 0)
            {
                coyoteTimeStart = Time.timeSinceLevelLoad;
                canCoyoteJump = true;
            }
        }

        if (isGrounded)
        {
            speedLimit = groundSpeedMax;
            speedDeceleration = groundMoveDeceleration;
            speedAcceleration = groundMoveAcceleration;

            ResetJumpCount();
        }

        if (jumpQueued && isGrounded)
        {
            jumpQueued = false;
            Jump();
        }
    }

    private void ResetJumpCount() => currentJumpCount = 0;
}
