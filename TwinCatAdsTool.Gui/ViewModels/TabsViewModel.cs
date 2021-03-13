using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class TabsViewModel : ViewModelBase
    {
        private readonly IViewModelFactory _viewModelFactory;

        public TabsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public BackupViewModel BackupViewModel { get; set; }
        public CompareViewModel CompareViewModel { get; set; }

        public ExploreViewModel ExploreViewModel { get; set; }

        public RestoreViewModel RestoreViewModel { get; set; }

        public override void Init()
        {
            BackupViewModel = _viewModelFactory.CreateViewModel<BackupViewModel>();
            BackupViewModel.AddDisposableTo(Disposables);

            CompareViewModel = _viewModelFactory.CreateViewModel<CompareViewModel>();
            CompareViewModel.AddDisposableTo(Disposables);


            ExploreViewModel = _viewModelFactory.CreateViewModel<ExploreViewModel>();
            ExploreViewModel.AddDisposableTo(Disposables);


            RestoreViewModel = _viewModelFactory.CreateViewModel<RestoreViewModel>();
            RestoreViewModel.AddDisposableTo(Disposables);
        }
    }
}