using UnityEngine;

// Fixed-key sheet music keeps the shape of the tune familiar in every vocal range.
public sealed class MelodyStaff
{
    const int Phrase = 7, Lane = 478;
    static readonly Color Ink = new Color32(32,36,40,255), Paper = new Color32(255,253,247,255),
        Active = new Color32(174,81,14,255), Wash = new Color32(255,226,178,255),
        Done = new Color32(34,110,86,255);
    const string Letters = "CDEFGABC";
    static readonly int[] Degree = {0,0,1,1,2,3,3,4,4,5,5,6};
    GUIStyle label;
    int page;
    float changed;
    public void Reset() { page=0; changed=-1; }

    public void Draw(float w,float h,SingingLock song)
    {
        if(label==null) label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,padding=new RectOffset()};
        int current=song.Count, nextPage=current/Phrase;
        if(nextPage!=page) { page=nextPage; changed=Time.unscaledTime; }
        float slide=Mathf.SmoothStep(0,1,(Time.unscaledTime-changed)/.16f);
        GUI.BeginGroup(new Rect((w-570)/2,h-276,570,264));
        Fill(0,0,570,264,Ink); Fill(2,2,566,260,Paper);
        Text(18,6,534,22,"Phrase "+(page+1)+" / "+(song.Target/Phrase)+"     •     Note "+(current+1)+" / "+song.Target+"     •     Score in C",14,Ink);
        for(int line=0;line<5;line++) Fill(20,50+9*line,530,1,Ink);
        Fill(20,50,1,37,Ink);
        // Bass clef curls around the F line, with one dot on either side.
        Oval(34,59,8,8,Ink);
        Curve(new Vector2(32,59),new Vector2(31,46),new Vector2(54,45),new Vector2(50,61));
        Curve(new Vector2(50,61),new Vector2(49,71),new Vector2(39,79),new Vector2(31,81));
        Oval(56,54.5f,3,3,Ink); Oval(56,63.5f,3,3,Ink);
        GUI.BeginGroup(new Rect(72,29,Lane,102));
        if(page>0 && slide<1) DrawPhrase(page-1,-slide*Lane,song);
        DrawPhrase(page,page==0?0:(1-slide)*Lane,song);
        GUI.EndGroup();
        int target=Degree[SingingLock.Notes[current]];
        const int keyWidth=37,left=137,top=136;
        for(int k=0;k<8;k++) {
            int x=left+k*keyWidth;
            Fill(x,top,keyWidth+1,52,Ink); Fill(x+1,top+1,keyWidth-1,50,k==target?Wash:Color.white);
            Text(x,top+29,keyWidth,22,Letters[k].ToString(),14,k==target?Active:Ink,k==target);
            if(k==target) Fill(x+1,top+48,keyWidth-1,3,Active);
        }
        for(int k=0;k<7;k++) if(k!=2 && k!=6) Fill(left+(k+1)*keyWidth-10,top,20,29,Ink);
        Text(12,194,546,27,song.Guidance,17,Ink,true);
        Fill(80,225,410,3,new Color(.83f,.81f,.77f));
        Fill(80,225,410*song.Progress,3,Active);
        Text(8,231,554,22,song.Hits+" matched   •   "+song.Required+" to pass   •   Esc   Cancel",13,Ink);
        GUI.EndGroup();
    }
    void DrawPhrase(int index,float offset,SingingLock song)
    {
        int current=song.Count;
        for(int i=0;i<Phrase;i++) {
            int n=index*Phrase+i,degree=Degree[SingingLock.Notes[n]];
            float x=offset+(i+.5f)*Lane/Phrase;
            // Bottom line is G2. C3 sits three diatonic steps above it.
            float y=57-(3+degree)*4.5f;
            Color color=n==current?Active:n<current?(song.Matched[n]?Done:new Color(.66f,.23f,.24f)):Ink;
            bool down=degree>=1;
            Fill(x+(down?-8:6),down?y:y-28,2,28,color);
            Oval(x,y,17,10,color);
            if(i==6) Oval(x,y,9,4,Paper);
            Text(x-20,72,40,23,Letters[degree].ToString(),19,color,true);
            if(n==current) Fill(x-13,95,26,3,Active);
            if(n<current) Text(x+10,70,17,16,song.Matched[n]?"✓":"×",12,color);
        }
    }
    static void Fill(float x,float y,float w,float h,Color c) {
        GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,c,0,0);
    }
    static void Oval(float x,float y,float w,float h,Color c) {
        // Horizontal scan lines form an ellipse at the current UI scale.
        for(int row=0;row<Mathf.CeilToInt(h);row++){
            float v=(row+.5f)*2/h-1,span=w*Mathf.Sqrt(Mathf.Max(0,1-v*v));
            Fill(x-span/2,y-h/2+row,span,1,c);
        }
    }
    static void Curve(Vector2 a,Vector2 b,Vector2 c,Vector2 d) {
        for(int i=0;i<=28;i++) {
            float t=i/28f,u=1-t;
            Vector2 pt=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
            Fill(pt.x,pt.y,2.5f,2,Ink);
        }
    }
    void Text(float x,float y,float w,float h,string value,int size,Color color,bool bold=false) {
        label.fontSize=size; label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
        label.normal.textColor=color; GUI.Label(new Rect(x,y,w,h),value,label);
    }
}
