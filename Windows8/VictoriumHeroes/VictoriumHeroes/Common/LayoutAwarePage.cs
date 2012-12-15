using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace VictoriumHeroes.Common
{
    /// <summary>
    /// Обычная реализация объекта Page, предоставляющая несколько важных и удобных возможностей:
    /// <list type="bullet">
    /// <item>
    /// <description>Сопоставление состояния просмотра приложения с визуальным состоянием</description>
    /// </item>
    /// <item>
    /// <description>Обработчики событий GoBack, GoForward и GoHome</description>
    /// </item>
    /// <item>
    /// <description>Сочетания клавиш и щелчки мышью для навигации</description>
    /// </item>
    /// <item>
    /// <description>Управление состоянием для навигации и управления жизненным циклом процессов</description>
    /// </item>
    /// <item>
    /// <description>Модель представления по умолчанию</description>
    /// </item>
    /// </list>
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class LayoutAwarePage : Page
    {
        /// <summary>
        /// Определяет свойство зависимостей <see cref="DefaultViewModel"/>.
        /// </summary>
        public static readonly DependencyProperty DefaultViewModelProperty =
            DependencyProperty.Register("DefaultViewModel", typeof(IObservableMap<String, Object>),
            typeof(LayoutAwarePage), null);

        private List<Control> _layoutAwareControls;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="LayoutAwarePage"/>.
        /// </summary>
        public LayoutAwarePage()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled) return;

            // Создание пустой модели представления по умолчанию
            this.DefaultViewModel = new ObservableDictionary<String, Object>();

            // Если данная страница является частью визуального дерева, возникают два изменения:
            // 1) Сопоставление состояния просмотра приложения с визуальным состоянием для страницы.
            // 2) Обработка запросов навигации с помощью мыши и клавиатуры.
            this.Loaded += (sender, e) =>
            {
                this.StartLayoutUpdates(sender, e);

                // Навигация с помощью мыши и клавиатуры применяется, только если страница занимает все окно
                if (this.ActualHeight == Window.Current.Bounds.Height &&
                    this.ActualWidth == Window.Current.Bounds.Width)
                {
                    // Непосредственное прослушивание окна, поэтому фокус не требуется
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                        CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed +=
                        this.CoreWindow_PointerPressed;
                }
            };

            // Отмена тех же изменений, когда страница перестает быть видимой
            this.Unloaded += (sender, e) =>
            {
                this.StopLayoutUpdates(sender, e);
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerPressed -=
                    this.CoreWindow_PointerPressed;
            };
        }

        /// <summary>
        /// Реализация интерфейса <see cref="IObservableMap&lt;String, Object&gt;"/>, предназначенного для
        /// использования в качестве тривиальной модели представления.
        /// </summary>
        protected IObservableMap<String, Object> DefaultViewModel
        {
            get
            {
                return this.GetValue(DefaultViewModelProperty) as IObservableMap<String, Object>;
            }

            set
            {
                this.SetValue(DefaultViewModelProperty, value);
            }
        }

        #region Поддержка навигации

        /// <summary>
        /// Вызывается как обработчик событий для перехода назад в связанном со страницей фрейме
        /// <see cref="Frame"/> до достижения верхнего элемента стека навигации.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="e">Данные события, описывающие условия, которые привели к возникновению события.</param>
        protected virtual void GoHome(object sender, RoutedEventArgs e)
        {
            // Используйте фрейм навигации для возврата на самую верхнюю страницу
            if (this.Frame != null)
            {
                while (this.Frame.CanGoBack) this.Frame.GoBack();
            }
        }

        /// <summary>
        /// Вызывается как обработчик событий для перехода назад в стеке навигации,
        /// связанном со фреймом <see cref="Frame"/> данной страницы.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="e">Данные события, описывающие условия, которые привели к
        /// возникновению события.</param>
        protected virtual void GoBack(object sender, RoutedEventArgs e)
        {
            // Используйте фрейм навигации для возврата на предыдущую страницу
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }

        /// <summary>
        /// Вызывается как обработчик событий для перехода вперед в стеке навигации
        /// связанном со фреймом <see cref="Frame"/> данной страницы.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="e">Данные события, описывающие условия, которые привели к
        /// возникновению события.</param>
        protected virtual void GoForward(object sender, RoutedEventArgs e)
        {
            // Используйте фрейм навигации для перехода на следующую страницу
            if (this.Frame != null && this.Frame.CanGoForward) this.Frame.GoForward();
        }

        /// <summary>
        /// Вызывается при каждом нажатии клавиши, включая системные клавиши, такие как клавиша ALT, если
        /// данная страница активна и занимает все окно. Используется для обнаружения навигации с помощью клавиатуры
        /// между страницами, даже если сама страница не имеет фокуса.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="args">Данные события, описывающие условия, которые привели к возникновению события.</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender,
            AcceleratorKeyEventArgs args)
        {
            var virtualKey = args.VirtualKey;

            // Дальнейшее изучение следует выполнять, только если нажата клавиша со стрелкой влево или вправо либо назначенная клавиша "Назад" или
            // "Вперед"
            if ((args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                args.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // Переход назад при нажатии клавиши "Назад" или сочетания клавиш ALT+стрелка влево
                    args.Handled = true;
                    this.GoBack(this, new RoutedEventArgs());
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // Переход вперед при нажатии клавиши "Вперед" или сочетания клавиш ALT+стрелка вправо
                    args.Handled = true;
                    this.GoForward(this, new RoutedEventArgs());
                }
            }
        }

        /// <summary>
        /// Вызывается при каждом щелчке мыши, касании сенсорного экрана или аналогичном действии, если эта
        /// страница активна и занимает все окно. Используется для обнаружения нажатий мышью кнопок "Вперед" и
        /// "Назад" в браузере для перехода между страницами.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="args">Данные события, описывающие условия, которые привели к возникновению события.</param>
        private void CoreWindow_PointerPressed(CoreWindow sender,
            PointerEventArgs args)
        {
            var properties = args.CurrentPoint.Properties;

            // Пропуск сочетаний кнопок, включающих левую, правую и среднюю кнопки
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed) return;

            // Если нажата кнопка "Назад" или "Вперед" (но не обе), выполняется соответствующий переход
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                args.Handled = true;
                if (backPressed) this.GoBack(this, new RoutedEventArgs());
                if (forwardPressed) this.GoForward(this, new RoutedEventArgs());
            }
        }

        #endregion

        #region Переключение визуальных состояний

        /// <summary>
        /// Вызывается в качестве обработчика событий, как правило, для события <see cref="FrameworkElement.Loaded"/>
        /// элемента управления <see cref="Control"/> на странице для указания того, что отправитель должен
        /// начать получать изменения управления визуальным состоянием, соответствующие изменениям состояния просмотра
        /// приложения.
        /// </summary>
        /// <param name="sender">Экземпляр <see cref="Control"/>, который поддерживает управление состоянием просмотра,
        /// соответствующее состояниям просмотра.</param>
        /// <param name="e">Данные события, описывающие способ выполнения запроса.</param>
        /// <remarks>Текущее состояние просмотра будет немедленно использоваться для задания соответствующего
        /// визуального состояния при запросе обновлений макета. Настоятельно рекомендуется
        /// использовать обработчик событий <see cref="FrameworkElement.Unloaded"/>, подключенный к
        /// объекту <see cref="StopLayoutUpdates"/>. Экземпляры
        /// <see cref="LayoutAwarePage"/> автоматически вызывают эти обработчики в своих событиях Loaded и
        /// Unloaded.</remarks>
        /// <seealso cref="DetermineVisualState"/>
        /// <seealso cref="InvalidateVisualState"/>
        public void StartLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null) return;
            if (this._layoutAwareControls == null)
            {
                // Запуск прослушивания изменений состояния просмотра при наличии элементов управления, заинтересованных в обновлениях
                Window.Current.SizeChanged += this.WindowSizeChanged;
                this._layoutAwareControls = new List<Control>();
            }
            this._layoutAwareControls.Add(control);

            // Задает начальное визуальное состояние элемента управления
            VisualStateManager.GoToState(control, DetermineVisualState(ApplicationView.Value), false);
        }

        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// Вызывается в качестве обработчика событий, как правило, для события <see cref="FrameworkElement.Unloaded"/>
        /// элемента управления <see cref="Control"/> для указания того, что отправитель должен начать получать
        /// изменения управления визуальным состоянием, соответствующие изменениям состояния просмотра приложения.
        /// </summary>
        /// <param name="sender">Экземпляр <see cref="Control"/>, который поддерживает управление состоянием просмотра,
        /// соответствующее состояниям просмотра.</param>
        /// <param name="e">Данные события, описывающие способ выполнения запроса.</param>
        /// <remarks>Текущее состояние просмотра будет немедленно использоваться для задания соответствующего
        /// визуальное состояние при запросе обновлений макета.</remarks>
        /// <seealso cref="StartLayoutUpdates"/>
        public void StopLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null || this._layoutAwareControls == null) return;
            this._layoutAwareControls.Remove(control);
            if (this._layoutAwareControls.Count == 0)
            {
                // Остановка прослушивания изменений состояния просмотра при отсутствии элементов управления, заинтересованных в обновлениях
                this._layoutAwareControls = null;
                Window.Current.SizeChanged -= this.WindowSizeChanged;
            }
        }

        /// <summary>
        /// Преобразует значения <see cref="ApplicationViewState"/> в строки для управления визуальным состоянием
        /// на странице. Реализация по умолчанию использует имена значений перечисления.
        /// Этот метод может переопределяться подклассами для управления используемой схемой сопоставления.
        /// </summary>
        /// <param name="viewState">Состояние просмотра, для которого требуется визуальное состояние.</param>
        /// <returns>Имя визуального состояния, используемое для инициирования
        /// <see cref="VisualStateManager"/></returns>
        /// <seealso cref="InvalidateVisualState"/>
        protected virtual string DetermineVisualState(ApplicationViewState viewState)
        {
            return viewState.ToString();
        }

        /// <summary>
        /// Обновляет все элементы управления, прослушивающие изменения визуального состояния, соответствующим
        /// визуальным состоянием.
        /// </summary>
        /// <remarks>
        /// Обычно используется вместе с переопределяющим <see cref="DetermineVisualState"/> для
        /// указания на возможность возвращения другого значения даже при отсутствии изменений состояния
        /// просмотра.
        /// </remarks>
        public void InvalidateVisualState()
        {
            if (this._layoutAwareControls != null)
            {
                string visualState = DetermineVisualState(ApplicationView.Value);
                foreach (var layoutAwareControl in this._layoutAwareControls)
                {
                    VisualStateManager.GoToState(layoutAwareControl, visualState, false);
                }
            }
        }

        #endregion

        #region Управление жизненным циклом процесса

        private String _pageKey;

        /// <summary>
        /// Вызывается перед отображением этой страницы во фрейме.
        /// </summary>
        /// <param name="e">Данные о событиях, описывающие, каким образом была достигнута эта страница.  Свойство Parameter
        /// задает группу для отображения.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Возвращение к кэшированной странице во время навигации не должно инициировать загрузку состояния
            if (this._pageKey != null) return;

            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // Очистка существующего состояния для перехода вперед при добавлении новой страницы в
                // стек навигации
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // Передача параметра навигации на новую страницу
                this.LoadState(e.Parameter, null);
            }
            else
            {
                // Передача на страницу параметра навигации и сохраненного состояния страницы с использованием
                // той же стратегии загрузки приостановленного состояния и повторного создания страниц, удаленных
                // из кэша
                this.LoadState(e.Parameter, (Dictionary<String, Object>)frameState[this._pageKey]);
            }
        }

        /// <summary>
        /// Вызывается, если данная страница больше не отображается во фрейме.
        /// </summary>
        /// <param name="e">Данные о событиях, описывающие, каким образом была достигнута эта страница.  Свойство Parameter
        /// задает группу для отображения.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<String, Object>();
            this.SaveState(pageState);
            frameState[_pageKey] = pageState;
        }

        /// <summary>
        /// Заполняет страницу содержимым, передаваемым в процессе навигации. Также предоставляется любое сохраненное состояние
        /// при повторном создании страницы из предыдущего сеанса.
        /// </summary>
        /// <param name="navigationParameter">Значение параметра, передаваемое
        /// <see cref="Frame.Navigate(Type, Object)"/> при первоначальном запросе этой страницы.
        /// </param>
        /// <param name="pageState">Словарь состояния, сохраненного данной страницей в ходе предыдущего
        /// сеанса. Это значение будет равно NULL при первом посещении страницы.</param>
        protected virtual void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Сохраняет состояние, связанное с данной страницей, в случае приостановки приложения или
        /// удаления страницы из кэша навигации. Значения должны соответствовать требованиям сериализации
        /// <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">Пустой словарь, заполняемый сериализуемым состоянием.</param>
        protected virtual void SaveState(Dictionary<String, Object> pageState)
        {
        }

        #endregion

        /// <summary>
        /// Реализация интерфейса IObservableMap, поддерживающего повторный вход для использования в качестве модели представления
        /// по умолчанию.
        /// </summary>
        private class ObservableDictionary<K, V> : IObservableMap<K, V>
        {
            private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<K>
            {
                public ObservableDictionaryChangedEventArgs(CollectionChange change, K key)
                {
                    this.CollectionChange = change;
                    this.Key = key;
                }

                public CollectionChange CollectionChange { get; private set; }
                public K Key { get; private set; }
            }

            private Dictionary<K, V> _dictionary = new Dictionary<K, V>();
            public event MapChangedEventHandler<K, V> MapChanged;

            private void InvokeMapChanged(CollectionChange change, K key)
            {
                var eventHandler = MapChanged;
                if (eventHandler != null)
                {
                    eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
                }
            }

            public void Add(K key, V value)
            {
                this._dictionary.Add(key, value);
                this.InvokeMapChanged(CollectionChange.ItemInserted, key);
            }

            public void Add(KeyValuePair<K, V> item)
            {
                this.Add(item.Key, item.Value);
            }

            public bool Remove(K key)
            {
                if (this._dictionary.Remove(key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                    return true;
                }
                return false;
            }

            public bool Remove(KeyValuePair<K, V> item)
            {
                V currentValue;
                if (this._dictionary.TryGetValue(item.Key, out currentValue) &&
                    Object.Equals(item.Value, currentValue) && this._dictionary.Remove(item.Key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                    return true;
                }
                return false;
            }

            public V this[K key]
            {
                get
                {
                    return this._dictionary[key];
                }
                set
                {
                    this._dictionary[key] = value;
                    this.InvokeMapChanged(CollectionChange.ItemChanged, key);
                }
            }

            public void Clear()
            {
                var priorKeys = this._dictionary.Keys.ToArray();
                this._dictionary.Clear();
                foreach (var key in priorKeys)
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                }
            }

            public ICollection<K> Keys
            {
                get { return this._dictionary.Keys; }
            }

            public bool ContainsKey(K key)
            {
                return this._dictionary.ContainsKey(key);
            }

            public bool TryGetValue(K key, out V value)
            {
                return this._dictionary.TryGetValue(key, out value);
            }

            public ICollection<V> Values
            {
                get { return this._dictionary.Values; }
            }

            public bool Contains(KeyValuePair<K, V> item)
            {
                return this._dictionary.Contains(item);
            }

            public int Count
            {
                get { return this._dictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                int arraySize = array.Length;
                foreach (var pair in this._dictionary)
                {
                    if (arrayIndex >= arraySize) break;
                    array[arrayIndex++] = pair;
                }
            }
        }
    }
}
