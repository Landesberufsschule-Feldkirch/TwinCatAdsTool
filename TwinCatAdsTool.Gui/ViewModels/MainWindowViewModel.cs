using TwinCatAdsTool.Interfaces;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _version;

        public MainWindowViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public ConnectionCabViewModel ConnectionCabViewModel { get; set; }
        public TabsViewModel TabsViewModel { get; set; }

        public string Version
        {
            get => _version;
            set
            {
                if (value == _version) return;
                _version = value;
                raisePropertyChanged();
            }
        }

        public override void Init()
        {
            Logger.Debug("Initialize main window view model");


            Version = $"v{Constants.Version}";

            ConnectionCabViewModel = _viewModelFactory.CreateViewModel<ConnectionCabViewModel>();
            ConnectionCabViewModel.AddDisposableTo(Disposables);

            TabsViewModel = _viewModelFactory.CreateViewModel<TabsViewModel>();
            TabsViewModel.AddDisposableTo(Disposables);
        }
    }
}