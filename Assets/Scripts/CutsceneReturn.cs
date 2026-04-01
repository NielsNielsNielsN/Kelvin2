using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.Playables;
public class CutsceneReturn : MonoBehaviour
{
    public GameObject[] cutsceneVcams;
    public PlayableDirector director;
    public GameObject cutsceneVcam;

    void OnEnable()
    {
        director.stopped += OnCutsceneEnd;
    }

    void OnDisable()
    {
        director.stopped -= OnCutsceneEnd;
    }

    void OnCutsceneEnd(PlayableDirector d)
    {
        foreach (var vcam in cutsceneVcams)
            vcam.SetActive(false);
    }
}
