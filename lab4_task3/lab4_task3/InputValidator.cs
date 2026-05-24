using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public static IReadOnlyList<string> ValidateByAnnotations(object obj)
        {
            var context = new ValidationContext(obj);

            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

            return results.Select(r => r.ErrorMessage).ToList();
        }

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

        public static void AttachIntOnly(Control control, bool allowNegative = false)
        {
            control.PreviewTextInput += (s, e) =>
            {
                TextBox textBox = s as TextBox;
                if (textBox == null) return;
                if (Regex.IsMatch(e.Text, @"^\d+$")) return;
                if (allowNegative && e.Text == "-" && textBox.SelectionStart == 0 && !textBox.Text.Contains("-"))
                    return;
                e.Handled = true;
            };
            DataObject.AddPastingHandler(control, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    string text = (string)e.DataObject.GetData(typeof(string));
                    if (int.TryParse(text, out int result))
                    {
                        if (!allowNegative && result < 0) e.CancelCommand();
                    }
                    else e.CancelCommand();
                }
                else e.CancelCommand();
            });
        }

        public static void AttachDecimalOnly(Control control, bool allowNegative = true)
        {
            control.PreviewTextInput += (s, e) =>
            {
                TextBox textBox = s as TextBox;
                if (textBox == null) return;

                if (Regex.IsMatch(e.Text, @"^\d+$")) return;

                if (e.Text == "," && !textBox.Text.Contains(","))
                    return;

                if (allowNegative && e.Text == "-" && textBox.SelectionStart == 0 && !textBox.Text.Contains("-"))
                    return;

                e.Handled = true;
            };

            DataObject.AddPastingHandler(control, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    string text = (string)e.DataObject.GetData(typeof(string));

                    text = text.Replace('.', ',');

                    if (double.TryParse(text, out double result))
                    {
                        if (!allowNegative && result < 0) e.CancelCommand();
                    }
                    else e.CancelCommand();
                }
                else e.CancelCommand();
            });
        }
        public static bool HasAtLeastOneLetter(string text)
        {
            return Regex.IsMatch(text ?? "", @"[а-яА-ЯіІїЇєЄґҐa-zA-Z]");
        }
    }
}