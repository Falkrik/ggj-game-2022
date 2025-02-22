﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpBuffer : MonoBehaviour
{
    [SerializeField] private Player player;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.GetComponent<PlatformGround>() != null)
            player.CanQueueJump = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlatformGround>() != null)
        {
            player.CanQueueJump = false;
        }
    }
}
