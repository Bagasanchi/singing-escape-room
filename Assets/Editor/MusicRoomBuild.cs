using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;

public static class MusicRoomBuild
{
    public static void Windows()
    {
        foreach (bool full in new[]{false,true}) {
            var lockTest=new SingingLock(full);
            for(int note=0;note<lockTest.Target;note++) {
                float frequency=196*Mathf.Pow(2,SingingLock.Notes[note]/12f);
                var samples=new float[2048];
                for(int i=0;i<samples.Length;i++) samples[i]=.3f*Mathf.Sin(i*2*Mathf.PI*frequency/22050);
                float detected=SingingLock.Detect(samples,22050);
                for(int i=0;i<7;i++)lockTest.Hear(detected,.04f);
                lockTest.Hear(0,.1f);
                if(lockTest.Count!=note+1) throw new Exception("Pitch sequence failed at "+note);
            }
        }
        var retry = new SingingLock(false);
        retry.Hear(220,.2f);
        retry.Hear(220,.2f);
        if(retry.Count!=1 || !retry.Guidance.StartsWith("Pause")) throw new Exception("Repeated note cue failed");
        retry.Hear(0,.1f);
        retry.Hear(220,.2f);
        retry.Hear(250,.2f);
        if(retry.Count!=2 || !retry.Guidance.StartsWith("Too low")) throw new Exception("Low note cue failed");
        retry.Hear(390,.2f);
        if(retry.Count!=2 || !retry.Guidance.StartsWith("Too high") || retry.TargetName!="E4") throw new Exception("High note cue failed");
        retry.Hear(retry.TargetHz,.2f);
        if(retry.Count!=3) throw new Exception("Retry did not advance");
        string output=Environment.GetEnvironmentVariable("MUSIC_ROOM_BUILD") ?? "Builds/Windows/Singing Escape Room.exe";
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/SampleScene.unity"},output,BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception(report.summary.result.ToString());
    }
}
