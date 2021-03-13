using System;
using System.Reactive.Disposables;

namespace TwinCatAdsTool.Interfaces.Extensions
{
	public static class DisposableExtensions
	{
		public static T AddDisposableTo<T>(this T source, CompositeDisposable disposables) where T : IDisposable
		{
			disposables.Add(source);
			return source;
		}
	}
}