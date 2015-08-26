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
    /// Interaction logic for StreamingOpenWindow.xaml
    /// </summary>
    public partial class StreamingOpenWindow : Window
    {
        public StreamingOpenWindow()
        {
            InitializeComponent();
        }

        private void CheckInput()
        {
            txtServerSession.Text = txtServerSession.Text.Trim();
            txtURL.Text = txtURL.Text.Trim();
            txtID.Text = txtID.Text.Trim();
            txtPassword.Password = txtPassword.Password.Trim();
            txtPort.Text = txtPort.Text.Trim();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            CheckInput();

            if (txtServerSession.Text.Length == 0)
            {
                MessageBox.Show("서버세션을 입력하세요", "에러");
                txtServerSession.Focus();
                return;
            }

            if (txtURL.Text.Length == 0)
            {
                MessageBox.Show("URL을 입력하세요", "에러");
                txtURL.Focus();
                return;
            }

            if (txtPort.Text.Length == 0)
            {
                MessageBox.Show("서버포트를 입력하세요", "에러");
                txtPort.Focus();
                return;
            }

            if (checkAuth.IsChecked == true)
            {
                if (txtID.Text.Length > 0 && txtPassword.Password.Length > 0)
                {
                    // make rtsp URL
                    int index = txtURL.Text.IndexOf("rtsp://") + 7;
                    if (index >= 0)
                        txtURL.Text = txtURL.Text.Insert(7, txtID.Text + ":" + txtPassword.Password + "@");
                }
            }

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void checkAuth_Checked(object sender, RoutedEventArgs e)
        {
            if (checkAuth.IsChecked == true)
            {
                txtID.IsEnabled = true;
                txtPassword.IsEnabled = true;
            }
            else
            {
                txtID.Text = "";
                txtPassword.Password = "";
                txtID.IsEnabled = false;
                txtPassword.IsEnabled = false;
            }
        }

        private void OnNumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = TextInputValidator.IsTextNumeric(e.Text);
        }

        private void txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                btnOK_Click(sender, e);
        }
    }
}
