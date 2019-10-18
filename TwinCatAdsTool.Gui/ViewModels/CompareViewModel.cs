﻿

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class CompareViewModel : ViewModelBase
    {

        private readonly Subject<JObject> leftTextSubject = new Subject<JObject>();
        private readonly Subject<JObject> rightTextSubject = new Subject<JObject>();
        private IClientService clientService;
        private IPersistentVariableService persistentVariableService;
        private SideBySideDiffBuilder comparisonBuilder = new SideBySideDiffBuilder(new Differ());
        private SideBySideDiffModel comparisonModel = new SideBySideDiffModel();
        private IEnumerable<ListBoxItem> leftBoxText;
        private IEnumerable<ListBoxItem> rightBoxText;

        public double? LinePaddingOverride { private get; set; }
        public double? TopOffsetOverride { private get; set; }

        public CompareViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
        }


        public override void Init()
        {

            var x = Observable
                .CombineLatest(leftTextSubject, rightTextSubject, 
                (l, r) => comparisonModel = GenerateDiffModel(l, r));

            x.ObserveOnDispatcher()
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables);

            leftTextSubject.OnNext(new JObject());
            rightTextSubject.OnNext(new JObject());

    
            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Load = ReactiveCommand.CreateFromTask(LoadJson, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);
        }

        private SideBySideDiffModel GenerateDiffModel(JObject left, JObject right)
        {
            var diffModel = comparisonBuilder.BuildDiffModel(left.ToString(), right.ToString());


            var leftBox = diffModel.OldText.Lines;
            var rightBox = diffModel.NewText.Lines;

            LeftBoxText = leftBox.Select(x => new ListBoxItem() { Content = x.Text, Background = GetBGColor(x)});
            RightBoxText = rightBox.Select(x => new ListBoxItem() { Content = x.Text, Background = GetBGColor(x) });

            return diffModel;
        }

        // https://github.com/SciGit/scigit-client/blob/master/DiffPlex/SilverlightDiffer/TextBoxDiffRenderer.cs
        private SolidColorBrush GetBGColor(DiffPiece diffPiece)
        {
      
                var fillColor = new SolidColorBrush(Colors.Transparent);
                if (diffPiece.Type == ChangeType.Deleted)
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 200, 100));
                else if (diffPiece.Type == ChangeType.Inserted)
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                else if (diffPiece.Type == ChangeType.Unchanged)
                    fillColor = new SolidColorBrush(Colors.White);
                else if (diffPiece.Type == ChangeType.Modified)
                {
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 220, 220, 255));
                }
                else if (diffPiece.Type == ChangeType.Imaginary)
                {
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                }

                return fillColor;
        }

        public ReactiveCommand<System.Reactive.Unit, Unit> Read { get; set; }
        public ReactiveCommand<System.Reactive.Unit, Unit> Load { get; set; }

        public IEnumerable<ListBoxItem> LeftBoxText
        {
            get
            {
                if(leftBoxText == null)
                {
                    leftBoxText = new List<ListBoxItem>();
                }

                return leftBoxText;
            }
            set
            {
                if (value == leftBoxText) return;
                leftBoxText = value;
                raisePropertyChanged();
            }
        }

        public IEnumerable<ListBoxItem> RightBoxText
        {
            get
            {
                if (rightBoxText == null)
                {
                    rightBoxText = new List<ListBoxItem>();
                }

                return rightBoxText;
            }
            set
            {
                if (value == rightBoxText) return;
                rightBoxText = value;
                raisePropertyChanged();
            }
        }

        private async Task<Unit> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client);
            leftTextSubject.OnNext(persistentVariables);
            return Unit.Empty;
        }

        private Task<Unit> LoadJson()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json files (*.json)|*.json";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                rightTextSubject.OnNext(json);
                
            }

            return Task.FromResult(Unit.Empty);
        }

    }
}
