using System.Windows.Controls;

namespace TwinCatAdsTool.Gui.Views
{
    /// <summary>
    /// Interaction logic for CompareView.xaml
    /// </summary>
    public partial class CompareView
    {

        public CompareView()
        {
            InitializeComponent();
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == LeftScroller)
            {
                RightScroller.ScrollToVerticalOffset(e.VerticalOffset);
                RightScroller.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else
            {
                LeftScroller.ScrollToVerticalOffset(e.VerticalOffset);
                LeftScroller.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }
    }
}
