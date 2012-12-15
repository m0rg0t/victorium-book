using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;

namespace VictoriumHeroes.Common
{
    /// <summary>
    /// Оболочка <see cref="RichTextBlock"/>, создающая столько дополнительных столбцов
    /// переполнения, сколько их нужно для размещения доступного содержимого.
    /// </summary>
    /// <example>
    /// В следующем коде создается коллекция столбцов шириной 400 пикселей, расположенных на расстоянии 50 пикселей друг от друга,
    /// для размещения произвольного содержимого, привязанного к данным:
    /// <code>
    /// <RichTextColumns>
    ///     <RichTextColumns.ColumnTemplate>
    ///         <DataTemplate>
    ///             <RichTextBlockOverflow Width="400" Margin="50,0,0,0"/>
    ///         </DataTemplate>
    ///     </RichTextColumns.ColumnTemplate>
    ///     
    ///     <RichTextBlock Width="400">
    ///         <Paragraph>
    ///             <Run Text="{Binding Content}"/>
    ///         </Paragraph>
    ///     </RichTextBlock>
    /// </RichTextColumns>
    /// </code>
    /// </example>
    /// <remarks>Обычно используется для области с горизонтальной прокруткой, в котором содержится неограниченное
    /// пространство для создания всех необходимых столбцов. При использовании в области
    /// с вертикальной прокруткой, дополнительные столбцы не создаются.</remarks>
    [Windows.UI.Xaml.Markup.ContentProperty(Name = "RichTextContent")]
    public sealed class RichTextColumns : Panel
    {
        /// <summary>
        /// Определяет свойство зависимостей <see cref="RichTextContent"/>.
        /// </summary>
        public static readonly DependencyProperty RichTextContentProperty =
            DependencyProperty.Register("RichTextContent", typeof(RichTextBlock),
            typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

        /// <summary>
        /// Определяет свойство зависимостей <see cref="ColumnTemplate"/>.
        /// </summary>
        public static readonly DependencyProperty ColumnTemplateProperty =
            DependencyProperty.Register("ColumnTemplate", typeof(DataTemplate),
            typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RichTextColumns"/>.
        /// </summary>
        public RichTextColumns()
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
        }

        /// <summary>
        /// Получает или задает исходное форматированное текстовое содержимое, используемое в качестве первого столбца.
        /// </summary>
        public RichTextBlock RichTextContent
        {
            get { return (RichTextBlock)GetValue(RichTextContentProperty); }
            set { SetValue(RichTextContentProperty, value); }
        }

        /// <summary>
        /// Получает или задает шаблон, используемый для создания дополнительных
        /// экземпляров <see cref="RichTextBlockOverflow"/>.
        /// </summary>
        public DataTemplate ColumnTemplate
        {
            get { return (DataTemplate)GetValue(ColumnTemplateProperty); }
            set { SetValue(ColumnTemplateProperty, value); }
        }

        /// <summary>
        /// Вызывается при изменении содержимого или шаблона переполнения для повторного создания макета столбцов.
        /// </summary>
        /// <param name="d">Экземпляр <see cref="RichTextColumns"/>, в котором произошло
        /// изменение.</param>
        /// <param name="e">Данные события, описывающие конкретное изменение.</param>
        private static void ResetOverflowLayout(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Перестроение макета столбцов с нуля в случае серьезных изменений
            var target = d as RichTextColumns;
            if (target != null)
            {
                target._overflowColumns = null;
                target.Children.Clear();
                target.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Перечисляет уже созданных столбцов переполнения. Должен обеспечивать отношение 1:1 с
        /// экземплярами в коллекции <see cref="Panel.Children"/>, следующими за исходным
        /// дочерним элементом RichTextBlock.
        /// </summary>
        private List<RichTextBlockOverflow> _overflowColumns = null;

        /// <summary>
        /// Определяет, нужны ли дополнительные столбцы переполнения и можно ли удалить
        /// существующие столбцы.
        /// </summary>
        /// <param name="availableSize">Размер доступного пространства, используемый для ограничения
        /// числа дополнительных столбцов, которые можно создать.</param>
        /// <returns>Результирующий размер исходного содержимого плюс все дополнительные столбцы.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.RichTextContent == null) return new Size(0, 0);

            // Убедитесь, что RichTextBlock является дочерним элементом; отсутствие
            // списка дополнительных столбцов означает, что он еще не сделан
            // дочерним
            if (this._overflowColumns == null)
            {
                Children.Add(this.RichTextContent);
                this._overflowColumns = new List<RichTextBlockOverflow>();
            }

            // Начните с измерения исходного содержимого RichTextBlock
            this.RichTextContent.Measure(availableSize);
            var maxWidth = this.RichTextContent.DesiredSize.Width;
            var maxHeight = this.RichTextContent.DesiredSize.Height;
            var hasOverflow = this.RichTextContent.HasOverflowContent;

            // Убедитесь в наличии достаточного количества столбцов переполнения
            int overflowIndex = 0;
            while (hasOverflow && maxWidth < availableSize.Width && this.ColumnTemplate != null)
            {
                // Используйте существующие столбцы переполнения, пока они не закончатся, затем создайте
                // дополнительные столбцы из предоставленного шаблона
                RichTextBlockOverflow overflow;
                if (this._overflowColumns.Count > overflowIndex)
                {
                    overflow = this._overflowColumns[overflowIndex];
                }
                else
                {
                    overflow = (RichTextBlockOverflow)this.ColumnTemplate.LoadContent();
                    this._overflowColumns.Add(overflow);
                    this.Children.Add(overflow);
                    if (overflowIndex == 0)
                    {
                        this.RichTextContent.OverflowContentTarget = overflow;
                    }
                    else
                    {
                        this._overflowColumns[overflowIndex - 1].OverflowContentTarget = overflow;
                    }
                }

                // Измерение нового столбца и подготовка к повторению в случае необходимости
                overflow.Measure(new Size(availableSize.Width - maxWidth, availableSize.Height));
                maxWidth += overflow.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, overflow.DesiredSize.Height);
                hasOverflow = overflow.HasOverflowContent;
                overflowIndex++;
            }

            // Отключение дополнительных столбцов от цепи переполнения, удаление их из нашего закрытого списка
            // столбцов и удаление их как дочерних элементов
            if (this._overflowColumns.Count > overflowIndex)
            {
                if (overflowIndex == 0)
                {
                    this.RichTextContent.OverflowContentTarget = null;
                }
                else
                {
                    this._overflowColumns[overflowIndex - 1].OverflowContentTarget = null;
                }
                while (this._overflowColumns.Count > overflowIndex)
                {
                    this._overflowColumns.RemoveAt(overflowIndex);
                    this.Children.RemoveAt(overflowIndex + 1);
                }
            }

            // Сообщение о конечном определенном размере
            return new Size(maxWidth, maxHeight);
        }

        /// <summary>
        /// Упорядочение исходного содержимого и всех дополнительных столбцов.
        /// </summary>
        /// <param name="finalSize">Определение размера области, в которой должны быть упорядочены дочерние
        /// элементы.</param>
        /// <returns>Размер области, которая фактически требуется дочерним элементам.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double maxWidth = 0;
            double maxHeight = 0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(maxWidth, 0, child.DesiredSize.Width, finalSize.Height));
                maxWidth += child.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }
            return new Size(maxWidth, maxHeight);
        }
    }
}
