using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.IO;

using AxDXMediaPlayerLib;

namespace DXPlayer
{
    enum PLAYER_STATE { STATE_STOPPED, STATE_PLAYING, STATE_PAUSED }
    enum STREAM_TYPE { UDP, TCP }
    enum MEDIA_TYPE { NETWORK, FILE, STREAMING }
    enum PLAY_DIRECTION { PLAY_FORWARD, PLAY_BACKWARD }
    enum AspectRatioType { Stretch = 0, Original = 1, Clip = 2 }
    enum ScaleMode { DirectDraw = 0, FFMPEG = 1 }

    class MediaInfo
    {
        public int width = 0, height = 0;
        public int totalFrames = 0;
        public double totalTime = 0.0;
        public long startTime = 0;
        public long endTime = 0;

        public void Reset()
        {
            width = height = 0;
            totalFrames = 0;
            totalTime = 0.0;
            startTime = endTime = 0;
        }
    }

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DXMediaPlayer 이벤트
        private const int PLAYER_EVENT_VIDEOSIZE = 0;
        private const int PLAYER_EVENT_STOPPED = 1;
        private const int PLAYER_EVENT_PLAYTIME = 2;
        private const int PLAYER_EVENT_FULLSCREEN = 7;
        #endregion

        #region DXMediaPlayer 마우스 이벤트
        public const int MOUSE_EVENT_MOVE = 0;

        public const int MOUSE_EVENT_LBUTTONDOWN = 1;
        public const int MOUSE_EVENT_LBUTTONUP = 2;
        public const int MOUSE_EVENT_LBUTTONDBCLK = 3;

        public const int MOUSE_EVENT_RBUTTONDOWN = 4;
        public const int MOUSE_EVENT_RBUTTONUP = 5;
        public const int MOUSE_EVENT_RBUTTONDBCLK = 6;

        public const int MOUSE_EVENT_MBUTTONDOWN = 7;
        public const int MOUSE_EVENT_MBUTTONUP = 8;
        public const int MOUSE_EVENT_MBUTTONDBCLK = 9;

        public const int MOUSE_EVENT_WHEEL = 10;
        #endregion


        #region 이미지

        private BitmapImage IMAGE_REC_START_BTN = new BitmapImage(new Uri("pack://application:,,,/Images/icon_rec_start.png"));
        private BitmapImage IMAGE_REC_STOP_BTN = new BitmapImage(new Uri("pack://application:,,,/Images/icon_rec_stop.png"));
        private BitmapImage IMAGE_PLAY_BTN = new BitmapImage(new Uri("pack://application:,,,/Images/icon_play.png"));
        private BitmapImage IMAGE_PAUSE_BTN = new BitmapImage(new Uri("pack://application:,,,/Images/icon_pause.png"));
        private BitmapImage IMAGE_SOUND_BTN = new BitmapImage(new Uri("pack://application:,,,/Images/icon_sound.png"));
        private BitmapImage IMAGE_NO_SOUND_BTN = new BitmapImage(new Uri("pack://application:,,,/Images/icon_no_sound.png"));

        #endregion

        private const int SEEK_FLAG = 1;

        private PLAYER_STATE state = PLAYER_STATE.STATE_STOPPED;
        private PLAYER_STATE State
        {
            get { return state; }
            set 
            {
                if (value == PLAYER_STATE.STATE_STOPPED)
                {
                    IsRecording = null;
                    playSpeed = 1.0f;
                    playDirection = PLAY_DIRECTION.PLAY_FORWARD;
                    mediaInfo.Reset();
                    ShowPlayTime(0.0);
                    Title = "";

                    if (state != PLAYER_STATE.STATE_STOPPED)
                    {
                        if (WindowState == System.Windows.WindowState.Normal)
                        {
                            gridMain.Width = orgGridWidth;
                            gridMain.Height = orgGridHeight;
                            SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                        }
                    }
                }
                else if (value == PLAYER_STATE.STATE_PLAYING)
                {
                    if (state != PLAYER_STATE.STATE_PAUSED)
                    {
                        IsRecording = false;

                        if (mediaType == MEDIA_TYPE.FILE)
                        {
                            Title = strURI;
                        }
                        else if (mediaType == MEDIA_TYPE.NETWORK)
                        {
                            Title = "[" + streamType.ToString() + "] " + strURI;
                        }
                        else if (mediaType == MEDIA_TYPE.STREAMING)
                        {
                            Title = serverSession + " [" + streamType.ToString() + "] " + strURI;
                            IsRecording = null;
                        }

                        ShowPlayTime(0.0);                        
                    }
                }
                state = value;

                UpdateMenuState();
            }
        }

        private bool? isRecording = false;
        private bool? IsRecording
        {
            get { return isRecording; }
            set
            {
                if (value == true)
                {
                    SaveFileDialog dlg = new SaveFileDialog();
                    dlg.Filter = "AVI (*.avi)|*.avi";
                    dlg.DefaultExt = ".avi";
                    Nullable<bool> res = dlg.ShowDialog();
                    if (res != true) return;

                    if (dxPlayer.RecordStart(dlg.FileName, GlobalEnv.recordType, GlobalEnv.recordDuration, 0) != 0)
                        return;
                }
                else
                {                    
                    if (IsLoaded)
                        dxPlayer.RecordStop();
                }
                isRecording = value;
                UpdateMenuState();
            }
        }

        private bool isSoundEnable = true;
        private bool IsSoundEnable
        {
            get { return isSoundEnable; }
            set
            {
                if (value == true)
                {
                    btnSound.Content = UpdateImage(IMAGE_SOUND_BTN);
                    btnSound.ToolTip = "음소거";

                    sliderVolume.IsEnabled = true;
                    uint volume = (uint)sliderVolume.Value;
                    volume = (volume << 16) | volume;
                    dxPlayer.AudioVolume = volume;
                }
                else
                {
                    btnSound.Content = UpdateImage(IMAGE_NO_SOUND_BTN);
                    btnSound.ToolTip = "음소거 해제";

                    sliderVolume.IsEnabled = false;
                    dxPlayer.AudioVolume = 0;
                }
                isSoundEnable = value;
            }
        }

        private bool isZoomEnable = false;
        private bool IsZoomEnable
        {
            get { return isZoomEnable; }
            set
            {
                if (value == true)
                {
                    mitemDigitalZoom.Header = "디지털줌 끄기";
                }
                else
                {
                    mitemDigitalZoom.Header = "디지털줌 켜기";
                }
                isZoomEnable = value;
            }
        }

        private ScaleMode scaleMode = ScaleMode.DirectDraw;
        private bool isBuffering = true;

        private PLAY_DIRECTION playDirection = PLAY_DIRECTION.PLAY_FORWARD;
        private float playSpeed = 1.0f;
        private const float PLAY_SPEED_UPDOWN = 0.5f;

        private AxDXMediaPlayer dxPlayer = null;
        private int videoWidth = 0, videoHeight = 0;
        private double orgGridWidth = 0, orgGridHeight = 0;

        private bool isWindowToVideoSize = false;
        private bool isDragging = false;

        private AspectRatioType aspectRatio = AspectRatioType.Original;
        private int logOutputType = 1;

        private MEDIA_TYPE mediaType = MEDIA_TYPE.FILE;
        private string strURI = null;
        private string serverSession = null;
        private ushort serverPort = 554;
        private STREAM_TYPE streamType = STREAM_TYPE.UDP;
        private MediaInfo mediaInfo = new MediaInfo();
        
        private DateTime startTime;
        private DateTime endTime;

        private bool isKeyCapture = true;

        private RecentSet<MediaPlayInfo> recentMediaList = GlobalEnv.recentMediaList;

        public MainWindow()
        {
            InitializeComponent();
            CreateDXPlayer();

            GlobalEnv.ReadConfiguration();

            sliderTimeline.Value = 0;
            sliderTimeline.IsEnabled = false;

            ShowPlayTime(0.0);

            sliderVolume.Minimum = 0;
            sliderVolume.Maximum = 0xFFFF;
            sliderVolume.Value = GlobalEnv.audioVolume;

            isWindowToVideoSize = GlobalEnv.isWindowToVideoSize == 0 ? false : true;
            IsRecording = null;
            scaleMode = (ScaleMode)GlobalEnv.scale_mode;
            aspectRatio = (AspectRatioType)GlobalEnv.aspect_ratio;

            Left = GlobalEnv.windowLocation.Left;
            Top = GlobalEnv.windowLocation.Top;
            gridMain.Width = GlobalEnv.windowLocation.Width;
            gridMain.Height = GlobalEnv.windowLocation.Height;
            SizeToContent = System.Windows.SizeToContent.WidthAndHeight;            

            State = PLAYER_STATE.STATE_STOPPED;
            
            EventManager.RegisterClassHandler(typeof(Control), Control.PreviewKeyDownEvent, new KeyEventHandler(winMain_KeyDown));
            EventManager.RegisterClassHandler(typeof(Control), Control.PreviewMouseWheelEvent, new MouseWheelEventHandler(winMain_MouseWheel));
        }

        private void CreateDXPlayer()
        {
            if (dxPlayer == null)
            {
                dxPlayer = new AxDXMediaPlayer();
                axHost.Child = dxPlayer;

                dxPlayer.OnDXMediaPlayerEvent += dxPlayer_OnEvent;
                dxPlayer.OnMouseEvent += dxPlayer_OnMouseEvent;
            }
        }

        private Image UpdateImage(BitmapImage bitmap)
        {
            Image img = new Image();
            img.Source = bitmap;
            return img;
        }

        private void UpdateMenuState()
        {
            if (State == PLAYER_STATE.STATE_STOPPED)
            {
                mitemPlayPause.Header = "재생";
                btnPlayPause.Content = UpdateImage(IMAGE_PLAY_BTN);

                mitemPlayDirection.IsEnabled = false;

                IsZoomEnable = false;
                mitemDigitalZoom.IsEnabled = false;
            }
            else if (State == PLAYER_STATE.STATE_PLAYING)
            {
                mitemPlayPause.Header = "일시정지";
                btnPlayPause.Content = UpdateImage(IMAGE_PAUSE_BTN);

                mitemPlayDirection.IsEnabled = true;
                mitemDigitalZoom.IsEnabled = true;                     
            }
            else if (State == PLAYER_STATE.STATE_PAUSED)
            {
                mitemPlayPause.Header = "재생";
                btnPlayPause.Content = UpdateImage(IMAGE_PLAY_BTN);
            }
            btnPlayPause.ToolTip = mitemPlayPause.Header;

            if (IsRecording == true)
            {
                mitemRec.Header = "녹화중지";
                btnRec.Content = UpdateImage(IMAGE_REC_STOP_BTN);
            }
            else if (IsRecording == false)
            {
                mitemRec.Header = "녹화시작";
                mitemRec.IsEnabled = true;
                btnRec.Content = UpdateImage(IMAGE_REC_START_BTN);
                btnRec.IsEnabled = true;
            }
            else if (IsRecording == null)
            {
                mitemRec.Header = "녹화중지";
                mitemRec.IsEnabled = false;
                btnRec.Content = UpdateImage(IMAGE_REC_STOP_BTN);
                btnRec.IsEnabled = false;
            }
            btnRec.ToolTip = mitemRec.Header;

            if (IsLoaded) UpdatePlayDirection();
        }

        #region Button Event Handler

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaType == MEDIA_TYPE.FILE)
            {
                if (State == PLAYER_STATE.STATE_PLAYING)
                {
                    dxPlayer.Pause();
                    State = PLAYER_STATE.STATE_PAUSED;
                }
                else if (State == PLAYER_STATE.STATE_PAUSED)
                {
                    dxPlayer.Pause();
                    State = PLAYER_STATE.STATE_PLAYING;
                }
                else if (State == PLAYER_STATE.STATE_STOPPED)
                {
                    if (strURI != null)
                        OpenFile(strURI);
                }
            }
            else if (mediaType == MEDIA_TYPE.NETWORK)
            {
                if (State == PLAYER_STATE.STATE_STOPPED)
                {
                    if (strURI != null)
                        Connect(strURI, streamType);
                }
            }
            else if (mediaType == MEDIA_TYPE.STREAMING)
            {
                if (State == PLAYER_STATE.STATE_STOPPED)
                    if (serverSession != null && strURI != null)
                        OpenStreamingSession(serverSession, strURI, streamType, serverPort);
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            ClosePlayer();
        }

        private void btnRec_Click(object sender, RoutedEventArgs e)
        {
            IsRecording = !IsRecording;
        }

        private void btnSound_Click(object sender, RoutedEventArgs e)
        {
            IsSoundEnable = !IsSoundEnable;
        }

        #endregion

        private const double AUDIO_VOLUME_RATE = 0.05;

        private void winMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (isKeyCapture == false) return;

            switch (e.Key)
            {
                case Key.D1:   // 0.5배 크기로
                    mitemWindowSize_0_5_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.D2:   // 원본 크기로
                    mitemWindowSize_1_0_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.D3:   // 1.5배 크기로
                    mitemWindowSize_1_5_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.Right:
                    if (State != PLAYER_STATE.STATE_STOPPED)
                    {                        
                        ulong ts = dxPlayer.GetPlayTime() / 1000;
                        ts += 10;
                        dxPlayer.Seek((uint)ts, 0);
                    }
                    e.Handled = true;
                    break;
                case Key.Left:
                    if (State != PLAYER_STATE.STATE_STOPPED)
                    {
                        ulong ts = dxPlayer.GetPlayTime() / 1000;
                        if (ts < 10) ts = 0;
                        else ts -= 10;
                        dxPlayer.Seek((uint)ts, 1);
                    }
                    e.Handled = true;
                    break;
                case Key.Space:
                    btnPlayPause_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.Z:
                    mitemPlaySpeedNormal_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.C:
                    mitemPlaySpeedUp_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.X:
                    mitemPlaySpeedDown_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.F:
                    mitemPlayForward_Checked(sender, e);
                    e.Handled = true;
                    break;
                case Key.D:
                    mitemPlayBackward_Checked(sender, e);
                    e.Handled = true;
                    break;
                case Key.S:
                    mitemPlayNextFrame_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.A:
                    mitemPlayContinue_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.Up:
                    if (sliderVolume.IsEnabled == true)
                    {
                        double unit = sliderVolume.Maximum * AUDIO_VOLUME_RATE;
                        sliderVolume.Value += unit;
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (sliderVolume.IsEnabled == true)
                    {
                        double unit = sliderVolume.Maximum * AUDIO_VOLUME_RATE;
                        sliderVolume.Value -= unit;
                    }
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void winMain_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sliderVolume.IsEnabled == false) return;

            double unit = sliderVolume.Maximum * AUDIO_VOLUME_RATE;
            if (e.Delta > 0)
                sliderVolume.Value += unit;
            else if (e.Delta < 0)
                sliderVolume.Value -= unit;

            e.Handled = true;
        }

        void dxPlayer_OnMouseEvent(object sender, _DDXMediaPlayerEvents_OnMouseEventEvent e)
        {
            if (isKeyCapture == false) return;

            if (e.event_type == MOUSE_EVENT_WHEEL && !IsZoomEnable)
            {
                if (sliderVolume.IsEnabled == true)
                {
                    int delta = e.flags;
                    double unit = sliderVolume.Maximum * AUDIO_VOLUME_RATE;
                    if (delta > 0)
                        sliderVolume.Value += unit;
                    else if (delta < 0)
                        sliderVolume.Value -= unit;
                }
            }
        }

        private void VideoSizeToWindowSize(int width, int height)
        {
            if (winMain.WindowState != System.Windows.WindowState.Normal) return;
            if (width == 0 && height == 0) return;
            if (State == PLAYER_STATE.STATE_STOPPED) return;

            double gridWidth = width;
            double gridHeight = height;

            for (int i = 0; i < gridMain.RowDefinitions.Count; i++)
            {
                if (i == 1) continue;
                gridHeight += gridMain.RowDefinitions[i].ActualHeight;
            }

            gridMain.Width = gridWidth;
            gridMain.Height = gridHeight;

            SizeToContent = SizeToContent.WidthAndHeight;
        }

        private delegate void DoActionEvent(_DDXMediaPlayerEvents_OnDXMediaPlayerEventEvent e);

        private void dxPlayer_OnEvent(object sender, _DDXMediaPlayerEvents_OnDXMediaPlayerEventEvent e)
        {
            DoActionEvent doActionEvent = delegate
            {
                if (e.event_type == PLAYER_EVENT_VIDEOSIZE)
                {
                    string str = e.strEvent;
                    string[] param = str.Split(',');

                    if (param.Length == 2)
                    {
                        videoWidth = int.Parse(param[0]);
                        videoHeight = int.Parse(param[1]);

                        if (isWindowToVideoSize)
                            VideoSizeToWindowSize(videoWidth, videoHeight);
                    }
                }
                else if (e.event_type == PLAYER_EVENT_STOPPED)
                {
                    ClosePlayer();
                }
                else if (e.event_type == PLAYER_EVENT_PLAYTIME)
                {
                    double ts = double.Parse(e.strEvent) / 1000;
                    if (startTime != DateTime.MinValue)
                    {
                        TimeSpan diff = TimeZone.CurrentTimeZone.GetUtcOffset(startTime);
                        TimeSpan span = startTime.Subtract(new DateTime(1970, 1, 1));
                        span -= diff;
                        ts -= span.TotalSeconds;
                    }
                    if (isDragging == false)
                        sliderTimeline.Value = ts;
                    ShowPlayTime(ts);
                }
                else if (e.event_type == PLAYER_EVENT_FULLSCREEN)
                {
                    int fullscreen = int.Parse(e.strEvent);
                    if (fullscreen == 0)
                    {
                        Show();
                    }
                    else
                    {
                        Hide();
                    }
                }
            };

            if (this.Dispatcher.Thread == System.Threading.Thread.CurrentThread)
                doActionEvent(e);
            else
                this.Dispatcher.BeginInvoke(doActionEvent, e);
        }

        private void winMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            gridMain.Width = Double.NaN;
            gridMain.Height = Double.NaN;

            if (isWindowToVideoSize == true)
            {
                if (State == PLAYER_STATE.STATE_STOPPED)
                {
                    orgGridWidth = gridMain.ActualWidth;
                    orgGridHeight = gridMain.ActualHeight;
                }
            }
            else
            {
                orgGridWidth = gridMain.ActualWidth;
                orgGridHeight = gridMain.ActualHeight;
            }
        }

        private void winMain_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAspectRatio();
            UpdateLogOutputType();
            UpdateVideoSizeCheck();
            UpdatePlayDirection();
            UpdateScaleMode();
            UpdateBufferingStatus();

            IsSoundEnable = true;

            uint volume = (uint)sliderVolume.Value;
            volume = (volume << 16) | volume;
            dxPlayer.AudioVolume = volume;

            sliderTimeline.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(sliderTimeline_PreviewMouseLeftButtonDown), true);
        }

        private void winMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClosePlayer();
            GlobalEnv.audioVolume = (uint)sliderVolume.Value;
            GlobalEnv.isWindowToVideoSize = isWindowToVideoSize == false ? 0 : 1;
            GlobalEnv.aspect_ratio = (int)aspectRatio;
            GlobalEnv.scale_mode = (int)scaleMode;

            GlobalEnv.windowLocation.X = Left;
            GlobalEnv.windowLocation.Y = Top;
            GlobalEnv.windowLocation.Width = gridMain.ActualWidth;
            GlobalEnv.windowLocation.Height = gridMain.ActualHeight;

            GlobalEnv.WriteConfiguration();
        }

        private void winMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
        }

        private void winMain_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

            if (files.Length > 0)
            {
                OpenFile(files[0]);
            }
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            uint volume = (uint)e.NewValue;
            volume = (volume << 16) | volume;

            if (IsLoaded) dxPlayer.AudioVolume = volume;
        }

        private bool ParseURL(string strURL, ref string username, ref string passwd, ref string url)
        {
            try
            {
                // rtsp://admin:1234@192.168.0.95:554/h264
                if (strURL.StartsWith("rtsp://") == false) return false;

                const int prefixLength = 7;

                do
                {
                    // Look for the ':' and '@':
                    int usernameIndex = prefixLength;
                    int colonIndex = 0, atIndex = 0;
                    for (int i = usernameIndex; i < strURL.Length && strURL[i] != '/'; ++i)
                    {
                        if (strURL[i] == ':' && colonIndex == 0)
                        {
                            colonIndex = i;
                        }
                        else if (strURL[i] == '@')
                        {
                            atIndex = i;
                            break; // we're done
                        }
                    }
                    if (atIndex == 0) break; // no '@' found

                    username = "";
                    for (int i = usernameIndex; i < colonIndex; i++) username += strURL[i];

                    passwd = "";
                    for (int i = colonIndex + 1; i < atIndex; i++) passwd += strURL[i];

                    url = "";
                    for (int i = atIndex + 1; i < strURL.Length; i++) url += strURL[i];

                    return true;
                } while (false);

                url = "";
                for (int i = prefixLength; i < strURL.Length; i++) url += strURL[i];

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        private void AddRecentMediaList(MediaPlayInfo newInfo)
        {
            for (int i = 0; i < recentMediaList.Count; i++)
            {
                MediaPlayInfo info = recentMediaList[i];
                if (info.Equals(newInfo))
                    recentMediaList.RemoveAt(i);
            }

            recentMediaList.Add(newInfo);
        }

        private void Connect(string strURL, STREAM_TYPE streamType)
        {         
            string username = "", passwd = "", url = "";
            if (ParseURL(strURL, ref username, ref passwd, ref url) == false)
            {
                MessageBox.Show("잘못된 URL 입니다", "에러");
                return;
            }

            dxPlayer.Test(0x9635371);
            int ret = dxPlayer.Connect(strURL, (int)streamType, 4);
            if (ret == -9)
            {
                LoginWindow login = new LoginWindow();
                login.Owner = this;
                if (login.ShowDialog() == true)
                {
                    strURL = "rtsp://" + login.txtBoxID.Text + ":" + login.passwd.Password + "@" + url;
                    Connect(strURL, streamType);
                }
                return;
            }

            if (ret < 0)
            {
                MessageBox.Show(strURL + " 을(를) 열지 못했습니다", "에러");
                return;
            }

            ret = dxPlayer.Play(0.0);
            if (ret < 0)
            {
                ClosePlayer();
                MessageBox.Show(strURL + " 을(를) 열지 못했습니다", "에러");
                return;
            }

            sliderTimeline.Value = 0;
            sliderTimeline.IsEnabled = false; 

            mediaInfo.Reset();
            this.mediaType = MEDIA_TYPE.NETWORK;
            this.strURI = strURL;
            this.streamType = streamType;

            State = PLAYER_STATE.STATE_PLAYING;

            AddRecentMediaList(new MediaPlayInfo(MEDIA_TYPE.NETWORK, strURI, streamType));
        }

        private bool ParseMediaInfo(string strMediaInfo)
        {
            try
            {
                int count = 0;
                string[] strTokens = strMediaInfo.Split(',');
                foreach (string strToken in strTokens)
                {
                    string[] key_value = strToken.Split(':');
                    if (key_value[0] == "width")
                    {
                        mediaInfo.width = int.Parse(key_value[1]);
                        count++;
                    }
                    else if (key_value[0] == "height")
                    {
                        mediaInfo.height = int.Parse(key_value[1]);
                        count++;
                    }
                    else if (key_value[0] == "total_frames")
                    {
                        mediaInfo.totalFrames = int.Parse(key_value[1]);
                        count++;
                    }
                    else if (key_value[0] == "total_time")
                    {
                        mediaInfo.totalTime = double.Parse(key_value[1]);
                        count++;
                    }
                    else if (key_value[0] == "start_time")
                    {
                        mediaInfo.startTime = long.Parse(key_value[1]);
                        count++;
                    }
                    else if (key_value[0] == "end_time")
                    {
                        mediaInfo.endTime = long.Parse(key_value[1]);
                        count++;
                    }
                }

                if (count == 6) return true;
                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        private void OpenFile(string filepath)
        {
            ClosePlayer();

            dxPlayer.SetTimer(1);
            int ret = dxPlayer.OpenFile(filepath);
            if (ret < 0)
            {
                MessageBox.Show(filepath + " 을(를) 열지 못했습니다", "에러");
                return;
            }

            if (ParseMediaInfo(dxPlayer.GetStringInfo("mediainfo")) == true)
            {
                sliderTimeline.Minimum = 0;
                sliderTimeline.Maximum = mediaInfo.totalTime;
                sliderTimeline.IsEnabled = true;

                if (mediaInfo.startTime != 0 && mediaInfo.endTime != 0)
                {
                    startTime = new DateTime(1970, 1, 1).AddMilliseconds(mediaInfo.startTime);
                    startTime += TimeZone.CurrentTimeZone.GetUtcOffset(startTime);

                    endTime = new DateTime(1970, 1, 1).AddMilliseconds(mediaInfo.endTime);
                    endTime += TimeZone.CurrentTimeZone.GetUtcOffset(endTime);
                }
                else
                {
                    startTime = DateTime.MinValue;
                    endTime = DateTime.MinValue;
                }
            }

            ret = dxPlayer.Play(0.0);
            if (ret < 0)
            {
                ClosePlayer();
                MessageBox.Show(filepath + " 을(를) 열지 못했습니다", "에러");
                return;
            }

            mediaType = MEDIA_TYPE.FILE;
            strURI = filepath;
            State = PLAYER_STATE.STATE_PLAYING;

            AddRecentMediaList(new MediaPlayInfo(MEDIA_TYPE.FILE, strURI, STREAM_TYPE.TCP));
        }

        private void OpenStreamingSession(string session, string strURL, STREAM_TYPE streamType, ushort port)
        {
            ClosePlayer();

            string username = "", passwd = "", url = "";
            if (ParseURL(strURL, ref username, ref passwd, ref url) == false)
            {
                MessageBox.Show("잘못된 URL 입니다", "에러");
                return;
            }

            dxPlayer.Test(0x9635371);
            int ret = dxPlayer.OpenLiveServerSession(session, strURL, (int)streamType, 2);
            if (ret == -9)
            {
                LoginWindow login = new LoginWindow();
                login.Owner = this;
                if (login.ShowDialog() == true)
                {
                    strURL = "rtsp://" + login.txtBoxID.Text + ":" + login.passwd.Password + "@" + url;
                    OpenStreamingSession(session, strURL, streamType, port);
                }
                return;
            }

            if (ret < 0)
            {
                MessageBox.Show(strURL + " 을(를) 열지 못했습니다", "에러");
                return;
            }

            ret = dxPlayer.Play(0.0);
            if (ret < 0)
            {
                ClosePlayer();
                MessageBox.Show(strURL + " 을(를) 열지 못했습니다", "에러");
                return;
            }

            ret = dxPlayer.StartServer(port);
            if (ret < 0)
            {
                ClosePlayer();
                MessageBox.Show(strURL + "서버를 시작하지 못했습니다", "에러");
                return;
            }

            sliderTimeline.Value = 0;
            sliderTimeline.IsEnabled = false;

            mediaInfo.Reset();
            this.mediaType = MEDIA_TYPE.STREAMING;
            this.strURI = strURL;
            this.serverSession = session;
            this.streamType = streamType;

            State = PLAYER_STATE.STATE_PLAYING;

            AddRecentMediaList(new MediaPlayInfo(session, strURI, streamType, port));
        }

        private void ClosePlayer()
        {
            dxPlayer.Close();
            dxPlayer.StopServer();

            sliderTimeline.IsEnabled = false;
            sliderTimeline.Value = 0;

            State = PLAYER_STATE.STATE_STOPPED;
        }

        #region 타임라인

        private void ShowPlayTime(double time)
        {
            TimeSpan playTime = TimeSpan.FromSeconds(time);
            TimeSpan totalTime = TimeSpan.FromSeconds((int)mediaInfo.totalTime);

            string strPlayTime = new DateTime(playTime.Ticks).ToString("HH:mm:ss");
            string strTime = strPlayTime + " / " + totalTime.ToString() + " x" + playSpeed;

            lbPlayTime.Content = strTime;

            if (time != 0.0 && startTime != DateTime.MinValue)
            {
                DateTime dateTime = startTime.AddSeconds(time);
                lbPlayDateTime.Content = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                lbPlayDateTime.Content = "";
            }
        }

        private void sliderTimeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowPlayTime(e.NewValue);
        }

        private void sliderTimeline_DragStarted(object sender, DragStartedEventArgs e)
        {
            isDragging = true;
        }

        private void sliderTimeline_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
        }

        private void sliderTimeline_DragDelta(object sender, DragDeltaEventArgs e)
        {
            uint timestamp = (uint)sliderTimeline.Value;
            if (startTime != DateTime.MinValue)
            {
                TimeSpan diff = TimeZone.CurrentTimeZone.GetUtcOffset(startTime);
                TimeSpan span = startTime.Subtract(new DateTime(1970, 1, 1));
                span -= diff;
                timestamp += (uint)span.TotalSeconds;
            }
            dxPlayer.Seek(timestamp, SEEK_FLAG);
        }

        private void sliderTimeline_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            uint timestamp = (uint)sliderTimeline.Value;
            if (startTime != DateTime.MinValue)
            {
                TimeSpan diff = TimeZone.CurrentTimeZone.GetUtcOffset(startTime);
                TimeSpan span = startTime.Subtract(new DateTime(1970, 1, 1));
                span -= diff;
                timestamp += (uint)span.TotalSeconds;
            }
            dxPlayer.Seek(timestamp, SEEK_FLAG);
        }

        private void sliderTimeline_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        #endregion

        private void mitemOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = GlobalEnv.MEDIA_FILE_FILTER;
            Nullable<bool> res = dlg.ShowDialog();
            if (res == true) OpenFile(dlg.FileName);
        }

        private void mitemOpenNetwork_Click(object sender, RoutedEventArgs e)
        {
            isKeyCapture = false;

            NetworkStreamOpenWindow dlg = new NetworkStreamOpenWindow();
            dlg.Owner = this;

            if (dlg.ShowDialog() == true)
            {
                string strURL = dlg.txtBoxURL.Text;
                STREAM_TYPE streamType = (STREAM_TYPE)dlg.comboStreamType.SelectedIndex;
                Connect(strURL, streamType);
            }

            isKeyCapture = true;
        }

        private void mitemClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void mitemPlayPause_Click(object sender, RoutedEventArgs e)
        {
            btnPlayPause_Click(sender, e);
        }

        private void mitemStop_Click(object sender, RoutedEventArgs e)
        {
            btnStop_Click(sender, e);
        }

        private void mitemRec_Click(object sender, RoutedEventArgs e)
        {
            btnRec_Click(sender, e);
        }

        private void mitemPlaySpeedNormal_Click(object sender, RoutedEventArgs e)
        {
            if (State != PLAYER_STATE.STATE_STOPPED)
            {
                playSpeed = 1.0f;
                dxPlayer.SetPlaySpeed(playSpeed);
                ShowPlayTime(sliderTimeline.Value);
            }
        }

        private void mitemPlaySpeedUp_Click(object sender, RoutedEventArgs e)
        {
            if (State != PLAYER_STATE.STATE_STOPPED)
            {
                if (playSpeed < 16.0)
                {
                    playSpeed += PLAY_SPEED_UPDOWN;
                    playSpeed = (float)Math.Round(playSpeed, 1);
                }
                dxPlayer.SetPlaySpeed((float)playSpeed);
                ShowPlayTime(sliderTimeline.Value);
            }
        }

        private void mitemPlaySpeedDown_Click(object sender, RoutedEventArgs e)
        {
            if (State != PLAYER_STATE.STATE_STOPPED)
            {
                if (playSpeed > 0.5f)
                {
                    playSpeed -= PLAY_SPEED_UPDOWN;
                    playSpeed = (float)Math.Round(playSpeed, 1);
                }
                dxPlayer.SetPlaySpeed(playSpeed);
                ShowPlayTime(sliderTimeline.Value);
            }
        }

        private void UpdatePlayDirection()
        {
            if (playDirection == PLAY_DIRECTION.PLAY_FORWARD)
            {
                mitemPlayForward.IsChecked = true;
                mitemPlayBackward.IsChecked = false;
                dxPlayer.PlayDirection((int)playDirection);
            }
            else
            {
                mitemPlayForward.IsChecked = false;
                mitemPlayBackward.IsChecked = true;
                dxPlayer.PlayDirection((int)playDirection);
            }
        }

        private void mitemPlayForward_Checked(object sender, RoutedEventArgs e)
        {
            playDirection = PLAY_DIRECTION.PLAY_FORWARD;
            UpdatePlayDirection();
        }

        private void mitemPlayBackward_Checked(object sender, RoutedEventArgs e)
        {
            playDirection = PLAY_DIRECTION.PLAY_BACKWARD;
            UpdatePlayDirection();
        }

        private void mitemPlayNextFrame_Click(object sender, RoutedEventArgs e)
        {
            dxPlayer.PlayNextFrame();
        }

        private void mitemPlayContinue_Click(object sender, RoutedEventArgs e)
        {
            dxPlayer.PlayContinue();
        }

        private void UpdateAspectRatio()
        {
            if (aspectRatio == AspectRatioType.Original)
            {
                mitemAspectRatioOriginal.IsChecked = true;
                mitemAspectRatioStretch.IsChecked = false;
                mitemAspectRatioClip.IsChecked = false;
                dxPlayer.SetAspectRatio(1);
            }
            else if (aspectRatio == AspectRatioType.Stretch)
            {
                mitemAspectRatioOriginal.IsChecked = false;
                mitemAspectRatioStretch.IsChecked = true;
                mitemAspectRatioClip.IsChecked = false;
                dxPlayer.SetAspectRatio(0);
            }
            else if (aspectRatio == AspectRatioType.Clip)
            {
                mitemAspectRatioOriginal.IsChecked = false;
                mitemAspectRatioStretch.IsChecked = false;
                mitemAspectRatioClip.IsChecked = true;
                dxPlayer.SetAspectRatio(2);
            }
        }

        private void mitemAspectRatioOriginal_Checked(object sender, RoutedEventArgs e)
        {
            aspectRatio = AspectRatioType.Original;
            UpdateAspectRatio();
        }

        private void mitemAspectRatioStretch_Checked(object sender, RoutedEventArgs e)
        {
            aspectRatio = AspectRatioType.Stretch;
            UpdateAspectRatio();
        }

        private void mitemAspectRatioClip_Checked(object sender, RoutedEventArgs e)
        {
            aspectRatio = AspectRatioType.Clip;
            UpdateAspectRatio();
        }

        private void mitemAspectRatioOriginal_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateAspectRatio();
        }

        private void mitemAspectRatioStretch_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateAspectRatio();
        }

        private void mitemAspectRatioClip_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateAspectRatio();
        }

        void UpdateScaleMode()
        {
            if (scaleMode == ScaleMode.DirectDraw)
            {
                mitemScaleDirectDraw.IsChecked = true;
                mitemScaleFFMPEG.IsChecked = false;
                dxPlayer.SetCommandString("DrawMode", "1");
            }
            else
            {
                mitemScaleDirectDraw.IsChecked = false;
                mitemScaleFFMPEG.IsChecked = true;
                dxPlayer.SetCommandString("DrawMode", "0");
            }
        }

        private void mitemScaleDirectDraw_Checked(object sender, RoutedEventArgs e)
        {
            scaleMode = ScaleMode.DirectDraw;
            UpdateScaleMode();
        }

        private void mitemScaleFFMPEG_Checked(object sender, RoutedEventArgs e)
        {
            scaleMode = ScaleMode.FFMPEG;
            UpdateScaleMode();
        }

        private void mitemDigitalZoom_Click(object sender, RoutedEventArgs e)
        {
            IsZoomEnable = !IsZoomEnable;
            dxPlayer.SetZoomEnable(IsZoomEnable);
        }

        private void mitemRecentMedia_MouseEnter(object sender, MouseEventArgs e)
        {
            mitemRecentMedia.Items.Clear();

            foreach (MediaPlayInfo info in recentMediaList)
            {
                string text = "";

                if (info.mediaType == MEDIA_TYPE.NETWORK)
                {
                    if (info.streamType == STREAM_TYPE.UDP) text = "[UDP] ";
                    else if (info.streamType == STREAM_TYPE.TCP) text = "[TCP] ";
                }
                else if (info.mediaType == MEDIA_TYPE.STREAMING)
                {
                    text = info.serverSession + " ";
                    if (info.streamType == STREAM_TYPE.UDP) text += "[UDP] ";
                    else if (info.streamType == STREAM_TYPE.TCP) text += "[TCP] ";
                }

                text += info.mediaURL;

                MenuItem item = new MenuItem();
                item.Header = text;
                item.Tag = info;
                item.Click += mitemRecentMedia_SubItemClicked;
                mitemRecentMedia.Items.Add(item);                
            }
        }

        private void UpdateBufferingStatus()
        {
            if (isBuffering == true)
            {
                mitemBufferingOn.IsChecked = true;
                mitemBufferingOff.IsChecked = false;
                dxPlayer.SetCommandString("TimerEnable", "1");
            }
            else
            {
                mitemBufferingOn.IsChecked = false;
                mitemBufferingOff.IsChecked = true;
                dxPlayer.SetCommandString("TimerEnable", "0");
            }
        }

        private void mitemBufferingOn_Checked(object sender, RoutedEventArgs e)
        {
            isBuffering = true;
            UpdateBufferingStatus();
        }

        private void mitemBufferingOff_Checked(object sender, RoutedEventArgs e)
        {
            isBuffering = false;
            UpdateBufferingStatus();
        }

        private void mitemRecentMedia_SubItemClicked(object sender, RoutedEventArgs e)
        {
            MenuItem mitem = sender as MenuItem;
            MediaPlayInfo info = mitem.Tag as MediaPlayInfo;
            if (info.mediaType == MEDIA_TYPE.NETWORK)
                Connect(info.mediaURL, info.streamType);
            else if (info.mediaType == MEDIA_TYPE.FILE)
                OpenFile(info.mediaURL);
            else if (info.mediaType == MEDIA_TYPE.STREAMING)
                OpenStreamingSession(info.serverSession, info.mediaURL, info.streamType, info.serverPort);
        }

        private void UpdateLogOutputType()
        {
            if (logOutputType == 0)
            {
                mitemLogOutputDbgView.IsChecked = false;
                mitemLogOutputConsole.IsChecked = true;
                dxPlayer.SetLogOutputType(0);
            }
            else
            {
                mitemLogOutputDbgView.IsChecked = true;
                mitemLogOutputConsole.IsChecked = false;
                dxPlayer.SetLogOutputType(1);
            }
        }

        private void mitemLogOutputDbgView_Checked(object sender, RoutedEventArgs e)
        {
            logOutputType = 1;
            UpdateLogOutputType();
        }

        private void mitemLogOutputConsole_Checked(object sender, RoutedEventArgs e)
        {
            logOutputType = 0;
            UpdateLogOutputType();
        }

        #region 화면크기

        private void mitemWindowSize_0_5_Click(object sender, RoutedEventArgs e)
        {
            VideoSizeToWindowSize((int)(videoWidth * 0.5), (int)(videoHeight * 0.5));
        }

        private void mitemWindowSize_1_0_Click(object sender, RoutedEventArgs e)
        {
            VideoSizeToWindowSize(videoWidth, videoHeight);
        }

        private void mitemWindowSize_1_5_Click(object sender, RoutedEventArgs e)
        {
            VideoSizeToWindowSize((int)(videoWidth * 1.5), (int)(videoHeight * 1.5));
        }

        private void UpdateVideoSizeCheck()
        {
            mitemWindowToVideoSize.IsChecked = isWindowToVideoSize;
            if (isWindowToVideoSize)
                VideoSizeToWindowSize(videoWidth, videoHeight);
        }

        private void mitemWindowToVideoSize_Click(object sender, RoutedEventArgs e)
        {
            isWindowToVideoSize = !isWindowToVideoSize;
            UpdateVideoSizeCheck();
        }

        #endregion

        private void mitemAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow win = new AboutWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void winMain_Activated(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void mitemSnapshot_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmapImage = Snapshot();
            if (bitmapImage != null)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Windows Bitmap (*.bmp)|*.bmp|PNG (*.png)|*.png";
                dlg.DefaultExt = ".bmp";
                Nullable<bool> result = dlg.ShowDialog();
                if (result == true)
                {                    
                    string fullPath = dlg.FileName;
                    string extName = dlg.FileName.Substring(dlg.FileName.LastIndexOf('.') + 1);

                    FileStream fs = new FileStream(fullPath, FileMode.Create);
                    BitmapEncoder encoder;
                    if (extName.ToLower() == "png")
                        encoder = new PngBitmapEncoder();
                    else
                        encoder = new BmpBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                    encoder.Save(fs);
                    fs.Close();
                }
            }
        }

        private BitmapImage Snapshot()
        {
            BitmapImage bitmapImage = null;
            string strBitmap = dxPlayer.SnapshotBase64();
            if (strBitmap.Length > 0)
            {
                byte[] bitmapData = Convert.FromBase64String(strBitmap);
                if (bitmapData.Length > 0)
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = new MemoryStream(bitmapData);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }
            }

            return bitmapImage;
        }

        private void mitemStreaming_Click(object sender, RoutedEventArgs e)
        {
            isKeyCapture = false;

            StreamingOpenWindow dlg = new StreamingOpenWindow();
            dlg.Owner = this;

            if (dlg.ShowDialog() == true)
            {
                string strServerSession = dlg.txtServerSession.Text;
                string strURL = dlg.txtURL.Text;
                STREAM_TYPE streamType = (STREAM_TYPE)dlg.comboStreamType.SelectedIndex;
                ushort port = ushort.Parse(dlg.txtPort.Text);
                OpenStreamingSession(strServerSession, strURL, streamType, port);
            }

            isKeyCapture = true;
        }
    }
}
 