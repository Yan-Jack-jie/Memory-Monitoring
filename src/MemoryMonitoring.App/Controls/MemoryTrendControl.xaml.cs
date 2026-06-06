using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.App.Controls;

/// <summary>
/// 根据近期采样点绘制轻量内存趋势图。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public partial class MemoryTrendControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(MemoryTrendControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private INotifyCollectionChanged? _currentCollection;

    public MemoryTrendControl()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (MemoryTrendControl)dependencyObject;
        control.DetachCollectionChanged();

        if (eventArgs.NewValue is INotifyCollectionChanged collection)
        {
            control._currentCollection = collection;
            collection.CollectionChanged += control.ItemsSource_OnCollectionChanged;
        }

        control.Redraw();
    }

    private void ItemsSource_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void ChartHost_OnSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void DetachCollectionChanged()
    {
        if (_currentCollection is not null)
        {
            _currentCollection.CollectionChanged -= ItemsSource_OnCollectionChanged;
            _currentCollection = null;
        }
    }

    private void Redraw()
    {
        if (MemoryUsageLine is null || ChartHost.ActualWidth <= 0 || ChartHost.ActualHeight <= 0)
        {
            return;
        }

        var points = ItemsSource?
            .OfType<MemoryHistoryPoint>()
            .ToArray() ?? Array.Empty<MemoryHistoryPoint>();

        MemoryUsageLine.Points = BuildLinePoints(points, ChartHost.ActualWidth, ChartHost.ActualHeight);
    }

    private static PointCollection BuildLinePoints(
        IReadOnlyList<MemoryHistoryPoint> history,
        double width,
        double height)
    {
        var collection = new PointCollection();
        if (history.Count == 0)
        {
            return collection;
        }

        var horizontalPadding = 12d;
        var verticalPadding = 18d;
        var drawableWidth = Math.Max(width - horizontalPadding * 2, 1);
        var drawableHeight = Math.Max(height - verticalPadding * 2, 1);
        var denominator = Math.Max(history.Count - 1, 1);

        for (var index = 0; index < history.Count; index++)
        {
            var usedPercent = Math.Clamp(history[index].UsedPercent, 0, 100);
            var x = horizontalPadding + drawableWidth * index / denominator;
            var y = verticalPadding + drawableHeight * (100 - usedPercent) / 100d;
            collection.Add(new Point(x, y));
        }

        return collection;
    }
}
