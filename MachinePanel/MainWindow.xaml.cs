using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MachinePanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private bool _isShuttingDown;
        private bool _canClose;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();

            DataContext = _viewModel;
            Closing += OnWindowClosing;
        }

        private async void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_canClose) return;

            e.Cancel = true;
            if (_isShuttingDown) return;

            _isShuttingDown = true;
            await _viewModel.DisposeAsync();
            _canClose = true;
            Close();
        }
    }
}