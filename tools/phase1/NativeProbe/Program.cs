// Isolated Phase 1 probe: native audio/clock/loop and Avalonia + Python process.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Hexa.NET.MiniAudio;

internal static class Program
{
    internal static string[] Arguments = [];
    [STAThread] public static int Main(string[] args)
    {
        Arguments = args;
        if(args.Length>=2){args[0]=Path.GetFullPath(args[0]);args[1]=Path.GetFullPath(args[1]);}
        if(args.Length>=4){args[2]=Path.GetFullPath(args[2]);args[3]=Path.GetFullPath(args[3]);}
        if (args.Length < 2) throw new ArgumentException("audio.wav output-directory [--offline | python worker.py]");
        Directory.CreateDirectory(args[1]);
        if(!args.Contains("--offline"))File.WriteAllText(Path.Combine(args[1],"native-integration.json"),"{\"status\":\"running\"}");
        if (args.Contains("--offline")) { Offline(args[0], args[1]); return 0; }
        AppBuilder.Configure<ProbeApp>().UsePlatformDetect().StartWithClassicDesktopLifetime([]);
        return ProbeApp.ExitCode;
    }
    internal static void Check(bool result, string label) { if (!result) throw new InvalidOperationException(label); }
    private static unsafe void Offline(string path,string output)
    {
        using var audio = new NativeAudio(path,true);
        audio.Loop(48000,96000);
        audio.Seek(48000); audio.Play();
        var buffer=(float*)NativeMemory.AllocZeroed(512*2*4);
        var wraps=0; ulong previous=0; var min=ulong.MaxValue; ulong max=0;
        try {
            for(var i=0;i<500;i++) {
                audio.Read(buffer,512);
                var cursor=audio.Cursor;
                if(cursor<previous)wraps++;
                min=Math.Min(min,cursor);max=Math.Max(max,cursor);previous=cursor;
            }
            Check(wraps>=4,"selection did not loop");
            Check(min>=48000 && max<=96000,"loop cursor escaped selection");
            audio.Pause(); var paused=audio.Cursor; audio.Read(buffer,512);
            Check(audio.Cursor==paused,"paused cursor advanced");
            audio.Seek(24000); audio.Play();audio.Read(buffer,512);
            var sought=audio.Cursor;
            Check(sought>=24000 && sought<=25024,"seek did not reach selected time");
            File.WriteAllText(Path.Combine(output,"native-offline.json"),JsonSerializer.Serialize(new {
                status="passed",native="miniaudio 0.11.25 / Hexa.NET.MiniAudio 1.0.1",mode="no-device PCM render",
                loop_start_frame=48000,loop_end_frame=96000,wraps,min_cursor=min,max_cursor=max,
                seek_cursor=sought,pause_stable=true,sample_rate=48000,frames_rendered=500*512,
                limitations=new[]{"no physical audio device","sample-cursor test, no acoustic loopback"}},new JsonSerializerOptions{WriteIndented=true}));
        } finally {NativeMemory.Free(buffer);}
    }
}
internal sealed unsafe class NativeAudio : IDisposable
{
    private readonly MaEnginePtr _engine;
    private readonly MaSoundPtr _sound;
    public NativeAudio(string file,bool offline) {
        _engine=new((MaEngine*)NativeMemory.AllocZeroed((nuint)sizeof(MaEngine)));
        _sound=new((MaSound*)NativeMemory.AllocZeroed((nuint)sizeof(MaSound)));
        var config=MiniAudio.EngineConfigInit();config.NoDevice=offline?1u:0u;config.SampleRate=48000;config.Channels=2;
        Result(MiniAudio.EngineInit(config,_engine));
        Result(MiniAudio.SoundInitFromFile(_engine,file,0,default,default,_sound));
        MiniAudio.SoundSetVolume(_sound,0.02f);
    }
    private static void Result(MaResult result) {if(result!=MaResult.Success)throw new InvalidOperationException("native result: "+result);}
    public ulong Cursor {get{ulong value=0;Result(MiniAudio.SoundGetCursorInPcmFrames(_sound,ref value));return value;}}
    public ulong Clock => MiniAudio.EngineGetTimeInPcmFrames(_engine);
    public void Play()=>Result(MiniAudio.SoundStart(_sound));
    public void Pause()=>Result(MiniAudio.SoundStop(_sound));
    public void Seek(ulong frame)=>Result(MiniAudio.SoundSeekToPcmFrame(_sound,frame));
    public void Loop(ulong begin,ulong end) {Result(MiniAudio.DataSourceSetLoopPointInPcmFrames(MiniAudio.SoundGetDataSource(_sound),begin,end));MiniAudio.SoundSetLooping(_sound,1);}
    public void Read(float* buffer,ulong frames){ulong read=0;Result(MiniAudio.EngineReadPcmFrames(_engine,buffer,frames,ref read));Program.Check(read==frames,"short PCM read");}
    public void Dispose(){MiniAudio.SoundUninit(_sound);MiniAudio.EngineUninit(_engine);NativeMemory.Free(_sound.Handle);NativeMemory.Free(_engine.Handle);}
}
public sealed class ProbeApp : Application
{
    public static int ExitCode;
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)return;
        var args=Program.Arguments;var offlineUi=args.Contains("--offline-ui");var audio=new NativeAudio(args[0],offlineUi);var pcm=new float[1536];
        var timeline=new AudioTimeline(args[0]);
        var window=new Window{Width=1200,Height=600,Title="UltraStar Phase 1 â€” native audio/timeline probe",Content=timeline};
        lifetime.MainWindow=window;
        Process? worker=null;var workerLines=0;
        if(args.Length>=4){
            var info=new ProcessStartInfo(args[2]){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Directory.GetCurrentDirectory()};
            info.ArgumentList.Add(args[3]);info.ArgumentList.Add(args[0]);
            worker=Process.Start(info)??throw new InvalidOperationException("worker start failed");
            worker.OutputDataReceived+=(_,e)=>{if(e.Data is not null)Interlocked.Increment(ref workerLines);};worker.BeginOutputReadLine();
            worker.ErrorDataReceived+=(_,e)=>{if(e.Data is not null)File.AppendAllText(Path.Combine(args[1],"worker-stderr.txt"),e.Data+Environment.NewLine);};worker.BeginErrorReadLine();
        }
        var intervals=new List<double>();var start=Stopwatch.StartNew();double prior=0;ulong previousCursor=0;
        var wraps=0;var paused=false;var resumed=false;var sought=false;var looped=false;var zoomed=false;var zoomReset=false;ulong pauseCursor=0;
        var clockStart=audio.Clock;audio.Play();
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(16)};
        timer.Tick+=(_,_)=>{
          try{
            if(offlineUi)unsafe{fixed(float* buffer=pcm)audio.Read(buffer,768);}
            var elapsed=start.Elapsed.TotalSeconds;intervals.Add((elapsed-prior)*1000);prior=elapsed;
            if(elapsed>=1.5 && !paused){audio.Pause();pauseCursor=audio.Cursor;paused=true;}
            if(elapsed>=1.9 && !resumed){Program.Check(audio.Cursor==pauseCursor,"native pause advanced");audio.Play();resumed=true;}
            if(elapsed>=2.5 && !sought){audio.Seek(24000);sought=true;}
            if(elapsed>=3.2 && !looped){audio.Loop(48000,96000);audio.Seek(48000);looped=true;}
            var cursor=audio.Cursor;if(looped && cursor<previousCursor)wraps++;previousCursor=cursor;
            if(elapsed>=4.3 && !zoomed){timeline.Viewport=4;zoomed=true;}
            if(elapsed>=5.3 && !zoomReset){timeline.Viewport=10;zoomReset=true;}
            timeline.Seconds=cursor/48000.0;timeline.InvalidateVisual();
            if(elapsed>=9){
              timer.Stop();Program.Check(wraps>=3,"native loop did not wrap");Program.Check(audio.Clock>clockStart,"device clock did not advance");
              Program.Check(timeline.RenderCount>100,"native rendering not exercised");Program.Check(zoomed&&zoomReset,"native zoom not exercised");
              var cancellation=Stopwatch.StartNew();
              if(worker is not null){Program.Check(workerLines>0,"Python worker produced no output");if(!worker.HasExited){worker.Kill(true);Program.Check(worker.WaitForExit(2000),"worker did not terminate");}}
              cancellation.Stop();
              intervals.Sort();
              var report=new{status="passed",platform=OperatingSystem.IsWindows()?"Windows":"Linux",mode=offlineUi?"no-device PCM + Xvfb native Avalonia window":"native device + native Avalonia window",
               pause_stable=true,seek_requested_frame=24000,loop_wraps=wraps,native_clock_frames=audio.Clock-clockStart,
               native_render_count=timeline.RenderCount,ui_tick_median_ms=intervals[intervals.Count/2],ui_tick_p95_ms=intervals[(int)(intervals.Count*.95)],
               render_p95_ms=timeline.RenderTimes.Order().ElementAt((int)(timeline.RenderTimes.Count*.95)),python_worker_messages=workerLines,worker_kind=Environment.GetEnvironmentVariable("ULTRASTAR_PHASE1_UMX_CACHE") is null?"SwiftF0 CPU":"Open-Unmix CPU four threads",worker_terminated=worker is null||worker.HasExited,cancellation_ms=cancellation.Elapsed.TotalMilliseconds,real_annotated_notes=File.Exists(args[0]+".notes.csv"),waveform=true,placeholder_word_labels=true,zoom_during_playback=zoomed&&zoomReset,
               limitations=new[]{offlineUi?"no physical device; PCM pumped on UI timer":"one default device","no acoustic loopback latency measurement","small proof, not full editor"}};
              File.WriteAllText(Path.Combine(args[1],"native-integration.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
              audio.Dispose();window.Close();lifetime.Shutdown();
            }
          }catch(Exception e){timer.Stop();ExitCode=1;File.WriteAllText(Path.Combine(args[1],"failure.txt"),e.ToString());audio.Dispose();if(worker is not null&&!worker.HasExited)worker.Kill(true);lifetime.Shutdown(1);}
        };
        timer.Start();base.OnFrameworkInitializationCompleted();
    }
}
internal sealed class AudioTimeline : Control
{
    private readonly float[] _peaks=new float[1000];
    private readonly List<(double start,double end,double pitch)> _notes=[];
    public double Seconds;public double Viewport=10;public int RenderCount;public readonly List<double> RenderTimes=[];
    public AudioTimeline(string file){
      if(File.Exists(file+".notes.csv"))foreach(var line in File.ReadLines(file+".notes.csv")){var parts=line.Split(',');_notes.Add((double.Parse(parts[0],System.Globalization.CultureInfo.InvariantCulture),double.Parse(parts[2],System.Globalization.CultureInfo.InvariantCulture)+double.Parse(parts[0],System.Globalization.CultureInfo.InvariantCulture),double.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture)));}
      PointerWheelChanged+=(_,e)=>{Viewport=Math.Clamp(Viewport*(e.Delta.Y>0?.8:1.25),2,40);InvalidateVisual();};
      using var reader=new BinaryReader(File.OpenRead(file));reader.ReadBytes(12);
      while(reader.BaseStream.Position<reader.BaseStream.Length-8){var tag=new string(reader.ReadChars(4));var length=reader.ReadUInt32();
       if(tag!="data"){reader.BaseStream.Seek(length+(length%2),SeekOrigin.Current);continue;}
       var samples=reader.ReadBytes((int)length);for(var i=0;i<1000;i++){var begin=(int)((long)i*samples.Length/1000);var end=(int)((long)(i+1)*samples.Length/1000);begin-=begin%2;for(var p=begin;p+1<end;p+=2)_peaks[i]=Math.Max(_peaks[i],Math.Abs(BitConverter.ToInt16(samples,p)/32768f));}break;}
    }
    public override void Render(DrawingContext c){var timer=Stopwatch.StartNew();c.FillRectangle(new SolidColorBrush(Color.Parse("#101B2B")),new Rect(Bounds.Size));
      for(var i=0;i<_peaks.Length;i++){var x=i*Bounds.Width/_peaks.Length;c.DrawLine(new Pen(Brushes.SkyBlue,1),new Point(x,Bounds.Height*.8-_peaks[i]*70),new Point(x,Bounds.Height*.8+_peaks[i]*70));}
      var offset=Math.Max(0,Seconds-Viewport*.6);
      if(_notes.Count==0)for(var i=0;i<4000;i++)_notes.Add((i*.02,i*.02+.1,220*Math.Pow(2,(i%30)/12.0)));
      for(var i=0;i<_notes.Count;i++){var n=_notes[i];if(n.end<offset||n.start>offset+Viewport)continue;
       var x=(n.start-offset)*Bounds.Width/Viewport;var width=Math.Max(1,(n.end-n.start)*Bounds.Width/Viewport);
       var midi=69+12*Math.Log2(n.pitch/440);var y=(84-midi)*Bounds.Height*.65/36;
       c.FillRectangle(Brushes.MediumAquamarine,new Rect(x,y,width,10));
       var text=new FormattedText("ord "+(i+1),System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,Typeface.Default,12,Brushes.GhostWhite);
       c.DrawText(text,new Point(x,Bounds.Height*.69));
      }
      var playhead=(Seconds-offset)*Bounds.Width/Viewport;c.DrawLine(new Pen(Brushes.White,2),new Point(playhead,0),new Point(playhead,Bounds.Height));RenderCount++;RenderTimes.Add(timer.Elapsed.TotalMilliseconds);
    }
}