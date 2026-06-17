using System.Windows;
using System.Windows.Controls;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public partial class MetricCard : UserControl
{
    public MetricCard() => InitializeComponent();

    internal void ApplyFullscreenLayout()
    {
        MinHeight = 0;
        ValueRow.Height = new GridLength(1, GridUnitType.Star);
        ValuePanel.VerticalAlignment = VerticalAlignment.Center;
        ValueText.FontSize = 72;
        UnitText.FontSize = 28;
        UnitText.Margin = new Thickness(16, 0, 0, 12);
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MetricCardModel model)
            return;

        ChartFullscreenService.Show(
            this,
            () =>
            {
                var card = new MetricCard { DataContext = model };
                return card;
            },
            model.Title);
    }
}
