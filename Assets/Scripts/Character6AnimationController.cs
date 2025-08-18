using UnityEngine;

public class Character6AnimationController : MonoBehaviour
{
    public Animator characterAnimator;
    private bool isWalking;
    
    void Start()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.Play("idle");
            isWalking = false;
        }
    }
    
    public void OnMovementStart()
    {
        if (characterAnimator == null) return;
        if (isWalking) return;
        characterAnimator.SetTrigger("Walk");
        isWalking = true;
    }
    
    public void OnMovementStop()
    {
        if (characterAnimator == null) return;
        if (!isWalking) return;
        characterAnimator.Play("idle");
        isWalking = false;
    }
}
