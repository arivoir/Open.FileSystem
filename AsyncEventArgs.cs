using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open.FileSystemAsync
{
    /// <summary>
    /// Provides event data for asynchronous events.
    /// </summary>
    public class AsyncEventArgs : EventArgs
    {
        #region initialization

        public AsyncEventArgs()
        {
        }

        #endregion

        #region object model        

        /// <summary>
        /// Gets the deferrals awaiter.
        /// </summary>
        protected AsyncEventArgsDeferralsAwaiter Awaiter { get; } = new AsyncEventArgsDeferralsAwaiter();

        #endregion

        #region methods

        /// <summary>
        /// Gets the deferral.
        /// </summary>
        /// <remarks>
        /// The event won't finish until all the deferrals call the comlete method.
        /// </remarks>
        public AsyncEventArgsDeferral GetDeferral()
        {
            return Awaiter.GetDeferral();
        }

        /// <summary>
        /// Waits until all the deferrals are complete.
        /// </summary>
        /// <returns></returns>
        public Task WaitDeferralsAsync()
        {
            return Awaiter.WaitDeferralsAsync();
        }

        #endregion
    }

    /// <summary>
    /// Deferrals awaiter used to wait for the deferrals of an async event.
    /// </summary>
    public class AsyncEventArgsDeferralsAwaiter
    {
        #region fields

        private bool isWaiting = false;
        private List<AsyncEventArgsDeferral> _deferrals = new List<AsyncEventArgsDeferral>();
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        #endregion

        /// <summary>
        /// Gets the deferral.
        /// </summary>
        /// <remarks>
        /// The event won't finish until all the deferrals call the comlete method.
        /// </remarks>
        internal AsyncEventArgsDeferral GetDeferral()
        {
            if (isWaiting)
                throw new NotSupportedException();

            var deferral = new AsyncEventArgsDeferral();
            deferral.Completed += deferral_Completed;
            _deferrals.Add(deferral);
            return deferral;
        }

        #region imlementation

        void deferral_Completed(object sender, EventArgs e)
        {
            var deferral = sender as AsyncEventArgsDeferral;
            deferral.Completed -= deferral_Completed;
            _deferrals.Remove(deferral);
            if (isWaiting && _deferrals.Count == 0)
            {
                _tcs.TrySetResult(true);
            }
        }

        /// <summary>
        /// Waits until all the deferrals are complete.
        /// </summary>
        /// <returns></returns>
        public async Task WaitDeferralsAsync()
        {
            isWaiting = true;
            if (_deferrals.Count == 0)
            {
                _tcs.TrySetResult(true);
            }
            await _tcs.Task;
        }

        #endregion
    }

    /// <summary>
    /// Deferral used to block the event until <see cref="Complete"/> method is called. 
    /// </summary>
    public sealed class AsyncEventArgsDeferral
    {
        internal event EventHandler Completed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncEventArgsDeferral"/> class.
        /// </summary>
        internal AsyncEventArgsDeferral()
        {
        }

        /// <summary>
        /// Notifies the event the handler is ready to continue.
        /// </summary>
        public void Complete()
        {
            if (Completed != null)
            {
                Completed(this, new EventArgs());
            }
        }
    }
}
