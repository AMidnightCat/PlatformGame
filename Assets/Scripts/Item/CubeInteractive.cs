using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider2D))] //要求gameobject必须有collider2D组件
public class CubeInteractive : MonoBehaviour
{
    public enum SkillName
    {
        [InspectorName("大跳")] HighJumpSkill,      //大跳
        [InspectorName("推箱子")] PushCaseSkill,    //推箱子
        [InspectorName("投掷")] ThrowSkill,         //投掷
        [InspectorName("下砸")] GroundPoundSkill,   //下砸

        [InspectorName("二段跳")] DoubleJumpSkill,  //二段跳
        [InspectorName("滑翔")] GlideSkill,         //滑翔
        [InspectorName("游泳")] SwimSkill,          //游泳
        [InspectorName("勘察")] SurveySkill,        //勘察
        [InspectorName("发光")] GlowSkill,          //发光
        [InspectorName("闪现")] FlashSkill,         //闪现

        [InspectorName("冲刺")] DashSkill,          //冲刺
        [InspectorName("蹬墙跳")] WallJumpSkill,    //蹬墙跳
        [InspectorName("回旋镖")] BoomerangSkill,   //回旋镖
        [InspectorName("钩爪")] FalculaClawSkill,   //钩爪
    }
    [SerializeField] private SkillTree skillTree;
    [SerializeField] private SkillName skillName;
    [SerializeField] private GameObject ShowButton;
    [SerializeField] private Collider2D col;
    [SerializeField] private GameObject TestCube;
    [SerializeField] private KeyCode InteractiveKey = KeyCode.X;

    private bool _isInTrigger = false;

    void Awake()
    {
        ShowButton.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(InteractiveKey) && _isInTrigger)
        {
            switch (skillName)
            {
                case SkillName.HighJumpSkill: skillTree.HighJumpSkill = true; break;
                case SkillName.PushCaseSkill: skillTree.PushCaseSkill = true; break;
                case SkillName.ThrowSkill: skillTree.ThrowSkill = true; break;
                case SkillName.GroundPoundSkill: skillTree.GroundPoundSkill = true; break;

                case SkillName.DoubleJumpSkill: skillTree.DoubleJumpSkill = true; break;
                case SkillName.GlideSkill: skillTree.GlideSkill = true; break;
                case SkillName.SwimSkill: skillTree.SwimSkill = true; break;
                case SkillName.SurveySkill: skillTree.SurveySkill = true; break;
                case SkillName.GlowSkill: skillTree.GlowSkill = true; break;
                case SkillName.FlashSkill: skillTree.FlashSkill = true; break;

                case SkillName.DashSkill: skillTree.DashSkill = true; break;
                case SkillName.WallJumpSkill: skillTree.WallJumpSkill = true; break;
                case SkillName.BoomerangSkill: skillTree.BoomerangSkill = true; break;
                case SkillName.FalculaClawSkill: skillTree.FalculaClawSkill = true; break;
            }
            ShowButton.SetActive(false);
            TestCube.SetActive(false);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 持续在触发器内
        ShowButton.SetActive(true);
        _isInTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ShowButton.SetActive(false);
        _isInTrigger = false;
    }

}
