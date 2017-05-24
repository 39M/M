using Leap.Unity;
using UnityEngine;

public class HandFlipDetector : Detector
{
    public LeapProvider provider;
    [SerializeField]
    bool activate;
    [SerializeField]
    float handRotationZ;
    [SerializeField]
    bool isUp;

    float minAngel;
    float maxAngel;

    void Start()
    {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
        activate = false;

        minAngel = 135;
        maxAngel = 225;
    }

    void Update()
    {
        if (activate && provider.CurrentFrame.Hands.Count <= 0)
        {
            activate = false;
        }

        if (!activate && provider.CurrentFrame.Hands.Count > 0)
        {
            activate = true;
            handRotationZ = provider.CurrentFrame.Hands[0].Rotation.ToQuaternion().eulerAngles.z;
            isUp = (minAngel < handRotationZ) && (handRotationZ < maxAngel);
        }

        if (activate)
        {
            float currentHandRotationZ = provider.CurrentFrame.Hands[0].Rotation.ToQuaternion().eulerAngles.z;
            bool isUpNow = (minAngel < currentHandRotationZ) && (currentHandRotationZ < maxAngel);
            if (isUpNow != isUp)
            {
                isUp = isUpNow;
                if (isUp)
                {
                    Activate();
                }
                else
                {
                    Deactivate();
                }
            }
        }
    }
}
