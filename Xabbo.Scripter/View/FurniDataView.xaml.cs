using System;
using System.Windows.Controls;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View;

public partial class FurniDataView : UserControl
{
    public FurniDataView()
    {
        InitializeComponent();
    }

    private void DataGridTemplateColumn_CopyingCellClipboardContent(object sender,
        DataGridCellClipboardEventArgs e)
    {
        if (e.Column == ColumnTypeKind &&
            e.Item is FurniInfoViewModel furniInfo)
        {
            e.Content = furniInfo.Kind.ToString();
        }
    }
}
