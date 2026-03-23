using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodzillaController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnAnimTrigger()
    {
        Debug.Log("Godzilla animation triggered");
        GetComponent<Animator>().SetBool("anim", true);
    }

    public void OnAnimFinish()
    {
        Debug.Log("Godzilla animation finished");
        GetComponent<Animator>().SetBool("anim", false);
    }
}
