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

    float minSwipeSpeed = 1f;
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
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
        else
        {
            var hands = provider.CurrentFrame.Hands;
            if (hands.Count >= 2)
            {
                bool leftSwipe = false;
                bool rightSwipe = false;
                foreach (var hand in hands)
                {
                    if (hand.IsRight && hand.PalmVelocity.x > minSwipeSpeed)
                    {
                        rightSwipe = true;
                    }

                    if (hand.IsLeft && hand.PalmVelocity.x < -minSwipeSpeed)
                    {
                        leftSwipe = true;
                    }
                }

                if (rightSwipe && leftSwipe)
                {
                    ExitGame();
                }
            }
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

    void ExitGame()
    {
        madeChoice = true;

        Utils.FadeOut(1, () =>
        {
            Application.Quit();
        });
    }
}
