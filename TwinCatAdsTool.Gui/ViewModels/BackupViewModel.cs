using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class BackupViewModel : ViewModelBase
    {
        private readonly IClientService _clientService;
        private readonly IPersistentVariableService _persistentVariableService;
        private readonly Subject<JObject> _variableSubject = new Subject<JObject>();
        private string _backupText;
        private ObservableAsPropertyHelper<string> _currentTaskHelper;

        public BackupViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            _clientService = clientService;
            _persistentVariableService = persistentVariableService;
        }

        public string BackupText
        {
            get => _backupText;
            set
            {
                if (value == _backupText) return;
                _backupText = value;
                raisePropertyChanged();
            }
        }

        public ReactiveCommand<Unit, Unit> Read { get; set; }
        public ReactiveCommand<Unit, Unit> Save { get; set; }

        public override void Init()
        {
            _variableSubject
                .ObserveOnDispatcher()
                .Do(o => BackupText = o.ToString(Formatting.Indented))
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            Read = ReactiveCommand.CreateFromTask(ReadVariables, _clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Save = ReactiveCommand.CreateFromTask(SaveVariables, _clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            _currentTaskHelper = _persistentVariableService.CurrentTask.ToProperty(this, vm => vm.CurrentTask);
        }

        public string CurrentTask => _currentTaskHelper.Value;

        private async Task<Unit> ReadVariables()
        {
            var persistentVariables = await _persistentVariableService.ReadPersistentVariables(_clientService.Client, _clientService.TreeViewSymbols);
            _variableSubject.OnNext(persistentVariables);
            Logger.Debug(Resources.ReadPersistentVariables);

            return Unit.Default;
        }

        private Task<Unit> SaveVariables()
        {
            var saveFileDialog1 = new SaveFileDialog
            {
                Filter = "Json|*.json",
                Title = "Save in a json file",
                FileName = $"Backup_{DateTime.Now:yyy-MM-dd-HHmmss}.json",
                RestoreDirectory = true
            };
            var result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                File.WriteAllText(saveFileDialog1.FileName, BackupText);
                Logger.Debug(string.Format(Resources.SavedBackupTo0Logging, saveFileDialog1.FileName));
            }

            return Task.FromResult(Unit.Default);
        }
    }
}