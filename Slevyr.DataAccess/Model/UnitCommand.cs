using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Slevyr.DataAccess.Model
{
    public enum CommandStatus
    {
        Created,
        WaitingToRun,
        Canceled,
        Faulted,
        Running,
        Completed
    }

    public class UnitCommand
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //private List<Object> _params;
        //private Dictionary<int, UnitMonitor> _unitDictionary;
        private readonly Func<bool> _func;
        private string _description;
        private int _unitAddr;

        #region properties

        public bool Result { get; set; }
        public string ExceptionMessage { get; set; }
        public CommandStatus CommandStatus { get; set; }

        public string Description
        {
            get { return _description; }
        }

        #endregion

        /*
        public List<object> Params
        {
            get { return _params; }
        }
        #endregion

        public void AddParam(object p)
        {
            if (_params == null)
            {
                _params = new List<object>();
            }
            _params.Add(p);
        }
        */

        public UnitCommand(Func<bool> func,string description, int unitAddr)
        {
            CommandStatus = CommandStatus.Created;
            _func = func;
            _description = description;
            _unitAddr = unitAddr;
        }

        //public UnitCommand(Dictionary<int, UnitMonitor> unitDictionary)
        //{
        //    CommandStatus = CommandStatus.Created;
        //    _unitDictionary = unitDictionary;
        //}

        public void Run()
        {
            CommandStatus =CommandStatus.Running;
            Logger.Info($"InvokeCmd {_description} on {_unitAddr}");

            try
            {
                Result = _func.Invoke();
                CommandStatus = CommandStatus.Completed;
                Logger.Info($"InvokeCmd OK {_description} on {_unitAddr}");
            }
            catch (Exception ex)
            {
                CommandStatus = CommandStatus.Faulted;
                ExceptionMessage = ex.Message;
                Logger.Error($"InvokeCmd {_description} on {_unitAddr}");
            }
        }
    }
}
