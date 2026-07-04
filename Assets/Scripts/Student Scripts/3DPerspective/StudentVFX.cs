using UnityEngine;

public class StudentVFX : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem smoke;
    public ParticleSystem graduated;
    public ParticleSystem droppedOut;
    public ParticleSystem distracted;
    public ParticleSystem finished;
    public ParticleSystem fire;
    public ParticleSystem flow;

    public void ActivateSmoke()
    {
        if (smoke != null)
        {
            smoke.gameObject.SetActive(true);
            smoke.Play();
        }
    }

    public void ActivateGraduated()
    {
        if (graduated != null)
        {
            graduated.gameObject.SetActive(true);
            graduated.Play();
        }
    }

    public void ActivateDroppedOut()
    {
        if (droppedOut != null)
        {
            droppedOut.gameObject.SetActive(true);
            droppedOut.Play();
        }
    }

    public void ActivateDistracted()
    {
        if (distracted != null)
        {
            distracted.gameObject.SetActive(true);
            distracted.Play();
        }
    }

    public void ActivateFinished()
    {
        if (finished != null)
        {
            finished.gameObject.SetActive(true);
            finished.Play();
        }
    }

    public void ActivateFire()
    {
        if (fire != null)
        {
            fire.gameObject.SetActive(true);
            fire.Play();
        }
    }

    public void ActivateFlow()
    {
        if (flow != null)
        {
            flow.gameObject.SetActive(true);
            flow.Play();
        }
    }

    public void DeactivateFlow()
    {
        if (flow != null)
        {
            flow.gameObject.SetActive(false);
            flow.Stop();
        }
    }
    

    public void DeactivateAllParticles()
    {
        if (smoke != null)
            smoke.gameObject.SetActive(false);
        if (graduated != null)
            graduated.gameObject.SetActive(false);
        if (droppedOut != null)
            droppedOut.gameObject.SetActive(false);
        if (distracted != null)
            distracted.gameObject.SetActive(false);
        if (finished != null)
            finished.gameObject.SetActive(false);
        if (fire != null)
            fire.gameObject.SetActive(false);
    }
}
