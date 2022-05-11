using System.ComponentModel;
using System.Threading;

using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace VideoCaptureWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly VideoCapture capture;
        private readonly CascadeClassifier cascadeClassifier;

        private readonly BackgroundWorker bkgWorker;

        public MainWindow()
        {
            InitializeComponent();

            capture = new VideoCapture();
            cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            capture.Open(0, VideoCaptureAPIs.ANY);
            if (!capture.IsOpened())
            {
                Close();
                return;
            }

            bkgWorker.RunWorkerAsync();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            bkgWorker.CancelAsync();

            capture.Dispose();
            cascadeClassifier.Dispose();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (var frameMat = capture.RetrieveMat())
                {
                    var rects = cascadeClassifier.DetectMultiScale(frameMat, 1.1, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));

                    //void detectMultiScale(                //C++注释
                    //    const Mat&image,                //待检测图像
                    //    CV_OUT vector<Rect>&objects,    //被检测物体的矩形框向量
                    //    double scaleFactor = 1.1,        //前后两次相继的扫描中搜索窗口的比例系数，默认为1.1 即每次搜索窗口扩大10%
                    //    int minNeighbors = 3,            //构成检测目标的相邻矩形的最小个数 如果组成检测目标的小矩形的个数和小于minneighbors - 1 都会被排除
                    //                                     //如果minneighbors为0 则函数不做任何操作就返回所有被检候选矩形框
                    //    int flags = 0,                   //若设置为CV_HAAR_DO_CANNY_PRUNING 函数将会使用Canny边缘检测来排除边缘过多或过少的区域 
                    //    Size minSize = Size(),              
                    //    Size maxSize = Size()            //最后两个参数用来限制得到的目标区域的范围     
                    //);

                    foreach (var rect in rects)
                    {
                        Cv2.Rectangle(frameMat, rect, Scalar.Red);
                    }

                    // Must create and use WriteableBitmap in the same thread(UI Thread).
                    Dispatcher.Invoke(() =>
                    {
                        FrameImage.Source = frameMat.ToWriteableBitmap();
                    });
                }

                Thread.Sleep(30);
            }
        }
    }
}
