using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Totem : MonoBehaviour
{
    public GameObject WhiteParticles;
    public GameObject CivParticles;
    public GameObject NatParticles;
    public string Type;

    void Start()
    {
        TurnOff();
    }

    public void TurnOn()
    {
        var isCiv = Type == "civ";
        CivParticles.SetActive(isCiv);
        NatParticles.SetActive(isCiv);
        WhiteParticles.SetActive(false);
    }

    public void TurnOff()
    {
        CivParticles.SetActive(false);
        NatParticles.SetActive(false);
        WhiteParticles.SetActive(true);
    }

    public void ShuffleType()
    {
        Type = Random.Range(0, 100) < 50 ? "civ" : "nat";
    }
}
