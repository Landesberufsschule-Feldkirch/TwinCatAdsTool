﻿using Ninject.Parameters;

namespace TwinCatAdsTool.Interfaces.Commons
{
	public interface IInstanceCreator
	{
		T CreateInstance<T>(IParameter[] arguments);
	}
}