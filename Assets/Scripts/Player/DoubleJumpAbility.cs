using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJumpAbility : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private SkillTree skillScript;  // 直接引用SkillTree脚本
    [SerializeField] private TestDragonControl playerController;
    [SerializeField] private WallSlide wallSlide;

    private SkillInterface skillInterface;
    public bool canDoubleJump = false;
    private bool isGrounded = false;

    private void Start()
    {
        if (skillScript != null)
        {
            skillInterface = skillScript as SkillInterface;
        }
    }

    private void Update()
    {
        isGrounded = playerController.isGrounded;

        if((isGrounded || (wallSlide.wasAgainstWall && skillInterface.IsWallJumpSkillAvailable())) && CheckDoubleJumpSkillEnabled()) 
        {
            canDoubleJump = true;
        }
    }

    bool CheckDoubleJumpSkillEnabled()
    {
        // 检查技能是否可用
        if (skillInterface != null && skillInterface.IsDoubleJumpSkillAvailable())
        {
            return true;
        }
        return false;
    }

}
