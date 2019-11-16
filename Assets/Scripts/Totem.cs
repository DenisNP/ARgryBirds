using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Totem : MonoBehaviour
{
    public GameObject WhiteParticles;
    public GameObject CivParticles;
    public GameObject NatParticles;

    public GameObject HitCiv;
    public GameObject HitNat;
    
    public string Type;

    private float prevAnim = 0f;
    private float animFrame = 0f;

    void Start()
    {
        TurnOff();
    }

    void Update()
    {
        if (animFrame > 0f)
        {
            prevAnim = animFrame;
            animFrame -= 0.05f;
            WhiteParticles.transform.localScale = new Vector3(1f + animFrame, 1f + animFrame, 1f + animFrame);
            TurnOn();
        }

        if (prevAnim > 0f && animFrame <= 0f)
        {
            prevAnim = 0f;
            TurnOff();
        }
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

    public void HitAnim()
    {
        animFrame = 1f;
    }

    public void FullHitAnim()
    {
        Instantiate(Type == "civ" ? HitCiv : HitNat, transform.position, Quaternion.identity);
    }
}
