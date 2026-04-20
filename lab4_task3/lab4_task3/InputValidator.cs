using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace lab4_task3
{
    public static class InputValidator
    {
        private static readonly string TextOnlyPattern = @"^[а-яА-ЯіІїЇєЄґҐa-zA-Z\s\-']+$";

        public static void AttachTextOnly(Control control)
        {
            control.PreviewTextInput += TextOnly_PreviewTextInput;
            DataObject.AddPastingHandler(control, TextOnly_Pasting);
        }

        private static void TextOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, TextOnlyPattern);
        }

        private static void TextOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, TextOnlyPattern))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        public static void AttachIntOnly(Control control)
        {
            control.PreviewTextInput += IntOnly_PreviewTextInput;
            DataObject.AddPastingHandler(control, IntOnly_Pasting);
        }

        private static void IntOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private static void IntOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, @"^\d+$"))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        public static void AttachDecimalOnly(Control control)
        {
            control.PreviewTextInput += DecimalOnly_PreviewTextInput;
            DataObject.AddPastingHandler(control, DecimalOnly_Pasting);
        }

        private static void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            if (Regex.IsMatch(e.Text, @"^\d+$")) return;

            if ((e.Text == "," || e.Text == ".") && !textBox.Text.Contains(",") && !textBox.Text.Contains("."))
                return;

            if (e.Text == "-" && textBox.SelectionStart == 0 && !textBox.Text.Contains("-"))
                return;

            e.Handled = true;
        }

        private static void DecimalOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                text = text.Replace('.', ',');
                if (!double.TryParse(text, out _))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        public static bool HasAtLeastOneLetter(string text)
        {
            return Regex.IsMatch(text ?? "", @"[а-яА-ЯіІїЇєЄґҐa-zA-Z]");
        }
    }
}