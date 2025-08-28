using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Move : MonoBehaviour
{
    public int speed;
    [SerializeField]
    private int _age;
    [SerializeField]
    private AudioSource _audioSource;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Mulai");
        Destroy(_audioSource);
    }


}
