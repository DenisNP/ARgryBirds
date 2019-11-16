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
        CivParticles.SetActive(Type == "civ");
        NatParticles.SetActive(Type == "nat");
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

    public void DisableType()
    {
        Type = "none";
        TurnOff();
    }

    public bool IsDisabled()
    {
        return Type == "none";
    }
}
