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

    /// <summary>
    /// Reprezentuje jeden prikaz ktery se naplanuje ke spusteni
    /// </summary>
    public class UnitCommand
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Func<bool> _func;
        private readonly string _description;
        private readonly int _unitAddr;

        #region properties

        public bool Result { get; private set; }
        private string ExceptionMessage { get; set; }
        private CommandStatus CommandStatus { get; set; }

        public string Description => _description;

        #endregion

 
        public UnitCommand(Func<bool> func,string description, int unitAddr)
        {
            CommandStatus = CommandStatus.Created;
            _func = func;
            _description = description;
            _unitAddr = unitAddr;
        }


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
