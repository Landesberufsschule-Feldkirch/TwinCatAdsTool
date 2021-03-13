using System.Windows.Input;

namespace TwinCatAdsTool.Gui.Views
{
    /// <summary>
    /// Interaction logic for ConnectionCabView.xaml
    /// </summary>
    public partial class ConnectionCabView
    {
        public ConnectionCabView()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.evopro-ag.de");
        }
    }
}
