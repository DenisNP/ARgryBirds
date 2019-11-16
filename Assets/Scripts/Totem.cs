using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Totem : MonoBehaviour
{
    public GameObject WhiteParticles;
    public GameObject Particles;
    public string Type;

    void Start()
    {
        TurnOff();
    }

    public void TurnOn()
    {
        Particles.SetActive(true);
        WhiteParticles.SetActive(false);
    }

    public void TurnOff()
    {
        Particles.SetActive(false);
        WhiteParticles.SetActive(true);
    }
}
