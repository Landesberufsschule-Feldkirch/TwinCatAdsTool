using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class CompareViewModel : ViewModelBase
    {
        private readonly Subject<string> _leftTextSubject = new Subject<string>();
        private readonly Subject<string> _rightTextSubject = new Subject<string>();
        private readonly IClientService _clientService;
        private readonly SideBySideDiffBuilder _comparisonBuilder = new SideBySideDiffBuilder(new Differ());
        private SideBySideDiffModel _comparisonModel = new SideBySideDiffModel();
        private IEnumerable<ListBoxItem> _leftBoxText;
        private readonly IPersistentVariableService _persistentVariableService;
        private IEnumerable<ListBoxItem> _rightBoxText;
        private string _sourceLeft;
        private string _sourceRight;

        public CompareViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            _clientService = clientService;
            _persistentVariableService = persistentVariableService;
        }

        public IEnumerable<ListBoxItem> LeftBoxText
        {
            get
            {
                if (_leftBoxText == null)
                {
                    _leftBoxText = new List<ListBoxItem>();
                }

                return _leftBoxText;
            }
            set
            {
                if (value == _leftBoxText)
                {
                    return;
                }

                _leftBoxText = value;
                raisePropertyChanged();
            }
        }

        public string SourceLeft
        {
            get
            {
                if (_sourceLeft == null)
                {
                    _sourceLeft = "";
                }

                return _sourceLeft;
            } set
            {
                if (value == _sourceLeft)
                {
                    return;
                }

                _sourceLeft = value;
                raisePropertyChanged();
            }
        }

        public string SourceRight
        {
            get
            {
                if (_sourceRight == null)
                {
                    _sourceRight = "";
                }

                return _sourceRight;
            } set
            {
                if (value == _sourceRight)
                {
                    return;
                }

                _sourceRight = value;
                raisePropertyChanged();
            }
        }

        public ReactiveCommand<Unit, Unit> LoadLeft { get; set; }
        public ReactiveCommand<Unit, Unit> LoadRight { get; set; }

        public ReactiveCommand<Unit, Unit> ReadLeft { get; set; }

        public ReactiveCommand<Unit, Unit> ReadRight { get; set; }

        public IEnumerable<ListBoxItem> RightBoxText
        {
            get
            {
                if (_rightBoxText == null)
                {
                    _rightBoxText = new List<ListBoxItem>();
                }

                return _rightBoxText;
            }
            set
            {
                if (value == _rightBoxText) return;
                _rightBoxText = value;
                raisePropertyChanged();
            }
        }


        public override void Init()
        {
            var x = _leftTextSubject.StartWith("")
                .CombineLatest(_rightTextSubject.StartWith(""),
                               (l, r) => _comparisonModel = GenerateDiffModel(l, r));

            x.ObserveOnDispatcher()
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables);

            AssignCommands();
        }

        private void AssignCommands()
        {
            ReadLeft = ReactiveCommand.CreateFromTask(ReadVariablesLeft,
                    _clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            LoadLeft = ReactiveCommand.CreateFromTask(LoadJsonLeft)
                .AddDisposableTo(Disposables);


            ReadRight = ReactiveCommand.CreateFromTask(ReadVariablesRight,
                    _clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            LoadRight = ReactiveCommand.CreateFromTask(LoadJsonRight)
                .AddDisposableTo(Disposables);
        }

        private SideBySideDiffModel GenerateDiffModel(string left, string right)
        {
            var diffModel = _comparisonBuilder.BuildDiffModel(left, right);


            var leftBox = diffModel.OldText.Lines;
            var rightBox = diffModel.NewText.Lines;

            // all items have the same fixed height. this makes synchronizing of the scrollbars easier
            LeftBoxText = leftBox.Select(x => new ListBoxItem
            {
                Content = x.Text,
                Background = GetBgColor(x),
                Height = 20
            });
            RightBoxText = rightBox.Select(x => new ListBoxItem
            {
                Content = x.Text,
                Background = GetBgColor(x),
                Height = 20
            });

            Logger.Debug("Generated Comparison Model");
            return diffModel;
        }

        //manually coloring the ListboxItems depending on their diff state
        //compare https://github.com/SciGit/scigit-client/blob/master/DiffPlex/SilverlightDiffer/TextBoxDiffRenderer.cs
        private SolidColorBrush GetBgColor(DiffPiece diffPiece)
        {
            var fillColor = new SolidColorBrush(Colors.Transparent);
            switch (diffPiece.Type)
            {
                case ChangeType.Deleted:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 200, 100));
                    break;
                case ChangeType.Inserted:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                    break;
                case ChangeType.Unchanged:
                    fillColor = new SolidColorBrush(Colors.White);
                    break;
                case ChangeType.Modified:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 220, 220, 255));
                    break;
                case ChangeType.Imaginary:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                    break;
            }

            return fillColor;
        }


        private Task<(JObject, string)> LoadJson()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Json files (*.json)|*.json", RestoreDirectory = true
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    var json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                    Logger.Debug(string.Format(Resources.LoadOfFile0Wasuccesful, openFileDialog.FileName));
                    return Task.FromResult((json, Path.GetFileName(openFileDialog.FileName)));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.ErrorDuringLoadOfFile, ex);
            }

            return Task.FromResult<(JObject, string)>((null, ""));
        }


        private Task LoadJsonLeft()
        {
            var (json, fileName) = LoadJson().Result;
            if (json != null)
            {
                _leftTextSubject.OnNext(json.ToString());
                SourceLeft = fileName;
                Logger.Debug(Resources.UpdatedLeftTextBox);
            }

            return Task.FromResult(Unit.Default);
        }

        private Task LoadJsonRight()
        {
            var (json, fileName) = LoadJson().Result;
            if (json != null)
            {
                _rightTextSubject.OnNext(json.ToString());
                SourceRight = fileName;
                Logger.Debug(Resources.UpdatedRightTextBox);
            }


            return Task.FromResult(Unit.Default);
        }

        private async Task<JObject> ReadVariables()
        {
            var persistentVariables = await _persistentVariableService.ReadPersistentVariables(_clientService.Client, _clientService.TreeViewSymbols);
            _leftTextSubject.OnNext(persistentVariables.ToString());

            Logger.Debug(Resources.ReadPersistentVariables);
            return persistentVariables;
        }

        private async Task ReadVariablesLeft()
        {
            var json = await ReadVariables().ConfigureAwait(false);
            _leftTextSubject.OnNext(json.ToString());
            SourceLeft = "PLC";

            Logger.Debug(Resources.UpdatedLeftTextBox);
        }

        private async Task ReadVariablesRight()
        {
            var json = await ReadVariables();
            _rightTextSubject.OnNext(json.ToString());
            SourceRight = "PLC";

            Logger.Debug(Resources.UpdatedRightTextBox);
        }
    }
}