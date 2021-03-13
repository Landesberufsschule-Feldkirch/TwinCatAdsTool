using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.JsonExtension;
using TwinCAT.TypeSystem;
using TwinCAT.TypeSystem.Generic;
using TwinCatAdsTool.Interfaces.Logging;
using TwinCatAdsTool.Interfaces.Services;
using TwinCatAdsTool.Logic.Properties;

namespace TwinCatAdsTool.Logic.Services
{
    public class PersistentVariableService : IPersistentVariableService
    {
        private readonly ILog _logger =LoggerFactory.GetLogger();
        private readonly Subject<string> _currentTaskSubject = new Subject<string>();
        public async Task<JObject> ReadPersistentVariables(TcAdsClient client, IInstanceCollection<ISymbol> symbols)
        {
            var jobj = new JObject();
            try
            {
                if (client.IsConnected)
                {
                    var iterator = new SymbolIterator(symbols, s => s.IsPersistent && s.InstancePath.Split('.').Length == 2 && !s.InstancePath.Contains("["));

                    var variables = new Dictionary<string, List<JObject>>();
                    foreach (var symbol in iterator)
                    {
                        var splitPath = symbol.InstancePath.Split('.');
                        var globalName = splitPath.First();
                        var localName = splitPath.Last();
                        if (!variables.ContainsKey(globalName))
                        {
                            variables.Add(globalName, new List<JObject>());
                        }

                        try
                        {
                            _logger.Debug($"reading symbol '{symbol.InstancePath}' in json format...");
                            _currentTaskSubject.OnNext($"Reading {symbol.InstancePath}...");

                            var json = await client.ReadJson(symbol.InstancePath, true);
                            if(json.ContainsKey(localName))
                                variables[globalName].Add(json);
                            else
                            {
                                var innerObject = new JObject {{localName, json}};
                                variables[globalName].Add(innerObject);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error(string.Format(Resources.ErrorDuringReadingVariable0InJsonFormat, symbol.InstancePath), e);
                        }

                    }

                    foreach (var element in variables)
                    {
                        var uo = new JObject();
                        foreach (var p in element.Value)
                        {
                            foreach (var up in p.Properties())
                            {
                                uo.Add(up);
                            }
                        }
                        jobj.Add(element.Key, uo);

                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(Resources.ErrorWhileReadingPersistentVariables,e);
            }
            
            _currentTaskSubject.OnNext(string.Empty);
            _logger.Debug($"Persistent variable successfully downloaded!");

            return jobj;
        }

        public IObservable<string> CurrentTask => _currentTaskSubject.AsObservable();
    }
}