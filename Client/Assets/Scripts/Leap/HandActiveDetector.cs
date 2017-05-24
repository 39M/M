using Leap.Unity;

public class HandActiveDetector : Detector
{
    public LeapProvider provider;
    bool activate;

    void Start()
    {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
        activate = false;
    }

    void Update()
    {
        if (activate && provider.CurrentFrame.Hands.Count <= 0)
        {
            activate = false;
            Deactivate();
        }

        if (!activate && provider.CurrentFrame.Hands.Count > 0)
        {
            activate = true;
            Activate();
        }
    }
}
