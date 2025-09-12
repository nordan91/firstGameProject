using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System;

public class CameraManager : MonoBehaviour
{
    public Action OnCameraSwitch;
    [SerializeField]
    public CameraState CameraState;
    [SerializeField]
    private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField]
    private CinemachineFreeLook _thirdPersonCamera;
    [SerializeField]
    private InputManager _inputManager;
  
    private void Start()
    {
        _inputManager.OnChangePOVInput += SwitchCamera;
    }

    private void OnDestroy()
    {
        _inputManager.OnChangePOVInput -= SwitchCamera;
    }

    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        CinemachinePOV pov = _firstPersonCamera.GetCinemachineComponent<CinemachinePOV>();
        if (isClamped)
        {
            pov.m_HorizontalAxis.m_Wrap = false;
            pov.m_HorizontalAxis.m_MinValue = playerRotation.y - 60;
            pov.m_HorizontalAxis.m_MaxValue = playerRotation.y + 60;
        }
        else
        {
            pov.m_HorizontalAxis.m_MinValue = -180;
            pov.m_HorizontalAxis.m_MaxValue = 180;
            pov.m_HorizontalAxis.m_Wrap = true;
        }
    }


    private void SwitchCamera()
    {
        OnCameraSwitch();
        if (CameraState == CameraState.ThirdPerson)
        {
            CameraState = CameraState.FirstPerson;
            _firstPersonCamera.gameObject.SetActive(true);
            _thirdPersonCamera.gameObject.SetActive(false);

        }
        else
        {
            CameraState = CameraState.ThirdPerson;
            _firstPersonCamera.gameObject.SetActive(false);
            _thirdPersonCamera.gameObject.SetActive(true);
        }
    }
    
    public void SetTPSFieldOfView(float fov)
    {
        _thirdPersonCamera.m_Lens.FieldOfView = fov;
    }
}
