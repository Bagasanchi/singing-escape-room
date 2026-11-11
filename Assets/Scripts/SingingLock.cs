using System;
using UnityEngine;

// The first sung note sets the key. A short release separates repeated notes.
public sealed class SingingLock
{
    public static readonly int[] Notes = {
        0,0,7,7,9,9,7, 5,5,4,4,2,2,0,
        7,7,5,5,4,4,2, 7,7,5,5,4,4,2,
        0,0,7,7,9,9,7, 5,5,4,4,2,2,0 };
    public int Count { get; private set; }
    public int Target { get; }
    public string Guidance { get; private set; } = "Sing a comfortable starting note.";
    public float TargetHz => Count == 0 ? 261.63f : 440f * Mathf.Pow(2, (tonic + Notes[Math.Min(Count, Target-1)]) / 12f);
    public string TargetName {
        get {
            if (Count == 0) return "Any starting note";
            int midi = Mathf.RoundToInt(69 + 12 * Mathf.Log(TargetHz / 440f, 2));
            return Names[midi % 12] + (midi / 12 - 1);
        }
    }
    static readonly string[] Names = {"C","C#","D","D#","E","F","F#","G","G#","A","A#","B"};
    float tonic, held, candidate, release;
    bool separated = true;
    public SingingLock(bool full) { Target = full ? Notes.Length : 7; }

    public void Hear(float hz, float seconds)
    {
        if (Count >= Target) return;
        if (hz <= 0) {
            held = 0; release += seconds;
            if (release >= .06f) {
                separated = true;
                if (Guidance.StartsWith("Pause") || Guidance.StartsWith("On pitch"))
                    Guidance = "Sing the highlighted note.";
            }
            return;
        }
        release = 0;
        float pitch = 12f * Mathf.Log(hz / 440f, 2);
        if (Count == 0 && (held == 0 || Mathf.Abs(pitch - candidate) > .8f)) { candidate = pitch; held = 0; }
        float relative = pitch - (Count == 0 ? candidate : tonic);
        if (Count > 0 && Mathf.Abs(relative - Notes[Count-1]) > 1.4f) separated = true;
        float difference = relative - Notes[Count];
        if (Mathf.Abs(difference) > 1f) {
            Guidance = difference < 0 ? "Too low  -  sing higher." : "Too high  -  sing lower.";
            held = 0; return;
        }
        if (!separated && Count > 0 && Notes[Count] == Notes[Count-1]) {
            Guidance = "Pause briefly, then sing that same note again.";
            held = 0; return;
        }
        Guidance = "On pitch  -  hold it.";
        held += seconds;
        if (held < .15f) return;
        if (Count == 0) tonic = candidate;
        Count++; held = 0; separated = false;
        Guidance = Count < Target && Notes[Count] == Notes[Count-1]
            ? "Pause briefly, then sing that same note again." : "Sing the highlighted note.";
    }

    public static float Detect(float[] samples, int rate)
    {
        // Compare a waveform with delayed copies, choosing its first strong peak.
        int n = samples.Length / 2;
        double energy = 0;
        for (int i=0; i<n; i++) energy += samples[i]*samples[i];
        if (energy/n < .000018) return 0;
        double prev = 0, before = 0;
        int end = Math.Min(rate / 65, n-1);
        for (int lag=Math.Max(2,rate/1050); lag<=end; lag++)
        {
            double dot=0, other=0;
            for (int i=0; i<n; i++) { float b=samples[i+lag]; dot+=samples[i]*b; other+=b*b; }
            double score=dot/Math.Sqrt(energy*other+1e-15);
            if (prev > .86 && prev >= score && prev > before) return (float)rate/(lag-1);
            before=prev; prev=score;
        }
        return 0;
    }
}
