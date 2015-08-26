using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DXPlayer
{
    /// <summary>
    /// AboutWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AboutWindow : Window
    {
        private const string AboutString =
            "개발자 : grinday96@gmail.com (greenday96.blogspot.com)" + "\n" +
            "버전 : 2.0.7\n" +
            "최종 업데이트 : 2015/07/08\n" +
            "라이센스 : 본 프로그램은 ffmpeg 라이브러리를 사용하고있으며" + "\n" +
            "              LGPLv2.1 라이센스를 준수합니다" + "\n" +
            "본 프로그램은 누구나 자유롭게 사용가능합니다.";

        public AboutWindow()
        {
            InitializeComponent();

            txtAbout.Text = AboutString;
        }
    }
}
