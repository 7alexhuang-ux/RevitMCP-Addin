using System;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitMCP.Utils
{
    public class RevitEvent : IExternalEventHandler
    {
        private Action<UIApplication> _action;
        private TaskCompletionSource<object> _tcs;
        private ExternalEvent _externalEvent;

        public RevitEvent()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        public Task RunAsync(Action<UIApplication> action)
        {
            _tcs = new TaskCompletionSource<object>();
            _action = action;
            _externalEvent.Raise();
            return _tcs.Task;
        }

        public void Execute(UIApplication app)
        {
            try
            {
                _action?.Invoke(app);
                _tcs?.SetResult(null);
            }
            catch (Exception ex)
            {
                _tcs?.SetException(ex);
            }
        }

        public string GetName()
        {
            return "RevitMCP Async Event";
        }
    }
}
