using DG.Tweening;
using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    public LeapProvider provider;

    void Awake()
    {

    }

    void Start()
    {
        // Init Leap
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
    }

    void Update()
    {
        CheckChoice();
    }

    void CheckChoice()
    {
        if (madeChoice)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            GoToCustomMusic();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            GoToSelectMusic();
        }
    }

    bool madeChoice = false;
    void GoToSelectMusic()
    {
        madeChoice = true;

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("SelectMusic");
        });
    }

    void GoToCustomMusic()
    {
        madeChoice = true;

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("CustomMusic");
        });
    }
}
