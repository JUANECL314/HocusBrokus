using UnityEngine;

public class SequenceHints : MonoBehaviour
{
    public MagicPillarPuzzleManager puzzleManager;
    public Transform soundOriginOverride;

    private int lastInputCount = 0;

    void Start()
    {
        if (puzzleManager == null)
            puzzleManager = FindObjectOfType<MagicPillarPuzzleManager>();
    }

    void Update()
    {
        int count = GetCurrentInputCount();
        if (count != lastInputCount)
        {
            HandleInputFeedback(count);
            lastInputCount = count;
        }
    }

    int GetCurrentInputCount()
    {
        var field = typeof(MagicPillarPuzzleManager).GetField("currentInput",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var list = field.GetValue(puzzleManager) as System.Collections.Generic.List<string>;
        return list.Count;
    }

    void HandleInputFeedback(int newCount)
    {
        // Reset happened
        if (newCount == 0)
            return;

        // Puzzle is solved (full length)
        if (newCount >= puzzleManager.correctSequence.Count)
            return;

        bool stillCorrect = IsPrefixMatch(newCount);

        if (stillCorrect)
            PlayAtSelf(SfxKey.CorrectAnswerDing);
        else
            PlayAtSelf(SfxKey.WrongAnswerBuzz);
    }

    bool IsPrefixMatch(int count)
    {
        var seq = puzzleManager.correctSequence;

        var field = typeof(MagicPillarPuzzleManager).GetField("currentInput",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var input = field.GetValue(puzzleManager) as System.Collections.Generic.List<string>;

        // Extra safety
        if (count > input.Count || count > seq.Count)
            return false;

        for (int i = 0; i < count; i++)
        {
            if (input[i] != seq[i])
                return false;
        }
        return true;
    }

    void PlayAtSelf(SfxKey key)
    {
        if (soundOriginOverride != null)
            SoundManager.Instance.Play(key, soundOriginOverride);
        else
            SoundManager.Instance.Play(key, transform);
    }
}
