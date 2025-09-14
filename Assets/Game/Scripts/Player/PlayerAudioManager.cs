using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _FootstepSFX;
    [SerializeField]
    private AudioSource _LandingSFX;
    [SerializeField]
    private AudioSource _PunchSFX;
    [SerializeField]
    private AudioSource _GlideSFX;


    private void PlayFootstepSFX()
    {
        _FootstepSFX.volume = Random.Range(0.8f, 1f);
        _FootstepSFX.pitch = Random.Range(0.8f, 1.8f);
        _FootstepSFX.Play();
    }

    private void PlayLandingSFX()
    {
        _LandingSFX.volume = Random.Range(0.8f, 1f);
        _LandingSFX.pitch = Random.Range(0.8f, 1.2f);
        _LandingSFX.Play();
    }

    private void PlayPunchSFX()
    {
        _PunchSFX.volume = Random.Range(0.8f, 1f);
        _PunchSFX.pitch = Random.Range(0.8f, 1.2f);
        _PunchSFX.Play();
    }

    public void PlayGlideSFX()
    {
        _GlideSFX.volume = Random.Range(0.8f, 1f);
        _GlideSFX.pitch = Random.Range(0.8f, 1.2f);
        _GlideSFX.Play();
    }

    public void StopGlideSFX()
    {
        _GlideSFX.Stop();
    }
}
