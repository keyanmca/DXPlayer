using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;

using Utilities;

namespace DXPlayer
{
    class MediaPlayInfo
    {
        public MEDIA_TYPE mediaType;
        public string mediaURL;
        public STREAM_TYPE streamType;
        public string serverSession;
        public ushort serverPort;

        public MediaPlayInfo()
        {
        }

        public MediaPlayInfo(MEDIA_TYPE type, string url, STREAM_TYPE stream)
        {
            mediaType = type;
            mediaURL = url;
            streamType = stream;
        }

        public MediaPlayInfo(string session, string url, STREAM_TYPE stream, ushort port)
        {
            mediaType = MEDIA_TYPE.STREAMING;
            serverSession = session;
            mediaURL = url;
            streamType = stream;
            serverPort = port;
        }

        public override bool Equals(object obj)
        {
            MediaPlayInfo info = obj as MediaPlayInfo;
            if (info.mediaType == mediaType && info.mediaURL == mediaURL && info.streamType == streamType)
            {
                if (info.mediaType == MEDIA_TYPE.STREAMING)
                {
                    if (info.serverSession != serverSession || info.serverPort != serverPort)
                        return false;
                }
                return true;
            }
            return false;
        }
    }

    class GlobalEnv
    {
        private const string INI_FILENAME = "DXMediaPlayer.ini";
        private static IniFile iniFile = new IniFile(INI_FILENAME);

        #region INI 속성변수

        public static RecentSet<MediaPlayInfo> recentMediaList = new RecentSet<MediaPlayInfo>(10);
        public static uint recordType = 0;
        public static uint recordDuration = 1800;
        public static int isWindowToVideoSize = 0;
        public static uint audioVolume = 0x7FFF;
        public static int aspect_ratio = 1;
        public static int scale_mode = 0;
        public static Rect windowLocation;

        #endregion

        public static string MEDIA_FILE_FILTER = 
            "미디어 파일 (*.avi,*.mp4,*.mkv,*.wmv,*.ts,*.asf;*.mpeg;*.mov;*.ogg;*.asx;*.flv;*.mp3;*.dxm)|" + 
            "*.avi;*.mp4;*.mkv;*.wmv;*.ts;*.asf;*.mpeg;*.mov;*.ogg;*.asx;*.flv;*.mp3;*.dxm|모든 파일 (*.*)|*.*";

        public static void ReadConfiguration()
        {
            try
            {
                string keyName, strValue;

                // read MRU
                recentMediaList.Clear();

                for (int i = 9; i >= 0; i--)
                {
                    keyName = "MRU_" + i.ToString();
                    strValue = iniFile.GetString("MRU List", keyName, "");
                    if (strValue != "")
                    {
                        MediaPlayInfo info = ParseMediaPlayInfo(strValue);
                        if (info != null) recentMediaList.Add(info);
                    }
                }

                // read Record
                recordType = (uint)iniFile.GetInt32("Record", "record_type", 0);
                recordDuration = (uint)iniFile.GetInt32("Record", "record_duration", 1800);

                // config
                isWindowToVideoSize = iniFile.GetInt32("Configuration", "isWindowToVideoSize", 1);
                audioVolume = (uint)iniFile.GetInt32("Configuration", "audio_volume", 0x7FFF);
                windowLocation = Rect.Parse(iniFile.GetString("Configuration", "window_location", "0,0,600,400"));
                aspect_ratio = iniFile.GetInt32("Configuration", "aspect_ratio", 1);
                scale_mode = iniFile.GetInt32("Configuration", "scale_mode", 0);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        private static MediaPlayInfo ParseMediaPlayInfo(string str)
        {
            MediaPlayInfo info = null;

            if (str.StartsWith("[UDP]") == true || str.StartsWith("[TCP]") == true)
            {
                info = new MediaPlayInfo();
                info.mediaType = MEDIA_TYPE.NETWORK;
                if (str.StartsWith("[UDP]") == true) info.streamType = STREAM_TYPE.UDP;
                else if (str.StartsWith("[TCP]") == true) info.streamType = STREAM_TYPE.TCP;
                info.mediaURL = str.Substring(str.IndexOf("rtsp://"));
            }
            else if (str.StartsWith("LiveStream") == true)
            {
                string[] strToken = str.Split(',');
                if (strToken.Length == 5)
                {
                    info = new MediaPlayInfo();
                    info.mediaType = MEDIA_TYPE.STREAMING;
                    info.serverSession = strToken[1];
                    if (strToken[2] == "UDP") info.streamType = STREAM_TYPE.UDP;
                    else if (strToken[2] == "TCP") info.streamType = STREAM_TYPE.TCP;
                    info.mediaURL = strToken[3];
                    info.serverPort = ushort.Parse(strToken[4]);
                }
            }
            else
            {
                info = new MediaPlayInfo();
                info.mediaType = MEDIA_TYPE.FILE;
                info.streamType = STREAM_TYPE.TCP;
                info.mediaURL = str;
            }

            return info;
        }

        public static void WriteConfiguration()
        {
            try
            {
                string keyName, strValue;

                for (int i = 0; i < recentMediaList.Count; i++)
                {
                    MediaPlayInfo info = recentMediaList[i];
                    keyName = "MRU_" + i.ToString();
                    if (info.mediaType == MEDIA_TYPE.NETWORK)
                    {
                        strValue = "[" + info.streamType + "] " + info.mediaURL;
                        iniFile.WriteValue("MRU List", keyName, strValue);
                    }
                    else if (info.mediaType == MEDIA_TYPE.FILE)
                    {
                        iniFile.WriteValue("MRU List", keyName, info.mediaURL);
                    }
                    else if (info.mediaType == MEDIA_TYPE.STREAMING)
                    {
                        strValue = "LiveStream" + "," + info.serverSession + "," + info.streamType + "," + info.mediaURL + "," + info.serverPort;
                        iniFile.WriteValue("MRU List", keyName, strValue);
                    }
                }

                iniFile.WriteValue("Configuration", "isWindowToVideoSize", isWindowToVideoSize);
                iniFile.WriteValue("Configuration", "audio_volume", audioVolume);
                iniFile.WriteValue("Configuration", "window_location", windowLocation.ToString());
                iniFile.WriteValue("Configuration", "aspect_ratio", aspect_ratio);
                iniFile.WriteValue("Configuration", "scale_mode", scale_mode);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
    }
}
