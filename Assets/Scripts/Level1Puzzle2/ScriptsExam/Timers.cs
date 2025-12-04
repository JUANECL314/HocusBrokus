using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
public class Timers : MonoBehaviour
{

    [Header("UI")]
    public Slider timerBar;
    public TMP_Text secondsText;
    public float maxTime = 15f;
    public float currentTime = 0f;
    private Coroutine _coroutineActualTimer;
    public bool isIncreasing = false;
    private bool _previousState = false;
    
    void Start()
    {
        SettingsUI();
        StartCoroutineTimer();
    }

    void StartCoroutineTimer()
    {
        if (_coroutineActualTimer != null) StopCoroutine(_coroutineActualTimer);
        
        _coroutineActualTimer = StartCoroutine(isIncreasing ? timerIncrease() : timerDecrease());
    }
    void Update()
    {
       if(isIncreasing != _previousState)
        {
            StartCoroutineTimer();
            _previousState = isIncreasing;
        }
    }
    
    void SettingsUI()
    {
        if (timerBar != null)
        {
            timerBar.interactable = false;
            timerBar.minValue = 0;
            timerBar.maxValue = maxTime;
            timerBar.value = currentTime;
        }
        if (secondsText != null) secondsText.text = $"{currentTime} s";
    }

    IEnumerator timerDecrease()
    {
        while (currentTime > 0)
        {
            yield return new WaitForSeconds(1f);
            currentTime--;
            UpdateTimer();
        }
    }

    IEnumerator timerIncrease()
    {
        while (currentTime < maxTime)
        {
            yield return new WaitForSeconds(1f);
            currentTime++;
            UpdateTimer();
        }
    } 

    void UpdateTimer()
    {
        float rangeInValueSliderBar = Mathf.Clamp(currentTime, 0, maxTime);
        if ( secondsText != null ) 
        {
            secondsText.text = $"{rangeInValueSliderBar} s";
        }
        
        if (timerBar != null) { 

            timerBar.value = rangeInValueSliderBar;
        }
    }
}
