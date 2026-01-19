using System;
using System.Threading.Tasks;

namespace FlightTracker.Web.Services
{
    // Simple service to notify components to refresh their data
    public class UiRefreshService
    {
        public event Func<Task>? RefreshRequested;

        public async Task RequestRefreshAsync()
        {
            var handler = RefreshRequested;
            if (handler != null)
            {
                await handler.Invoke();
            }
        }
    }
}
