using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class MusicRoom : MonoBehaviour
{
    readonly string[] clues = {
        "A brass star. Its inscription reads: a little light that twinkles twice.",
        "An astronomer's sketch shows a diamond hanging high above the world.",
        "The music box plays the same tune as the alphabet song. Its lid reads: Twinkle, Twinkle, Little Star." };
    readonly GameObject[] seals = new GameObject[3];
    readonly bool[] found = new bool[3];
    readonly Color brass = new Color(.93f,.65f,.22f);
    GameObject door;
    Camera eye;
    FirstPersonPlayer walker;
    AudioSource speaker;
    AudioClip mic;
    string device, message = "", prompt = "";
    float messageUntil, nextSample, started, performanceStart;
    int cluesFound, lastPosition;
    bool full, open, encore, escaped, preview;
    readonly MelodyStaff staff = new MelodyStaff();
    SingingLock song;
    float[] raw, mono;
    AudioClip example;

    void Start()
    {
        walker=GetComponent<FirstPersonPlayer>(); eye=walker.playerCamera.GetComponent<Camera>();
        speaker=gameObject.AddComponent<AudioSource>();
        var teal=new Color(.10f,.25f,.28f);
        foreach (string name in new[]{"Wall_Left","Wall_Right","Wall_Front"}) Paint(GameObject.Find(name),teal);
        Paint(GameObject.Find("Floor"),new Color(.42f,.31f,.23f));
        Paint(GameObject.Find("Table_Top"),new Color(.49f,.26f,.11f));
        Paint(GameObject.Find("Table_Leg"),new Color(.28f,.15f,.08f));
        GameObject.Find("Wall_Back").SetActive(false);
        Box("Left jamb",new Vector3(-1.8f,1.5f,3.1f),new Vector3(4.4f,3,.2f),teal);
        Box("Right jamb",new Vector3(2.9f,1.5f,3.1f),new Vector3(2.2f,3,.2f),teal);
        Box("Lintel",new Vector3(1.1f,2.65f,3.1f),new Vector3(1.4f,.7f,.2f),teal);
        door=Box("Singing door",new Vector3(1.1f,1.15f,3.1f),new Vector3(1.4f,2.3f,.18f),new Color(.55f,.32f,.12f));
        Box("Threshold",new Vector3(1.1f,-.1f,4.4f),new Vector3(2.5f,.2f,3),teal);
        Box("Ceiling",new Vector3(0,3.1f,0),new Vector3(8.4f,.2f,6.4f),teal);
        var lamp=new GameObject("Reading light").AddComponent<Light>();
        lamp.type=LightType.Point; lamp.transform.position=new Vector3(0,2.7f,0); lamp.range=10; lamp.intensity=5; lamp.color=new Color(1,.88f,.72f);
        seals[0]=GameObject.Find("Hint"); Paint(seals[0],brass);
        seals[1]=Box("Star chart",new Vector3(-3.95f,1.35f,-1.7f),new Vector3(.09f,.42f,.55f),brass);
        seals[2]=Box("Music box",new Vector3(2.9f,.17f,-2.4f),new Vector3(.52f,.34f,.38f),brass);
    }
    static void Paint(GameObject obj,Color color) { obj.GetComponent<Renderer>().material.color=color; }
    static GameObject Box(string name,Vector3 position,Vector3 size,Color color)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name;
        obj.transform.position=position; obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=GameObject.Find("Table_Top").GetComponent<Renderer>().sharedMaterial;
        Paint(obj,color); return obj;
    }
    void Say(string text) { message=text; messageUntil=Time.unscaledTime+7; }
    void Update()
    {
        var keys=Keyboard.current; if(keys==null)return;
        if(escaped) { if(keys.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
        if(open) {
            var pos=door.transform.position; pos.y=Mathf.MoveTowards(pos.y,3.55f,Time.deltaTime*2); door.transform.position=pos;
            if(transform.position.z>3.35f) { StopListening(); speaker.Stop(); escaped=true; walker.enabled=false; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; return; }
        }
        prompt="";
        if(preview)return;
        if(song!=null) {
            if(keys.escapeKey.wasPressedThisFrame) StopListening();
            else {
                song.Advance(Time.unscaledTime-performanceStart);
                if(song.Count==song.Target) FinishPerformance();
                else Sample();
            }
            return;
        }
        if(Physics.Raycast(eye.transform.position,eye.transform.forward,out var hit,2.6f)) {
            int index=System.Array.IndexOf(seals,hit.collider.gameObject);
            if(index>=0) {
                prompt="E   Read clue";
                if(keys.eKey.wasPressedThisFrame) {
                    if(!found[index]) { found[index]=true; cluesFound++; }
                    Say(clues[index]);
                }
            }
        }
        if(cluesFound==3 && !encore && Vector2.Distance(new Vector2(transform.position.x,transform.position.z),new Vector2(1.1f,3))<2.5f) {
            if(open)full=true;
            prompt="E   Sing     P   Hear melody"+(open?"":"     Tab   Change length");
            if(!open && keys.tabKey.wasPressedThisFrame) full=!full;
            if(keys.pKey.wasPressedThisFrame)StartCoroutine(Preview());
            else if(keys.eKey.wasPressedThisFrame)Begin();
        }
    }
    IEnumerator Preview()
    {
        preview=true;
        int count=full?42:7, rate=22050, beat=Mathf.RoundToInt(rate*SingingLock.Beat);
        var data=new float[(count+count/7)*beat]; int start=0;
        for(int n=0;n<count;n++) {
            int length=beat*(n%7==6?2:1);
            float hz=261.63f*Mathf.Pow(2,SingingLock.Notes[n]/12f);
            for(int i=0;i<length*.88f;i++) data[start+i]=.18f*Mathf.Sin(2*Mathf.PI*hz*i/rate)*Mathf.Min(1,i/100f)*Mathf.Exp(-3f*i/length);
            start+=length;
        }
        example=AudioClip.Create("Melody",data.Length,1,rate,false); example.SetData(data,0);
        speaker.clip=example; speaker.Play();
        while(speaker.isPlaying) { if(Keyboard.current.escapeKey.wasPressedThisFrame)speaker.Stop(); yield return null; }
        speaker.clip=null; Destroy(example);

        preview=false;
    }
    void Begin()
    {
        try {
            if(Microphone.devices.Length==0) { Say("No microphone found. Connect one and press E again."); return; }
            device=Microphone.devices[0]; mic=Microphone.Start(device,true,2,22050);
            if(!mic) { Say("Microphone unavailable. Check microphone access and retry."); return; }
            mono=new float[Mathf.NextPowerOfTwo(mic.frequency/32)]; raw=new float[mono.Length*mic.channels];
            staff.Reset(); song=new SingingLock(full); performanceStart=started=Time.unscaledTime; lastPosition=-1;
        } catch(System.Exception) { StopListening(); Say("Microphone unavailable. Check input permissions and retry."); }
    }
    void Sample()
    {
        float now=Time.unscaledTime;
        if(now<nextSample)return; nextSample=now+.02f;
        int pos=Microphone.GetPosition(device);
        if(pos<=0 || pos==lastPosition) {
            if(now-started>3) { StopListening(); Say("No microphone signal. Check the input device and retry."); } return;
        }
        if(lastPosition<0 && pos<mono.Length)return;
        lastPosition=pos; started=now;
        if(!mic.GetData(raw,(pos-mono.Length+mic.samples)%mic.samples)) { StopListening(); Say("Could not read microphone. Try again."); return; }
        for(int i=0;i<mono.Length;i++) mono[i]=raw[i*mic.channels];
        song.Hear(SingingLock.Detect(mono,mic.frequency),now-performanceStart);
    }
    void FinishPerformance() {
        string score=song.Hits+" / "+song.Target+" notes";
        bool passed=song.Hits>=song.Required;
        int needed=song.Required;
        StopListening();
        if (!passed) { Say(score+". Match "+needed+" to pass. Press E at the door to retry."); return; }
        open=true; encore=full;
        Say(score+(encore?"   Golden Star earned!":"   Door unlocked!")+" Walk through the doorway.");
    }
    void StopListening() { if(mic) { Microphone.End(device); Destroy(mic); } mic=null; song=null; }
    void OnDisable() { StopListening(); }
    void OnGUI()
    {
        float scale=Mathf.Min(Screen.width/800f,Screen.height/480f);
        GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        float w=Screen.width/scale,h=Screen.height/scale;
        if(escaped) { GUI.Box(new Rect(w/2-190,h/2-50,380,100),"ROOM CLEARED\n\n"+(encore?"Golden Star  •  Full verse":"Silver Star  •  Opening phrase")+"\n\nR   Play again"); return; }
        GUI.Box(new Rect(14,14,310,64),"THE MUSIC ROOM    •    "+cluesFound+"/3 clues\n"+(open?"Exit through the singing door":cluesFound<3?"Find the three brass objects":"Approach the door   •   "+(full?"Full verse":"Opening phrase"))+"\nWASD   Walk      Mouse   Look");
        GUI.Label(new Rect(w/2-3,h/2-10,18,20),"+");
        if(song!=null) {
            staff.Draw(w,h,song);
            return;
        }
        string text=
            preview?"Listen to the melody    •    Esc   Stop":prompt;
        if(text.Length>0)GUI.Box(new Rect(w/2-255,h-72,510,52),text);
        if(song==null && Time.unscaledTime<messageUntil)GUI.Box(new Rect(w/2-315,h-132,630,50),message);
    }
}
