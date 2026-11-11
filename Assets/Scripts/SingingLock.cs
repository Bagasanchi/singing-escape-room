using System;
using UnityEngine;

// Each note has a fixed time slot. The first voiced pitch sets the singer's key.
public sealed class SingingLock
{
    public static readonly int[] Notes = {
        0,0,7,7,9,9,7, 5,5,4,4,2,2,0,
        7,7,5,5,4,4,2, 7,7,5,5,4,4,2,
        0,0,7,7,9,9,7, 5,5,4,4,2,2,0 };
    public const float Beat = .4f;
    public const float LeadIn = 4 * Beat;
    public int Count { get; private set; }
    public int Target { get; }
    public int Hits { get; private set; }
    public int Required => Mathf.CeilToInt(Target * .70f);
    public readonly bool[] Matched;
    public float Progress { get; private set; }
    public string Guidance { get; private set; }
    float start, end, lastHeard = -1, credit, tonic;
    bool keyed;
    public SingingLock(bool full) {
        Target = full ? Notes.Length : 7;
        Matched = new bool[Target];
        start = LeadIn; end = start + Beat;
        Guidance = "Get ready";
    }
    public void Advance(float time) {
        while (Count < Target && time >= end) {
            Count++; start = end; end += Beat * (Count % 7 == 6 ? 2 : 1);
            credit = 0; Guidance = "Sing the highlighted note";
        }
        Progress = Mathf.Clamp01((time-start)/(end-start));
        if (time < LeadIn) Guidance = "Get ready   " + Mathf.CeilToInt((LeadIn-time)/Beat);
    }
    public void Hear(float hz, float time) {
        Advance(time);
        float dt = Mathf.Clamp(time - Mathf.Max(lastHeard,start), 0, .04f);
        lastHeard = time;
        if (Count >= Target || time < start || Matched[Count]) return;
        if (hz <= 0) { Guidance = "Sing the highlighted note"; return; }
        float pitch = 12*Mathf.Log(hz/440f,2);
        if (!keyed) { tonic = pitch - Notes[Count]; keyed = true; }
        float error = pitch - tonic - Notes[Count];
        if (Mathf.Abs(error) > 1f) { Guidance = error < 0 ? "Sing higher" : "Sing lower"; return; }
        credit += dt;
        Guidance = "On pitch";
        if (credit >= .08f) { Matched[Count] = true; Hits++; Guidance = "Matched"; }
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
