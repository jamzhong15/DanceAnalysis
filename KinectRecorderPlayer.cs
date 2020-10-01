﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

/// <summary>
/// Kinect recorder and player is the component that manages recording and replaying of Kinect body-data files.
/// </summary>
public class KinectRecorderPlayer : MonoBehaviour
{
    // "Path to the file used to record or replay the recorded data."
    private static string filePath = "BodyRecording.txt";
    
    //"UI-Text to display information messages."
    public UnityEngine.UI.Text infoText;

    // Dictionary to store all the data from the file
    private Dictionary<float, string> danceData = new Dictionary<float, string>();

    //"Whether to start playing the recorded data, right after the scene start."
    public bool playAtStart = false;

    public kinectButtonScript startButton;
    public Dropdown selectSpeed;

    // singleton instance of the class
    private static KinectRecorderPlayer instance = null;

    // whether it is recording or playing saved data at the moment
    private bool isRecording = false;
    private bool isPlaying = false;
    
    // reference to the KM
    private KinectManager manager = null;

    // time variables used for recording and playing
    private long liRelTime = 0;
    private float fStartTime = 0f;
    private float fCurrentTime = 0f;
    private int fCurrentFrame = 0;
    private bool isPaused = true;
    private float speed = 2f;
    public Slider slider;

    // player variables
    private StreamReader fileReader = null;
    private float fPlayTime = 0f;
    private string sPlayLine = string.Empty;


    /// <summary>
    /// Gets the singleton KinectRecorderPlayer instance.
    /// </summary>
    /// <value>The KinectRecorderPlayer instance.</value>
    public static KinectRecorderPlayer Instance
    {
        get
        {
            return instance;
        }
    }

    // starts recording
    public bool StartRecording()
    {
        if (isRecording)
            return false;

        isRecording = true;

        // avoid recording an playing at the same time
        if (isPlaying && isRecording)
       /* {
            CloseFile();
            isPlaying = false;

            Debug.Log("Playing stopped.");
        } */

        // stop recording if there is no file name specified
        if (filePath.Length == 0)
        {
            isRecording = false;

            Debug.LogError("No file to save.");
            if (infoText != null)
            {
                infoText.text = "No file to save.";
            }
        }

        if (isRecording)
        {
            Debug.Log("Recording started.");
            if (infoText != null)
            {
                infoText.text = "Recording... Say 'Stop' to stop the recorder.";
            }

            // delete the old csv file
            if (filePath.Length > 0 && File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (StreamWriter writer = File.AppendText(filePath))
            {
                int kinectNum = kinectButtonScript.kinectNum;
                writer.WriteLine(kinectNum);
            }

            // initialize times
            fStartTime = fCurrentTime = Time.time;
            fCurrentFrame = 0;
        }

        return isRecording;
    }


    // starts playing
    public bool StartPlaying()
    {
        filePath = FileManager.path;
        loadFile();

        if (isPlaying)
            return false;

        isPlaying = true;

        // avoid recording an playing at the same time
        if (isRecording && isPlaying)
        {
            isRecording = false;
            Debug.Log("Recording stopped.");
        }

        // stop playing if there is no file name specified
        if (filePath.Length == 0 || !File.Exists(filePath))
        {
            isPlaying = false;
            Debug.LogError("No file to play.");

            if (infoText != null)
            {
                infoText.text = "No file to play.";
            }
        }

        if (isPlaying)
        {
            Debug.Log("Playing started.");
            if (infoText != null)
            {
                infoText.text = "Playing... Say 'Stop' to stop the player.";
            }

            // initialize times
            fStartTime = fCurrentTime = 0f;
            fCurrentFrame = -1;

            // open the file and read a line
#if !UNITY_WSA
            fileReader = new StreamReader(filePath);
#endif
            ReadLineFromFile();

            // enable the play mode
            if (manager)
            {
                manager.EnablePlayMode(true);
            }
        }

        return isPlaying;
    }


    // stops recording or playing
    public void StopRecordingOrPlaying()
    {
        if (isRecording)
        {
            isRecording = false;

            Debug.Log("Recording stopped.");
            if (infoText != null)
            {
                infoText.text = "Recording stopped.";
            }
        }

        if (isPlaying)
        {
            // close the file, if it is playing
            CloseFile();
            isPlaying = false;

            Debug.Log("Playing stopped.");
            if (infoText != null)
            {
                infoText.text = "Playing stopped.";
            }
        }

        if (infoText != null)
        {
            infoText.text = "Say: 'Record' to start the recorder, or 'Play' to start the player.";
        }
    }

    // returns if file recording is in progress at the moment
    public bool IsRecording()
    {
        return isRecording;
    }

    // returns if file-play is in progress at the moment
    public bool IsPlaying()
    {
        return isPlaying;
    }


    // ----- end of public functions -----

    void Awake()
    {
        instance = this;
    }

    void Start()
    {

        if (infoText != null)
        {
            infoText.text = "Say: 'Record' to start the recorder, or 'Play' to start the player.";
        }

        if (!manager)
        {
            manager = KinectManager.Instance;
        }
        else
        {
            Debug.Log("KinectManager not found, probably not initialized.");

            if (infoText != null)
            {
                infoText.text = "KinectManager not found, probably not initialized.";
            }
        }

        selectSpeed.onValueChanged.AddListener(delegate {
            DropdownValueChanged(selectSpeed);
        }); 

        if (playAtStart)
        {
            StartPlaying();
        }
    }

    void FixedUpdate()
    {
        if (isRecording)
        {
            // save the body frame, if any
            if (manager && manager.IsInitialized())
            {
                const char delimiter = ',';
                string sBodyFrame = manager.GetBodyFrameData(ref liRelTime, ref fCurrentTime, delimiter);

                CultureInfo invCulture = CultureInfo.InvariantCulture;

                if (sBodyFrame.Length > 0)
                {
#if !UNITY_WSA
                    using (StreamWriter writer = File.AppendText(filePath))
                    {
                        string sRelTime = string.Format(invCulture, "{0:F3}", (fCurrentTime - fStartTime));
                        writer.WriteLine(sRelTime + "|" + sBodyFrame);

                        if (infoText != null)
                        {
                            infoText.text = string.Format("Recording @ {0}s., frame {1}. Say 'Stop' to stop the player.", sRelTime, fCurrentFrame);
                        }

                        fCurrentFrame++;
                    }
#else
					string sRelTime = string.Format(invCulture, "{0:F3}", (fCurrentTime - fStartTime));
					Debug.Log(sRelTime + "|" + sBodyFrame);
#endif
                }
            }
        }

        if (isPlaying)
        {

            if (!isPaused)
            {
                //fCurrentTime += 0.01f;
                slider.value += speed;
            }
            //slider.value = fCurrentTime;
            fCurrentTime = slider.value;
            // wait for the right time
            


            float fRelTime = fCurrentTime - fStartTime;

            //Debug.Log(slider.value);
            sPlayLine = danceData[slider.value];
           // Debug.Log(slider.value);
            //Debug.Log(sPlayLine);

            //slider.value = fCurrentTime;

            if (sPlayLine != null && fRelTime >= fPlayTime)
            {
                // then play the line
                if (manager && sPlayLine.Length > 0)
                {
                    manager.SetBodyFrameData(sPlayLine);
                }

                // and read the next line
               // ReadLineFromFile();
            }

            if (sPlayLine == null)
            {
                // finish playing, if we reached the EOF
                StopRecordingOrPlaying();
            }
        }
    }

    void OnDestroy()
    {
        // don't forget to release the resources
        CloseFile();
        isRecording = isPlaying = false;
        selectSpeed.onValueChanged.RemoveAllListeners();
    }

    // reads a line from the file
    private bool ReadLineFromFile()
    {
        if (fileReader == null)
            return false;

        // read a line
        //sPlayLine = fileReader.ReadLine();
        sPlayLine = danceData[slider.value];
        //Debug.Log(slider.value);
        if (sPlayLine == null)
            return false;

        CultureInfo invCulture = CultureInfo.InvariantCulture;
        NumberStyles numFloat = NumberStyles.Float;

        //extract the unity time and the body frame
        char[] delimiters = { '|' };
        string[] sLineParts = sPlayLine.Split(delimiters);

        if (sLineParts.Length >= 2)
        {
            //float.TryParse(sLineParts[0], numFloat, invCulture, out fPlayTime);
            sPlayLine = sLineParts[1];
            fCurrentFrame++;

            if (infoText != null)
            {
                infoText.text = string.Format("Playing @ {0:F3}s., frame {1}. Say 'Stop' to stop the player.", sLineParts[0], fCurrentFrame);
            }

            return true;
        }

        return false;
    }

    // close the file and disable the play mode
    private void CloseFile()
    {
        // close the file
        if (fileReader != null)
        {
            fileReader.Dispose();
            fileReader = null;
        }

        // disable the play mode
        if (manager)
        {
            manager.EnablePlayMode(false);
        }
    }

    public void pausePlayBack()
    {
        isPaused = !isPaused;
    }


    public void loadFile()
    {
        string kinect_line;
        fileReader = new StreamReader(filePath);
        float time = 0f;
        float timeStamps = -1f;
        fileReader.ReadLine();
        while ((kinect_line = fileReader.ReadLine()) != null)
        {
            string[] kinectAsArray = kinect_line.Split('|');
            //float.TryParse(kinectAsArray[0], NumberStyles.Float, CultureInfo.InvariantCulture, out time);
            danceData.Add(time, kinectAsArray[1]);
            time += 1f;
            timeStamps += 1f;
        }
        slider.maxValue = timeStamps;
    }

    void DropdownValueChanged(Dropdown dropdown)
    {
        if (dropdown.value == 1)
        {
            speed = 1f;
        }
        else if (dropdown.value == 2)
        {
            speed = 3f;
        }
        else
        {
            speed = 2f;
        }
    }


}