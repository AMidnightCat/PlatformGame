using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraControl : MonoBehaviour
{
    private CinemachineVirtualCamera vCam;
    private void Awake()
    {
        // 获取虚拟相机组件
        vCam = GetComponent<CinemachineVirtualCamera>();

        // 场景加载完成后绑定玩家
        if (vCam != null && TestDragonControl.instance != null)
        {
            vCam.Follow = TestDragonControl.instance.transform;
        }
    }
}
