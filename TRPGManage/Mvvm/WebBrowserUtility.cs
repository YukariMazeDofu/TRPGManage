using mshtml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ItoKonnyaku.Mvvm
{
    public static class WebBrowserUtility
    {
        #region WebBrowserUtility.BindableSourceProperty

        //WebBrowserにバインドしたい。

        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached(
                "BindableSource", 
                typeof(string), 
                typeof(WebBrowserUtility), 
                new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static string GetBindableSource(DependencyObject obj) =>
            (string)obj.GetValue(BindableSourceProperty);

        public static void SetBindableSource(DependencyObject obj, string value) =>
            obj.SetValue(BindableSourceProperty, value);

        public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is WebBrowser browser)
            {
                string uri = e.NewValue as string;
                browser.Source = !String.IsNullOrEmpty(uri) ? new Uri(uri) : null;
            }
        }

        #endregion

        #region WebBrowserUtility.HideScrollBarProperty

        //WebBrowserからスクロールバーを消し去りたい。
        //https://stackoverflow.com/questions/12930297/disable-wpf-webbrowser-scrollbar


        public static readonly DependencyProperty HideScrollBarProperty =
            DependencyProperty.RegisterAttached(
                "HideScrollBar",
                typeof(string),
                typeof(WebBrowserUtility),
                new UIPropertyMetadata(null, HideScrollBarPropertyChanged)
            );

        public static string GetHideScrollBar(DependencyObject obj) => 
            (string)obj.GetValue(HideScrollBarProperty);

        public static void SetHideScrollBar(DependencyObject obj, string value) => 
            obj.SetValue(HideScrollBarProperty, value);

        public static void HideScrollBarPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            WebBrowser browser = obj as WebBrowser;
            if (args.NewValue is string str && bool.TryParse(str, out bool isHidden))
            {
                browser.HideScrollBar(isHidden);
            }
        }

        #endregion

    }

    public static class WebBrowserExtension
    {
        public static void HideScrollBar(this WebBrowser browser, bool isHidden)
        {
            if (browser != null)
            {
                if (!(browser.Document is IHTMLDocument2 document))
                {
                    // If too early
                    browser.LoadCompleted += (o, e) => HideScrollBar(browser, isHidden);
                    return;
                }

                //string bodyOverflow = string.Format("document.body.style.overflow='{0}';", isHidden ? "hidden" : "auto");
                //document.parentWindow.execScript(bodyOverflow); // This does not work for me...

                string elementOverflow = string.Format("document.documentElement.style.overflow='{0}';", isHidden ? "hidden" : "auto");
                document.parentWindow.execScript(elementOverflow);
            }
        }
    }
}
