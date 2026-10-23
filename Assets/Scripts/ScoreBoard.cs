using System;

// Keeps the training stats: shots, hits, headshots and the points from the scoring targets.
public static class ScoreBoard
{
    public static int Shots { get; private set; }
    public static int Hits { get; private set; }
    public static int Headshots { get; private set; }
    public static int Points { get; private set; }
    public static int LastPoints { get; private set; }

    public static float Accuracy => Shots == 0 ? 0f : (float)Hits / Shots;

    // true = the hit was on a head
    public static event Action<bool> HitRegistered;

    public static void AddShots(int count)
    {
        Shots += count;
    }

    public static void AddBodyHit(bool head)
    {
        Hits++;
        if (head) Headshots++;
        HitRegistered?.Invoke(head);
    }

    public static void AddTargetHit(int points)
    {
        Hits++;
        Points += points;
        LastPoints = points;
        HitRegistered?.Invoke(points >= 10);
    }

    public static void Reset()
    {
        Shots = 0;
        Hits = 0;
        Headshots = 0;
        Points = 0;
        LastPoints = 0;
    }
}
