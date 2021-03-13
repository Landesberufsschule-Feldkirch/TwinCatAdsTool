using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.Commands;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ExploreViewModel : ViewModelBase
    {
        private readonly IClientService _clientService;
        private readonly ISelectionService<ISymbol> _symbolSelection;

        private readonly Subject<ReadOnlySymbolCollection> _variableSubject = new Subject<ReadOnlySymbolCollection>();

        private readonly IViewModelFactory _viewModelFactory;
        private bool _isConnected;
        private ObservableAsPropertyHelper<bool> _isConnectedHelper;

        private ObservableCollection<IValueSymbol> _observedSymbols;

        private string _searchText;

        private ObservableCollection<ISymbol> _treeNodes;


        public ExploreViewModel(IClientService clientService,
            IViewModelFactory viewModelFactory, ISelectionService<ISymbol> symbolSelection)
        {
            _clientService = clientService;
            _viewModelFactory = viewModelFactory;
            _symbolSelection = symbolSelection;
        }

        public ReactiveCommand<ISymbol, Unit> AddObserverCmd { get; set; }

        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdAddGraph { get; set; }
        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdDelete { get; set; }

        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdRemoveGraph { get; set; }
        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdSubmit { get; set; }

        public GraphViewModel GraphViewModel { get; set; }

        public bool IsConnected
        {
            get => _isConnectedHelper.Value;
            set
            {
                if (_isConnectedHelper.Value == value)
                {
                    return;
                }

                _isConnected = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<IValueSymbol> ObservedSymbols
        {
            get => _observedSymbols ?? (_observedSymbols = new ObservableCollection<IValueSymbol>());
            set
            {
                if (value == _observedSymbols) return;
                _observedSymbols = value;
                raisePropertyChanged();
            }
        }

        public ObserverViewModel ObserverViewModel { get; set; }

        public ReactiveCommand<Unit, Unit> Read { get; set; }

        public ObservableCollection<ISymbol> SearchResults { get; } = new ObservableCollection<ISymbol>();


        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                raisePropertyChanged();
            }
        }

        public ReactiveRelayCommand TextBoxEnterCommand { get; set; }

        public ObservableCollection<ISymbol> TreeNodes
        {
            get => _treeNodes ?? (_treeNodes = new ObservableCollection<ISymbol>());
            set
            {
                if (value == _treeNodes)
                {
                    return;
                }

                _treeNodes = value;
                raisePropertyChanged();
            }
        }

        public override void Init()
        {
            ObserverViewModel = _viewModelFactory.Create<ObserverViewModel>();
            ObserverViewModel.AddDisposableTo(Disposables);


            _variableSubject
                .ObserveOnDispatcher()
                .Do(UpdateTree)
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables);

            var treeNodeChangeSet = TreeNodes
                .ToObservableChangeSet()
                .ObserveOnDispatcher();

            treeNodeChangeSet
                .Subscribe()
                .AddDisposableTo(Disposables);

            var connected = _clientService.ConnectionState.Select(state => state == ConnectionState.Connected);

            _clientService.ConnectionState
                .DistinctUntilChanged()
                .Where(state => state == ConnectionState.Connected)
                .Do(_ => _variableSubject.OnNext(_clientService.TreeViewSymbols))
                .Subscribe()
                .AddDisposableTo(Disposables);

            connected.ToProperty(this, x => x.IsConnected, out _isConnectedHelper);

            AssignCommands(connected);

            GraphViewModel = _viewModelFactory.CreateViewModel<GraphViewModel>();
            GraphViewModel.AddDisposableTo(Disposables);

            this.WhenAnyValue(x => x.ObservedSymbols).Subscribe().AddDisposableTo(Disposables);

            // Listen to all property change events on SearchText
            var searchTextChanged = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        ev => PropertyChanged += ev,
                        ev => PropertyChanged -= ev
                    )
                    .Where(ev => ev.EventArgs.PropertyName == "SearchText")
                ;

            // Transform the event stream into a stream of strings (the input values)
            var input = searchTextChanged
                .Where(ev => SearchText == null || SearchText.Length < 5)
                .Throttle(TimeSpan.FromSeconds(3))
                .Merge(searchTextChanged
                           .Where(ev => SearchText != null && SearchText.Length >= 5)
                           .Throttle(TimeSpan.FromMilliseconds(400)))
                .Select(args => SearchText)
                .Merge(
                    TextBoxEnterCommand.Executed.Select(e => SearchText))
                .DistinctUntilChanged();

            // Setup an Observer for the search operation
            var search = Observable.ToAsync<string, SearchResult>(DoSearch);


            // Chain the input event stream and the search stream, cancelling searches when input is received
            var results = from searchTerm in input
                from result in search(searchTerm).TakeUntil(input)
                select result;


            // Log the search result and add the results to the results collection
            results
                .ObserveOnDispatcher()
                .Subscribe(result =>
                    {
                        SearchResults.Clear();
                        result.Results.ToList().ForEach(item => SearchResults.Add(item));
                    }
                );
        }

        private void AssignCommands(IObservable<bool> connected)
        {
// Setup the command for the enter key on the textbox
            TextBoxEnterCommand = new ReactiveRelayCommand(obj => { });

            AddObserverCmd = ReactiveCommand.CreateFromTask<ISymbol, Unit>(RegisterSymbolObserver)
                .AddDisposableTo(Disposables);

            CmdDelete = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(DeleteSymbolObserver)
                .AddDisposableTo(Disposables);

            CmdSubmit = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(SubmitSymbol)
                .AddDisposableTo(Disposables);

            CmdAddGraph = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(AddGraph)
                .AddDisposableTo(Disposables);

            CmdRemoveGraph = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(RemoveGraph)
                .AddDisposableTo(Disposables);

            Read = ReactiveCommand.CreateFromTask(ReadVariables, connected)
                .AddDisposableTo(Disposables);
        }

        private Task<Unit> AddGraph(SymbolObservationViewModel symbolObservationViewModel)
        {
            GraphViewModel.AddSymbol(symbolObservationViewModel);
            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> DeleteSymbolObserver(SymbolObservationViewModel model)
        {
            try
            {
                ObserverViewModel.ViewModels.Remove(model);
                RemoveGraph(model);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(Resources.CouldNotDeleteObserverForSymbol0, model?.Name), ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return Task.FromResult(Unit.Default);
        }

        private SearchResult DoSearch(string searchTerm)
        {
            var searchResult = new SearchResult {Results = new List<ISymbol>(), SearchTerm = searchTerm};
            try
            {
                var iterator = new SymbolIterator(_clientService.FlatViewSymbols, s => s.InstancePath.ToLower().Contains(searchTerm.ToLower()));
                searchResult.Results = iterator;
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.ErrorDuringSearch, ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return searchResult;
        }

        private async Task<Unit> ReadVariables()
        {
            try
            {
                await _clientService.Reload();
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.CouldNotReloadVariables, ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return Unit.Default;
        }


        private Task<Unit> RegisterSymbolObserver(ISymbol symbol)
        {
            try
            {
                if (symbol.SubSymbols.Any())
                {
                    return Task.FromResult(Unit.Default);
                }

                if (symbol.DataType.IsContainer)
                {
                    return Task.FromResult(Unit.Default);
                }

                _symbolSelection.Select(symbol);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(Resources.CouldNotRegisterObserverForSymbol0, symbol?.InstanceName), ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> RemoveGraph(SymbolObservationViewModel symbolObservationViewModel)
        {
            GraphViewModel.RemoveSymbol(symbolObservationViewModel);
            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> SubmitSymbol(SymbolObservationViewModel model)
        {
            return Task.FromResult(Unit.Default);
        }

        private void UpdateTree(ReadOnlySymbolCollection symbolList)
        {
            try
            {
                TreeNodes.Clear();
                foreach (var s in symbolList)
                {
                    TreeNodes.Add(s);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.CouldNotUpdateTree, ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }
            finally
            {
                raisePropertyChanged("TreeNodes");
            }
        }
    }
}