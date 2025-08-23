using System.Collections;
using UnityEngine;

public class Button : MonoBehaviour
{
    public GameObject Game;
    public GameObject Loading;
    public GameObject Lobby;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void Switch()
    {
        if(Lobby != null)
          Lobby.SetActive(false);
        if (Game != null)
            Game.SetActive(true);

        StartCoroutine(Loadingg());

    }

    IEnumerator Loadingg()
    {
        yield return new WaitForSeconds(2f);
        Loading.SetActive(false);
    }
}
