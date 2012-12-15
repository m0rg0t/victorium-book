using VictoriumHeroes.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Шаблон элемента страницы с разделением задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234234

namespace VictoriumHeroes
{
    /// <summary>
    /// Страница, на которой отображается заголовок группы, список элементов этой группы и сведения о
    /// выбранном в данный момент элементе.
    /// </summary>
    public sealed partial class SplitPage : VictoriumHeroes.Common.LayoutAwarePage
    {
        public SplitPage()
        {
            this.InitializeComponent();
        }

        #region Управление состоянием страницы

        /// <summary>
        /// Заполняет страницу содержимым, передаваемым в процессе навигации. Также предоставляется любое сохраненное состояние
        /// при повторном создании страницы из предыдущего сеанса.
        /// </summary>
        /// <param name="navigationParameter">Значение параметра, передаваемое
        /// <see cref="Frame.Navigate(Type, Object)"/> при первоначальном запросе этой страницы.
        /// </param>
        /// <param name="pageState">Словарь состояния, сохраненного данной страницей в ходе предыдущего
        /// сеанса. Это значение будет равно NULL при первом посещении страницы.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Создание соответствующей модели данных для области проблемы, чтобы заменить пример данных
            var group = SampleDataSource.GetGroup((String)navigationParameter);
            this.DefaultViewModel["Group"] = group;
            this.DefaultViewModel["Items"] = group.Items;

            if (pageState == null)
            {
                this.itemListView.SelectedItem = null;
                // Если это новая страница, первый элемент выбирается автоматически, если не действует логическая
                // навигация по страницам (см. ниже область #region логической навигации по страницам).
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Восстановление ранее сохраненного состояния, связанного с этой страницей
                if (pageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    var selectedItem = SampleDataSource.GetItem((String)pageState["SelectedItem"]);
                    this.itemsViewSource.View.MoveCurrentTo(selectedItem);
                }
            }
        }

        /// <summary>
        /// Сохраняет состояние, связанное с данной страницей, в случае приостановки приложения или
        /// удаления страницы из кэша навигации. Значения должны соответствовать требованиям сериализации
        /// <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">Пустой словарь, заполняемый сериализуемым состоянием.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = (SampleDataItem)this.itemsViewSource.View.CurrentItem;
                if (selectedItem != null) pageState["SelectedItem"] = selectedItem.UniqueId;
            }
        }

        #endregion

        #region Логическая навигация по страницам

        // Управление состоянием отображения обычно непосредственно отражает одно из четырех состояний представления приложения
        // (полноэкранные книжное и альбомное, прикрепленное и представление с заполнением.)  Разделенная страница
        // разрабатывается таким образом, чтобы книжное и прикрепленное состояния имели по два различных вложенных состояния:
        // одновременно может отображаться список элементов или сведения, но не оба эти модуля.
        //
        // Все это реализуется с помощью единственной физической страницы, которая может представлять две логические
        // страницы.  В следующем коде эта цель достигается без необходимости ставить пользователя в известность о
        // разделении.

        /// <summary>
        /// Вызывается, чтобы определить, должна страница использоваться в качестве одной логической страницы или в качестве двух.
        /// </summary>
        /// <param name="viewState">Состояние представления, для которого формулируется вопрос, или null
        /// для текущего состояния представления.  Этот параметр является необязательным, значение по умолчанию - 
        /// null.</param>
        /// <returns>Значение true, если состояние представления книжное или прикрепленное, false - 
        /// в противном случае.</returns>
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }

        /// <summary>
        /// Вызывается при выделении элемента списка.
        /// </summary>
        /// <param name="sender">Объект GridView (или ListView, если приложение прикреплено),
        /// где отображается выбранный элемент.</param>
        /// <param name="e">Данные о событии, описывающие, каким образом изменилось выделение.</param>
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Аннулировать состояние представления, когда действует логическая навигация по страницам, поскольку изменение
            // выделения может вызвать соответствующее изменение в текущей логической странице.  Если
            // элемент выделен, это приводит к переходу от отображения списка элементов к
            // отображению сведений о выделенном элементе.  Когда выделение очищается, это приводит к
            // обратному эффекту.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Назад" страницы.
        /// </summary>
        /// <param name="sender">Экземпляр кнопки "Назад".</param>
        /// <param name="e">Данные о событии, описывающие, каким образом была нажата кнопка "Назад".</param>
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.UsingLogicalPageNavigation() && itemListView.SelectedItem != null)
            {
                // Если действует логическая навигация по страницам и выделен элемент,
                // сведения о котором в данный момент отображаются.  Очистка выделения приведет к возврату к
                // списку элементов.  С точки зрения пользователя это логический переход
                // назад.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                // Если логическая навигация по страницам не действует или если нет выделенного
                // элемента, используйте поведение кнопки "Назад" по умолчанию.
                base.GoBack(sender, e);
            }
        }

        /// <summary>
        /// Вызывается, чтобы определить имя состояния отображения, соответствующее состоянию
        /// отображения приложения.
        /// </summary>
        /// <param name="viewState">Состояние видимости, для которого формулируется вопрос.</param>
        /// <returns>Имя требуемого состояния отображения.  Это имя совпадает с именем состояния
        /// отображения, кроме случаев, когда есть выделенный элемент в книжном или прикрепленном представлении, где
        /// эта дополнительная логическая страница представляется добавлением суффикса _Detail.</returns>
        protected override string DetermineVisualState(ApplicationViewState viewState)
        {
            // Обновить включенное состояние кнопки "Назад" при изменении состояния представления
            var logicalPageBack = this.UsingLogicalPageNavigation(viewState) && this.itemListView.SelectedItem != null;
            var physicalPageBack = this.Frame != null && this.Frame.CanGoBack;
            this.DefaultViewModel["CanGoBack"] = logicalPageBack || physicalPageBack;

            // Определение визуальных состояний альбомных макетов не на основании состояния представления, а на
            // основании ширины окна. У этой страницы имеется один макет, который подходит для
            // 1366 или более виртуальных пикселей, и один макет для более узких экранов или для случаев, когда
            // приложение в "привязанном" состоянии уменьшает доступное по горизонтали место до менее 1366 пикселей.
            if (viewState == ApplicationViewState.Filled ||
                viewState == ApplicationViewState.FullScreenLandscape)
            {
                var windowWidth = Window.Current.Bounds.Width;
                if (windowWidth >= 1366) return "FullScreenLandscapeOrWide";
                return "FilledOrNarrow";
            }

            // В книжном и "привязанном" режиме запускается с именем визуального состояния по умолчанию, а затем добавляется
            // суффикс при просмотре подробностей вместо списка
            var defaultStateName = base.DetermineVisualState(viewState);
            return logicalPageBack ? defaultStateName + "_Detail" : defaultStateName;
        }

        #endregion
    }
}
