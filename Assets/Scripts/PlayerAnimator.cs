using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAnimator : NetworkBehaviour {

    private const string IS_WALKING = "IsWalking";

    [SerializeField] private Player player;
    private Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        // Only update animations if this is our local player
        if (!IsOwner && !IsServer) {
            return;
        }
        
        if (animator != null) {
            animator.SetBool(IS_WALKING, player.IsWalking());
        }
    }
}