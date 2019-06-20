using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
//
using WW.Cad.Base;
using WW.Cad.Drawing;
using WW.Cad.Drawing.GDI;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Objects;
using WW.Cad.Model.Entities;
using WW.Math;
//
using Color = System.Drawing.Color;
//
//using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
//using System.IO;

using System.Runtime.InteropServices;
//
using System.Collections;

//

namespace ReadCADFile
{
   // public partial class ShowDwgFile : Form
   // public  class ShowDwgFile : Form
    public partial class ShowDwgFile : Form
    {

        public DxfModel model;
        public GDIGraphics3D gdiGraphics3D;
        public Bounds3D bounds;
        public Matrix4D modelTransform = Matrix4D.Identity;
        public double scaleFactor = 1d;
        public Matrix4D from2DTransform;
        public Vector3D translation = Vector3D.Zero;
        public bool mouseDown;
        public bool shiftPressed;
        public Point lastMouseLocation;
        public Point mouseClickLocation;
        public event EventHandler<EntityEventArgs> EntitySelected;
        //
        public Control ImageControl;

        public ShowDwgFile()
        {
            ImageControl =this;
            gdiGraphics3D = new GDIGraphics3D(GraphicsConfig.BlackBackgroundCorrectForBackColor);
            //InitializeComponent();
           
        }

        public void CalculateTo2DTransform()
        {
            if (bounds != null)
            {
                Matrix4D to2DTransform = DxfUtil.GetScaleTransform(
                    bounds.Corner1,
                    bounds.Corner2,
                    bounds.Center,
                    new Point3D(0d, ImageControl.ClientSize.Height, 0d),
                    new Point3D(ImageControl.ClientSize.Width, 0d, 0d),
                    new Point3D(ImageControl.ClientSize.Width / 2, ImageControl.ClientSize.Height / 2, 0d)
                );
                gdiGraphics3D.To2DTransform = to2DTransform * modelTransform;
               
            }
        }

       // protected override void OnResize(EventArgs e)
        public virtual void ImageControl_OnResize(object sender, EventArgs e)
        {
            base.OnResize(e);
            
            CalculateTo2DTransform1();
            ImageControl.Invalidate();
           
        }

        public virtual void ImageControl_OnPaint(object sender, PaintEventArgs e)
        {

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            gdiGraphics3D.Draw(e.Graphics, ClientRectangle);
           
        }

        public virtual void ImageControl_OnMouseDown(object sender, MouseEventArgs e)
        {
            base.OnMouseDown(e);
            lastMouseLocation = e.Location;
            mouseClickLocation = e.Location;
            mouseDown = true;
            //shiftPressed = ModifierKeys == Keys.Shift;
        }
        public virtual void ImageControl_OnMouseUp(object sender, MouseEventArgs e)
        {
            base.OnMouseUp(e);
            mouseDown = false;

            // Use shift key for rectangle zoom.
            if (shiftPressed)
            {
                DrawReversibleRectangle(mouseClickLocation, lastMouseLocation);
                Point2D corner1 = new Point2D(
                    Math.Min(mouseClickLocation.X, e.Location.X),
                    Math.Min(mouseClickLocation.Y, e.Location.Y)
                );
                Point2D corner2 =
                    new Point2D(
                        Math.Max(mouseClickLocation.X, e.Location.X),
                        Math.Max(mouseClickLocation.Y, e.Location.Y)
                    );
                Vector2D delta = corner2 - corner1;
                if (!(MathUtil.AreApproxEqual(delta.X, 0d) || MathUtil.AreApproxEqual(delta.Y, 0d)))
                {
                    Matrix4D oldTo2DTransform = CalculateTo2DTransform1();

                    // Update scaleFactor
                    double scale = Math.Min(ImageControl.ClientSize.Width / delta.X, ImageControl.ClientSize.Height / delta.Y);
                    scaleFactor *= scale;

                    //// Update translation
                    Point3D screenSpaceCenter = new Point3D(corner1 + 0.5d * delta, 0d);
                    Point3D newModelSpaceCenter = oldTo2DTransform.GetInverse().Transform(new Point3D(corner1 + 0.5d * delta, 0d));
                    Matrix4D intermediateTo2DTransform = CalculateTo2DTransform1();
                    intermediateTo2DTransform.TransformTo2D(newModelSpaceCenter);
                    Point3D intermediateScreenSpaceCenter = intermediateTo2DTransform.Transform(newModelSpaceCenter);
                    translation += (
                        new Point3D(0.5d * ImageControl.ClientSize.Width, 0.5d * ImageControl.ClientSize.Height, 0d) -
                        intermediateScreenSpaceCenter
                    );

                    CalculateTo2DTransform1();
                    ImageControl.Invalidate();
                    shiftPressed = false;
                }
            }
            else
            {
                // Select entity at mouse location if mouse didn't move
                // and show entity in property grid.
                if (mouseClickLocation == e.Location)
                {
                    try
                    {
                        Point2D referencePoint = new Point2D(e.X, e.Y);
                        double distance;
                        IList<RenderedEntityInfo> closestEntities =
                            EntitySelector.GetClosestEntities(
                                model,
                                GraphicsConfig.BlackBackgroundCorrectForBackColor,
                                gdiGraphics3D.To2DTransform,
                                referencePoint,
                                out distance
                            );
                        if (closestEntities.Count > 0)
                        {
                            DxfEntity entity = closestEntities[0].Entity;
                            OnEntitySelected(new EntityEventArgs(entity));
                        }

                    }
                    catch
                    {

                    }
                }
            }

        }

        public virtual void ImageControl_OnMouseMove(object sender, MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (mouseDown == true)
            {
                if (shiftPressed)
                {
                    //按shif选择放大区域,绘制区域线
                    DrawReversibleRectangle(mouseClickLocation, lastMouseLocation);
                    DrawReversibleRectangle(mouseClickLocation, e.Location);
                }
                else
                {
                    //drag event handle
                    int dx = (e.X - lastMouseLocation.X);
                    int dy = (e.Y - lastMouseLocation.Y);
                    translation += new Vector3D(dx, dy, 0);
                    CalculateTo2DTransform1();
                    ImageControl.Invalidate();
                }
            }
            lastMouseLocation = e.Location;
        }

        public virtual void ImageControl_OnMouseWheel(object sender, MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            Matrix4D oldTo2DTransform = CalculateTo2DTransform1();
            int compare = Math.Sign(e.Delta);
            // wheel movement is forward 
            if (compare > 0)
            {
                scaleFactor *= 1.1d;
            }
            // wheel movement is backward 
            else if (compare < 0)
            {
                scaleFactor /= 1.1d;
            }

            // --- Begin of correction on the translation to zoom into mouse position.
            // Comment out this section to zoom into center of model.
            Point3D currentScreenPoint = new Point3D(e.X, e.Y, 0d);
            Point3D modelPoint = oldTo2DTransform.GetInverse().Transform(currentScreenPoint);
            Matrix4D intermediateTo2DTransform = CalculateTo2DTransform1();
            Point3D screenPoint = intermediateTo2DTransform.Transform(modelPoint);
            translation += (currentScreenPoint - screenPoint);
            // --- End of translation correction.

            CalculateTo2DTransform1();

            ImageControl.Invalidate();
        }
        public virtual void OnEntitySelected(EntityEventArgs e)
        {
            if (EntitySelected != null)
            {
                EntitySelected(this, e);
            }
        }

       public Matrix4D CalculateTo2DTransform1()
        {
            Matrix4D to2DTransform = Matrix4D.Identity;
            if (model != null && bounds != null)
            {
                double halfHeight = ImageControl.ClientSize.Height / 2;
                double halfWidth = ImageControl.ClientSize.Width / 2;
                double margin = 5d; // 5 pixels margin on each size.
                to2DTransform =
                    Transformation4D.Translation(translation) *
                    Transformation4D.Translation(halfWidth, halfHeight, 0) *
                    Transformation4D.Scaling(scaleFactor) *
                    DxfUtil.GetScaleTransform(
                        bounds.Corner1,
                        bounds.Corner2,
                        bounds.Center,
                        new Point3D(margin, ImageControl.ClientSize.Height - margin, 0d),
                        new Point3D(ImageControl.ClientSize.Width - margin, margin, 0d),
                        Point3D.Zero
                    );
            }
            gdiGraphics3D.To2DTransform = to2DTransform * modelTransform;
            from2DTransform = gdiGraphics3D.To2DTransform.GetInverse();
            return to2DTransform;
        }
        public void DrawReversibleRectangle(Point p1, Point p2)
        {
            p1 = ImageControl.PointToScreen(p1);
            p2 = ImageControl.PointToScreen(p2);
            ControlPaint.DrawReversibleLine(new Point(p1.X, p1.Y), new Point(p1.X, p2.Y), Color.White);
            ControlPaint.DrawReversibleLine(new Point(p1.X, p2.Y), new Point(p2.X, p2.Y), Color.White);
            ControlPaint.DrawReversibleLine(new Point(p2.X, p2.Y), new Point(p2.X, p1.Y), Color.White);
            ControlPaint.DrawReversibleLine(new Point(p2.X, p1.Y), new Point(p1.X, p1.Y), Color.White);
        }

        public void openFile(string filename)
        {
            modelTransform = Matrix4D.Identity;
            from2DTransform = gdiGraphics3D.To2DTransform.GetInverse();
            translation = Vector3D.Zero;
            mouseDown = false;
            shiftPressed = false;
            lastMouseLocation.X = (int)0.5d * ClientSize.Width;
            lastMouseLocation.Y = (int)0.5d * ClientSize.Height;
            mouseClickLocation = lastMouseLocation;
            scaleFactor = 1d;

            if (filename == ""|| filename ==null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "AutoCad files (*.dwg, *.dxf)|*.dxf;*.dwg";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    filename = dialog.FileName;
                }
            }
            if (!string.IsNullOrEmpty(filename))
            {
                // To prevent flicker.
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.DoubleBuffer, true);
                SetStyle(ControlStyles.UserPaint, true);

                ImageControl.BackColor = System.Drawing.Color.Black;

                try
                {
                    string extension = Path.GetExtension(filename);
                    if (string.Compare(extension, ".dwg", true) == 0)
                    {
                        model = DwgReader.Read(filename);
                    }
                    else
                    {
                        model = DxfReader.Read(filename);
                    }

                    this.Text = filename;

                    gdiGraphics3D.CreateDrawables(model);

                   
                    
                    bounds = new Bounds3D();
                    gdiGraphics3D.BoundingBox(bounds, modelTransform);
                    CalculateTo2DTransform();
                    ImageControl.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show( ex.Message);
                }

            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
      [DllImport("user32.dll", SetLastError = true)]
       public static extern int GetSystemMetrics(int nIndex);
        public void SaveToImageFile()
        {
           // Bitmap curBitmap = new Bitmap(this.Width, this.Height);//实例化一个和窗体一样大的bitmap
            Bitmap curBitmap = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);//实例化一个和窗体一样大的bitmap
            Graphics g = Graphics.FromImage(curBitmap);
            g.CompositingQuality = CompositingQuality.HighQuality;//质量设为最高
           // g.CopyFromScreen(this.Left, this.Top, 0, 0, new Size(this.Width, this.Height));//保存整个窗体为图片
            //g.CopyFromScreen(panel游戏区 .PointToScreen(Point.Empty), Point.Empty, panel游戏区.Size);//只保存某个控件（这里是panel游戏区）
           // lastMouseLocation
            /*
            Console.WriteLine("主显示器完整尺寸：");
            Console.WriteLine("宽：" + Screen.PrimaryScreen.Bounds.Width);
            Console.WriteLine("高：" + Screen.PrimaryScreen.Bounds.Height);

            Console.WriteLine("主显示器工作尺寸（排除任务栏、工具栏）：");
            Console.WriteLine("宽：" + Screen.PrimaryScreen.WorkingArea.Width);
            Console.WriteLine("高：" + Screen.PrimaryScreen.WorkingArea.Height);

            Console.WriteLine("当前显示器完整尺寸：");
            Console.WriteLine("宽：" + Screen.GetBounds(this).Width);
            Console.WriteLine("高：" + Screen.GetBounds(this).Height);

            Console.WriteLine("当前显示器工作尺寸（排除任务栏、工具栏）：");
            Console.WriteLine("宽：" + Screen.GetWorkingArea(this).Width);
            Console.WriteLine("高：" + Screen.GetWorkingArea(this).Height);
            */
           // g.CopyFromScreen(this.Left, this.Top, 0, 0, new Size(Screen.GetWorkingArea(this).Width,this.ClientSize.Height));//保存整个窗体为图片
             this.DrawToBitmap(curBitmap, this.ClientRectangle);
            /*
             Bitmap bmp = new Bitmap(this.Width, this.Height);
                this.DrawToBitmap(bmp, this.ClientRectangle);
                bmp.Save("xxx.gif");
           */
            //如果没有创建图像，则退出
            if (curBitmap == null)
            {
                return;
            }
            //调用SaveFileDialog
            SaveFileDialog saveDlg = new SaveFileDialog();
            //设置对话框标题
            saveDlg.Title = "保存为";
            //读写已存在文件时提示用户
            saveDlg.OverwritePrompt = true;
            //为图像选择一个筛选器
            saveDlg.Filter = "BMP文件(*.bmp)|*.bmp|" + "Gif文件(*.gif)|*.gif|" +
                "JPEG文件(*.jpg)|*.jpg|" + "PNG文件(*.png)|*.png";
            //启用“帮助”按钮
            saveDlg.ShowHelp = true;
            //如果选择了格式，则保存图像
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                //获取用户选择的文件名
                string fileName = saveDlg.FileName;
                //获取用户选择的扩展名
                string strFilExtn = fileName.Remove(0, fileName.Length - 3);
                //保存文件
                switch (strFilExtn)
                {
                    case "bmp":
                        //bmp格式；
                        curBitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;
                    case "jpg":
                        curBitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    case "gif":
                        curBitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Gif);
                        break;
                    case "tif":
                        curBitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Tiff);
                        break;
                    case "png":
                        curBitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    default:
                        break;

                }
            }
          
            
          //  bit.Save("weiboTemp.png");//默认保存格式为PNG，保存成jpg格式质量不是很好
        }
        /*
        #region menu
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
           //打开文件
            this.pictureBox1.Hide();
            openFile(null);
            //savefile();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //查看缩略图
            //this.Controls.Clear();

            pictureBox1.Image = GetDwgImage(null);
            pictureBox1.Show();
        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
             //保存图像
            SaveToImageFile();
        }

        #endregion menu
        */
        #region dwgThumbnailfile view
        struct BITMAPFILEHEADER
        {
            public short bfType;
            public int bfSize;
            public short bfReserved1;
            public short bfReserved2;
            public int bfOffBits;
        }
        public  Image GetDwgImage(string FileName)
        {
            if (FileName == "" || FileName == null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "AutoCad files (*.dwg, *.dxf)|*.dxf;*.dwg";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FileName = dialog.FileName;
                }
            }


            if (!(File.Exists(FileName)))
            {
               // throw new FileNotFoundException("文件没有被找到");
                MessageBox.Show("文件没有被找到");
                return null;
                
            }
            if (string.IsNullOrEmpty(FileName)) return null;
           
            FileStream DwgF;  //文件流
            int PosSentinel;  //文件描述块的位置
            BinaryReader br;  //读取二进制文件
            int TypePreview;  //缩略图格式
            int PosBMP;       //缩略图位置 
            int LenBMP;       //缩略图大小
            short biBitCount; //缩略图比特深度 
            BITMAPFILEHEADER biH; //BMP文件头，DWG文件中不包含位图文件头，要自行加上去
            byte[] BMPInfo;       //包含在DWG文件中的BMP文件体
            MemoryStream BMPF = new MemoryStream(); //保存位图的内存文件流
            BinaryWriter bmpr = new BinaryWriter(BMPF); //写二进制文件类
            Image myImg = null;
            try
            {
                DwgF = new FileStream(FileName, FileMode.Open, FileAccess.Read);   //文件流
                br = new BinaryReader(DwgF);
                DwgF.Seek(13, SeekOrigin.Begin); //从第十三字节开始读取
                PosSentinel = br.ReadInt32();  //第13到17字节指示缩略图描述块的位置
                DwgF.Seek(PosSentinel + 30, SeekOrigin.Begin);  //将指针移到缩略图描述块的第31字节
                TypePreview = br.ReadByte();  //第31字节为缩略图格式信息，2 为BMP格式，3为WMF格式
                if (TypePreview == 1)
                {
                    MessageBox.Show(FileName);
                }
                else if (TypePreview == 2 || TypePreview == 3)
                {
                    PosBMP = br.ReadInt32(); //DWG文件保存的位图所在位置
                    LenBMP = br.ReadInt32(); //位图的大小
                    DwgF.Seek(PosBMP + 14, SeekOrigin.Begin); //移动指针到位图块
                    biBitCount = br.ReadInt16(); //读取比特深度
                    DwgF.Seek(PosBMP, SeekOrigin.Begin); //从位图块开始处读取全部位图内容备用
                    BMPInfo = br.ReadBytes(LenBMP); //不包含文件头的位图信息
                    br.Close();
                    DwgF.Close();
                    biH.bfType = 19778; //建立位图文件头
                    if (biBitCount < 9)
                    {
                        biH.bfSize = 54 + 4 * (int)(Math.Pow(2, biBitCount)) + LenBMP;
                    }
                    else
                    {
                        biH.bfSize = 54 + LenBMP;
                    }
                    biH.bfReserved1 = 0; //保留字节
                    biH.bfReserved2 = 0; //保留字节
                    biH.bfOffBits = 14 + 40 + 1024; //图像数据偏移
                    //以下开始写入位图文件头
                    bmpr.Write(biH.bfType); //文件类型
                    bmpr.Write(biH.bfSize);  //文件大小
                    bmpr.Write(biH.bfReserved1); //0
                    bmpr.Write(biH.bfReserved2); //0
                    bmpr.Write(biH.bfOffBits); //图像数据偏移
                    bmpr.Write(BMPInfo); //写入位图
                    BMPF.Seek(0, SeekOrigin.Begin); //指针移到文件开始处
                    myImg = Image.FromStream(BMPF); //创建位图文件对象
                    bmpr.Close();
                    BMPF.Close();
                }
                return myImg;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion dwgThumbnailfile view end

        //
    
    }

 }
