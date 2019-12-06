using ItoKonnyaku.Commons.Extensions;
using mshtml;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ItoKonnyaku.Mvvm
{
    public class BindableValue<T> : BindableBase
    {
        public BindableValue() { }

        public BindableValue(T value = default) => this._value = value;

        private T _value;
        public T Value
        {
            get => _value;
            set => SetProperty(ref this._value, value);
        }

        public override bool Equals(object obj) => 
            obj is BindableValue<T> test ? this.Value.Equals(test.Value) : false;

        public override int GetHashCode() => 
            this.Value.GetHashCode();

        public override string ToString() => 
            this.Value.ToString();

    }

    public class BindableList<T> : BindableBase where T : INotifyPropertyChanged
    {
        #region コンストラクタ

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BindableList()
        {
            this.InitializeDelegateCommand();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BindableList(IEnumerable<T> items)
        {
            _items = new ObservableCollection<T>(items);
            this.InitializeDelegateCommand();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BindableList(params T[] values)
        {
            _items = new ObservableCollection<T>(values);
            this.InitializeDelegateCommand();
        }

        #endregion

        #region　デストラクタ

        ~BindableList()
        {
            ClearAllEvents();
        }


        #endregion

        #region　オペレータ

        public T this[int key]
        {
            get => this.Items[key];
            set => this.Items[key] = value;
        }

        #endregion

        #region　プロパティ

        private ObservableCollection<T> _items = new ObservableCollection<T>();
        public ObservableCollection<T> Items
        {
            get => _items;
            set => this.SetProperty(ref this._items, value);
        }

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                this.SetProperty(ref this._selectedIndex, value);
                this.RaisePropertyChanged(nameof(SelectedItem));
            }
        }

        public T SelectedItem
        {
            get => (this.Items.CheckIndex(this.SelectedIndex)) ? this[this.SelectedIndex] : default;
            private set
            {
                if (this.Items.CheckIndex(this.SelectedIndex)) this[this.SelectedIndex] = value;
            }
        }

        #endregion

        #region　コマンド
        protected virtual void InitializeDelegateCommand()
        {
            this.MoveAtSelectedItemCommand = new DelegateCommand<string>(i =>
                    MoveAt(this.SelectedIndex, i)
                );

            this.RemoveSelectedItemCommand = new DelegateCommand( () =>
                    RemoveItem(this.SelectedIndex)
                );
        }

        public ICommand MoveAtSelectedItemCommand { get; private set; }
        public int MoveAt(int index, string direction) => 
            this.MoveAt(index, (MoveDirection)Enum.Parse(typeof(MoveDirection), direction, true));

        public int MoveAt(int index, MoveDirection direction)
        {
            if (!this.Items.CheckIndex(index)) return -1;
            var destindex =  this.Items.MoveAt(index, direction);
            if (destindex != -1) this.SelectedIndex = destindex;
            return destindex;
        }

        public ICommand RemoveSelectedItemCommand { get; private set; }
        public int RemoveItem(int index)
        {
            if (!this.Items.CheckIndex(index)) return -1;

            this[index].PropertyChanged -= this.OnItemChanged;
            this.SelectedIndex = this.Items.TryRemoveAt(index);
            return this.SelectedIndex;
        }

        public virtual int AddItem(T item)
        {
            this.Items.Add(item);
            item.PropertyChanged += this.OnItemChanged;
            return this.Items.Count - 1;
        }

        public override string ToString() => 
            $"要素数：{this.Items?.Count()} / 選択中：{this.SelectedItem?.ToString()}";

        #endregion

        #region　イベント処理

        public event PropertyChangedEventHandler ItemChanged;

        public virtual void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            //IndexOfはequalで判定、==判定にする必要あり。
            //this.ItemChanged?.Invoke(this, new PropertyChangedEventArgs(this.Items.IndexOf((T)sender).ToString()));

            var index = 0;
            for(var i = 0; i<this.Items.Count; i++)
            {
                if (sender == (object)this.Items[i])
                {
                    index = i;
                    break;
                }  
            }
            this.ItemChanged?.Invoke(this, new PropertyChangedEventArgs(index.ToString()));
        }

        public virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e) { }

        public virtual void ClearAllEvents()
        {
            this.PropertyChanged -= this.OnPropertyChanged;

            if (this.Items.Count != 0)
            {
                foreach (var item in this.Items)
                {
                    item.PropertyChanged -= this.OnItemChanged;
                }
            }
        }

        protected virtual void SetAllEvents()
        {
            this.PropertyChanged += this.OnPropertyChanged;

            if (this.Items.Count != 0)
            {
                foreach (var item in this.Items)
                {
                    item.PropertyChanged += this.OnItemChanged;
                }
            }
        }

        public virtual void RefreshAllEvents()
        {
            this.ClearAllEvents();
            this.SetAllEvents();
        }

        #endregion

    }

    public class BindableValueList<T> : BindableList<BindableValue<T>>
    {
        public BindableValueList() : base(){}
        public BindableValueList(IEnumerable<BindableValue<T>> items) : base(items) { }
        public BindableValueList(params BindableValue<T>[] values) : base(values) { }
    }

    [ValueConversion(typeof(Enum), typeof(bool))]
    public class Radio2EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return Binding.DoNothing;
            if ((bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }
            return Binding.DoNothing;
        }
    }


}
