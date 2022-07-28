using System;
using System.Windows.Controls;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View.Pages
{
    public partial class ToolsPage : Page
    {
        public ToolsPage(ToolsViewManager manager)
        {
            DataContext = manager;

            InitializeComponent();
        }
    }
}
