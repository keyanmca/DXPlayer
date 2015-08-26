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
    /// LoginWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtBoxID.Focus();
        }

        private bool CheckInput()
        {
            if (txtBoxID.Text.Length == 0 && passwd.Password.Length == 0)
                return false;
            return true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (CheckInput() == false) return;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void txtBoxID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (CheckInput() == false) return;
                DialogResult = true;
            }
        }

        private void passwd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (CheckInput() == false) return;
                DialogResult = true;
            }
        }
    }
}
