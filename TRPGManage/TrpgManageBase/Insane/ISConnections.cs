using ItoKonnyaku.Mvvm;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{
    public class ISConnections : BindableList<ISConnection>
    {
        #region プロパティ

        private string _sendingText = "";
        public string SendingText
        {
            get { return _sendingText; }
            set { SetProperty(ref _sendingText, value); }
        }

        #endregion

        #region コマンド

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.AddNewConnectionCommand = new DelegateCommand(this.AddNewConnection);
        }

        public ICommand AddNewConnectionCommand { get; private set; }
        public void AddNewConnection()
        {
            this.AddItem(new ISConnection());
        }

        public void SetSendingText()
        {
            var target = this.SelectedItem;
            if (target == null) return;

            var sign = (target.IsPositive) ? "プラス" : "マイナス";
            this.SendingText = $"[ 感情({target.EmotionText}) ]{target.TargetName}に{sign}感情修正";
        }

        #endregion

        #region イベント
        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SetSendingText();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            string[] setSendingText = { nameof(this.SelectedIndex) };
            if (setSendingText.Contains(e.PropertyName)) this.SetSendingText();
        }

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender, e);
            this.SetSendingText();
        }

        #endregion

    }

    public class ISConnection : BindableBase
    {
        #region プロパティ

        private string _targetName = "";
        public string TargetName
        {
            get { return _targetName; }
            set { SetProperty(ref _targetName, value); }
        }

        private bool _hasPosition = false;
        public bool HasPosition
        {
            get { return _hasPosition; }
            set { SetProperty(ref _hasPosition, value); }
        }

        private bool _hasSecret = false;
        public bool HasSecret
        {
            get { return _hasSecret; }
            set { SetProperty(ref _hasSecret, value); }
        }

        private ISEmotion _emotion = ISEmotion.Null;
        public ISEmotion Emotion
        {
            get { return _emotion; }
            set { SetProperty(ref _emotion, value); }
        }

        public string EmotionText
        {
            get => this.Emotion.Name(this.IsPositive);
            private set { }
        }

        private bool _isPositive = true;
        public bool IsPositive
        {
            get { return _isPositive; }
            set { SetProperty(ref _isPositive, value); }
        }

        #endregion

        #region コマンド
        public override string ToString()
        {
            return $"対象：{this.TargetName}/居所：{TF(HasPosition)}/秘密：{TF(HasSecret)}/感情：{EmotionText}";

            static string TF(bool var) => var ? "〇" : "×";
        }

        #endregion

    }


    public enum ISEmotion
    {
        Null, Empathy, Friendship, Love, Loyalty, Longing, Fanaticism
    }

    public static class ISEmotionExtention
    {
        public static string Name(this ISEmotion target)
        {
            string[] names = { "未設定", "共感/不信", "友情/怒り", "愛情/妬み", "忠誠/侮蔑", "憧憬/劣等感", "狂信/殺意" };
            return names[(int)target];
        }
        public static string Name(this ISEmotion target,bool positive)
        {
            string[] pnames = { "未設定", "共感", "友情", "愛情", "忠誠", "憧憬",   "狂信" };
            string[] nnames = { "未設定", "不信", "怒り", "妬み", "侮蔑", "劣等感", "殺意" };

            return positive? pnames[(int)target] : nnames[(int)target];
        }

    }

    [ValueConversion(typeof(ISEmotion), typeof(string))]
    public class ISEmotionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ISEmotion target)
            {
                return target.Name();
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string target)
            {
                return (ISEmotion)Enum.Parse(typeof(ISEmotion), target, true);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }

}
