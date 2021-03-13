﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using DynamicData.Annotations;
using log4net;
using ReactiveUI;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Logging;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public abstract class ViewModelBase : ReactiveObject, IDisposable, IInitializable
    {
        protected CompositeDisposable Disposables = new CompositeDisposable();
        private bool _disposed;
        private string _title;

        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                raisePropertyChanged();
            }
        }

        protected ILog Logger { get; } = LoggerFactory.GetLogger();

        public virtual void Dispose()
        {
            Dispose(true);
        }

        public abstract void Init();

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "Disposables")]
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            Disposables?.Dispose();
            Disposables = null;

            _disposed = true;
        }

        [NotifyPropertyChangedInvocator]
        // ReSharper disable once InconsistentNaming
        protected void raisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.RaisePropertyChanged(propertyName);
        }

        ~ViewModelBase()
        {
            Dispose(false);
        }
    }
}