using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads;
using TwinCatAdsTool.Gui.Extensions;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Models;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ConnectionCabViewModel : ViewModelBase
    {
        private readonly IClientService _clientService;
        private ObservableAsPropertyHelper<ConnectionState> _connectionStateHelper;
        private int _port = 851;
        private string _selectedNetId;
        private NetId _selectedAmsNetId;
        private ObservableAsPropertyHelper<string> _adsStatusHelper;


        public ConnectionCabViewModel(IClientService clientService)
        {
            _clientService = clientService;
        }

        public ObservableCollection<NetId> AmsNetIds { get; set; } = new ObservableCollection<NetId>();
        public ReactiveCommand<Unit, Unit> Connect { get; set; }

        public ConnectionState ConnectionState => _connectionStateHelper.Value;
        public ReactiveCommand<Unit, Unit> Disconnect { get; set; }

        public int Port
        {
            get => _port;
            set
            {
                if (value == _port) return;
                _port = value;
                raisePropertyChanged();
            }
        }

        public NetId SelectedAmsNetId
        {
            get => _selectedAmsNetId;

            set
            {
                if (_selectedAmsNetId != value)
                {
                    _selectedAmsNetId = value;
                    raisePropertyChanged();
                }
            }
        }


        public string SelectedNetId
        {
            get => _selectedNetId;
            set
            {
                if (_selectedNetId != value)
                {
                    _selectedNetId = value;
                    raisePropertyChanged();
                }
            }
        }


        public override void Init()
        {
            Connect = ReactiveCommand.CreateFromTask(ConnectClient, _clientService.ConnectionState.Select(state => state != ConnectionState.Connected))
                .AddDisposableTo(Disposables).SetupErrorHandling(Logger, Disposables);
            Disconnect = ReactiveCommand.CreateFromTask(DisconnectClient, _clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables).SetupErrorHandling(Logger, Disposables);
            
            _connectionStateHelper = _clientService
                .ConnectionState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.ConnectionState);

            _adsStatusHelper = _clientService
                .AdsState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.AdsStatus);


            _clientService.DevicesFound
                .Where(d => d != null)
                .ObserveOnDispatcher()
                .Do(devices => AmsNetIds.AddRange(devices))
                .Subscribe()
                .AddDisposableTo(Disposables);
            
            AmsNetIds.Add(new NetId(){Address = "", Name = "*"});
            SelectedAmsNetId = AmsNetIds.FirstOrDefault();

            this.WhenAnyValue(vm => vm.SelectedAmsNetId)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Do(s => SelectedNetId = s.Address)
                .Subscribe()
                .AddDisposableTo(Disposables);
            
        }

        public string AdsStatus => _adsStatusHelper.Value;

        private async Task ConnectClient()
        {
            try
            {
                await _clientService.Connect(SelectedNetId, Port);
                Logger.Debug(string.Format(Resources.ClientConnectedToDevice0WithAddress1, SelectedAmsNetId?.Name,
                    SelectedAmsNetId?.Address));
            }
            catch (AdsInitializeException ex) when (ex.InnerException is DllNotFoundException && ex.InnerException.Source == "TwinCAT.Ads")
            {
                Logger.Error("Dll not found TwinCAT.Ads");
                MessageBox.Show("Dll for TwinCAT.Ads not found. Have you installed the drivers?");
            }
        }

        private async Task DisconnectClient()
        {
            await _clientService.Disconnect();
            Logger.Debug("Client disconnected");
        }
    }
}