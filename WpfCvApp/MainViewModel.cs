using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VL.Mvvm.Ass;

namespace WpfCvApp
{
    class MainViewModel:ObservableObject,IDisposable
    {
        IAsyncContext _asyncContext;
        VideoCapture _capture;
        CascadeClassifier _cascadeClassifier;
        
        public MainViewModel()
        {
            _asyncContext = new AsyncContext();
            _capture = new VideoCapture();
            _cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");

        }

        #region Image source

        private BitmapSource _bitmapSource;

        public BitmapSource BitmapSource
        {
            get { return _bitmapSource; }
            set
            {
                _bitmapSource = value;
                OnPropertyChanged(nameof(BitmapSource));
            }
        }

        #endregion


        #region Capture

        private Task _executeTask;

        private CancellationTokenSource _tokenSource;

        protected async Task _executeAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var frameMat = _capture.RetrieveMat())
                    {
                        var rects = _cascadeClassifier.DetectMultiScale(frameMat, 1.1, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));

                        foreach (var rect in rects)
                        {
                            Cv2.Rectangle(frameMat, rect, Scalar.Red);
                        }
                        var bs = frameMat.ToWriteableBitmap();

                        _asyncContext.AsyncPost(x =>
                        {
                            BitmapSource = bs;
                        }, null);
                    }
                }
                catch(Exception ex)
                {

                }
                await Task.Delay(10, stoppingToken);
            }
        }
       
        public Task StartAsync()
        {
            _capture.Open(0, VideoCaptureAPIs.ANY);
            if (!_capture.IsOpened())
            {
                return Task.CompletedTask;
            }

            // Create linked token to allow cancelling executing task from provided token
            _tokenSource = new CancellationTokenSource();

            // Store the task we're executing
            _executeTask = _executeAsync(_tokenSource.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (_executeTask.IsCompleted)
            {
                return _executeTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            // Stop called without start
            if (_executeTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _tokenSource.Cancel();
            }
            finally
            {
                await _executeTask;
                //await Task.WhenAny(_executeTask).ConfigureAwait(false);
            }

        }

        #endregion


        #region IDispose

        public virtual void Dispose()
        {
            _tokenSource?.Cancel();

            _capture.Dispose();
            _cascadeClassifier.Dispose();
        }

        #endregion
    }
}
