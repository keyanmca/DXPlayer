using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Controls;

namespace DXPlayer
{
    static class TextInputValidator
    {
        private static string regexPortNumber = "^0*(?:6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[1-9][0-9]{1,3}|[0-9])$";
        private static string regexIPAddress = @"^(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))$";

        public static void PreviewTextInputPortNumberCheck(object sender, TextCompositionEventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            string strInput = txtBox.Text + e.Text;
            if (strInput.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                ushort port;
                e.Handled = ushort.TryParse(strInput, out port);
                e.Handled = !Regex.IsMatch(strInput, regexPortNumber);
            }
        }

        public static void PreviewTextInputNumberCheck(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[0-9]$");
        }

        public static bool IsTextNumeric(string str)
        {
            return !Regex.IsMatch(str, "[0-9]$");
        }

        public static bool CheckValidIPAddress(string ip)
        {
            return Regex.IsMatch(ip, regexIPAddress);
        }
    }
}
