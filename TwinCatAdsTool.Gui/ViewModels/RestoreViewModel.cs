using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using DynamicData;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCAT.JsonExtension;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class RestoreViewModel : ViewModelBase
    {
        private readonly BehaviorSubject<bool> _canWrite = new BehaviorSubject<bool>(false);
        private readonly IClientService _clientService;
        private readonly BehaviorSubject<JObject> _fileVariableSubject = new BehaviorSubject<JObject>(new JObject());
        private readonly BehaviorSubject<JObject> _liveVariableSubject = new BehaviorSubject<JObject>(new JObject());
        private readonly IPersistentVariableService _persistentVariableService;
        private ObservableCollection<VariableViewModel> _displayVariables;
        private ObservableCollection<VariableViewModel> _fileVariables;
        private ObservableCollection<VariableViewModel> _liveVariables;

        public RestoreViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            _clientService = clientService;
            _persistentVariableService = persistentVariableService;
        }

        public ObservableCollection<VariableViewModel> DisplayVariables
        {
            get => _displayVariables ?? (_displayVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == _displayVariables)
                {
                    return;
                }

                _liveVariables = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<VariableViewModel> FileVariables
        {
            get => _fileVariables ?? (_fileVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == _fileVariables)
                {
                    return;
                }

                _fileVariables = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<VariableViewModel> LiveVariables
        {
            get => _liveVariables ?? (_liveVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == _liveVariables) return;
                _liveVariables = value;
                raisePropertyChanged();
            }
        }

        public ReactiveCommand<Unit, Unit> Load { get; set; }
        public ReactiveCommand<Unit, Unit> Write { get; set; }

        public override void Init()
        {
            _fileVariableSubject
                .ObserveOnDispatcher()
                .Do(x => UpdateVariables(x, FileVariables))
                .Do(x => UpdateDisplayIfMatching())
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            _canWrite.Subscribe().AddDisposableTo(Disposables);


            Load = ReactiveCommand.CreateFromTask(LoadVariables, _clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Write = ReactiveCommand.CreateFromTask(WriteVariables, _canWrite.Select(x => x))
                .AddDisposableTo(Disposables);
        }

        private void AddVariable(IEnumerable<JProperty> token, ObservableCollection<VariableViewModel> variables)
        {
            try
            {
                foreach (var prop in token)
                {
                    if (prop.Value is JObject)
                    {
                        var variable = new VariableViewModel {Name = prop.Name, Json = prop.Value.ToString()};
                        variables.Add(variable);
                    }
                }
            }
            finally
            {
                raisePropertyChanged("LiveVariables");
            }
        }


        private async Task<Unit> LoadVariables()
        {
            await LoadVariablesFromFile();

            return Unit.Default;
        }

        private Task<Unit> LoadVariablesFromFile()
        {
            var openFileDialog = new OpenFileDialog {Filter = "Json files (*.json)|*.json", RestoreDirectory = true};
            if (openFileDialog.ShowDialog() == true)
            {
                var json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                _fileVariableSubject.OnNext(json);
                _canWrite.OnNext(true);
            }

            return Task.FromResult(Unit.Default);
        }

        private void UpdateDisplayIfMatching()
        {
            DisplayVariables.Clear();
            var array = new VariableViewModel[FileVariables.Count];
            FileVariables.CopyTo(array, 0);
            DisplayVariables.AddRange(array);

            raisePropertyChanged("DisplayVariables");
        }

        private void UpdateVariables(JObject json, ObservableCollection<VariableViewModel> viewModels)
        {
            viewModels.Clear();
            AddVariable(json.Properties(), viewModels);
            Logger.Debug(Resources.UpdatedRestoreView);
        }

        private async Task<Unit> WriteVariables()
        {
            var messageBoxResult = MessageBox.Show(Resources.AreYouSureYouWantToOverwriteTheLiveVariablesOnThePLC, Resources.OverwriteConfirmation, MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                foreach (var variable in DisplayVariables)
                {
                    try
                    {
                        var jObject = JObject.Load(new JsonTextReader(new StringReader(variable.Json)));
                        foreach (var p in jObject.Properties())
                        {
                            Logger.Debug($"Restoring variable '{variable.Name}.{p.Name}' from backup...");
                            if(p.Value is JObject)
                                await _clientService.Client.WriteJson(variable.Name + "." + p.Name, (JObject) p.Value, true);
                            else if(p.Value is JArray)
                                await _clientService.Client.WriteJson(variable.Name + "." + p.Name, (JArray) p.Value, true);
                            else if (p.Value is JValue)
                                await _clientService.Client.WriteAsync(variable.Name + "." + p.Name, p.Value);
                            else
                                Logger.Error($"Unable to write variable '{variable.Name}.{p.Name}' from backup: no type case match!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            return Unit.Default;
        }
    }
}