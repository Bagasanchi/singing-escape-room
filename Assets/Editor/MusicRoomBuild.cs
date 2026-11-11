using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;

public static class MusicRoomBuild
{
    public static void Windows()
    {
        foreach(bool full in new[]{false,true}) {
            var game=new SingingLock(full);
            float time=SingingLock.LeadIn;
            for(int n=0;n<game.Target;n++) {
                float length=SingingLock.Beat*(n%7==6?2:1);
                float hz=196*Mathf.Pow(2,SingingLock.Notes[n]/12f);
                float[] wave=new float[1024];
                for(int i=0;i<wave.Length;i++) wave[i]=.3f*Mathf.Sin(i*2*Mathf.PI*hz/22050);
                float input=n==1?0:n==3?900:SingingLock.Detect(wave,22050);
                for(int j=1;j<=10;j++) game.Hear(input,time+j*.022f);
                time+=length;
                game.Advance(time+.001f);
                if(game.Count!=n+1 || game.Matched[n]!=(n!=1 && n!=3)) throw new Exception("Timed scoring failed at "+n);
            }
            if(game.Hits!=game.Target-2 || game.Hits<game.Required) throw new Exception("Passing score failed");
            var silent=new SingingLock(full);
            silent.Advance(100);
            if(silent.Count!=silent.Target || silent.Hits!=0) throw new Exception("Silent performance did not finish");
        }
        string output=Environment.GetEnvironmentVariable("MUSIC_ROOM_BUILD") ?? "Builds/Windows/Singing Escape Room.exe";
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/SampleScene.unity"},output,BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception(report.summary.result.ToString());
    }
}
