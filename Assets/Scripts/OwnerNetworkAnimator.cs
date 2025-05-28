using Unity.Netcode.Components;
using Unity.Netcode;
using UnityEngine;

public class OwnerNetworkAnimator : NetworkAnimator
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Make sure Animator is enabled for all instances
        if (Animator != null)
        {
            Animator.enabled = true;
        }
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;  // Keep animations client-authoritative
    }
}