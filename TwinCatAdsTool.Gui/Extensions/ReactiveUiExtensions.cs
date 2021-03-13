﻿using System;
using System.Reactive.Disposables;
using log4net;
using ReactiveUI;

namespace TwinCatAdsTool.Gui.Extensions
{
	public static class ReactiveUiExtensions
	{
		public static T SetupErrorHandling<T>(this T obj, ILog logger, CompositeDisposable disposables) where T : IDisposable, IHandleObservableErrors
        {
            return obj.SetupErrorHandling(logger, disposables, "Error in ReactiveCommand");
        }

        public static T SetupErrorHandling<T>(this T obj, ILog logger, CompositeDisposable disposables, string message) where T : IDisposable, IHandleObservableErrors
        {
            disposables.Add(obj.ThrownExceptions.Subscribe<Exception>(ex => logger.Error(message, ex)));
            disposables.Add(obj);
            return obj;
        }
    }
}