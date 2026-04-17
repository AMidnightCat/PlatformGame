using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTree : MonoBehaviour , SkillInterface
{
    [Header("NormalForm")]
    [SerializeField] public bool HighJumpSkill = false;     //´óÌø
    [SerializeField] public bool PushCaseSkill = false;     //ÍÆÏä×Ó
    [SerializeField] public bool ThrowSkill = false;        //Í¶ÖÀ
    [SerializeField] public bool GroundPoundSkill = false;  //ÏÂÔÒ

    [Header("ChimeraForm")]
    [SerializeField] public bool DoubleJumpSkill = false;   //¶þ¶ÎÌø
    [SerializeField] public bool GlideSkill = false;        //»¬Ïè
    [SerializeField] public bool SwimSkill = false;         //ÓÎÓ¾
    [SerializeField] public bool SurveySkill = false;       //¿±²ì
    [SerializeField] public bool GlowSkill = false;         //·¢¹â
    [SerializeField] public bool FlashSkill = false;        //ÉÁÏÖ

    [Header("TalosForm")]
    [SerializeField] public bool DashSkill = false;         //³å´Ì
    [SerializeField] public bool WallJumpSkill = false;     //µÅÇ½Ìø
    [SerializeField] public bool WallSlideSkill = false;     //°ÇÇ½
    [SerializeField] public bool BoomerangSkill = false;    //»ØÐýïÚ
    [SerializeField] public bool FalculaClawSkill = false;  //¹³×¦

    public bool IsDashSkillAvailable()
    {
        return DashSkill;
    }
    public bool IsDoubleJumpSkillAvailable()
    {
        return DoubleJumpSkill;
    }
    public bool IsWallJumpSkillAvailable()
    {
        return WallJumpSkill;
    }
    public bool IsWallSlideSkillAvailable()
    {
        return WallSlideSkill;
    }
}
