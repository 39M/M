using UnityEngine;
using UnityEngine.SceneManagement;

public class InitManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Utils.WaitAndAction(4, () =>
        {
            Utils.FadeOut(1, () =>
            {
                SceneManager.LoadScene("Startup");
            });
        }));
    }
}
