using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewAnimController : MonoBehaviour
{
    private enum PlayerState
    {
        Default,
        Jumping,
        Vaulting,
        JumpClimbing
    }

    private PlayerState currentState = PlayerState.Default;
    private Animator animator;
    private PlayerController player;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        switch (currentState)
        {
            case PlayerState.Default:
                HandleDefaultState();
                break;
            case PlayerState.Jumping:
                HandleJumpingState();
                break;
            case PlayerState.Vaulting:
                HandleVaultingState();
                break;
            case PlayerState.JumpClimbing:
                HandleJumpClimbingState();
                break;
        }
    }

    private void HandleDefaultState()
    {
        GetPlayerActionBoolean();
        if (!player.IsOnDynamicMove)
        {
            GetPlayerSpeed();
        }

        if (player.IsJumping)
        {
            switch (player.JumpMode)
            {
                case PlayerParkour.JumpState.DefaultJump:
                    currentState = PlayerState.Jumping;
                    StartCoroutine(DefaultJumpCoroutine());
                    break;
                case PlayerParkour.JumpState.Vault:
                    if (!player.IsOnDynamicMove)
                    {
                        currentState = PlayerState.Vaulting;
                        SetVaultType();
                        StartCoroutine(VaultCoroutine());
                    }
                    break;
                case PlayerParkour.JumpState.JumpClimb:
                    if (!player.IsOnDynamicMove)
                    {
                        currentState = PlayerState.JumpClimbing;
                        SetJumpClimbType();
                        StartCoroutine(JumpClimbCoroutine());
                    }
                    break;
            }
        }
    }

    private void HandleJumpingState()
    {
        // Handle jumping state logic here
    }

    private void HandleVaultingState()
    {
        // Handle vaulting state logic here
    }

    private void HandleJumpClimbingState()
    {
        // Handle jump climbing state logic here
    }

    private void GetPlayerSpeed()
    {
        // Get player speed logic here
    }

    private void GetPlayerActionBoolean()
    {
        // Get player action boolean logic here
    }

    private void SetJumpClimbType()
    {
        // Set jump climb type logic here
    }

    private void SetVaultType()
    {
        // Set vault type logic here
    }

    private IEnumerator DefaultJumpCoroutine()
    {
        // Default jump coroutine logic here
        yield return null;
    }

    private IEnumerator VaultCoroutine()
    {
        // Vault coroutine logic here
        yield return null;
    }

    private IEnumerator JumpClimbCoroutine()
    {
        // Jump climb coroutine logic here
        yield return null;
    }
}
