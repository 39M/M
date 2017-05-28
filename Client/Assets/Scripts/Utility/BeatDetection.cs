using UnityEngine;
using System.Collections;

public class BeatDetection : MonoBehaviour
{

    public enum beatmode { Energy, Frequency, Both };
    public enum beatType { None = 0, kick = 1, snare = 2, hithat = 4, energy = 8 };

    public beatmode beatMode;									//We support energy, frequency and both, for beat detection

    public enum EventType
    {
        Energy, Kick, Snare, HitHat
    }

    public class EventInfo
    {
        public EventType messageInfo;
        public BeatDetection sender;
    }

    public delegate void CallbackEventHandler(EventInfo eventInfo);
    public CallbackEventHandler CallBackFunction;

    //MAGIC NUMBERS. USE WITH CAUTION
    private int numSamples = 1024;                              //Num samples to precess MUST be a power of 2
    private int minFrequency = 60;                              //Low pass frequency... frequencies less than 60 are tricky
    private const int MAX_FRQSEC = 50;                          //Number of frequency divisions. 50 is ok
    private const int LINEAR_OCTAVE_DIVISIONS = 3;              //Linear divisions per octave
    private const int HISTORY_LENGTH = 43;                      //Real number of history buffer per frequency.
    private const int MAX_HISTORY = 500;                        //Allocation buffer. Must be greater than MAX_FRQSEC*LINEAR_OCTAVE_DIVISIONS*HISTORY_LENGTH
    private const float MIN_BEAT_SEPARATION = 0.05f;            //Minimum beat separation in time



    int numHistory, circularHistory;

    float[] energyHistory = new float[MAX_HISTORY];
    float[] mediasHistory = new float[MAX_HISTORY];

    float[,] freqHistory = new float[MAX_FRQSEC, MAX_HISTORY];
    float[,] medHistory = new float[MAX_FRQSEC, MAX_HISTORY];
    float[] averages = new float[MAX_FRQSEC];
    bool[] detectados = new bool[MAX_FRQSEC];

    int acumula;
    float[] estad1 = new float[MAX_FRQSEC];
    float[] estad2 = new float[MAX_FRQSEC];

    float sampleRate;
    int avgPerOctave;
    int octaves;
    int totalBfLen;

    int historyLength;
    int sampleRange;

    float tIni;
    float[] tIniF = new float[MAX_FRQSEC];

    int kg = 0;

    float[] spectrum0;
    float[] spectrum1;
    float[] frames0;
    float[] frames1;

    /********************************************
	 ********************************************
	 This is th only function you ned to use
	 It catch the type of beat detected.
	 When using frequency mode you can get several
	 types of beat at the same time.
	 Add your code to manage the beats
	******************************************** 
	********************************************/
    void Update()
    {
        int theBeat = isBeat();

        if ((theBeat & (int)beatType.kick) != (int)beatType.None)
            SendEvent(EventType.Kick);
        if ((theBeat & (int)beatType.snare) != (int)beatType.None)
            SendEvent(EventType.Snare);
        if ((theBeat & (int)beatType.hithat) != (int)beatType.None)
            SendEvent(EventType.HitHat);
        if ((theBeat & (int)beatType.energy) != (int)beatType.None)
            SendEvent(EventType.Energy);
    }

    void SendEvent(EventType theEvent)
    {
        if (CallBackFunction != null)
        {
            EventInfo myEvent = new EventInfo();
            myEvent.sender = this;
            myEvent.messageInfo = theEvent;
            CallBackFunction(myEvent);
        }
    }

    void Start()
    {
        spectrum0 = new float[numSamples];
        spectrum1 = new float[numSamples];
        frames0 = new float[numSamples];
        frames1 = new float[numSamples];

        setUpEnergy();
        setUpFrequency();
        //Start beat detection timers, we don't wish two beats very close each other
        tIni = Time.time;
        for (int i = 0; i < MAX_FRQSEC; i++)
            tIniF[i] = Time.time;
    }

    int isBeat()
    {

        GetComponent<AudioSource>().GetSpectrumData(spectrum0, 0, FFTWindow.BlackmanHarris);
        GetComponent<AudioSource>().GetSpectrumData(spectrum1, 1, FFTWindow.BlackmanHarris);
        GetComponent<AudioSource>().GetOutputData(frames0, 0);
        GetComponent<AudioSource>().GetOutputData(frames1, 1);

        beatType energy = beatType.None;
        switch (beatMode)
        {
            case beatmode.Energy:
                if (isBeatEnergy())
                    return (int)beatType.energy;
                break;
            case beatmode.Frequency:
                isBeatFrequency();
                int val = (int)isKick() | (int)isSnare() | (int)isHat();
                return val;
            case beatmode.Both:
                if (isBeatEnergy())
                    energy = beatType.energy;
                isBeatFrequency();
                int val2 = (int)isKick() | (int)isSnare() | (int)isHat() | (int)energy;
                return val2;
        }
        return 0;
    }

    void initDetector()
    {
        numHistory = 0;
        circularHistory = 0;
        for (int i = 0; i < MAX_HISTORY; i++)
        {
            energyHistory[i] = 0f;
            mediasHistory[i] = 0f;
        }
        acumula = 0;
        historyLength = 0;
    }

    void setUpEnergy()
    {
        sampleRate = AudioSettings.outputSampleRate;
        sampleRange = numSamples;
        historyLength = HISTORY_LENGTH;
        numHistory = 0;
        circularHistory = 0;
    }

    void setUpFrequency()
    {
        sampleRange = numSamples;
        historyLength = HISTORY_LENGTH;
        sampleRate = AudioSettings.outputSampleRate;
        numHistory = 0;
        circularHistory = 0;

        //number of samples per block nyquist limit
        float nyq = (float)sampleRate / 2f;
        octaves = 1;
        while ((nyq /= 2) > minFrequency)
            octaves++;
        avgPerOctave = LINEAR_OCTAVE_DIVISIONS;
        totalBfLen = octaves * avgPerOctave;

        //inicialize array
        for (int i = 0; i < totalBfLen; i++)
            for (int j = 0; j < historyLength; j++)
            {
                freqHistory[i, j] = 0f;
                medHistory[i, j] = 0f;
            }
    }

    /*
	//Old method not so precise... only for study purpouse
	bool isBeatEnergy () {
		float energy=0f;
		for(int i=0; i<sampleRange; i++) {
			energy+=(frames0[i]*frames0[i])+(frames1[i]*frames1[i]);
		}
		
		float media=0f;
		for(int i=0; i<numHistory; i++)
			media+=energyHistory[i];
		if(numHistory>0)
			media/=(float)numHistory;
		
		
		float c=1.4f;
		numHistory=(numHistory<historyLength)?numHistory+1:numHistory;
		
		energyHistory[circularHistory]=energy;
		circularHistory++;
		circularHistory%=historyLength;
		
		if(energy>c*media)
			return true;
		return false;
	}
	*/

    bool isBeatEnergy()
    {
        float level = 0f;
        for (int i = 0; i < sampleRange; i++)
        {
            level += (frames0[i] * frames0[i]) + (frames1[i] * frames1[i]);
        }

        level /= (float)sampleRange;
        float instant = Mathf.Sqrt(level) * 100f;

        float E = 0f;
        for (int i = 0; i < numHistory; i++)
            E += energyHistory[i];
        if (numHistory > 0)
            E /= (float)numHistory;

        float V = 0f;
        for (int i = 0; i < numHistory; i++)
            V += (energyHistory[i] - E) * (energyHistory[i] - E);
        if (numHistory > 0)
            V /= (float)numHistory;

        float C = (-0.0025714f * V) + 1.5142857f;
        float diff = (float)Mathf.Max(instant - C * E, 0f);

        float dAvg = 0f;
        int num = 0;
        for (int i = 0; i < numHistory; i++)
        {
            if (mediasHistory[i] > 0)
            {
                dAvg += mediasHistory[i];
                num++;
            }
        }
        if (num > 0)
            dAvg /= (float)num;

        float diff2 = (float)Mathf.Max(diff - dAvg, 0f);

        bool detectado;
        if (Time.time - tIni < MIN_BEAT_SEPARATION)
            detectado = false;
        else if (diff2 > 0.0 && instant > 2.0)
        {
            detectado = true;
            tIni = Time.time;
        }
        else
            detectado = false;

        numHistory = (numHistory < historyLength) ? numHistory + 1 : numHistory;

        energyHistory[circularHistory] = instant;
        mediasHistory[circularHistory] = diff;

        circularHistory++;
        circularHistory %= historyLength;

        return detectado;
    }

    int freqToIndex(float freq)
    {
        float bandwidth = (float)sampleRate / (float)sampleRange;
        // special case: freq is lower than the bandwidth of spectrum[0]
        if (freq < bandwidth / 2) return 0;
        // special case: freq is within the bandwidth of spectrum[spectrum.length - 1]
        if (freq > sampleRate / 2 - bandwidth / 2) return (sampleRange / 2) - 1;
        // all other cases
        float fraction = freq / (float)sampleRate;
        int i = (int)(sampleRange * fraction);
        return i;
    }

    float calcAvg(float lowFreq, float hiFreq, float[] spectrum)
    {
        int lowBound = freqToIndex(lowFreq);
        int hiBound = freqToIndex(hiFreq);
        float avg = 0f;
        for (int i = lowBound; i <= hiBound; i++)
            avg += spectrum[i];
        avg /= (hiBound - lowBound + 1);
        return avg;
    }

    void isBeatFrequency()
    {
        for (int i = 0; i < octaves; i++)
        {
            float lowFreq, hiFreq, freqStep;
            if (i == 0)
                lowFreq = 0f;
            else
                lowFreq = (float)(sampleRate / 2) / (float)Mathf.Pow(2, octaves - i);

            hiFreq = (float)(sampleRate / 2) / (float)Mathf.Pow(2, octaves - i - 1);
            freqStep = (hiFreq - lowFreq) / avgPerOctave;
            float f = lowFreq;
            for (int j = 0; j < avgPerOctave; j++)
            {
                int offset = j + i * avgPerOctave;
                float cl = calcAvg(f, f + freqStep, spectrum0);
                float cr = calcAvg(f, f + freqStep, spectrum1);

                averages[offset] = cr;
                if (cl > cr)
                    averages[offset] = cl;
                f += freqStep;
            }
        }

        acumula++;
        for (int i = 0; i < totalBfLen; i++)
        {
            if (kg == 2)
            {
                estad1[i] = averages[i];
                estad2[i] = averages[i];
            }
            else
            {
                estad1[i] += averages[i];
                if (averages[i] > estad2[i])
                    estad2[i] = averages[i];
            }
        }


        //int lower = 8 >= totalBfLen ? totalBfLen : 8;
        //int upper = totalBfLen - 1;

        for (int i = 1; i < totalBfLen; i++)
        {
            float instant, E = 0f, V = 0f, C = 0f, diff, dAvg, diff2;
            instant = averages[i];

            //instant=Mathf.Sqrt(instant)*100f;

            E = 0f;
            for (int k = 0; k < numHistory; k++)
                E += freqHistory[i, k];
            if (numHistory > 0)
                E /= (float)numHistory;

            V = 0f;
            for (int k = 0; k < numHistory; k++)
                V += (freqHistory[i, k] - E) * (freqHistory[i, k] - E);
            if (numHistory > 0)
                V /= (float)numHistory;

            C = (-0.0025714f * V) + 1.5142857f;
            diff = (float)Mathf.Max(instant - C * E, 0f);

            dAvg = 0f;
            int num = 0;
            for (int k = 0; k < numHistory; k++)
            {
                if (medHistory[i, k] > 0)
                {
                    dAvg += medHistory[i, k];
                    num++;
                }
            }
            if (num > 0)
                dAvg /= (float)num;

            diff2 = (float)Mathf.Max(diff - dAvg, 0);

            float corte, mul;
            if (i < 7)
            {
                corte = 0.003f; //500f;
                mul = 2f;
            }
            else if (i > 6 && i < 20)
            {
                corte = 0.001f; //30f;
                mul = 3f;
            }
            else
            {
                corte = 0.001f; //20f;
                mul = 4f;
            }

            //if (instant > mul * E)
            //    Debug.Log("instan=" + instant + " mul*E=" + (mul * E) + " mul=" + mul + " E=" + E);

            if (Time.time - tIniF[i] < MIN_BEAT_SEPARATION)
                detectados[i] = false;
            //else if(diff2 > 0.0 && instant>2.0) {
            else if (instant > mul * E && instant > corte)
            {
                detectados[i] = true;
                tIniF[i] = Time.time;
            }
            else
            {
                detectados[i] = false;
            }

            numHistory = (numHistory < historyLength) ? numHistory + 1 : numHistory;

            freqHistory[i, circularHistory] = instant;
            medHistory[i, circularHistory] = diff;

            circularHistory++;
            circularHistory %= historyLength;
        }
    }

    beatType isKick()
    {
        int upper = 6 >= totalBfLen ? totalBfLen : 6;
        if (isRange(1, upper, 2))
            return beatType.kick;
        return beatType.None;
    }

    beatType isSnare()
    {
        int lower = 8 >= totalBfLen ? totalBfLen : 8;
        int upper = totalBfLen - 5;
        int thresh = ((upper - lower) / 3) - 0;
        if (isRange(lower, upper, thresh))
            return beatType.snare;
        return beatType.None;
    }

    beatType isHat()
    {
        int lower = totalBfLen - 6 < 0 ? 0 : totalBfLen - 6;
        int upper = totalBfLen - 1;
        if (isRange(lower, upper, 1))
            return beatType.hithat;
        return beatType.None;
    }

    bool isRange(int low, int high, int threshold)
    {
        int num = 0;
        for (int i = low; i < high + 1; i++)
        {
            if (detectados[i])
                num++;
        }
        if (num >= threshold)
            return true;
        return false;
    }

}
