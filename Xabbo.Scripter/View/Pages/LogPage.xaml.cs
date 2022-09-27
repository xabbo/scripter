using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View.Pages;

/// <summary>
/// Interaction logic for LogPage.xaml
/// </summary>
public partial class LogPage : Page
{
    private readonly LogViewManager _manager;

    public LogPage(LogViewManager manager)
    {
        _manager = manager;
        DataContext = manager;

        InitializeComponent();
    }
}
