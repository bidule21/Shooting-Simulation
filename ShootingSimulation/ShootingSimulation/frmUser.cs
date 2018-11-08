using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using AForge.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.IO;

namespace ShootingSimulation
{
    public partial class frmUser : Form
    {
        // declaring global variables
        private FilterInfoCollection videoDevicesList;
        private IVideoSource videoSource;
        private HaarCascade haar; // the viola-jones classifier (detector)
        private Image<Bgr, byte> imageFrame; // the global "input image"

        // set the default values of the parameters,
        // to be used as a variable in call to DetectionHaarCascade()
        private int windowSize = 25;
        private double scaleIncreaseRate = 1.1;
        private int minNeighbors = 2;

        Graphics g;
        Bitmap[] extFaces;
        int faceNo = 0;

        public frmUser()
        {
            InitializeComponent();
            // get list of video devices
            videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevicesList)
            {
                cmbVideoSource.Items.Add(videoDevice.Name);
            }
            if (cmbVideoSource.Items.Count > 0)
            {
                cmbVideoSource.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("No video sources found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // stop the camera on window close
            this.Closing += frmUser_Closing;
        }

        private void frmUser_Load(object sender, EventArgs e)
        {
            // adjust path to find your xml at loading
            haar = new HaarCascade("haarcascade_frontalface_alt_tree.xml");
        }

        private void frmUser_Closing(object sender, CancelEventArgs e)
        {
            // signal to stop
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            g = Graphics.FromImage(bitmap);
            int p1 = bitmap.Width / 2;
            int p2 = bitmap.Height / 2;
            Pen redPen = new Pen(Color.Red, 8);
            g.DrawLine(redPen, (p1 - 20), p2, (p1 + 20), p2);
            g.DrawLine(redPen, p1, (p2 - 20), p1, (p2 + 20));
            g.Dispose();
            pictureBox1.Image = bitmap;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            videoSource = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            videoSource.Start();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            videoSource.SignalToStop();
            if (videoSource != null && videoSource.IsRunning && pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = (Bitmap)pictureBox1.Image.Clone();
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            //int w = pictureBox1.Image.Width / 2;
            //int h = pictureBox1.Image.Height / 2;

            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                System.Drawing.Image InputImg = System.Drawing.Image.FromFile(openFileDialog.FileName);
                imageFrame = new Image<Bgr, byte>(new Bitmap(InputImg));
            }
            detectFaces();
            
            /*
            imageFrame = new Image<Bgr, byte>(new Bitmap(pictureBox2.Image));
            pictureBox2.Image = imageFrame.ToBitmap();
            detectFaces();*/
            
        }

        
        //FUNCTIONS USED TO DETECT FACES IN INPUT IMAGE
        private void detectFaces()
        {
            // detec faces from the imageFrame

            Image<Gray, byte> grayFrame = imageFrame.Convert<Gray, byte>();


            // detect faces from gray-scale and store into an array of type 'var',i.e
            var faces = grayFrame.DetectHaarCascade(haar, scaleIncreaseRate, minNeighbors,
                                                   HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                                                   new Size(windowSize, windowSize))[0];

            if (faces.Length > 0)
            {
                MessageBox.Show("Total Faces Detected: " + faces.Length.ToString());
                Bitmap bmpInput = grayFrame.ToBitmap();
                Bitmap extractedFace;   //empty
                Graphics faceCanvas;
                extFaces = new Bitmap[faces.Length];
                faceNo = 0;



                //draw a green rectangle on each detected face in image
                foreach (var face in faces)
                {
                    imageFrame.Draw(face.rect, new Bgr(Color.Green), 4);
                    pictureBox2.Image = imageFrame.ToBitmap();

                    // draw center point on pictureBox2's image
                    Graphics graphic = Graphics.FromImage(pictureBox2.Image);
                    int w = imageFrame.Width / 2;
                    int h = imageFrame.Height / 2;
                    SolidBrush redBrush = new SolidBrush(Color.Red);

                    graphic.FillEllipse(redBrush, w, h, 80, 80);


                    //set the size of the empty box(ExtractedFace) which will later contain the detected face
                    extractedFace = new Bitmap(face.rect.Width, face.rect.Height);

                    //set empty image as FaceCanvas, for painting
                    faceCanvas = Graphics.FromImage(extractedFace);

                    faceCanvas.DrawImage(bmpInput, 0, 0, face.rect, GraphicsUnit.Pixel);

                    extFaces[faceNo] = extractedFace;
                    faceNo++;
                }

                pictureBox3.Image = extFaces[0];

                //Display the detected faces in imagebox
                //pictureBox2.Image = imageFrame.ToBitmap();


                btnNext.Enabled = true;
                btnPrev.Enabled = true;
            }
        }


        // CALCULATE POINT BASED ON TWO POINT
        private double distance(int point1, int point2)
        {
            int w = pictureBox1.Image.Width / 2;
            int h = pictureBox1.Image.Height / 2;

            return Math.Sqrt(Math.Pow((point1 - w), 2) + Math.Pow((point2 - h), 2));
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (faceNo > 0)
            {
                faceNo--;
                pictureBox3.Image = extFaces[faceNo];
            }
            else
                MessageBox.Show("1st image!");
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (faceNo < extFaces.Length - 1)
            {
                faceNo++;
                pictureBox3.Image = extFaces[faceNo];
            }
            else
                MessageBox.Show("Last image!");
        }
    }
}
