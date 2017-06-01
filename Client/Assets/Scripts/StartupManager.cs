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

    public RectTransform customOption;
    public RectTransform selectOption;

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

    enum Choices { None, Custom, BuildIn }
    Choices choice = Choices.None;
    float minSwipeSpeed = 1f;
    void CheckSwipe()
    {
        if (choosing || madeChoice)
        {
            return;
        }

        Frame frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            if (hand.PalmVelocity.x > minSwipeSpeed)
            {
                SwipeTo(Direction.Right);
            }
            else if (hand.PalmVelocity.x < -minSwipeSpeed)
            {
                SwipeTo(Direction.Left);
            }

            break;
        }
    }

    void SwipeTo(Direction direction)
    {
        if (direction == Direction.Left)
        {
            choice = Choices.Custom;
            customOption.DOScale(0.6f, 0.3f);
            selectOption.DOScale(0.5f, 0.3f);
        }
        else if (direction == Direction.Right)
        {
            choice = Choices.BuildIn;
            selectOption.DOScale(0.6f, 0.3f);
            customOption.DOScale(0.5f, 0.3f);
        }
    }

    bool choosing = false;
    public void ActiveChoosing()
    {
        choosing = true;

        if (choice == Choices.BuildIn)
        {
            transform.DOScale(0, 1).OnComplete(() =>
            {
                GoToSelectMusic();
            });
            selectOption.DOScale(0.7f, 2);
        }
        else if (choice == Choices.Custom)
        {
            transform.DOScale(0, 1).OnComplete(() =>
            {
                GoToCustomMusic();
            });
            customOption.DOScale(0.7f, 2);
        }
    }

    public void DeactiveChoosing()
    {
        if (madeChoice)
        {
            return;
        }

        choosing = false;

        transform.DOPause();
        transform.localScale = Vector3.one;
        if (choice == Choices.BuildIn)
        {
            customOption.DOPause();
            customOption.DOScale(0.6f, 0.3f);
        }
        else if (choice == Choices.Custom)
        {
            selectOption.DOPause();
            selectOption.DOScale(0.6f, 0.3f);
        }
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

        CheckSwipe();
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
